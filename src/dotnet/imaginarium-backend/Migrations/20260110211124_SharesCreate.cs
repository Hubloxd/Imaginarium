using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImaginariumBackend.Migrations
{
    /// <inheritdoc />
    public partial class SharesCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: true),
                    SharedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SharedWithGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShareToken = table.Column<string>(type: "text", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PermissionLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shares_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shares_Groups_SharedWithGroupId",
                        column: x => x.SharedWithGroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shares_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shares_Users_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shares_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shares_AlbumId",
                table: "Shares",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_MediaId",
                table: "Shares",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_SharedByUserId",
                table: "Shares",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_SharedWithGroupId",
                table: "Shares",
                column: "SharedWithGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_SharedWithUserId",
                table: "Shares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_ShareToken",
                table: "Shares",
                column: "ShareToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shares");
        }
    }
}
