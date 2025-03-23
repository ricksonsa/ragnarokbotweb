using Microsoft.EntityFrameworkCore.Migrations;

namespace RagnarokBotWeb.Infrastructure.Seed
{
    public static class Seeder
    {
        public static void Seed(MigrationBuilder migrationBuilder)
        {
            var sqlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sql");
            if (File.Exists(Path.Combine(sqlFilePath, "items.sql")))
            {
                string sqlScript = File.ReadAllText(Path.Combine(sqlFilePath, "items.sql"));
                migrationBuilder.Sql(sqlScript);
            }

            if (File.Exists(Path.Combine(sqlFilePath, "channel_templates.sql")))
            {
                string sqlScript = File.ReadAllText(Path.Combine(sqlFilePath, "channel_templates.sql"));
                migrationBuilder.Sql(sqlScript);
            }
        }
    }
}
