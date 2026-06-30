using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScorerApp.Migrations
{
    /// <inheritdoc />
    public partial class AlignAppUserWithSharedServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCulture",
                table: "AspNetUsers",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsBanned",         table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "PreferredCulture", table: "AspNetUsers");
        }
    }
}
