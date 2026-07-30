using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Naqi.ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addAuditForSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "PromoCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ProductVariants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "ProductVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ProductSpecifications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ProductSpecifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "ProductSpecifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ProductSpecifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductSpecifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "ProductSpecifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "ProductSpecifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ProductOffers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ProductOffers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "ProductOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ProductOffers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductOffers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "ProductOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "ProductOffers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ProductInstallations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ProductInstallations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "ProductInstallations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ProductInstallations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductInstallations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "ProductInstallations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "ProductInstallations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ProductCategories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ProductCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "ProductCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ProductCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "ProductCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "ProductCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "OfferGroups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ProductOffers");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ProductInstallations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "OfferGroups");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Categories");
        }
    }
}
