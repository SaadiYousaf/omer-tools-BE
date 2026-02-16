using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedwarrantyClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductClaim_warranty_claims_WarrantyClaimId",
                table: "ProductClaim");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductClaim",
                table: "ProductClaim");

            migrationBuilder.RenameTable(
                name: "ProductClaim",
                newName: "productclaim");

            migrationBuilder.RenameIndex(
                name: "IX_ProductClaim_WarrantyClaimId",
                table: "productclaim",
                newName: "IX_productclaim_WarrantyClaimId");

            migrationBuilder.AlterColumn<string>(
                name: "WarrantyClaimId",
                table: "productclaim",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "productclaim",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelNumber",
                table: "productclaim",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "productclaim",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "FaultDescription",
                table: "productclaim",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DisplayOrder",
                table: "productclaim",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "productclaim",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "productclaim",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_productclaim",
                table: "productclaim",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_productclaim_warranty_claims_WarrantyClaimId",
                table: "productclaim",
                column: "WarrantyClaimId",
                principalTable: "warranty_claims",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productclaim_warranty_claims_WarrantyClaimId",
                table: "productclaim");

            migrationBuilder.DropPrimaryKey(
                name: "PK_productclaim",
                table: "productclaim");

            migrationBuilder.RenameTable(
                name: "productclaim",
                newName: "ProductClaim");

            migrationBuilder.RenameIndex(
                name: "IX_productclaim_WarrantyClaimId",
                table: "ProductClaim",
                newName: "IX_ProductClaim_WarrantyClaimId");

            migrationBuilder.AlterColumn<string>(
                name: "WarrantyClaimId",
                table: "ProductClaim",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "ProductClaim",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModelNumber",
                table: "ProductClaim",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ProductClaim",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "FaultDescription",
                table: "ProductClaim",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "DisplayOrder",
                table: "ProductClaim",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ProductClaim",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "ProductClaim",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductClaim",
                table: "ProductClaim",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductClaim_warranty_claims_WarrantyClaimId",
                table: "ProductClaim",
                column: "WarrantyClaimId",
                principalTable: "warranty_claims",
                principalColumn: "Id");
        }
    }
}
