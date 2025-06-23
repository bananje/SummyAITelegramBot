using FluentResults;
using SummyAITelegramBot.Core.Enums;

namespace SummyAITelegramBot.Core.Extensions;

public class ErrorWithCode : Error
{
    public ErrorCode Code { get; }

    public ErrorWithCode(ErrorCode code, string? message = null)
        : base(message)
    {
        Code = code;
        Metadata.Add("Code", code);
    }
}

public static class ResultExtensions
{
    public static Result Warn(this Result result, string warningMessage)
    {
        result.WithWarning(warningMessage);
        return result;
    }

    public static Result<T> Warn<T>(this Result<T> result, string warningMessage)
    {
        result.WithWarning(warningMessage);
        return result;
    }

    public static Result WithWarning(this Result result, string warningMessage)
    {
        result.Reasons.Add(new Error(warningMessage));
        return result;
    }

    public static Result<T> WithWarning<T>(this Result<T> result, string warningMessage)
    {
        result.Reasons.Add(new Error(warningMessage));
        return result;
    }
}

