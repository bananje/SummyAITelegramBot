using UserEn = SummyAITelegramBot.Core.Domain.Models.User;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Features.User.Abstractions;

public interface IUserService
{
    /// <summary>
    /// Обновить информацию о пользователе
    /// </summary>
    /// <returns></returns>
    Task<UserEn> UpdateOrCreateUserByTelegramAsync(long userId, Message message);
}
