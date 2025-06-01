using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Infrastructure.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<User> Users => Set<User>();
}