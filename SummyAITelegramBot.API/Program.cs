using Microsoft.AspNetCore.HttpOverrides;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Common;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
{ 
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(builder.Configuration["Token"]));

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

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
        KnownNetworks = { }, // разрешить все сети
        KnownProxies = { }   // разрешить всех прокси
    });

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}