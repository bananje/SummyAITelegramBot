using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SummyAITelegramBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DelayedUserPost_ChannelPosts_ChannelPostChannelId_ChannelPo~",
                table: "DelayedUserPost");

            migrationBuilder.DropForeignKey(
                name: "FK_DelayedUserPost_Users_UserId",
                table: "DelayedUserPost");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscription_Users_UserId",
                table: "Subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscription",
                table: "Subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DelayedUserPost",
                table: "DelayedUserPost");

            migrationBuilder.RenameTable(
                name: "Subscription",
                newName: "Subscriptions");

            migrationBuilder.RenameTable(
                name: "DelayedUserPost",
                newName: "DelayedUserPosts");

            migrationBuilder.RenameIndex(
                name: "IX_Subscription_UserId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DelayedUserPost_UserId",
                table: "DelayedUserPosts",
                newName: "IX_DelayedUserPosts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DelayedUserPost_ChannelPostChannelId_ChannelPostId",
                table: "DelayedUserPosts",
                newName: "IX_DelayedUserPosts_ChannelPostChannelId_ChannelPostId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DelayedUserPosts",
                table: "DelayedUserPosts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DelayedUserPosts_ChannelPosts_ChannelPostChannelId_ChannelP~",
                table: "DelayedUserPosts",
                columns: new[] { "ChannelPostChannelId", "ChannelPostId" },
                principalTable: "ChannelPosts",
                principalColumns: new[] { "ChannelId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DelayedUserPosts_Users_UserId",
                table: "DelayedUserPosts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_UserId",
                table: "Subscriptions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DelayedUserPosts_ChannelPosts_ChannelPostChannelId_ChannelP~",
                table: "DelayedUserPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_DelayedUserPosts_Users_UserId",
                table: "DelayedUserPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_UserId",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DelayedUserPosts",
                table: "DelayedUserPosts");

            migrationBuilder.RenameTable(
                name: "Subscriptions",
                newName: "Subscription");

            migrationBuilder.RenameTable(
                name: "DelayedUserPosts",
                newName: "DelayedUserPost");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscription",
                newName: "IX_Subscription_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DelayedUserPosts_UserId",
                table: "DelayedUserPost",
                newName: "IX_DelayedUserPost_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DelayedUserPosts_ChannelPostChannelId_ChannelPostId",
                table: "DelayedUserPost",
                newName: "IX_DelayedUserPost_ChannelPostChannelId_ChannelPostId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscription",
                table: "Subscription",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DelayedUserPost",
                table: "DelayedUserPost",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DelayedUserPost_ChannelPosts_ChannelPostChannelId_ChannelPo~",
                table: "DelayedUserPost",
                columns: new[] { "ChannelPostChannelId", "ChannelPostId" },
                principalTable: "ChannelPosts",
                principalColumns: new[] { "ChannelId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DelayedUserPost_Users_UserId",
                table: "DelayedUserPost",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscription_Users_UserId",
                table: "Subscription",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
