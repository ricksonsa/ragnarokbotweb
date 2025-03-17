using Microsoft.EntityFrameworkCore.Migrations;

namespace RagnarokBotWeb.Infrastructure.Seed
{
    public static class Seeder
    {
        public static void Seed(MigrationBuilder migrationBuilder)
        {
            var sqlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sql", "items.sql");
            if (File.Exists(sqlFilePath))
            {
                string sqlScript = File.ReadAllText(sqlFilePath);
                migrationBuilder.Sql(sqlScript);
            }

            sqlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sql", "bunkers.sql");
            if (File.Exists(sqlFilePath))
            {
                string sqlScript = File.ReadAllText(sqlFilePath);
                migrationBuilder.Sql(sqlScript);
            }
        }
    }
}
