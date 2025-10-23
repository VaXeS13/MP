using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;
using MP.LocalAgent.Contracts.Exceptions;
using MP.LocalAgent.Contracts.Responses;

namespace MP.Application.Tests.Devices
{
    /// <summary>
    /// Unit tests for PCI DSS compliance validation in terminal payment responses
    /// Ensures payment card data is properly masked and not exposed
    /// </summary>
    public class PciComplianceTests
    {
        #region MaskedPan Validation Tests

        [Fact]
        public void ValidatePciCompliance_Should_Pass_When_MaskedPan_Contains_Exactly_Four_Digits()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-001",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****1234",  // Exactly 4 digits - SAFE
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should not throw
        }

        [Fact]
        public void ValidatePciCompliance_Should_Pass_When_MaskedPan_Is_Null()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-002",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = null,  // Null is acceptable
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should not throw
        }

        [Fact]
        public void ValidatePciCompliance_Should_Pass_When_MaskedPan_Is_Empty()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-003",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = string.Empty,  // Empty is acceptable
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should not throw
        }

        [Fact]
        public void ValidatePciCompliance_Should_Throw_When_MaskedPan_Contains_More_Than_Four_Digits()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-004",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****12345",  // 5 digits - UNSAFE
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            var ex = Should.Throw<PciComplianceException>(() => response.ValidatePciCompliance());
            ex.Message.ShouldContain("PCI DSS Violation");
            ex.Message.ShouldContain("5 digits");
            ex.Message.ShouldContain("Only last 4 digits");
        }

        [Fact]
        public void ValidatePciCompliance_Should_Throw_When_MaskedPan_Contains_Full_Card_Number()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-005",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "4242424242424242",  // Full card number - CRITICAL VIOLATION
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            var ex = Should.Throw<PciComplianceException>(() => response.ValidatePciCompliance());
            ex.Message.ShouldContain("PCI DSS Violation");
            ex.Message.ShouldContain("16 digits");  // Full card numbers have many digits
        }

        [Fact]
        public void ValidatePciCompliance_Should_Pass_When_MaskedPan_Has_Non_Digit_Characters()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-006",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****-1234",  // Has dash separator - only 4 digits count
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should not throw
        }

        [Fact]
        public void ValidatePciCompliance_Should_Count_Only_Digits_In_MaskedPan()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-007",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "VISA-****-1234-CARD",  // 4 digits among non-digits - SAFE
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should not throw - only 4 digits
        }

        #endregion

        #region P2PE Compliance Tests

        [Fact]
        public void ValidatePciCompliance_Should_Pass_When_P2PE_Compliant()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-008",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****5678",
                IsP2PECompliant = true,  // P2PE certified
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should not throw
        }

        [Fact]
        public void ValidatePciCompliance_Should_Throw_When_P2PE_Not_Compliant()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-009",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****9999",
                IsP2PECompliant = false,  // NOT P2PE certified - VIOLATION
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            var ex = Should.Throw<PciComplianceException>(() => response.ValidatePciCompliance());
            ex.Message.ShouldContain("Terminal is not P2PE certified");
            ex.Message.ShouldContain("Cannot process payments");
        }

        #endregion

        #region Multiple Violations Tests

        [Fact]
        public void ValidatePciCompliance_Should_Throw_For_Both_MaskedPan_And_P2PE_Violations()
        {
            // Arrange - First violation to be caught is MaskedPan check
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-010",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****123456",  // 6 digits - VIOLATION #1
                IsP2PECompliant = false,    // NOT P2PE - VIOLATION #2
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001"
                }
            };

            // Act & Assert
            var ex = Should.Throw<PciComplianceException>(() => response.ValidatePciCompliance());
            // MaskedPan check happens first
            ex.Message.ShouldContain("PCI DSS Violation");
        }

        #endregion

        #region SafeMetadata Tests

        [Fact]
        public void SafeMetadata_Should_Never_Contain_Card_Data()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-011",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****1234",
                IsP2PECompliant = true,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "TERM-001",
                    ["ProcessingTime"] = "1250ms",
                    ["CardType"] = "Visa"  // Safe - not sensitive data
                }
            };

            // Act
            response.ValidatePciCompliance();

            // Assert
            response.SafeMetadata.ShouldNotBeNull();
            response.SafeMetadata.ShouldNotContainKey("PAN");
            response.SafeMetadata.ShouldNotContainKey("CardNumber");
            response.SafeMetadata.ShouldNotContainKey("CVV");
            response.SafeMetadata.ShouldNotContainKey("FullCardData");
        }

        [Fact]
        public void SafeMetadata_Should_Be_Empty_Dictionary_By_Default()
        {
            // Arrange
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-012",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "****1234",
                IsP2PECompliant = true
                // SafeMetadata not explicitly set - should default to empty dict
            };

            // Act & Assert
            response.SafeMetadata.ShouldNotBeNull();
            response.SafeMetadata.Count.ShouldBe(0);
            response.ValidatePciCompliance();  // Should not throw
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void ValidatePciCompliance_Should_Pass_For_Valid_Visa_Response()
        {
            // Arrange - Realistic Visa transaction
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-VIS-12345",
                AuthorizationCode = "654321",
                Status = "captured",
                Amount = 299.99m,
                Currency = "PLN",
                CardType = "Visa",
                LastFourDigits = "4242",
                MaskedPan = "****4242",  // Correct format
                IsP2PECompliant = true,
                ProcessedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "POS-SHOP-001",
                    ["ProcessingTime"] = "2150ms",
                    ["CardType"] = "Visa",
                    ["PaymentMethod"] = "Card"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should pass

            // Verify no sensitive data exposed
            response.MaskedPan.ShouldNotContain("4242424242424242");
            response.MaskedPan.ShouldBe("****4242");
        }

        [Fact]
        public void ValidatePciCompliance_Should_Pass_For_Valid_Mastercard_Response()
        {
            // Arrange - Realistic Mastercard transaction
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-MC-67890",
                AuthorizationCode = "789456",
                Status = "captured",
                Amount = 150.50m,
                Currency = "PLN",
                CardType = "Mastercard",
                LastFourDigits = "5555",
                MaskedPan = "****5555",  // Correct format
                IsP2PECompliant = true,
                ProcessedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
                SafeMetadata = new()
                {
                    ["TerminalId"] = "POS-SHOP-001",
                    ["ProcessingTime"] = "1850ms",
                    ["CardType"] = "Mastercard"
                }
            };

            // Act & Assert
            response.ValidatePciCompliance();  // Should pass
        }

        [Fact]
        public void ValidatePciCompliance_Should_Catch_Accidentally_Stored_Full_Pan()
        {
            // Arrange - Simulating accidental full PAN exposure
            var response = new TerminalPaymentResponse
            {
                CommandId = Guid.NewGuid(),
                Success = true,
                TransactionId = "TXN-ERR-001",
                Status = "captured",
                Amount = 100m,
                Currency = "PLN",
                MaskedPan = "5555555555554444",  // ERROR: Full card number!
                IsP2PECompliant = true,
                SafeMetadata = new()
            };

            // Act & Assert
            var ex = Should.Throw<PciComplianceException>(() => response.ValidatePciCompliance());
            ex.Message.ShouldContain("PCI DSS Violation");
            ex.Message.ShouldContain("16 digits");
        }

        #endregion

        #region Exception Type Tests

        [Fact]
        public void PciComplianceException_Should_Be_Throwable_With_Message()
        {
            // Arrange
            var message = "Test PCI violation";

            // Act
            var ex = new PciComplianceException(message);

            // Assert
            ex.Message.ShouldBe(message);
            ex.InnerException.ShouldBeNull();
        }

        [Fact]
        public void PciComplianceException_Should_Be_Throwable_With_Inner_Exception()
        {
            // Arrange
            var message = "Test PCI violation";
            var innerEx = new InvalidOperationException("Card data exposed");

            // Act
            var ex = new PciComplianceException(message, innerEx);

            // Assert
            ex.Message.ShouldBe(message);
            ex.InnerException.ShouldBe(innerEx);
            ex.InnerException?.Message.ShouldBe("Card data exposed");
        }

        #endregion
    }
}
