using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.CommandHandlers;
using SummyAITelegramBot.Core.Bot.Features.Settings;
using SummyAITelegramBot.Core.Factories;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Infrastructure.Repository;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Infrastructure.Context;

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

    builder.Host.UseSerilog();

    builder.Services.AddSingleton(_ => Log.Logger);
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(builder.Configuration["Token"]));

    builder.Services.Scan(scan => scan
        .FromAssemblyOf<ICommandHandler>()
        .AddClasses(classes => classes.AssignableTo<ICommandHandler>())
        .AsSelfWithInterfaces()
        .WithScopedLifetime());

    builder.Services.Scan(scan => scan
        .FromAssemblyOf<ICallbackHandler>()
        .AddClasses(c => c.AssignableTo<ICallbackHandler>())
        .AsImplementedInterfaces()
        .WithScopedLifetime());

    builder.Services.AddSingleton<ICommandFactory, CommandFactory>();
    builder.Services.AddSingleton<ICallbackFactory, CallbackFactory>();

    builder.Services.AddSingleton<SettingsChainOfStepsHandler>();

    builder.Services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<ICommandHandler, SettingsCommandHandler>();
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
    app.UseSerilogRequestLogging();

    app.MapControllers();

    app.Run();
}