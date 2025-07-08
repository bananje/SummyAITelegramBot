using Microsoft.EntityFrameworkCore;
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
    IStaticImageService imageService) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository;

    public ShowSubscriptionPlansHandler(
        ITelegramBotClient bot,
        IUnitOfWork unitOfWork,
        IStaticImageService imageService) : this(bot, imageService)
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

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("199р. за месяц", $"/pay"),

                InlineKeyboardButton.WithCallbackData("1500р. навсегда", $"/pay"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Продолжить настройку каналов", $"/showchannelsettings")
            }
        });
        
        var text = $"""
            Нравится как работает Summy?
            Для того, чтобы добавить больше каналов, Summy советует купить подписку 💌

            <b> *Покупая подписку навсегда вы поддерживаете проект и
            Сова Summy приподнесёт небольшой подарок❤️</b>
            """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: new InputFileStream(stream),
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }

    private async Task ShowMessageForMonthSubscribersAsync(Subscription subscription, long chatId, Update update)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("1500р. навсегда", $""),
            }
        });

        if (!subscription.HasAutoPayment)
        {
            keyboard.AddButton(InlineKeyboardButton.WithCallbackData("Подключить автоаплатёж", $""));
        }

        var text = $"""
            Ваше текущий план - 199р./мес
            Действует с {subscription.StartDate} по {subscription.EndDate}.
                     
            <b> *Покупая подписку навсегда вы поддерживаете проект и
            Сова Summy приподнесёт небольшой подарок❤️</b>
            """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: new InputFileStream(stream),
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
                InlineKeyboardButton.WithCallbackData("Каналы📣", $""),
            }
        });

        var text = $"""
            Уважаемый, {user.FirstName}
            У вас подключен безлимитный план навсегда.

            Спасибо за поддержку Совы Summy ❤️
            """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: new InputFileStream(stream),
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }
}