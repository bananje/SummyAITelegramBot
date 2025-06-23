using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Infrastructure.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ChannelPost> ChannelPosts => Set<ChannelPost>();

    public DbSet<Channel> Channels => Set<Channel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelPost>()
            .HasKey(cp => new { cp.ChannelId, cp.Id });

        base.OnModelCreating(modelBuilder);
    }
}