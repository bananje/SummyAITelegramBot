﻿using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/showsubscription")]
public class ShowSubscriptionPlansHandler(
    ITelegramBotClient bot,
    IUserCommandCache commandCache,
    IStaticImageService imageService) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository;

    public ShowSubscriptionPlansHandler(
        ITelegramBotClient bot,
        IUserCommandCache commandCache,
        IUnitOfWork unitOfWork,
        IStaticImageService imageService) : this(bot, commandCache, imageService)
    {
        _userRepository = unitOfWork.Repository<long, Domain.Models.User>();
    }

    public async Task HandleAsync(Update update)
    {
        var message = update.Message is null ? update.CallbackQuery.Message
            : update.Message;

        var chatId = message.Chat.Id;
        var user = await _userRepository.GetIQueryable()
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(user => user.Id == chatId)  
                ?? throw new Exception($"Ошибка при настройке пользователя {chatId}.");

        if (user.Subscription?.Type == Domain.Enums.SubscriptionType.UnlimitedSubscription)
        {
            await ShowMessageForUnlimitedSubscribersAsync(user, chatId, update);
            return;
        }

        if (user.Subscription?.Type == Domain.Enums.SubscriptionType.MonthSubscription)
        {
            await ShowMessageForMonthSubscribersAsync(user.Subscription, chatId, update);
            return;
        }

        if (user.Subscription?.Type == Domain.Enums.SubscriptionType.TrialSubscription)
        {
            await ShowMessageForTrialSubscribersAsync(user.Subscription, chatId, update);
            return;
        }

        var keyboardButtons = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("199р/меc", "/pay"),
                InlineKeyboardButton.WithCallbackData("1500р/навсегда", "/pay")
            }
        };

        var backCommand = commandCache.GetLastCommand(chatId) ?? "/mychannels";

        if (backCommand == "/add")
        {
            keyboardButtons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Продолжить настройку каналов", "/showchannelsettings")
            });
        }
        else
        {
            keyboardButtons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account")
            });
        }

        var text = $"""
            Нравится как работает Summy?
            Для того, чтобы добавить больше каналов, Summy советует купить подписку 💌

            <b> *Покупая подписку навсегда вы поддерживаете проект и
            Сова Summy приподнесёт небольшой подарок❤️</b>
            """;

        var stream = imageService.GetImageStream("summy_sub.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: stream,
            userMessage: update.Message,
            caption: text,
            replyMarkup: new InlineKeyboardMarkup(keyboardButtons)
        );
    }

    private async Task ShowMessageForMonthSubscribersAsync(Subscription subscription, long chatId, Update update)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("1500р/навсегда", "/PAY"),
            }
        });

        if (!subscription.HasAutoPayment)
        {
            keyboard.AddButtons(new[] { InlineKeyboardButton.WithCallbackData("Подключить автоаплатёж", $"") });
        }

        var text = $"""
            Ваше текущий план - 199р./мес
            Действует с {subscription.StartDate} по {subscription.EndDate}.
                     
            <b> *Покупая подписку навсегда вы поддерживаете проект и
            Сова Summy приподнесёт небольшой подарок❤️</b>
            """;

        var stream = imageService.GetImageStream("summy_sub.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: stream,
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    } 

    private async Task ShowMessageForUnlimitedSubscribersAsync(Domain.Models.User user, long chatId, Update update)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Каналы 📣", $""),
            }
        });

        var text = $"""
            Уважаемый, {user.FirstName}
            У вас подключен безлимитный план навсегда.

            Спасибо за поддержку Совы Summy ❤️
            """;

        var stream = imageService.GetImageStream("summy_sub.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: stream,
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }

    private async Task ShowMessageForTrialSubscribersAsync(Subscription subscription, long chatId, Update update)
    {
        var keyboardButtons = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("199р/меc", "/pay"),
                InlineKeyboardButton.WithCallbackData("1500р/навсегда", "/pay")
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account")
            }
        };

        var subTrialDays = subscription.EndDate - DateTime.UtcNow;

        var text = $"""
                Добавляйте безлимитное количество каналов ещё {subTrialDays.Days} дней

                <b> *Далее, вы можете приобрести подписку или 
                остаться на бесплатном тарифе (доступно к добавлению 3 канала)❤️</b>
            """;

        var stream = imageService.GetImageStream("summy_sub.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: stream,
            userMessage: update.Message,
            caption: text,
            replyMarkup: new InlineKeyboardMarkup(keyboardButtons)
        );
    }
}