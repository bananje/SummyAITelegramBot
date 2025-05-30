using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using Telegram.Bot.Types;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.User.Services;

public class UserService(IRepository<long, UserEn> repository) : IUserService
{
    public async Task GetUserInfoFromTelegramAsync(Message message)
    {
        var from = message?.From;
        var contact = message?.Contact;
        var location = message?.Location;

        var user = await repository.GetByIdAsync(from!.Id);

        if (user is null)
        {
            user = new UserEn();
        }

        user.TelegramId = from?.Id ?? 0;
        user.FirstName = from?.FirstName;
        user.LastName = from?.LastName;
        user.Username = from?.Username;
        user.LanguageCode = from?.LanguageCode;
        user.IsPremium = from?.IsPremium;
        user.IsBot = from?.IsBot ?? false;
        user.AddedToAttachmentMenu = from?.AddedToAttachmentMenu;
        user.PhoneNumber = contact?.PhoneNumber;
        user.Latitude = (float)location?.Latitude;
        user.Longitude = (float)location?.Longitude;
        user.LastInteractionAt = DateTime.UtcNow;

        await repository.UpdateAsync(user);
    }
}