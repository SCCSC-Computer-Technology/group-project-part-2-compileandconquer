using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetiesWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoodFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentMood",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoodEmoticon",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentMood",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MoodEmoticon",
                table: "UserProfiles");
        }
    }
}
