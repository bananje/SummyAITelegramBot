using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SummyAITelegramBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialMigrationjj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DelayedUserPosts_ChannelPosts_ChannelPostChannelId_ChannelP~",
                table: "DelayedUserPosts");

            migrationBuilder.DropIndex(
                name: "IX_DelayedUserPosts_ChannelPostChannelId_ChannelPostId",
                table: "DelayedUserPosts");

            migrationBuilder.DropColumn(
                name: "ChannelPostChannelId",
                table: "DelayedUserPosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChannelPostChannelId",
                table: "DelayedUserPosts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_DelayedUserPosts_ChannelPostChannelId_ChannelPostId",
                table: "DelayedUserPosts",
                columns: new[] { "ChannelPostChannelId", "ChannelPostId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DelayedUserPosts_ChannelPosts_ChannelPostChannelId_ChannelP~",
                table: "DelayedUserPosts",
                columns: new[] { "ChannelPostChannelId", "ChannelPostId" },
                principalTable: "ChannelPosts",
                principalColumns: new[] { "ChannelId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
