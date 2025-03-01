using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppDbContext>();
            
            builder.Services.AddSingleton<DiscordSocketClient>(_ =>
            {
                var config = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds | 
                                     GatewayIntents.GuildMessages |
                                     GatewayIntents.MessageContent
                };
                return new DiscordSocketClient(config);
            });

            builder.Services.AddHostedService<LoginHostedService>();
            builder.Services.AddHostedService<GameplayHostedService>();
            builder.Services.AddHostedService<EconomyHostedService>();
            builder.Services.AddHostedService<SeedDataHostedService>();
            builder.Services.AddHostedService<KillHostedService>();
            builder.Services.AddHostedService<OrderCommandHostedService>();
            builder.Services.AddHostedService<ListPlayersHostedService>();
            builder.Services.AddHostedService<DiscordBotService>();
            // uncomment this to run the template creation test
            // builder.Services.AddHostedService<TestDiscordTemplate>();

            builder.Services.AddSingleton<IMessageEventHandlerFactory, MessageEventHandlerFactory>();

            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Services.Configure<AppSettings>(options => builder.Configuration.GetSection(nameof(AppSettings)).Bind(options));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IBotRepository, BotRepository>();
            builder.Services.AddScoped<IItemRepository, ItemRepository>();
            builder.Services.AddScoped<IPackRepository, PackRepository>();
            builder.Services.AddScoped<IPackItemRepository, PackItemRepository>();
            builder.Services.AddScoped<IChannelTemplateRepository, ChannelTemplateRepository>();
            builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
            builder.Services.AddScoped<IGuildRepository, GuildRepository>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ILockpickService, LockpickService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IBunkerService, BunkerService>();
            builder.Services.AddScoped<IItemService, ItemService>();
            builder.Services.AddScoped<IPackService, PackService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IBotService, BotService>();
            builder.Services.AddScoped<IChannelTemplateService, ChannelTemplateService>();
            builder.Services.AddScoped<IChannelService, ChannelService>();
            builder.Services.AddScoped<IGuildService, GuildService>();
            
            builder.Services.AddScoped<StartupDiscordTemplate>();

            builder.Services.AddSingleton<IFtpService, FtpService>();
            builder.Services.AddSingleton<ICacheService, CacheService>();

            builder.Services.AddHealthChecks();

            builder.Services.AddMvc();

            var app = builder.Build();

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

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
