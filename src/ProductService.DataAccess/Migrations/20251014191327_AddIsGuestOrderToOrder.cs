using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGuestOrderToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGuestOrder",
                table: "orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGuestOrder",
                table: "orders");
        }
    }
}
