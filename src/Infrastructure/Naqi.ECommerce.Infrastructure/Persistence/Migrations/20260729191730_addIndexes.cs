using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Naqi.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_ExternalPromoId",
                table: "PromoCodes",
                column: "ExternalPromoId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ExternalProductId",
                table: "Products",
                column: "ExternalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferGroups_ExternalOfferGroupId",
                table: "OfferGroups",
                column: "ExternalOfferGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ExternalCategoryId",
                table: "Categories",
                column: "ExternalCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromoCodes_ExternalPromoId",
                table: "PromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_Products_ExternalProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_OfferGroups_ExternalOfferGroupId",
                table: "OfferGroups");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ExternalCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
