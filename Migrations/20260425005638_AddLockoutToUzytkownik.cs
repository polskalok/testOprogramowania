using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace przychodnia.Migrations
{
    /// <inheritdoc />
    public partial class AddLockoutToUzytkownik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration modifies existing table: add columns for lockout support
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Uzytkownicy",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Uzytkownicy",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Uzytkownicy");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Uzytkownicy");
        }
    }
}
