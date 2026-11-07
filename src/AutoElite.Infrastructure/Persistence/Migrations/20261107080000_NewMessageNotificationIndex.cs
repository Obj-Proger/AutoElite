using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoElite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Written by hand: a database object EF Core does not model, so the model, the snapshot and the target model of
    /// this migration are those of <see cref="InitialCreate"/>.
    /// </summary>
    public partial class NewMessageNotificationIndex : Migration
    {
        private static readonly string[] Scripts = ["06-notification-new-message"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => DatabaseObjects.Create(migrationBuilder, Scripts);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => DatabaseObjects.Drop(migrationBuilder, Scripts);
    }
}
