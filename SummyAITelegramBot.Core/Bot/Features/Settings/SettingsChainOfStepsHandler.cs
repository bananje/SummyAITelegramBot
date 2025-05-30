using Telegram.Bot.Types;
using Telegram.Bot;
using System.Collections.Concurrent;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace SummyAITelegramBot.Core.Bot.Features.Settings;

[CallbackHandler("settings")]
public class SettingsChainOfStepsHandler(
    ITelegramBotClient bot,
    IServiceProvider serviceProvider) : ICallbackHandler
{
    private readonly ConcurrentDictionary<long, IChainOfStepsHandler<UserSettings>> _activeChains = new();

    public async Task HandleAsync(CallbackQuery query)
    {
        using var scope = serviceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IRepository<long, UserSettings>>();

        var chatId = query.Message.Chat.Id;

        if (!_activeChains.TryGetValue(chatId, out var handler))
            return;

        var userSettings = await repository.GetByIdAsync(query.From.Id);

        await handler.HandleAsync(bot, query, userSettings);

        if (handler.Next != null)
        {
            _activeChains[chatId] = handler.Next;
        }
        else
        {
            _activeChains.TryRemove(chatId, out _);


        }
    }

    public async Task StartChainAsync(long chatId)
    {
        var settings = new UserSettings();

        // Создаём цепочку шагов
        var lang = new LanguageSettingsStepHandler();
        var notify = new NotificationsSettingsHandler();

        lang.Next = notify;

        _activeChains[chatId] = lang;

        await lang.ShowStepAsync(bot, chatId);
    }
}