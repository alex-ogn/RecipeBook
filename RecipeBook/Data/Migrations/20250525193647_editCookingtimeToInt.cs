using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    public partial class editCookingtimeToInt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Добави нова временна колонa от тип int
            migrationBuilder.AddColumn<int>(
                name: "TempCookingTime",
                table: "Recipies",
                type: "int",
                nullable: false,
                defaultValue: 30); // по избор

            // 2. Копирай данните от TimeSpan → минути
            migrationBuilder.Sql(
                @"UPDATE Recipies SET TempCookingTime = DATEDIFF(MINUTE, 0, CookingTime)");

            // 3. Изтрий старата колонa
            migrationBuilder.DropColumn(
                name: "CookingTime",
                table: "Recipies");

            // 4. Преименувай новата на CookingTime
            migrationBuilder.RenameColumn(
                name: "TempCookingTime",
                table: "Recipies",
                newName: "CookingTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "CookingTime",
                table: "Recipies",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 30, 0));

            migrationBuilder.Sql(
                @"UPDATE Recipies SET CookingTime = CAST(DATEADD(MINUTE, CookingTime, 0) AS time)");

            migrationBuilder.DropColumn(
                name: "CookingTime",
                table: "Recipies");

            migrationBuilder.RenameColumn(
                name: "CookingTime",  // this would have to be managed carefully if needed
                table: "Recipies",
                newName: "CookingTime");
        }

    }
}
