using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot.Types;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.User.Services;

public class UserService(IUnitOfWork unitOfWork) : IUserService
{
    public async Task<UserEn> UpdateOrCreateUserByTelegramAsync(Update update)
    {
        var from = TelegramHelper.GetUserFromUpdate(update);
        if (from == null || from.IsBot)
            throw new InvalidOperationException("Невозможно извлечь пользователя или пользователь является ботом");

        var userId = from.Id;

        var repository = unitOfWork.Repository<long, UserEn>();

        var message = update.Message ?? update.CallbackQuery?.Message;

        var contact = message?.Contact;
        var location = message?.Location;

        var user = await repository.GetByIdAsync(userId);
        if (user is null)
        {
            user = new UserEn();
        }

        user.Id = userId;
        user.TelegramId = userId;
        user.FirstName = from.FirstName;
        user.LastName = from.LastName;
        user.Username = from.Username;
        user.LanguageCode = from.LanguageCode;
        user.HasTgPremium = from.IsPremium;
        user.IsBot = from.IsBot;
        user.AddedToAttachmentMenu = from.AddedToAttachmentMenu;
        user.PhoneNumber = contact?.PhoneNumber;
        //user.Latitude = (float?)location?.Latitude;
        //user.Longitude = (float?)location?.Longitude;
        user.LastInteractionAt = DateTime.UtcNow;

        await repository.CreateOrUpdateAsync(user);
        await unitOfWork.CommitAsync();

        return user;
    }
}