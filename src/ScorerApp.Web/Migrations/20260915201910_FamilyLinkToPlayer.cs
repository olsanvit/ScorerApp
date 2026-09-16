using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScorerApp.Migrations
{
    /// <inheritdoc />
    public partial class FamilyLinkToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyLinks_AspNetUsers_ChildUserId",
                table: "FamilyLinks");

            migrationBuilder.DropIndex(
                name: "IX_FamilyLinks_ChildUserId",
                table: "FamilyLinks");

            migrationBuilder.DropIndex(
                name: "IX_FamilyLinks_OrganizationId_ParentUserId_ChildUserId",
                table: "FamilyLinks");

            migrationBuilder.DropColumn(
                name: "ChildUserId",
                table: "FamilyLinks");

            migrationBuilder.AddColumn<Guid>(
                name: "ChildPlayerId",
                table: "FamilyLinks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FamilyLinks_ChildPlayerId",
                table: "FamilyLinks",
                column: "ChildPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyLinks_OrganizationId_ParentUserId_ChildPlayerId",
                table: "FamilyLinks",
                columns: new[] { "OrganizationId", "ParentUserId", "ChildPlayerId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyLinks_Players_ChildPlayerId",
                table: "FamilyLinks",
                column: "ChildPlayerId",
                principalTable: "Players",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyLinks_Players_ChildPlayerId",
                table: "FamilyLinks");

            migrationBuilder.DropIndex(
                name: "IX_FamilyLinks_ChildPlayerId",
                table: "FamilyLinks");

            migrationBuilder.DropIndex(
                name: "IX_FamilyLinks_OrganizationId_ParentUserId_ChildPlayerId",
                table: "FamilyLinks");

            migrationBuilder.DropColumn(
                name: "ChildPlayerId",
                table: "FamilyLinks");

            migrationBuilder.AddColumn<string>(
                name: "ChildUserId",
                table: "FamilyLinks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyLinks_ChildUserId",
                table: "FamilyLinks",
                column: "ChildUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyLinks_OrganizationId_ParentUserId_ChildUserId",
                table: "FamilyLinks",
                columns: new[] { "OrganizationId", "ParentUserId", "ChildUserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyLinks_AspNetUsers_ChildUserId",
                table: "FamilyLinks",
                column: "ChildUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
