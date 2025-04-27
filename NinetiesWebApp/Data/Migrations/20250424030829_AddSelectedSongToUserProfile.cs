using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetiesWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedSongToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedSong",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedSong",
                table: "UserProfiles");
        }
    }
}
