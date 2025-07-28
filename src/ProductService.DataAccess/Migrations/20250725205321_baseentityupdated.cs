using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class baseentityupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "subcategories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "subcategories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "product_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "product_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "product_images",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "product_images",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "brands",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "brands",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "subcategories");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "subcategories");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "brands");
        }
    }
}
