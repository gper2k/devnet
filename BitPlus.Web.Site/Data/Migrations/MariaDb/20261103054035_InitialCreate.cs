using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitPlus.Web.Site.Data.Migrations.MariaDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Configs",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Configs", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContentRuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TargetNo = table.Column<int>(type: "int", nullable: true),
                    ReporterId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReporterName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentReports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_FileRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllowExtensions = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllowCount = table.Column<int>(type: "int", nullable: false),
                    AllowTotalSize = table.Column<long>(type: "bigint", nullable: false),
                    AllowSize = table.Column<long>(type: "bigint", nullable: false),
                    RegistWebRoot = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_FileRules", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Languages",
                columns: table => new
                {
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Culture = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Languages", x => new { x.Category, x.Key, x.Culture });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Menus",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Extend = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ParentKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Menus", x => x.Key);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Menus_BPXPREFIX_Menus_ParentKey",
                        column: x => x.ParentKey,
                        principalTable: "BPXPREFIX_Menus",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Policies", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Popups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Area = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Popups", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Grade = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Roles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_SectionTypes",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Layout = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Available = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_SectionTypes", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastAccess = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedUserName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecurityStamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_WidgetTypes",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categories = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinWidth = table.Column<double>(type: "double", nullable: false),
                    MinHeight = table.Column<int>(type: "int", nullable: false),
                    MaxWidth = table.Column<double>(type: "double", nullable: false),
                    MaxHeight = table.Column<int>(type: "int", nullable: false),
                    Settings = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Available = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_WidgetTypes", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RuleNo = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FileRuleId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Category = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OnlyView = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Enabler = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentRules_BPXPREFIX_FileRules_FileRuleId",
                        column: x => x.FileRuleId,
                        principalTable: "BPXPREFIX_FileRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_FileAccessRules",
                columns: table => new
                {
                    RuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_FileAccessRules", x => new { x.RuleId, x.Mode, x.TargetType, x.TargetKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_FileAccessRules_BPXPREFIX_FileRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "BPXPREFIX_FileRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_MenuAccesses",
                columns: table => new
                {
                    MenuKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_MenuAccesses", x => new { x.MenuKey, x.TargetType, x.TargetKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_MenuAccesses_BPXPREFIX_Menus_MenuKey",
                        column: x => x.MenuKey,
                        principalTable: "BPXPREFIX_Menus",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_MenuDpNames",
                columns: table => new
                {
                    OwnerID = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_MenuDpNames", x => new { x.OwnerID, x.Culture });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_MenuDpNames_BPXPREFIX_Menus_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Menus",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_PolicyTargets",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_PolicyTargets", x => new { x.PolicyId, x.TargetType, x.TargetId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_PolicyTargets_BPXPREFIX_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "BPXPREFIX_Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_PopupTargets",
                columns: table => new
                {
                    PopupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_PopupTargets", x => new { x.PopupId, x.TargetType, x.TargetKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_PopupTargets_BPXPREFIX_Popups_PopupId",
                        column: x => x.PopupId,
                        principalTable: "BPXPREFIX_Popups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_RoleClaims_BPXPREFIX_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "BPXPREFIX_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_RoleDpNames",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_RoleDpNames", x => new { x.OwnerID, x.Culture });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_RoleDpNames_BPXPREFIX_Roles_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ApiTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prefix = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Expire = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUsed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastIP = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatorId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ApiTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ApiTokens_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_DeletedUsers",
                columns: table => new
                {
                    DeleteNo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_DeletedUsers", x => x.DeleteNo);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_DeletedUsers_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UploaderId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Password = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadIP = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileMime = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileExtension = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DownCount = table.Column<int>(type: "int", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Files_BPXPREFIX_FileRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "BPXPREFIX_FileRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Files_BPXPREFIX_Users_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Grade = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ManagerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ParentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Inherit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Location = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Groups_BPXPREFIX_Groups_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BPXPREFIX_Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Groups_BPXPREFIX_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RefreshStamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Expire = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Revoked = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_RefreshTokens_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserClaims_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserDpNames",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserDpNames", x => new { x.OwnerID, x.Culture });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserDpNames_BPXPREFIX_Users_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderKey = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserLogins_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReceiverId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SenderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceiverDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SenderDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserMessages_BPXPREFIX_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserMessages_BPXPREFIX_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserNotices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReceiverId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Args = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserNotices_BPXPREFIX_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserProperties",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserProperties", x => new { x.OwnerID, x.Key });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserProperties_BPXPREFIX_Users_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserRoles_BPXPREFIX_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "BPXPREFIX_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserRoles_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserTokens_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentAccessRules",
                columns: table => new
                {
                    ContentRuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AccessMode = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentAccessRules", x => new { x.ContentRuleId, x.AccessMode, x.TargetType, x.TargetKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentAccessRules_BPXPREFIX_ContentRules_ContentR~",
                        column: x => x.ContentRuleId,
                        principalTable: "BPXPREFIX_ContentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentApprovers",
                columns: table => new
                {
                    ContentRuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ApproverId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SortNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentApprovers", x => new { x.ContentRuleId, x.ApproverId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentApprovers_BPXPREFIX_ContentRules_ContentRul~",
                        column: x => x.ContentRuleId,
                        principalTable: "BPXPREFIX_ContentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentApprovers_BPXPREFIX_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentRuleDpNames",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentRuleDpNames", x => new { x.OwnerID, x.Culture });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentRuleDpNames_BPXPREFIX_ContentRules_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_ContentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentRuleProperties",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentRuleProperties", x => new { x.OwnerID, x.Key });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentRuleProperties_BPXPREFIX_ContentRules_Owner~",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_ContentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContentRuleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WriterId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Password = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentBody = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentNo = table.Column<int>(type: "int", nullable: true),
                    TargetNo = table.Column<int>(type: "int", nullable: true),
                    LevelNo = table.Column<int>(type: "int", nullable: true),
                    SortNo = table.Column<int>(type: "int", nullable: true),
                    ReadCount = table.Column<int>(type: "int", nullable: false),
                    LikePoint = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsNotice = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Approved = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Contents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Contents_BPXPREFIX_ContentRules_ContentRuleId",
                        column: x => x.ContentRuleId,
                        principalTable: "BPXPREFIX_ContentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Contents_BPXPREFIX_Users_WriterId",
                        column: x => x.WriterId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_FileDownloaders",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_FileDownloaders", x => new { x.FileId, x.UserId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_FileDownloaders_BPXPREFIX_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "BPXPREFIX_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_FileDownloaders_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_FileTargets",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_FileTargets", x => new { x.FileId, x.TargetType, x.TargetKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_FileTargets_BPXPREFIX_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "BPXPREFIX_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_PreFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OwnerType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Registed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_PreFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_PreFiles_BPXPREFIX_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "BPXPREFIX_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_GroupDpNames",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_GroupDpNames", x => new { x.OwnerID, x.Culture });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_GroupDpNames_BPXPREFIX_Groups_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_GroupProperties",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_GroupProperties", x => new { x.OwnerID, x.Key });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_GroupProperties_BPXPREFIX_Groups_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_GroupRoles",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_GroupRoles", x => new { x.GroupId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_GroupRoles_BPXPREFIX_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "BPXPREFIX_Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_GroupRoles_BPXPREFIX_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "BPXPREFIX_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_UserGroups",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserGroups_BPXPREFIX_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "BPXPREFIX_Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_UserGroups_BPXPREFIX_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CommentBody = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommentUserIP = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommentUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Password = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LikePoint = table.Column<int>(type: "int", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Comments_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_Comments_BPXPREFIX_Users_CommentUserId",
                        column: x => x.CommentUserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentFiles",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileInfoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Registed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentFiles", x => new { x.ContentId, x.FileInfoId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentFiles_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentFiles_BPXPREFIX_Files_FileInfoId",
                        column: x => x.FileInfoId,
                        principalTable: "BPXPREFIX_Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentHistories",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentHistories", x => new { x.ContentId, x.VersionNo });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentHistories_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentLikers",
                columns: table => new
                {
                    OwnerDataId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LikeUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Liked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentLikers", x => new { x.OwnerDataId, x.LikeUserId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentLikers_BPXPREFIX_Contents_OwnerDataId",
                        column: x => x.OwnerDataId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentLikers_BPXPREFIX_Users_LikeUserId",
                        column: x => x.LikeUserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentProperties",
                columns: table => new
                {
                    OwnerID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentProperties", x => new { x.OwnerID, x.Key });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentProperties_BPXPREFIX_Contents_OwnerID",
                        column: x => x.OwnerID,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentReaders",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReaderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentReaders", x => new { x.ContentId, x.ReaderId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentReaders_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentReaders_BPXPREFIX_Users_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentReadRules",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetKey = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentReadRules", x => new { x.ContentId, x.TargetType, x.TargetKey });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentReadRules_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentTags",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentTags", x => new { x.ContentId, x.Tag });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentTags_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_ContentTranslations",
                columns: table => new
                {
                    ContentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Summary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMachine = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SourceUDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    WriterId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_ContentTranslations", x => new { x.ContentId, x.Culture });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_ContentTranslations_BPXPREFIX_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "BPXPREFIX_Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BPXPREFIX_CommentLikers",
                columns: table => new
                {
                    OwnerDataId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LikeUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Liked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPXPREFIX_CommentLikers", x => new { x.OwnerDataId, x.LikeUserId });
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_CommentLikers_BPXPREFIX_Comments_OwnerDataId",
                        column: x => x.OwnerDataId,
                        principalTable: "BPXPREFIX_Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BPXPREFIX_CommentLikers_BPXPREFIX_Users_LikeUserId",
                        column: x => x.LikeUserId,
                        principalTable: "BPXPREFIX_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ApiTokens_CDate",
                table: "BPXPREFIX_ApiTokens",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ApiTokens_Hash",
                table: "BPXPREFIX_ApiTokens",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ApiTokens_UserId",
                table: "BPXPREFIX_ApiTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_CommentLikers_LikeUserId",
                table: "BPXPREFIX_CommentLikers",
                column: "LikeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Comments_CommentUserId",
                table: "BPXPREFIX_Comments",
                column: "CommentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Comments_ContentId_CDate",
                table: "BPXPREFIX_Comments",
                columns: new[] { "ContentId", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Configs_CDate",
                table: "BPXPREFIX_Configs",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Configs_UDate",
                table: "BPXPREFIX_Configs",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentAccessRules_CDate",
                table: "BPXPREFIX_ContentAccessRules",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentAccessRules_TargetType_TargetKey",
                table: "BPXPREFIX_ContentAccessRules",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentApprovers_ApproverId",
                table: "BPXPREFIX_ContentApprovers",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentApprovers_ContentRuleId_SortNo",
                table: "BPXPREFIX_ContentApprovers",
                columns: new[] { "ContentRuleId", "SortNo" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentFiles_FileInfoId",
                table: "BPXPREFIX_ContentFiles",
                column: "FileInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentFiles_Registed_CDate",
                table: "BPXPREFIX_ContentFiles",
                columns: new[] { "Registed", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentHistories_CDate",
                table: "BPXPREFIX_ContentHistories",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentLikers_LikeUserId",
                table: "BPXPREFIX_ContentLikers",
                column: "LikeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentProperties_CDate",
                table: "BPXPREFIX_ContentProperties",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentProperties_UDate",
                table: "BPXPREFIX_ContentProperties",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentReaders_ReaderId_CDate",
                table: "BPXPREFIX_ContentReaders",
                columns: new[] { "ReaderId", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentReadRules_TargetType_TargetKey",
                table: "BPXPREFIX_ContentReadRules",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentReports_ContentRuleId_CDate",
                table: "BPXPREFIX_ContentReports",
                columns: new[] { "ContentRuleId", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentReports_ReporterId",
                table: "BPXPREFIX_ContentReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentReports_TargetType_TargetId_ReporterId",
                table: "BPXPREFIX_ContentReports",
                columns: new[] { "TargetType", "TargetId", "ReporterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRuleProperties_CDate",
                table: "BPXPREFIX_ContentRuleProperties",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRuleProperties_UDate",
                table: "BPXPREFIX_ContentRuleProperties",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRules_CDate",
                table: "BPXPREFIX_ContentRules",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRules_FileRuleId",
                table: "BPXPREFIX_ContentRules",
                column: "FileRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRules_RuleNo",
                table: "BPXPREFIX_ContentRules",
                column: "RuleNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRules_Type",
                table: "BPXPREFIX_ContentRules",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentRules_UDate",
                table: "BPXPREFIX_ContentRules",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_CDate",
                table: "BPXPREFIX_Contents",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_ContentRuleId_CDate",
                table: "BPXPREFIX_Contents",
                columns: new[] { "ContentRuleId", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_ContentRuleId_ContentNo",
                table: "BPXPREFIX_Contents",
                columns: new[] { "ContentRuleId", "ContentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_ContentRuleId_IsNotice",
                table: "BPXPREFIX_Contents",
                columns: new[] { "ContentRuleId", "IsNotice" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_ContentRuleId_LevelNo_IsNotice_UDate",
                table: "BPXPREFIX_Contents",
                columns: new[] { "ContentRuleId", "LevelNo", "IsNotice", "UDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_ContentRuleId_TargetNo_SortNo",
                table: "BPXPREFIX_Contents",
                columns: new[] { "ContentRuleId", "TargetNo", "SortNo" },
                descending: new[] { false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_ContentRuleId_TargetNo_UDate",
                table: "BPXPREFIX_Contents",
                columns: new[] { "ContentRuleId", "TargetNo", "UDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_Published_UDate",
                table: "BPXPREFIX_Contents",
                columns: new[] { "Published", "UDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_UDate",
                table: "BPXPREFIX_Contents",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Contents_WriterId",
                table: "BPXPREFIX_Contents",
                column: "WriterId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_ContentTags_Tag",
                table: "BPXPREFIX_ContentTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_DeletedUsers_CDate",
                table: "BPXPREFIX_DeletedUsers",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_DeletedUsers_Email",
                table: "BPXPREFIX_DeletedUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_DeletedUsers_UserId",
                table: "BPXPREFIX_DeletedUsers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_DeletedUsers_UserName",
                table: "BPXPREFIX_DeletedUsers",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileAccessRules_CDate",
                table: "BPXPREFIX_FileAccessRules",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileAccessRules_TargetType_TargetKey",
                table: "BPXPREFIX_FileAccessRules",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileDownloaders_UserId_CDate",
                table: "BPXPREFIX_FileDownloaders",
                columns: new[] { "UserId", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileRules_CDate",
                table: "BPXPREFIX_FileRules",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileRules_Name",
                table: "BPXPREFIX_FileRules",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileRules_UDate",
                table: "BPXPREFIX_FileRules",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Files_CDate",
                table: "BPXPREFIX_Files",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Files_RuleId",
                table: "BPXPREFIX_Files",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Files_UploaderId",
                table: "BPXPREFIX_Files",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_FileTargets_TargetType_TargetKey",
                table: "BPXPREFIX_FileTargets",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupProperties_CDate",
                table: "BPXPREFIX_GroupProperties",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupProperties_UDate",
                table: "BPXPREFIX_GroupProperties",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupRoles_CDate",
                table: "BPXPREFIX_GroupRoles",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupRoles_GroupId_Active",
                table: "BPXPREFIX_GroupRoles",
                columns: new[] { "GroupId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupRoles_GroupId_Default",
                table: "BPXPREFIX_GroupRoles",
                columns: new[] { "GroupId", "Default" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupRoles_RoleId",
                table: "BPXPREFIX_GroupRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_GroupRoles_UDate",
                table: "BPXPREFIX_GroupRoles",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_CDate",
                table: "BPXPREFIX_Groups",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_Grade",
                table: "BPXPREFIX_Groups",
                column: "Grade");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_Location",
                table: "BPXPREFIX_Groups",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_ManagerId",
                table: "BPXPREFIX_Groups",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_Name",
                table: "BPXPREFIX_Groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_ParentId",
                table: "BPXPREFIX_Groups",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_UDate",
                table: "BPXPREFIX_Groups",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Groups_Visible",
                table: "BPXPREFIX_Groups",
                column: "Visible");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Languages_CDate",
                table: "BPXPREFIX_Languages",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Languages_UDate",
                table: "BPXPREFIX_Languages",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_MenuAccesses_CDate",
                table: "BPXPREFIX_MenuAccesses",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_MenuAccesses_TargetType_TargetKey",
                table: "BPXPREFIX_MenuAccesses",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Menus_CDate",
                table: "BPXPREFIX_Menus",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Menus_Enabler",
                table: "BPXPREFIX_Menus",
                column: "Enabler");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Menus_Location",
                table: "BPXPREFIX_Menus",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Menus_ParentKey",
                table: "BPXPREFIX_Menus",
                column: "ParentKey");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Menus_SortOrder",
                table: "BPXPREFIX_Menus",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Menus_UDate",
                table: "BPXPREFIX_Menus",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Policies_CDate",
                table: "BPXPREFIX_Policies",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Policies_Enabler",
                table: "BPXPREFIX_Policies",
                column: "Enabler");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Policies_Name",
                table: "BPXPREFIX_Policies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Policies_UDate",
                table: "BPXPREFIX_Policies",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Policies_ValidFrom",
                table: "BPXPREFIX_Policies",
                column: "ValidFrom");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Policies_ValidTo",
                table: "BPXPREFIX_Policies",
                column: "ValidTo");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PolicyTargets_CDate",
                table: "BPXPREFIX_PolicyTargets",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PolicyTargets_TargetType_TargetId",
                table: "BPXPREFIX_PolicyTargets",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PolicyTargets_UDate",
                table: "BPXPREFIX_PolicyTargets",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Popups_Area_Enabler_End",
                table: "BPXPREFIX_Popups",
                columns: new[] { "Area", "Enabler", "End" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Popups_CDate",
                table: "BPXPREFIX_Popups",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PopupTargets_TargetType_TargetKey",
                table: "BPXPREFIX_PopupTargets",
                columns: new[] { "TargetType", "TargetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PreFiles_FileId",
                table: "BPXPREFIX_PreFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PreFiles_OwnerType_OwnerKey",
                table: "BPXPREFIX_PreFiles",
                columns: new[] { "OwnerType", "OwnerKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_PreFiles_Registed_CDate",
                table: "BPXPREFIX_PreFiles",
                columns: new[] { "Registed", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RefreshTokens_CDate",
                table: "BPXPREFIX_RefreshTokens",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RefreshTokens_Expire",
                table: "BPXPREFIX_RefreshTokens",
                column: "Expire");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RefreshTokens_RefreshStamp",
                table: "BPXPREFIX_RefreshTokens",
                column: "RefreshStamp");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RefreshTokens_UDate",
                table: "BPXPREFIX_RefreshTokens",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RefreshTokens_UserId",
                table: "BPXPREFIX_RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RefreshTokens_UserId_Expire",
                table: "BPXPREFIX_RefreshTokens",
                columns: new[] { "UserId", "Expire" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_RoleClaims_RoleId",
                table: "BPXPREFIX_RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Roles_CDate",
                table: "BPXPREFIX_Roles",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Roles_Grade",
                table: "BPXPREFIX_Roles",
                column: "Grade");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Roles_IsSystem",
                table: "BPXPREFIX_Roles",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Roles_NormalizedName",
                table: "BPXPREFIX_Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Roles_UDate",
                table: "BPXPREFIX_Roles",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Roles_Visible",
                table: "BPXPREFIX_Roles",
                column: "Visible");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserClaims_UserId",
                table: "BPXPREFIX_UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserGroups_CDate",
                table: "BPXPREFIX_UserGroups",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserGroups_GroupId",
                table: "BPXPREFIX_UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserGroups_UDate",
                table: "BPXPREFIX_UserGroups",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserGroups_UserId_Active",
                table: "BPXPREFIX_UserGroups",
                columns: new[] { "UserId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserGroups_UserId_Default",
                table: "BPXPREFIX_UserGroups",
                columns: new[] { "UserId", "Default" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserLogins_UserId",
                table: "BPXPREFIX_UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserMessages_ReceiverId_RDate",
                table: "BPXPREFIX_UserMessages",
                columns: new[] { "ReceiverId", "RDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserMessages_ReceiverId_ReceiverDeleted_CDate",
                table: "BPXPREFIX_UserMessages",
                columns: new[] { "ReceiverId", "ReceiverDeleted", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserMessages_SenderId_SenderDeleted_CDate",
                table: "BPXPREFIX_UserMessages",
                columns: new[] { "SenderId", "SenderDeleted", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserNotices_ReceiverId_CDate",
                table: "BPXPREFIX_UserNotices",
                columns: new[] { "ReceiverId", "CDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserNotices_ReceiverId_RDate",
                table: "BPXPREFIX_UserNotices",
                columns: new[] { "ReceiverId", "RDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserProperties_CDate",
                table: "BPXPREFIX_UserProperties",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserProperties_UDate",
                table: "BPXPREFIX_UserProperties",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserRoles_CDate",
                table: "BPXPREFIX_UserRoles",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserRoles_RoleId",
                table: "BPXPREFIX_UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserRoles_UDate",
                table: "BPXPREFIX_UserRoles",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserRoles_UserId_Active",
                table: "BPXPREFIX_UserRoles",
                columns: new[] { "UserId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_UserRoles_UserId_Default",
                table: "BPXPREFIX_UserRoles",
                columns: new[] { "UserId", "Default" });

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_CDate",
                table: "BPXPREFIX_Users",
                column: "CDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_Enabler",
                table: "BPXPREFIX_Users",
                column: "Enabler");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_IsSystem",
                table: "BPXPREFIX_Users",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_LockoutEnd",
                table: "BPXPREFIX_Users",
                column: "LockoutEnd");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_NormalizedEmail",
                table: "BPXPREFIX_Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_NormalizedUserName",
                table: "BPXPREFIX_Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_UDate",
                table: "BPXPREFIX_Users",
                column: "UDate");

            migrationBuilder.CreateIndex(
                name: "IX_BPXPREFIX_Users_Visible",
                table: "BPXPREFIX_Users",
                column: "Visible");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BPXPREFIX_ApiTokens");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_CommentLikers");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Configs");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentAccessRules");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentApprovers");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentFiles");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentHistories");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentLikers");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentProperties");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentReaders");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentReadRules");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentReports");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentRuleDpNames");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentRuleProperties");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentTags");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentTranslations");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_DeletedUsers");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_FileAccessRules");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_FileDownloaders");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_FileTargets");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_GroupDpNames");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_GroupProperties");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_GroupRoles");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Languages");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_MenuAccesses");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_MenuDpNames");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_PolicyTargets");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_PopupTargets");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_PreFiles");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_RefreshTokens");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_RoleClaims");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_RoleDpNames");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_SectionTypes");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserClaims");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserDpNames");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserGroups");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserLogins");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserMessages");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserNotices");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserProperties");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserRoles");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_UserTokens");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_WidgetTypes");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Comments");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Menus");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Policies");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Popups");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Files");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Groups");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Roles");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Contents");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_ContentRules");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_Users");

            migrationBuilder.DropTable(
                name: "BPXPREFIX_FileRules");
        }
    }
}
