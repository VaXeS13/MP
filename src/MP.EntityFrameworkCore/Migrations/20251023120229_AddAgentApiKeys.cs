using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppAgentApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Unique agent identifier"),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "mp_agent_", comment: "API key prefix for identification"),
                    Suffix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, comment: "Last 8 characters of API key (for display)"),
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, comment: "SHA256 hash of the API key (never store raw key)"),
                    Salt = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Salt for SHA256 hashing"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Friendly name for the API key"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Description of the API key's purpose"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "When the API key expires (90 days from creation)"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "When this API key was last used for authentication"),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Number of times this key has been used"),
                    IpWhitelist = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Comma-separated list of allowed IP addresses (empty = all IPs allowed)"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether the API key is currently active"),
                    ShouldRotate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether the API key should be rotated soon"),
                    RotatedFromKeyId = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "When this key was rotated from a previous key"),
                    FailedAuthenticationAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Current number of failed authentication attempts"),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "When this key's lock will expire (after 5 failed attempts)"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAgentApiKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentApiKeys_ExpiresAt",
                table: "AppAgentApiKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApiKeys_IsActive",
                table: "AppAgentApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApiKeys_LastUsedAt",
                table: "AppAgentApiKeys",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApiKeys_TenantId_AgentId",
                table: "AppAgentApiKeys",
                columns: new[] { "TenantId", "AgentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentApiKeys_TenantId_KeyHash",
                table: "AppAgentApiKeys",
                columns: new[] { "TenantId", "KeyHash" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAgentApiKeys");
        }
    }
}
