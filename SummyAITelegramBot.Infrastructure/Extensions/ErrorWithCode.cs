using FluentResults;
using SummyAITelegramBot.Core.Enums;

namespace SummyAITelegramBot.Infrastructure.Extensions;

public class ErrorWithCode : Error
{
    public ErrorCode Code { get; }

    public ErrorWithCode(ErrorCode errorCode, string message)
        : base(message)
    {
        Code = errorCode;
        Metadata.Add("Code", errorCode);
    }
}