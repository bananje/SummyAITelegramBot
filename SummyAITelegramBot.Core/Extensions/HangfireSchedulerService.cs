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

    public void ScheduleJobTwiceDaily()
    {
        var moscowTz = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        var nowUtc = DateTimeOffset.UtcNow;

        var timesInMoscow = new[]
        {
            new TimeSpan(10, 0, 0), // 10:00 МСК
            new TimeSpan(18, 0, 0)  // 18:00 МСК
        };

        foreach (var targetTime in timesInMoscow)
        {
            var todayInMoscow = TimeZoneInfo.ConvertTime(nowUtc, moscowTz).Date;
            var scheduledMoscowTime = todayInMoscow + targetTime;
            var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledMoscowTime, moscowTz);

            // Если время уже прошло — перенести на следующий день
            if (scheduledUtc <= nowUtc)
                scheduledUtc = scheduledUtc.AddDays(1);

            // Ставим две задачи на каждый интервал
            _jobClient.Schedule<TelegramSenderService>(
                service => service.SendSubscriptionOffersToEligibleUsersAsync(),
                scheduledUtc
            );
        }
    }
}