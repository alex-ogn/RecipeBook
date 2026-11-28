using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    /// <summary>
    /// Profile pictures move from bytes in AspNetUsers.ProfilePicture to files under wwwroot/images/profiles.
    /// Adds the URL column; DataSeeder.MoveProfilePicturesToFilesAsync then moves the existing pictures on startup.
    /// The old ProfilePicture column stays until a later migration drops it. ProfilePictureVersion is no longer
    /// needed, since each upload gets a new file name.
    /// </summary>
    public partial class MoveProfilePicturesToFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ProfilePictureVersion",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "ProfilePictureVersion",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
