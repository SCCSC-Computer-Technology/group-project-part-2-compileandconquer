using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinetiesWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestbookEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SenderUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Emoticon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestbookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestbookEntries_UserProfiles_ProfileUserId",
                        column: x => x.ProfileUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuestbookEntries_UserProfiles_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestbookEntries");
        }
    }
}
