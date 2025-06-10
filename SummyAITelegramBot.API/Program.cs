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
using SummyAITelegramBot.Core.Utils;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.User.Services;
using SummyAITelegramBot.API.ExceptionHandlers;
using SummyAITelegramBot.Core.AI.AiStrategies;
using SummyAITelegramBot.Core.AI.Factories;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Services;
using System.Net.Http.Headers;
using SummyAITelegramBot.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using SummyAITelegramBot.Infrastructure.Jobs;
using System.Reflection;

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
    builder.Services.AddMemoryCache();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(builder.Configuration["Token"]));

    builder.Services.Scan(scan => scan
        .FromAssemblyOf<ICommandHandler>()
        .AddClasses(classes => classes.AssignableTo<ICommandHandler>())
        .AsSelfWithInterfaces()
        .WithScopedLifetime());

    builder.Services.Scan(scan => scan
       .FromAssemblyOf<ICallbackHandler>()
       .AddClasses(classes => classes
           .AssignableTo<ICallbackHandler>()
           .Where(t => !t.IsAbstract)) 
       .AsSelfWithInterfaces()
       .WithScopedLifetime());


    builder.Services.AddScoped<ICommandFactory, CommandFactory>();
    builder.Services.AddScoped<ICallbackFactory, CallbackFactory>();
    builder.Services.AddScoped<IStaticImageService, StaticImageService>();

    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<SettingsChainOfStepsHandler>();
    builder.Services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddScoped<ISummarizationStrategyFactory, SummarizationStrategyFactory>();
    builder.Services.AddScoped<OpenAISummarizationStrategy>();
    builder.Services.AddScoped<DeepSeekSummarizationStrategy>();
    builder.Services.Scan(scan => scan
       .FromAssemblyOf<ISummarizationStrategy>()
       .AddClasses(classes => classes
           .AssignableTo<ISummarizationStrategy>()
           .Where(t => !t.IsAbstract))
       .AsSelfWithInterfaces()
       .WithScopedLifetime());

    builder.Services.AddHttpClient();

    builder.Services.AddScoped<ITelegramChannelAdapter, TelegramChannelAdapter>();

    builder.Services.AddHttpClient("DeepSeek", client =>
    {
        client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");

        client.DefaultRequestHeaders.Add("X-Title", "SummyAI");
    });
    builder.Services.AddScoped<ICommandHandler, SettingsCommandHandler>();

    // Добавляем Hangfire + PostgreSQL
    builder.Services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseRecommendedSerializerSettings()
              .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireDb"));
    });

    builder.Services.AddHangfireServer(); // Добавляет фоновые процессы
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    });
    builder.Services.AddHostedService<ChannelMonitoringService>();

    builder.Services.AddSingleton<WTelegram.Client>(provider =>
    {      
        return new WTelegram.Client(Config); 
    });

}
static string Config(string what)
{
    return what switch
    {
        "api_id" => "28909018",                   // Твой api_id из Telegram
        "api_hash" => "e2ddd24db858eefbf3c2434b895a40cf", // Твой api_hash из Telegram
        "phone_number" => "+79183207444",       // Номер телефона (только при первом запуске) // Код из SMS/Telegram при первом входе    // Пароль, если включена двухфакторка
        _ => null
    };
}

var app = builder.Build();
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при применении миграций: {ex.Message}");
            throw;
        }
    }

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
    app.UseExceptionHandler();
    app.Run();
}