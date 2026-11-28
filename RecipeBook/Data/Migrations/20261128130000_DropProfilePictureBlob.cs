using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Data.Migrations
{
    /// <summary>
    /// Drops the old AspNetUsers.ProfilePicture bytes column. Profile pictures are files under
    /// wwwroot/images/profiles (ApplicationUser.ProfilePictureUrl) since MoveProfilePicturesToFiles.
    /// A database must have started once with that version, whose startup step moved the stored
    /// pictures to files, before this migration runs; otherwise those pictures are lost.
    /// </summary>
    public partial class DropProfilePictureBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Brings back the empty column only; the pictures stay as files.
            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "AspNetUsers",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
