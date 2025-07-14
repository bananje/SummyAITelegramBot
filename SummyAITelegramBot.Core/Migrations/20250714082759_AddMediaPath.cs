using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SummyAITelegramBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaPath",
                table: "ChannelPosts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaPath",
                table: "ChannelPosts");
        }
    }
}
