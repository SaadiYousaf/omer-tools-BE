using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class dbupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "subcategories");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "subcategories",
                type: "tsvector",
                nullable: true);
        }
    }
}
