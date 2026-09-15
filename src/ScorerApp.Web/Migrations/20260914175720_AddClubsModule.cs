using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScorerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClubsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "SeasonParticipants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Organizations", x => x.Guid);
                });

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LicensePlate = table.Column<string>(type: "text", nullable: true),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Cars", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Cars_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShortName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinCode = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Clubs", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Clubs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyLinks",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserId = table.Column<string>(type: "text", nullable: false),
                    ChildUserId = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_FamilyLinks", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_FamilyLinks_AspNetUsers_ChildUserId",
                        column: x => x.ChildUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyLinks_AspNetUsers_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyLinks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarReservations",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    KmAtStart = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    KmAtEnd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_CarReservations", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_CarReservations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarReservations_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Circulars",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderUserId = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SendEmail = table.Column<bool>(type: "boolean", nullable: false),
                    SendNtfy = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Circulars", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Circulars_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Circulars_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Circulars_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubMembers",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_ClubMembers", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_ClubMembers_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubMembers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubThreads",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ThreadType = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ClubThreads", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_ClubThreads_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubThreads_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Invitations", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Invitations_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NtfyEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MinPriority = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CircularRecipients",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    CircularId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EmailStatus = table.Column<int>(type: "integer", nullable: false),
                    NtfyStatus = table.Column<int>(type: "integer", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_CircularRecipients", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_CircularRecipients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CircularRecipients_Circulars_CircularId",
                        column: x => x.CircularId,
                        principalTable: "Circulars",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ChatMessages", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_ChatMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ClubThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "ClubThreads",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThreadParticipants",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_ThreadParticipants", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_ThreadParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThreadParticipants_ClubThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "ClubThreads",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessageReads",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_ChatMessageReads", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_ChatMessageReads_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ClubId",
                table: "Teams",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonParticipants_ClubId",
                table: "SeasonParticipants",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CarReservations_CarId_DateFrom_DateTo",
                table: "CarReservations",
                columns: new[] { "CarId", "DateFrom", "DateTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CarReservations_UserId",
                table: "CarReservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_OrganizationId",
                table: "Cars",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReads_MessageId_UserId",
                table: "ChatMessageReads",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderUserId",
                table: "ChatMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ThreadId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ThreadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CircularRecipients_CircularId_UserId",
                table: "CircularRecipients",
                columns: new[] { "CircularId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircularRecipients_UserId",
                table: "CircularRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Circulars_ClubId",
                table: "Circulars",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Circulars_OrganizationId",
                table: "Circulars",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Circulars_SenderUserId",
                table: "Circulars",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembers_ClubId_PlayerId",
                table: "ClubMembers",
                columns: new[] { "ClubId", "PlayerId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembers_PlayerId",
                table: "ClubMembers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_JoinCode",
                table: "Clubs",
                column: "JoinCode",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_OrganizationId_Name",
                table: "Clubs",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ClubThreads_ClubId",
                table: "ClubThreads",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubThreads_CreatedByUserId",
                table: "ClubThreads",
                column: "CreatedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_FamilyLinks_ParentUserId",
                table: "FamilyLinks",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ClubId",
                table: "Invitations",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_ClubId",
                table: "NotificationPreferences",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_ClubId",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "ClubId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_UserId",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_UserId",
                table: "OrganizationMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadParticipants_ThreadId_UserId",
                table: "ThreadParticipants",
                columns: new[] { "ThreadId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadParticipants_UserId",
                table: "ThreadParticipants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonParticipants_Clubs_ClubId",
                table: "SeasonParticipants",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Guid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Clubs_ClubId",
                table: "Teams",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Guid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeasonParticipants_Clubs_ClubId",
                table: "SeasonParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Clubs_ClubId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "CarReservations");

            migrationBuilder.DropTable(
                name: "ChatMessageReads");

            migrationBuilder.DropTable(
                name: "CircularRecipients");

            migrationBuilder.DropTable(
                name: "ClubMembers");

            migrationBuilder.DropTable(
                name: "FamilyLinks");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "ThreadParticipants");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Circulars");

            migrationBuilder.DropTable(
                name: "ClubThreads");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ClubId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_SeasonParticipants_ClubId",
                table: "SeasonParticipants");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "SeasonParticipants");
        }
    }
}
