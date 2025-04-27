using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetiesWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarImageToUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
            name: "AvatarImage",
            table: "UserProfiles",
            nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
            name: "AvatarImage",
            table: "UserProfiles");

        }
    }
}
