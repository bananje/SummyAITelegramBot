using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Common;
using SummyAITelegramBot.Core.Options;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
var bot = builder.Configuration.GetSection(TelegramBot.SectionName).Get<TelegramBot>();

if (string.IsNullOrEmpty(bot?.Token))
    throw new Exception("TelegramBot:Token не настроен в appsettings.json");
{
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(bot.Token));

    builder.Services.Scan(scan => scan
        .FromAssemblyOf<IMessageHandler>()
        .AddClasses(classes => classes.AssignableTo<IMessageHandler>())
        .AsImplementedInterfaces()
        .WithTransientLifetime());

    builder.Services.AddSingleton<ICommandFactory, CommandFactory>();
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    using (var scope = app.Services.CreateScope())
    {
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        await botClient.SetWebhook($"{bot.Host}/api/webhook");
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}