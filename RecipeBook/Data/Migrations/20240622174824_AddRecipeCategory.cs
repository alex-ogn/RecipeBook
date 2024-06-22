using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    public partial class AddRecipeCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the RecipeCategories table
            migrationBuilder.CreateTable(
                name: "RecipeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeCategories", x => x.Id);
                });

            // Step 2: Insert a default category
            migrationBuilder.Sql("INSERT INTO RecipeCategories (Name) VALUES ('Default Category')");

            // Step 3: Add the RecipeCategoryId column with default value
            migrationBuilder.AddColumn<int>(
                name: "RecipeCategoryId",
                table: "Recipies",
                type: "int",
                nullable: false,
                defaultValue: 1); // Assuming the ID of 'Default Category' is 1

            // Step 4: Ensure existing Recipies are updated to use the default category
            // This assumes that 'Default Category' gets ID = 1, which should be verified or dynamically obtained
            migrationBuilder.Sql("UPDATE Recipies SET RecipeCategoryId = 1");

            // Step 5: Create the index on RecipeCategoryId
            migrationBuilder.CreateIndex(
                name: "IX_Recipies_RecipeCategoryId",
                table: "Recipies",
                column: "RecipeCategoryId");

            // Step 6: Add the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Recipies_RecipeCategories_RecipeCategoryId",
                table: "Recipies",
                column: "RecipeCategoryId",
                principalTable: "RecipeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipies_RecipeCategories_RecipeCategoryId",
                table: "Recipies");

            migrationBuilder.DropTable(
                name: "RecipeCategories");

            migrationBuilder.DropIndex(
                name: "IX_Recipies_RecipeCategoryId",
                table: "Recipies");

            migrationBuilder.DropColumn(
                name: "RecipeCategoryId",
                table: "Recipies");
        }
    }
}
