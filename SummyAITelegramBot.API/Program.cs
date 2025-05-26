using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Common;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
{
    Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();
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

    builder.Host.UseSerilog();
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
        KnownNetworks = { }, // ��������� ��� ����
        KnownProxies = { }   // ��������� ���� ������
    });

    app.UseHttpsRedirection();

    app.UseAuthorization();
    app.UseSerilogRequestLogging();

    app.MapControllers();

    app.Run();
}