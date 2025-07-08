using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Infrastructure.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ChannelUserSettings> UserSettings => Set<ChannelUserSettings>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ChannelPost> ChannelPosts => Set<ChannelPost>();

    public DbSet<SentUserPost> SentUserPosts => Set<SentUserPost>();

    public DbSet<Channel> Channels => Set<Channel>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<DelayedUserPost> DelayedUserPosts => Set<DelayedUserPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelPost>()
            .HasKey(cp => new { cp.ChannelId, cp.Id });

        modelBuilder.Entity<User>()
            .HasOne(u => u.Subscription)
            .WithOne(s => s.User)
            .HasForeignKey<Subscription>(s => s.UserId);

        modelBuilder.Entity<SentUserPost>()
            .HasOne(s => s.ChannelPost)
            .WithOne() // если в ChannelPost нет навигационного свойства на SentUserPost
            .HasForeignKey<SentUserPost>(s => new { s.ChannelId, s.ChannelPostId })
            .HasPrincipalKey<ChannelPost>(cp => new { cp.ChannelId, cp.Id });

        base.OnModelCreating(modelBuilder);
    }
}