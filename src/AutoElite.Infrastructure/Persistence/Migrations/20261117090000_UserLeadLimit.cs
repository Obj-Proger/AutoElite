using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoElite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the limit of leads in progress of each user; the users already there get the default of ten leads.
    /// </summary>
    public partial class UserLeadLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lead_limit",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lead_limit",
                table: "users");
        }
    }
}
