using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    public partial class AddViewCountToRecipe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Recipies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Recipies");
        }
    }
}
