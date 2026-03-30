using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Editpermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Action", "Name" },
                values: new object[] { "manage_read", "order.manage_read" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Action", "Name" },
                values: new object[] { "manage_update", "order.manage_update" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Entity", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 22, "cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cancel order", "order", false, "order.cancel", null },
                    { 23, "create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create order", "order", false, "order.create", null },
                    { 24, "read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Read order", "order", false, "order.read", null },
                    { 25, "checkout", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Checkout order", "order", false, "order.checkout", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Action", "Name" },
                values: new object[] { "manage.read", "order.manage.read" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Action", "Name" },
                values: new object[] { "manage.update", "order.manage.update" });
        }
    }
}
