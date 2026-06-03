using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScorerApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsWhitelisted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWhitelisted",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWhitelisted",
                table: "AspNetUsers");
        }
    }
}
