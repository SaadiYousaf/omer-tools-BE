using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSEOFieldsToProductDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgDescription",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgImage",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgTitle",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterCard",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterDescription",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterImage",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterTitle",
                table: "products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "products");

            migrationBuilder.DropColumn(
                name: "OgDescription",
                table: "products");

            migrationBuilder.DropColumn(
                name: "OgImage",
                table: "products");

            migrationBuilder.DropColumn(
                name: "OgTitle",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TwitterCard",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TwitterDescription",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TwitterImage",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TwitterTitle",
                table: "products");
        }
    }
}
