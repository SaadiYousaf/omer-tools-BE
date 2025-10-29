using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PhoneNumberInShippingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "shipping_addresses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "shipping_addresses");
        }
    }
}
