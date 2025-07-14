using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagnarokBotWeb.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    BlockDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryName = table.Column<string>(type: "text", nullable: true),
                    ChannelType = table.Column<string>(type: "text", nullable: false),
                    Admin = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ftps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<long>(type: "bigint", nullable: false),
                    RootFolder = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ftps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordName = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DiscordLink = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ButtonTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Command = table.Column<string>(type: "text", nullable: false),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ButtonTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ButtonTemplates_ChannelTemplates_ChannelTemplateId",
                        column: x => x.ChannelTemplateId,
                        principalTable: "ChannelTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelType = table.Column<string>(type: "text", nullable: false),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScumServers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: true),
                    FtpId = table.Column<long>(type: "bigint", nullable: true),
                    RestartTimes = table.Column<string>(type: "text", nullable: true),
                    TimeZoneId = table.Column<string>(type: "text", nullable: true),
                    UseKillFeed = table.Column<bool>(type: "boolean", nullable: false),
                    ShowKillDistance = table.Column<bool>(type: "boolean", nullable: false),
                    ShowKillSector = table.Column<bool>(type: "boolean", nullable: false),
                    ShowKillWeapon = table.Column<bool>(type: "boolean", nullable: false),
                    HideKillerName = table.Column<bool>(type: "boolean", nullable: false),
                    HideMineKill = table.Column<bool>(type: "boolean", nullable: false),
                    ShowSameSquadKill = table.Column<bool>(type: "boolean", nullable: false),
                    UseLockpickFeed = table.Column<bool>(type: "boolean", nullable: false),
                    ShowLockpickSector = table.Column<bool>(type: "boolean", nullable: false),
                    ShowLockpickContainerName = table.Column<bool>(type: "boolean", nullable: false),
                    SendVipLockpickAlert = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScumServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScumServers_Ftps_FtpId",
                        column: x => x.FtpId,
                        principalTable: "Ftps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScumServers_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScumServers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Buttons",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Command = table.Column<string>(type: "text", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buttons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buttons_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    LastInteracted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bots_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bunkers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sector = table.Column<string>(type: "text", nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    Available = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bunkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bunkers_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KillerSteamId64 = table.Column<string>(type: "text", nullable: true),
                    TargetSteamId64 = table.Column<string>(type: "text", nullable: true),
                    KillerName = table.Column<string>(type: "text", nullable: true),
                    TargetName = table.Column<string>(type: "text", nullable: true),
                    Weapon = table.Column<string>(type: "text", nullable: true),
                    Distance = table.Column<float>(type: "real", nullable: true),
                    Sector = table.Column<string>(type: "text", nullable: true),
                    KillerX = table.Column<float>(type: "real", nullable: false),
                    KillerY = table.Column<float>(type: "real", nullable: false),
                    KillerZ = table.Column<float>(type: "real", nullable: false),
                    VictimX = table.Column<float>(type: "real", nullable: false),
                    VictimY = table.Column<float>(type: "real", nullable: false),
                    VictimZ = table.Column<float>(type: "real", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kills_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lockpicks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LockType = table.Column<string>(type: "text", nullable: false),
                    SteamId64 = table.Column<string>(type: "text", nullable: false),
                    ScumId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lockpicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lockpicks_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Packs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DeliveryText = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    VipPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Commands = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DiscordChannelId = table.Column<string>(type: "text", nullable: true),
                    PurchaseCooldownSeconds = table.Column<long>(type: "bigint", nullable: true),
                    StockPerPlayer = table.Column<long>(type: "bigint", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlockPurchaseRaidTime = table.Column<bool>(type: "boolean", nullable: false),
                    IsVipOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packs_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerRegisters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WelcomePackId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerRegisters_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ScumId = table.Column<string>(type: "text", nullable: true),
                    SteamId64 = table.Column<string>(type: "text", nullable: true),
                    SteamName = table.Column<string>(type: "text", nullable: true),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    DiscordName = table.Column<string>(type: "text", nullable: true),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    HideAdminCommands = table.Column<bool>(type: "boolean", nullable: false),
                    Money = table.Column<long>(type: "bigint", nullable: true),
                    Gold = table.Column<long>(type: "bigint", nullable: true),
                    Fame = table.Column<long>(type: "bigint", nullable: true),
                    X = table.Column<float>(type: "real", nullable: true),
                    Y = table.Column<float>(type: "real", nullable: true),
                    Z = table.Column<float>(type: "real", nullable: true),
                    Coin = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReaderPointers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    FileDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderPointers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReaderPointers_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Readings_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CronExpression = table.Column<string>(type: "text", nullable: false),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledTasks_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "text", nullable: false),
                    BotId = table.Column<long>(type: "bigint", nullable: true),
                    Executed = table.Column<bool>(type: "boolean", nullable: false),
                    ExecuteDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commands_Bots_BotId",
                        column: x => x.BotId,
                        principalTable: "Bots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    PackId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    AmmoCount = table.Column<int>(type: "integer", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackItems_Packs_PackId",
                        column: x => x.PackId,
                        principalTable: "Packs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: true),
                    ScumServerId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Packs_PackId",
                        column: x => x.PackId,
                        principalTable: "Packs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_ScumServers_ScumServerId",
                        column: x => x.ScumServerId,
                        principalTable: "ScumServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Players_UserId",
                        column: x => x.UserId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bots_ScumServerId",
                table: "Bots",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bunkers_ScumServerId",
                table: "Bunkers",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Buttons_ChannelId",
                table: "Buttons",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ButtonTemplates_ChannelTemplateId",
                table: "ButtonTemplates",
                column: "ChannelTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_ChannelType",
                table: "Channels",
                column: "ChannelType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_DiscordId",
                table: "Channels",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_GuildId",
                table: "Channels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelTemplates_ChannelType",
                table: "ChannelTemplates",
                column: "ChannelType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commands_BotId",
                table: "Commands",
                column: "BotId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_DiscordId",
                table: "Guilds",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kills_ScumServerId",
                table: "Kills",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Lockpicks_ScumServerId",
                table: "Lockpicks",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PackId",
                table: "Orders",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlayerId",
                table: "Orders",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ScumServerId",
                table: "Orders",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PackItems_ItemId",
                table: "PackItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PackItems_PackId",
                table: "PackItems",
                column: "PackId");

            migrationBuilder.CreateIndex(
                name: "IX_Packs_ScumServerId",
                table: "Packs",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRegisters_ScumServerId",
                table: "PlayerRegisters",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ScumServerId",
                table: "Players",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderPointers_ScumServerId",
                table: "ReaderPointers",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_ScumServerId",
                table: "Readings",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_ScumServerId",
                table: "ScheduledTasks",
                column: "ScumServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScumServers_FtpId",
                table: "ScumServers",
                column: "FtpId");

            migrationBuilder.CreateIndex(
                name: "IX_ScumServers_GuildId",
                table: "ScumServers",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScumServers_TenantId",
                table: "ScumServers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Bunkers");

            migrationBuilder.DropTable(
                name: "Buttons");

            migrationBuilder.DropTable(
                name: "ButtonTemplates");

            migrationBuilder.DropTable(
                name: "Commands");

            migrationBuilder.DropTable(
                name: "Kills");

            migrationBuilder.DropTable(
                name: "Lockpicks");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "PackItems");

            migrationBuilder.DropTable(
                name: "PlayerRegisters");

            migrationBuilder.DropTable(
                name: "ReaderPointers");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "ScheduledTasks");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "ChannelTemplates");

            migrationBuilder.DropTable(
                name: "Bots");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Packs");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "ScumServers");

            migrationBuilder.DropTable(
                name: "Ftps");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
