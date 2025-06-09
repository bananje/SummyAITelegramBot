using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace SummyAITelegramBot.API.ExceptionHandlers;

public class GlobalExceptionHandler(
    IHostEnvironment env, 
    Serilog.ILogger logger,
    ITelegramBotClient bot)
    : IExceptionHandler
{
    private const string UnhandledExceptionMsg = "An unhandled exception has occurred while executing the request.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        //If your logger logs "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", you should remove the string below to avoid the exception being logged twice.
        logger.Error(exception,  exception.Message);

        var problemDetails = CreateProblemDetails(context, exception);
        var json = ToJson(problemDetails);

        const string contentType = "application/problem+json";
        context.Response.ContentType = contentType;
        await context.Response.WriteAsync(json, cancellationToken);

        var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "/start" },
            });

        var chatId = context.Items["chatId"] as string;

        await bot.SendMessage(chatId, "Кажется, я сломалась! Перезапусти меня через /start.", replyMarkup: keyboard);

        return true;
    }

    private ProblemDetails CreateProblemDetails(in HttpContext context, in Exception exception)
    {
        var statusCode = context.Response.StatusCode;
        var reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);
        if (string.IsNullOrEmpty(reasonPhrase))
        {
            reasonPhrase = UnhandledExceptionMsg;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = reasonPhrase,
        };

        problemDetails.Detail = exception.ToString();
        problemDetails.Extensions["traceId"] = Activity.Current?.Id;
        problemDetails.Extensions["requestId"] = context.TraceIdentifier;
        problemDetails.Extensions["data"] = exception.Data;

        return problemDetails;
    }

    private string ToJson(in ProblemDetails problemDetails)
    {
        try
        {
            return JsonSerializer.Serialize(problemDetails, SerializerOptions);
        }
        catch (Exception ex)
        {
            const string msg = "An exception has occurred while serializing error to JSON";
            logger.Error(ex, msg);
        }

        return string.Empty;
    }
}