using Microsoft.AspNetCore.Mvc;
using SummyAITelegramBot.Core.Abstractions;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TelegramWebHookController(ICommandFactory commandFactory) : ControllerBase
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
}