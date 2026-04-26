using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace przychodnia.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordChangeFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MuszZmieniHaslo",
                table: "Uzytkownicy",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OstatniaHasla",
                table: "Uzytkownicy",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MuszZmieniHaslo",
                table: "Uzytkownicy");

            migrationBuilder.DropColumn(
                name: "OstatniaHasla",
                table: "Uzytkownicy");
        }
    }
}
