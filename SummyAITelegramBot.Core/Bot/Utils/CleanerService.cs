using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace SummyAITelegramBot.Core.Bot.Utils;

public class CleanerService(
    IUnitOfWork _unitOfWork,
    IWebHostEnvironment webHostEnvironment,
    ILogger logger)
{
    public async Task CleanupOldSentPostsAsync()
    {
        try
        {
            var delayedRepo = _unitOfWork.Repository<long, DelayedUserPost>();
            var postsRepo = _unitOfWork.Repository<int, ChannelPost>();

            var cutoffDate = DateTime.UtcNow.AddHours(-24);

            // Получаем составной ключ (PostId + ChannelId) для удаления
            var oldSentPostKeys = await delayedRepo.GetIQueryable()
                .Where(p => p.CreatedDate <= cutoffDate)
                .Select(p => new { p.Id, p.ChannelId })
                .ToListAsync();

            if (!oldSentPostKeys.Any())
            {
                logger.Information("No old sent posts to clean up.");
                return;
            }

            var postsToDelete = await postsRepo.GetIQueryable()
                .Where(cp => oldSentPostKeys
                  .Any(key => key.Id == cp.Id && key.ChannelId == cp.ChannelId))
                .Select(u => u.Id)
                .ToListAsync();

            if (postsToDelete.Any())
            {
                await postsRepo.RemoveRangeAsync(postsToDelete);
                await _unitOfWork.CommitAsync();
                logger.Information("Deleted {Count} old ChannelPosts.", postsToDelete.Count);
            }
            else
            {
                logger.Information("No matching ChannelPosts found for deletion.");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
        }      
    }

    public async Task CleanupMediaCacheAsync()
    {
        try
        {
            var cachePath = Path.Combine(webHostEnvironment.WebRootPath, "media_cache");

            if (!Directory.Exists(cachePath))
            {
                logger.Warning("Directory {CachePath} does not exist.", cachePath);
                return;
            }

            var files = Directory.GetFiles(cachePath);

            if (!files.Any())
            {
                logger.Information("No files to clean up in media_cache.");
                return;
            }

            int deleted = 0;
            foreach (var file in files)
            {
                try
                {
                    var info = new FileInfo(file);

                    // Удаляем, если файл старше 24 часов
                    if (info.CreationTimeUtc < DateTime.UtcNow.AddHours(-24))
                    {
                        info.Delete();
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to delete file: {File}", file);
                }
            }

            logger.Information("Deleted {Count} old files from media_cache.", deleted);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to clean up media_cache directory.");
        }
    }
}
