using Microsoft.AspNetCore.Mvc;
using SummyAITelegramBot.Core.Bot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TelegramWebHookController(
    ICommandFactory commandFactory, 
    ITelegramBotClient botClient,
    ICallbackFactory callbackFactory) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {       
        return Ok(DateTime.UtcNow);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text is { } messageText)
        {
            await commandFactory.ProcessCommandAsync(messageText, update.Message);
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            await callbackFactory.DispatchAsync(update.CallbackQuery!);
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