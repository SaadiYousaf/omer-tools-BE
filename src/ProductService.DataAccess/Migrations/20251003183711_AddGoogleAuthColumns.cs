using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Local");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "users");
        }
    }
}
