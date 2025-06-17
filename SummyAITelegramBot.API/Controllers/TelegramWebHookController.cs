using Microsoft.AspNetCore.Mvc;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TelegramWebHookController(
    ITelegramUpdateFactory telegramUpdateFactory) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {       
        return Ok(DateTime.UtcNow);
    }

    [HttpPost("webhookп")]
    public IActionResult HandleUpdate1([FromBody] Update update)
    { return Ok(); }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        if (update.Type == UpdateType.Message 
                && update.Message?.Text is { } commandPrefix
                && update.Message!.ReplyToMessage is null)
        {
            HttpContext.Items["chatId"] = update.Message.Chat.Id;
            
            await telegramUpdateFactory.DispatchAsync(update.Message!, commandPrefix);
            return Ok();
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            HttpContext.Items["chatId"] = update.CallbackQuery.Message.Chat.Id;
            if (update.CallbackQuery!.Data!.StartsWith("/"))
            {
                var callBack = update.CallbackQuery!;

                await telegramUpdateFactory.DispatchAsync(
                    callBack.Message,
                    callBack.Message.Text
                        .TrimStart('/')
                        .ToLowerInvariant());

                return Ok();
            }

            await telegramUpdateFactory.DispatchAsync(
                update.CallbackQuery.Message!,
                update.CallbackQuery.Data?.Split(':').FirstOrDefault());

            return Ok();
        }

        // обработка ответа с ссылкой на канал (частный случай)
        if (update.Type == UpdateType.Message
            && TelegramHelper.IsTelegramChannelLink(update.Message?.Text))
        {
            HttpContext.Items["chatId"] = update.Message.Chat.Id;
            await telegramUpdateFactory.DispatchAsync(update.Message, "add");

            return Ok();
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
}