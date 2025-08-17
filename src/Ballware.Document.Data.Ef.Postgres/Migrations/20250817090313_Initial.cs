using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ballware.Document.Data.Ef.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    entity = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    report_parameter = table.Column<string>(type: "text", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_changer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_change_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    document_params = table.Column<string>(type: "text", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_changer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_change_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mail = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    attachment = table.Column<bool>(type: "boolean", nullable: false),
                    attachment_file_name = table.Column<string>(type: "text", nullable: true),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    last_send_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_changer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_change_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_tenant_id",
                table: "document",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_tenant_id_uuid",
                table: "document",
                columns: new[] { "tenant_id", "uuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_id",
                table: "notification",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_id_identifier",
                table: "notification",
                columns: new[] { "tenant_id", "identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_tenant_id_uuid",
                table: "notification",
                columns: new[] { "tenant_id", "uuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_frequency",
                table: "subscription",
                column: "frequency");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_tenant_id",
                table: "subscription",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_tenant_id_uuid",
                table: "subscription",
                columns: new[] { "tenant_id", "uuid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "subscription");
        }
    }
}
