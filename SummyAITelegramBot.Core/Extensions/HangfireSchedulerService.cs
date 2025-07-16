using Cronos;
using Hangfire;
using SummyAITelegramBot.Core.Bot.Utils;

namespace SummyAITelegramBot.Core.Extensions;

public class HangfireSchedulerService
{
    private readonly IBackgroundJobClient _jobClient;

    public HangfireSchedulerService(IBackgroundJobClient jobClient)
    {
        _jobClient = jobClient;
    }

    public void ScheduleCleanupJob()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        var cron = CronExpression.Parse("0 2 * * *");
        var nextRun = cron.GetNextOccurrence(DateTimeOffset.UtcNow, tz);

        if (nextRun.HasValue)
        {
            _jobClient.Schedule<CleanerService>(
                service => service.CleanupOldSentPostsAsync(),
                nextRun.Value.UtcDateTime
            );

            _jobClient.Schedule<CleanerService>(
                service => service.CleanupMediaCacheAsync(),
                nextRun.Value.UtcDateTime
            );
        }
    }
}