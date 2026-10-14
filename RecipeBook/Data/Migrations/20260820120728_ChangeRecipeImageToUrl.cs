using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRecipeImageToUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Recipies");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Recipies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Recipies");

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Recipies",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
