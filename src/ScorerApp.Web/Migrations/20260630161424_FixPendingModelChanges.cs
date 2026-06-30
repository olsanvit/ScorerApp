using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScorerApp.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Teams",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Teams",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Teams",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TeamPlayers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "TeamPlayers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TeamPlayers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TeamPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TeamPlayers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Sports",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Sports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Sports",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Sports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Sports",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Seasons",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Seasons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Seasons",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Seasons",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SeasonParticipants",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "SeasonParticipants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SeasonParticipants",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SeasonParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SeasonParticipants",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Races",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Races",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Races",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Races",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Races",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RaceResults",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "RaceResults",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RaceResults",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RaceResults",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "RaceResults",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Players",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Players",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Players",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MatchSets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "MatchSets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MatchSets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MatchSets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MatchSets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "MatchEvents",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "MatchEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MatchEvents",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MatchEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MatchEvents",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Matches",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Matches",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Matches",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Leagues",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Leagues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Leagues",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Leagues",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TeamPlayers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TeamPlayers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TeamPlayers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TeamPlayers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TeamPlayers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SeasonParticipants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SeasonParticipants");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SeasonParticipants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SeasonParticipants");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SeasonParticipants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MatchSets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MatchSets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MatchSets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MatchSets");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MatchSets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");
        }
    }
}
