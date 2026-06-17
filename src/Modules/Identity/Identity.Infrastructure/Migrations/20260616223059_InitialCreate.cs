using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "Homes",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "bytea", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                MaxDevices = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Homes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventType = table.Column<string>(type: "text", nullable: false),
                EventPayload = table.Column<string>(type: "text", nullable: false),
                OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Processed = table.Column<bool>(type: "boolean", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                Error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "bytea", nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                EmailHash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                EmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "HomeMemberships",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "bytea", nullable: false),
                HomeId = table.Column<byte[]>(type: "bytea", nullable: false),
                UserId = table.Column<byte[]>(type: "bytea", nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HomeMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_HomeMemberships_Homes_HomeId",
                    column: x => x.HomeId,
                    principalSchema: "identity",
                    principalTable: "Homes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "bytea", nullable: false),
                UserId = table.Column<byte[]>(type: "bytea", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => new { x.Id, x.UserId });
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Sessions",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "bytea", nullable: false),
                UserId = table.Column<byte[]>(type: "bytea", nullable: false),
                DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                IsTrusted = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sessions", x => new { x.Id, x.UserId });
                table.ForeignKey(
                    name: "FK_Sessions_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TrustedDevices",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "bytea", nullable: false),
                UserId = table.Column<byte[]>(type: "bytea", nullable: false),
                DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TrustedDevices", x => new { x.Id, x.UserId });
                table.ForeignKey(
                    name: "FK_TrustedDevices_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HomeMemberships_HomeId",
            schema: "identity",
            table: "HomeMemberships",
            column: "HomeId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            schema: "identity",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_UserId",
            schema: "identity",
            table: "Sessions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_TrustedDevices_UserId",
            schema: "identity",
            table: "TrustedDevices",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            schema: "identity",
            table: "Users",
            column: "Email");

        migrationBuilder.CreateIndex(
            name: "IX_Users_EmailHash",
            schema: "identity",
            table: "Users",
            column: "EmailHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HomeMemberships",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "OutboxMessages",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "RefreshTokens",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "Sessions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "TrustedDevices",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "Homes",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "identity");
    }
}
