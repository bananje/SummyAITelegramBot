using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Subsciption.Abstractions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TelegramWebHookController(
    ITelegramUpdateFactory telegramUpdateFactory,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork,
    ITelegramBotClient botClient) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {       
        return Ok(DateTime.UtcNow);
    }

    [HttpPost("webhook4")]
    public IActionResult HandleUpdate1([FromBody] Update update)
    { return Ok(); }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        try
        {
            // Если пользователь новичок
            if (!await CheckUser(update))
            {
                await telegramUpdateFactory.DispatchAsync(update, "/start");

                var userInfo = TelegramHelper.GetUserAndChatId(update);

                await subscriptionService.SetTrialSubscriptionAsync(userInfo.chatId);

                return Ok();
            }

            // обработка ответа с ссылкой на канал (частный случай)
            if (update.Type == UpdateType.Message
                && TelegramHelper.IsTelegramChannelLink(update.Message?.Text))
            {
                HttpContext.Items["chatId"] = update.Message.Chat.Id;
                await telegramUpdateFactory.DispatchAsync(update, "/add");
                return Ok();
            }

            if (update.Type == UpdateType.Message
                    && update.Message?.Text is { } command
                    && update.Message!.ReplyToMessage is null)
            {
                HttpContext.Items["chatId"] = update.Message.Chat.Id;

                var parts = command.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var commandPrefix = parts[0];

                await telegramUpdateFactory.DispatchAsync(update, commandPrefix);
                return Ok();
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                HttpContext.Items["chatId"] = update.CallbackQuery.Message.Chat.Id;
                if (update.CallbackQuery!.Data!.StartsWith("/"))
                {
                    var callBack = update.CallbackQuery!;

                    var callBackPrefix = update.CallbackQuery.Data is null
                        ? callBack.Message.Text
                        : update.CallbackQuery.Data;

                    callBackPrefix = callBackPrefix != null && callBackPrefix.Contains(':')
                        ? callBackPrefix.Substring(0, callBackPrefix.IndexOf(':'))  // берём до двоеточия включительно
                        : callBackPrefix;

                    await telegramUpdateFactory.DispatchAsync(
                        update,
                        callBackPrefix);

                    return Ok();
                }

                var data = update.CallbackQuery.Data;
                var prefix = data != null && data.Contains(':')
                ? data.Substring(0, data.IndexOf(':') + 1)  // берём до двоеточия включительно
                : data;

                await telegramUpdateFactory.DispatchAsync(
                    update,
                    prefix);

                return Ok();
            }
        }
        catch (Exception ex)
        {
        }

        return Ok();
    }

    [HttpPost("set-webhook")]
    public async Task<IActionResult> SetWebhook()
    {
        var host = $"{Request.Scheme}://{Request.Host}";

        try
        {
            var webhookUrl = $"{host}/api/telegramwebhook/webhook";

            await botClient.SetWebhook(webhookUrl);

            return Ok(new { webhook = webhookUrl });
        }
        catch (Exception EX)
        {
            return BadRequest(EX.Message + host);
        }
    }

    private async Task<bool> CheckUser(Update update)
    {
        var userInfo = TelegramHelper.GetUserAndChatId(update);

        var userId = userInfo.chatId;
        var userRepository = unitOfWork.Repository<long, Core.Domain.Models.User>();

        var user = await userRepository.GetIQueryable()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return false;
        }

        return true;
    }
}