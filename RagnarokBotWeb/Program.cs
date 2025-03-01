using Quartz;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Configuration;
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

            builder.Services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
            });
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
            builder.Services.AddTransient<CustomJob>();

            builder.Services.AddAuthenticationModule();

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            });
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppDbContext>();

            builder.Services.AddHostedService<LoginHostedService>();
            builder.Services.AddHostedService<GameplayHostedService>();
            builder.Services.AddHostedService<EconomyHostedService>();
            builder.Services.AddHostedService<SeedDataHostedService>();
            builder.Services.AddHostedService<KillHostedService>();
            builder.Services.AddHostedService<OrderCommandHostedService>();
            builder.Services.AddHostedService<ListPlayersHostedService>();
            builder.Services.AddHostedService<BotAliveHostedService>();

            builder.Services.AddHostedService<DiscordBotService>();

            builder.Services.AddSingleton<IMessageEventHandlerFactory, MessageEventHandlerFactory>();

            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Services.Configure<AppSettings>(options => builder.Configuration.GetSection(nameof(AppSettings)).Bind(options));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
            builder.Services.AddScoped<ITenantRepository, TenantRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IBotRepository, BotRepository>();
            builder.Services.AddScoped<IItemRepository, ItemRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPackRepository, PackRepository>();
            builder.Services.AddScoped<IPackItemRepository, PackItemRepository>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ILockpickService, LockpickService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IBunkerService, BunkerService>();
            builder.Services.AddScoped<IItemService, ItemService>();
            builder.Services.AddScoped<IPackService, PackService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IBotService, BotService>();

            builder.Services.AddSingleton<IFtpService, FtpService>();
            builder.Services.AddSingleton<ICacheService, CacheService>();

            builder.Services.AddScoped<ITokenIssuer, TokenIssuer>();

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
            app.UseAuthentication();

            app.MapControllers();

            app.Run();
        }
    }
}
