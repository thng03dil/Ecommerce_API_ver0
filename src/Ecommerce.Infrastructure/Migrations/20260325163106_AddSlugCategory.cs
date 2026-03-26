using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bước 1: Thêm cột Slug (nếu chưa có)
            migrationBuilder.Sql(@"
    IF COL_LENGTH(N'dbo.Categories', N'Slug') IS NULL
    BEGIN
        ALTER TABLE [Categories] ADD [Slug] nvarchar(100) NOT NULL CONSTRAINT [DF_Categories_Slug_restore] DEFAULT N'';
    END");

            // Bước 2: Cập nhật dữ liệu cho cột Slug (Tách ra lệnh riêng)
            migrationBuilder.Sql(@"
    UPDATE [Categories]
    SET [Slug] = LEFT(REPLACE(LOWER(LTRIM(RTRIM([Name]))), N' ', N'-'), 85) + N'-' + CAST([Id] AS nvarchar(10))
    WHERE [Slug] = N'';");

            // Bước 3: Dọn dẹp Constraint và tạo Index
            migrationBuilder.Sql(@"
    IF COL_LENGTH(N'dbo.Categories', N'Slug') IS NOT NULL
    BEGIN
        ALTER TABLE [Categories] DROP CONSTRAINT [DF_Categories_Slug_restore];
        
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes 
            WHERE name = N'IX_Categories_Slug' AND object_id = OBJECT_ID(N'dbo.Categories'))
            CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_Slug] ON [dbo].[Categories]([Slug]);
    END");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Categories', N'Slug') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_Categories_Slug' AND object_id = OBJECT_ID(N'dbo.Categories'))
        DROP INDEX [IX_Categories_Slug] ON [dbo].[Categories];
    ALTER TABLE [dbo].[Categories] DROP COLUMN [Slug];
END
");
        }
    }
}