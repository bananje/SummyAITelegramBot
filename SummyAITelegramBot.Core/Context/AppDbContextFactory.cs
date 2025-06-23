using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace SummyAITelegramBot.Infrastructure.Context;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Здесь укажи свою строку подключения
        optionsBuilder.UseNpgsql("host=localhost;port=5432;database=SummyAIDb;username=postgres;password=09012004;CommandTimeout=30;Timeout=30");

        return new AppDbContext(optionsBuilder.Options);
    }
}
