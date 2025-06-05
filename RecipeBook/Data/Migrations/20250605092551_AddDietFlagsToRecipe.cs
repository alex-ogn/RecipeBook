using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    public partial class AddDietFlagsToRecipe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGlutenFree",
                table: "Recipies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLactoseFree",
                table: "Recipies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVegan",
                table: "Recipies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVegetarian",
                table: "Recipies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGlutenFree",
                table: "Recipies");

            migrationBuilder.DropColumn(
                name: "IsLactoseFree",
                table: "Recipies");

            migrationBuilder.DropColumn(
                name: "IsVegan",
                table: "Recipies");

            migrationBuilder.DropColumn(
                name: "IsVegetarian",
                table: "Recipies");
        }
    }
}
