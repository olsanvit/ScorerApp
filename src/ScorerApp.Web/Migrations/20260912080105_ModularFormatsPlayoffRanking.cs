using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScorerApp.Migrations
{
    /// <inheritdoc />
    public partial class ModularFormatsPlayoffRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormatJson",
                table: "Seasons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupIndex",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModuleIndex",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayoffMatches",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    BracketPosition = table.Column<int>(type: "integer", nullable: false),
                    ParticipantAId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipantBId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeedA = table.Column<int>(type: "integer", nullable: true),
                    SeedB = table.Column<int>(type: "integer", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Emoji = table.Column<string>(type: "text", nullable: false),
                    Colors = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffMatches", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_SeasonParticipants_ParticipantAId",
                        column: x => x.ParticipantAId,
                        principalTable: "SeasonParticipants",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_SeasonParticipants_ParticipantBId",
                        column: x => x.ParticipantBId,
                        principalTable: "SeasonParticipants",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SportRatings",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    SportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    Games = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Emoji = table.Column<string>(type: "text", nullable: false),
                    Colors = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportRatings", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_SportRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SportRatings_Sports_SportId",
                        column: x => x.SportId,
                        principalTable: "Sports",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SportRatings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_MatchId",
                table: "PlayoffMatches",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_ParticipantAId",
                table: "PlayoffMatches",
                column: "ParticipantAId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_ParticipantBId",
                table: "PlayoffMatches",
                column: "ParticipantBId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_SeasonId_Round_BracketPosition",
                table: "PlayoffMatches",
                columns: new[] { "SeasonId", "Round", "BracketPosition" });

            migrationBuilder.CreateIndex(
                name: "IX_SportRatings_PlayerId",
                table: "SportRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SportRatings_SportId_PlayerId",
                table: "SportRatings",
                columns: new[] { "SportId", "PlayerId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"PlayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SportRatings_SportId_TeamId",
                table: "SportRatings",
                columns: new[] { "SportId", "TeamId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"TeamId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SportRatings_TeamId",
                table: "SportRatings",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoffMatches");

            migrationBuilder.DropTable(
                name: "SportRatings");

            migrationBuilder.DropColumn(
                name: "FormatJson",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GroupIndex",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ModuleIndex",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Matches");
        }
    }
}
