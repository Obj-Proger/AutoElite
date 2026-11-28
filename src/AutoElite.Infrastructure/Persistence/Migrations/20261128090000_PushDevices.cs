using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoElite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the devices that receive push notifications: the token of each installation in the push service, unique,
    /// with the user signed in there, whose devices go with the user.
    /// </summary>
    public partial class PushDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "push_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    platform = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    registered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_push_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_push_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_push_devices_token",
                table: "push_devices",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_push_devices_user_id",
                table: "push_devices",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "push_devices");
        }
    }
}
