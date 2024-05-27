using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    public partial class AddPictures2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Recipies");

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Recipies",
                type: "varbinary(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Recipies");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Recipies",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
