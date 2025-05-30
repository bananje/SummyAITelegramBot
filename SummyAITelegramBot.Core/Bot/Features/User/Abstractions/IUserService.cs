using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Features.User.Abstractions;

public interface IUserService
{
    /// <summary>
    /// Сбор информации о пользователе с доступных источников
    /// </summary>
    /// <returns></returns>
    Task GetUserInfoFromTelegramAsync(Message update);
}
