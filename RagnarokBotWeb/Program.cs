using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Quartz;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Application.Mapping;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Configuration;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.FTP;
using RagnarokBotWeb.Infrastructure.Repositories;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using RagnarokBotWeb.Middlewares;
using Serilog;

namespace RagnarokBotWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // ensure this works even without config
                .CreateBootstrapLogger(); // <== temporary early logger

            Log.Information("RagnarokBotWeb is starting up...");
            var builder = WebApplication.CreateBuilder(args);

            // This MUST come right after builder is created
            builder.Host.UseSerilog((ctx, services, loggerConfig) => loggerConfig
                .WriteTo.Console()
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
            );

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            builder.Configuration
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", true)
                .AddEnvironmentVariables();

            var DefaultCorsPolicy = "_defaultCorsPolicy";

            Environment.SetEnvironmentVariable("jwt_secret", "p2tfCQNn6FJrM7XmdAsW5zKc4DHyYbELwuPV93BRv8xeqkSjZaVhN64mSPatj9H5FqfU2rCTEWvpskKQy3eZwLGXnb8RudD7zBYMwRJXr2b6tsQZWNLUDV4C8nmpKyc7fagGqh5MFux39kASvPEdBzZd7wKDnsq8j9WTHaGmbAkeYN4RPJrEp3UXS5LCvQy6hzxBVMcFsDh3SGNHjf7qARkxzMe2VpyPncmbvJCKTX4ruZWtB86dLQ9YF5");

            builder.Services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
            });
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            builder.Services.AddAuthenticationModule();
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            });
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                            },
                        new List<string>()
                    }
                });
            });

            builder.Services.AddDbContextFactory<AppDbContext>();
            builder.Services.AddDbContext<AppDbContext>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSingleton(_ =>
            {
                var config = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds |
                                     GatewayIntents.GuildMessages |
                                     GatewayIntents.MessageContent |
                                     GatewayIntents.AllUnprivileged |
                                     GatewayIntents.GuildMembers
                };
                return new DiscordSocketClient(config);
            });

            builder.Services.AddHostedService<LoadServerTaskService>();
            builder.Services.AddHostedService<LoadFtpTaskService>();
            builder.Services.AddHostedService<LoadRaidTimesHostedService>();
            builder.Services.AddHostedService<LoadSquadsHostedService>();

            builder.Services.AddHostedService<DiscordBotService>();
            builder.Services.AddHostedService<DiscordEventService>();

            builder.Services.AddSingleton<IMessageEventHandlerFactory, MessageEventHandlerFactory>();
            builder.Services.AddSingleton<IInteractionEventHandlerFactory, InteractionEventHandlerFactory>();


            builder.Services.Configure<AppSettings>(options =>
            {
                var section = builder.Configuration.GetSection(nameof(AppSettings));
                section.Bind(options);
            });

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
            builder.Services.AddScoped<ITenantRepository, TenantRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IItemRepository, ItemRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPackRepository, PackRepository>();
            builder.Services.AddScoped<IPackItemRepository, PackItemRepository>();
            builder.Services.AddScoped<IChannelTemplateRepository, ChannelTemplateRepository>();
            builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
            builder.Services.AddScoped<IGuildRepository, GuildRepository>();
            builder.Services.AddScoped<IScumServerRepository, ScumServerRepository>();
            builder.Services.AddScoped<IPlayerRegisterRepository, PlayerRegisterRepository>();
            builder.Services.AddScoped<IReaderRepository, ReaderRepository>();
            builder.Services.AddScoped<IReaderPointerRepository, ReaderPointerRepository>();
            builder.Services.AddScoped<IBunkerRepository, BunkerRepository>();
            builder.Services.AddScoped<IWarzoneRepository, WarzoneRepository>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<ILockpickService, LockpickService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IBunkerService, BunkerService>();
            builder.Services.AddScoped<IItemService, ItemService>();
            builder.Services.AddScoped<IPackService, PackService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IDiscordService, DiscordService>();
            builder.Services.AddScoped<IBotService, BotService>();
            builder.Services.AddScoped<IChannelTemplateService, ChannelTemplateService>();
            builder.Services.AddScoped<IChannelService, ChannelService>();
            builder.Services.AddScoped<IGuildService, GuildService>();
            builder.Services.AddScoped<IServerService, ServerService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IPlayerRegisterService, PlayerRegisterService>();
            builder.Services.AddScoped<IReaderPointerService, ReaderPointerService>();
            builder.Services.AddScoped<IWarzoneService, WarzoneService>();

            builder.Services.AddScoped<StartupDiscordTemplate>();

            builder.Services.AddSingleton<IFtpService, FtpService>();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddSingleton<FtpConnectionPool>();
            builder.Services.AddSingleton<DiscordChannelPublisher>();

            builder.Services.AddScoped<ITokenIssuer, TokenIssuer>();

            builder.Services.AddHealthChecks();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: DefaultCorsPolicy,
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
            });

            builder.Services.AddMvc();
            var app = builder.Build();

            app.UseCors(DefaultCorsPolicy);

            app.UseMiddleware<ExceptionMiddleware>();

            // Serve Angular static files
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapHealthChecks("/healthz");

            // Apply migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.MigrateDatabase();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseAuthentication();

            app.MapControllers();

            // Redirect to Angular for non-API requests
            app.MapFallbackToFile("index.html");

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "cdn-storage")),
                RequestPath = "/images",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
                }
            });

            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
