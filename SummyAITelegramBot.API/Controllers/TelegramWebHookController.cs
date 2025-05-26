using Microsoft.AspNetCore.Mvc;
using SummyAITelegramBot.Core.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TelegramWebHookController(ICommandFactory commandFactory, ITelegramBotClient botClient) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update)
    {
        if (update.Message?.Text is { } messageText)
        {
            await commandFactory.ProcessCommandAsync(messageText, update.Message);
        }
        return Ok();
    }

    [HttpPost("set-webhook")]
    public async Task<IActionResult> SetWebhook()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var webhookUrl = $"{host}/api/webhook";

        await botClient.SetWebhook(webhookUrl);

        return Ok(new { webhook = webhookUrl });
    }
}