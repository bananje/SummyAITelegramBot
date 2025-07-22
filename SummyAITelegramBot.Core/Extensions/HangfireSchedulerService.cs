using Cronos;
using Hangfire;
using SummyAITelegramBot.Core.Bot.Features.Channel.Services;
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
        var targetDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var nextRun = cron.GetNextOccurrence(targetDate, tz); 

        if (nextRun.HasValue)
        {
            var localTime = nextRun.Value;

            _jobClient.Schedule<CleanerService>(
                service => service.CleanupOldSentPostsAsync(),
                localTime
            );

            _jobClient.Schedule<CleanerService>(
                service => service.CleanupMediaCacheAsync(),
                localTime
            );
        }
    }

    public void ScheduleJobTwiceDaily()
    {
        var moscowTz = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        var targetDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var nowMoscow = TimeZoneInfo.ConvertTime(targetDate, moscowTz);

        var timesInMoscow = new[]
        {
        new TimeSpan(9, 0, 0),
        new TimeSpan(13, 0, 0)
    };

        foreach (var targetTime in timesInMoscow)
        {
            var scheduledTime = nowMoscow.Date + targetTime;

            if (scheduledTime <= nowMoscow)
                scheduledTime = scheduledTime.AddDays(1);

            // Тут scheduledTime имеет Kind=Unspecified, что нужно PostgreSQL
            _jobClient.Schedule<TelegramSenderService>(
                service => service.SendSubscriptionOffersToEligibleUsersAsync(),
                scheduledTime
            );
        }
    }
}