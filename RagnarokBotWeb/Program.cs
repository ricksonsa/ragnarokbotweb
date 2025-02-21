using RagnarokBotWeb.Application.Discord;
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

            builder.Services.AddHostedService<LoginHostedService>();
            builder.Services.AddHostedService<GameplayHostedService>();
            builder.Services.AddHostedService<EconomyHostedService>();
            builder.Services.AddHostedService<SeedDataHostedService>();
            builder.Services.AddHostedService<KillHostedService>();
            builder.Services.AddHostedService<OrderCommandHostedService>();
            builder.Services.AddHostedService<ListPlayersHostedService>();
            builder.Services.AddHostedService<DiscordBotService>();

            builder.Services.AddSingleton<IMessageEventHandlerFactory, MessageEventHandlerFactory>();

            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Services.Configure<AppSettings>(options => builder.Configuration.GetSection(nameof(AppSettings)).Bind(options));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IBotRepository, BotRepository>();
            builder.Services.AddScoped<IItemRepository, ItemRepository>();
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
