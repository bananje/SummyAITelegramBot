﻿namespace SummyAITelegramBot.Core.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TelegramUpdateHandlerAttribute : Attribute
{
    public string Prefix { get; }

    public TelegramUpdateHandlerAttribute(string prefix)
    {
        Prefix = prefix;
    }
}