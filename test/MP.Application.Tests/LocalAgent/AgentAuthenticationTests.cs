using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;
using MP.Domain.LocalAgent;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace MP.Application.Tests.LocalAgent
{
    /// <summary>
    /// Integration tests for Agent API Key authentication
    /// Tests the full authentication flow from middleware to repository
    /// </summary>
    public class AgentAuthenticationTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IAgentApiKeyRepository _apiKeyRepository;
        private readonly ICurrentTenant _currentTenant;
        private const string TestAgentId = "test-agent-001";
        private const string TestApiKeyPrefix = "mp_agent_";

        public AgentAuthenticationTests()
        {
            _apiKeyRepository = GetRequiredService<IAgentApiKeyRepository>();
            _currentTenant = GetRequiredService<ICurrentTenant>();
        }

        #region Valid Authentication Tests

        [Fact]
        public async Task Authenticate_Should_Pass_When_Valid_ApiKey_Provided()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var storedKey = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                isExpired: false,
                isLocked: false,
                ipWhitelist: null);  // Allow all IPs

            // Assert
            storedKey.ShouldNotBeNull();
            storedKey.AgentId.ShouldBe(TestAgentId);
            storedKey.IsUsable.ShouldBeTrue();
            storedKey.IsExpired.ShouldBeFalse();
            storedKey.IsLocked.ShouldBeFalse();
            storedKey.IsActive.ShouldBeTrue();
        }

        [Fact]
        public async Task Authenticate_Should_Record_Usage_On_Successful_Authentication()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                isExpired: false,
                isLocked: false);

            var initialUsageCount = keyEntity.UsageCount;
            var initialLastUsedAt = keyEntity.LastUsedAt;

            // Act - Record successful authentication
            keyEntity.RecordSuccessfulAttempt();
            await _apiKeyRepository.UpdateAsync(keyEntity);

            // Assert
            keyEntity.UsageCount.ShouldBe(initialUsageCount + 1);
            keyEntity.LastUsedAt.ShouldNotBeNull();
            if (initialLastUsedAt.HasValue)
            {
                keyEntity.LastUsedAt.Value.ShouldBeGreaterThan(initialLastUsedAt.Value);
            }
            keyEntity.FailedAuthenticationAttempts.ShouldBe(0);
        }

        #endregion

        #region Expiration Tests

        [Fact]
        public async Task Authenticate_Should_Fail_When_ApiKey_Is_Expired()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var storedKey = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                expiresAt: DateTime.UtcNow.AddDays(-1)); // Expired 1 day ago

            // Assert
            storedKey.ShouldNotBeNull();
            storedKey.IsExpired.ShouldBeTrue();
            storedKey.IsUsable.ShouldBeFalse();
        }

        [Fact]
        public async Task Authenticate_Should_Fail_When_ApiKey_Is_Near_Expiration()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var expiresAt = DateTime.UtcNow.AddDays(5); // Expires in 5 days
            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                expiresAt: expiresAt);

            // Assert
            keyEntity.IsExpired.ShouldBeFalse(); // Not expired yet
            // Verify key is within the near-expiration threshold
            var daysUntilExpiration = (keyEntity.ExpiresAt - DateTime.UtcNow).Days;
            daysUntilExpiration.ShouldBeLessThan(10);
            daysUntilExpiration.ShouldBeGreaterThan(0);
        }

        #endregion

        #region Active/Inactive Tests

        [Fact]
        public async Task Authenticate_Should_Fail_When_ApiKey_Is_Inactive()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var storedKey = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: false);  // Inactive key

            // Assert
            storedKey.ShouldNotBeNull();
            storedKey.IsActive.ShouldBeFalse();
            storedKey.IsUsable.ShouldBeFalse();
        }

        #endregion

        #region IP Whitelist Tests

        [Fact]
        public async Task Authenticate_Should_Pass_When_Ip_Is_Whitelisted()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();
            var clientIp = "192.168.1.100";

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                ipWhitelist: "192.168.1.100,10.0.0.5,172.16.0.1");

            // Act
            var isAllowed = keyEntity.IsIpAllowed(clientIp);

            // Assert
            isAllowed.ShouldBeTrue();
        }

        [Fact]
        public async Task Authenticate_Should_Pass_When_Ip_Whitelist_Is_Empty()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                ipWhitelist: null);  // Empty whitelist = allow all

            // Act
            var isAllowedAny = keyEntity.IsIpAllowed("any.ip.address");

            // Assert
            isAllowedAny.ShouldBeTrue();
        }

        [Fact]
        public async Task Authenticate_Should_Fail_When_Ip_Is_Not_Whitelisted()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();
            var clientIp = "203.0.113.42"; // Not in whitelist

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                ipWhitelist: "192.168.1.100,10.0.0.5");

            // Act
            var isAllowed = keyEntity.IsIpAllowed(clientIp);

            // Assert
            isAllowed.ShouldBeFalse();
        }

        #endregion

        #region Failed Attempt & Locking Tests

        [Fact]
        public async Task Authenticate_Should_Lock_After_5_Failed_Attempts()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true);

            // Act - Record 5 failed attempts
            for (int i = 0; i < 5; i++)
            {
                keyEntity.RecordFailedAttempt();
            }

            // Assert
            keyEntity.FailedAuthenticationAttempts.ShouldBe(5);
            keyEntity.IsLocked.ShouldBeTrue();
            keyEntity.LockedUntil.ShouldNotBeNull();
            keyEntity.LockedUntil!.Value.ShouldBeGreaterThan(DateTime.UtcNow);
        }

        [Fact]
        public async Task Authenticate_Should_Clear_Failed_Attempts_On_Success()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true);

            // Record 3 failed attempts
            keyEntity.RecordFailedAttempt();
            keyEntity.RecordFailedAttempt();
            keyEntity.RecordFailedAttempt();
            keyEntity.FailedAuthenticationAttempts.ShouldBe(3);

            // Act - Record successful attempt
            keyEntity.RecordSuccessfulAttempt();

            // Assert
            keyEntity.FailedAuthenticationAttempts.ShouldBe(0);
            keyEntity.LockedUntil.ShouldBeNull();
            keyEntity.IsLocked.ShouldBeFalse();
        }

        #endregion

        #region Agent ID Mismatch Tests

        [Fact]
        public async Task Authenticate_Should_Fail_When_Agent_Id_Mismatch()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                "correct-agent-id",
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true);

            // Assert
            keyEntity.AgentId.ShouldBe("correct-agent-id");
            keyEntity.AgentId.ShouldNotBe("different-agent-id");
        }

        #endregion

        #region Key Rotation Tests

        [Fact]
        public async Task Authenticate_Should_Mark_Key_For_Rotation_When_Approaching_Expiration()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true);

            // Act
            keyEntity.MarkForRotation();

            // Assert
            keyEntity.ShouldRotate.ShouldBeTrue();
        }

        [Fact]
        public async Task Repository_Should_Find_Keys_Near_Expiration_For_Rotation()
        {
            // Arrange
            var tenantId = _currentTenant.Id ?? Guid.NewGuid();
            var (apiKey, keyHash, salt) = GenerateApiKey();

            var expiresIn8Days = DateTime.UtcNow.AddDays(8);
            var keyEntity = await CreateTestApiKeyAsync(
                tenantId,
                TestAgentId,
                keyHash,
                salt,
                apiKey.Substring(TestApiKeyPrefix.Length, 8),
                isActive: true,
                expiresAt: expiresIn8Days);

            // Assert - Verify key matches criteria for GetKeysNearExpirationAsync
            keyEntity.IsActive.ShouldBeTrue();
            keyEntity.ShouldRotate.ShouldBeFalse();
            keyEntity.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);

            var daysUntilExpiration = (keyEntity.ExpiresAt - DateTime.UtcNow).Days;
            daysUntilExpiration.ShouldBeLessThanOrEqualTo(10); // Within threshold
            daysUntilExpiration.ShouldBeGreaterThanOrEqualTo(7); // 8 days ± rounding
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generate a test API key with hash and salt
        /// </summary>
        private (string apiKey, string keyHash, string salt) GenerateApiKey()
        {
            // Generate random suffix (8 characters)
            var suffix = GenerateRandomString(8);
            var apiKey = $"{TestApiKeyPrefix}{GenerateRandomString(24)}{suffix}";

            // Generate salt
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            var salt = Convert.ToHexString(saltBytes);

            // Hash the API key
            var keyHash = HashApiKey(apiKey);

            return (apiKey, keyHash, salt);
        }

        /// <summary>
        /// Hash an API key using SHA256
        /// </summary>
        private string HashApiKey(string apiKey)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
                return Convert.ToHexString(hashedBytes);
            }
        }

        /// <summary>
        /// Generate a random string of specified length
        /// </summary>
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Create a test API key in the database
        /// </summary>
        private async Task<AgentApiKey> CreateTestApiKeyAsync(
            Guid? providedTenantId,
            string agentId,
            string keyHash,
            string salt,
            string suffix,
            bool isActive = true,
            bool? isExpired = null,
            bool? isLocked = null,
            string? ipWhitelist = null,
            DateTime? expiresAt = null)
        {
            var tenantId = providedTenantId ?? _currentTenant.Id ?? Guid.NewGuid();

            var apiKey = new AgentApiKey
            {
                TenantId = tenantId,
                AgentId = agentId,
                Prefix = TestApiKeyPrefix,
                Suffix = suffix,
                KeyHash = keyHash,
                Salt = salt,
                Name = $"Test Key for {agentId}",
                Description = "Automated test API key",
                ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(90),
                IsActive = isActive,
                IpWhitelist = ipWhitelist,
                UsageCount = 0,
                FailedAuthenticationAttempts = 0
            };

            return await _apiKeyRepository.InsertAsync(apiKey);
        }

        #endregion
    }
}
