using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderManagePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Entity", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 20, "manage.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin: list and view orders", "order", false, "order.manage_read", null },
                    { 21, "manage.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin: update order status", "order", false, "order.manage_update", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21);
        }
    }
}
