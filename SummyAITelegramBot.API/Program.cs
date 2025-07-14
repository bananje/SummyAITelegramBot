using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Abstractions;
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
using SummyAITelegramBot.Core.Commands;
using SummyAITelegramBot.Core.Utils.Repository;
using SummyAITelegramBot.API.Jobs;
using SummyAITelegramBot.Core.Bot.Utils;
using TL;

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
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Services.AddSingleton(_ => Log.Logger);
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddMemoryCache();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddHttpContextAccessor();

    var token = builder.Configuration["Telegram:BotToken"]
        ?? throw new Exception("Bot token is not configured");
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

    builder.Services.Scan(scan => scan
       .FromAssemblyOf<ITelegramUpdateHandler>()
       .AddClasses(classes => classes
           .AssignableTo<ITelegramUpdateHandler>()
           .Where(t => !t.IsAbstract))
       .AsSelfWithInterfaces()
       .WithScopedLifetime());

    builder.Services.AddScoped<ITelegramUpdateFactory, TelegramUpdateFactory>();

    builder.Services.AddScoped<IStaticImageService, StaticImageService>();
    builder.Services.AddScoped<IMediaCacheService, TelegramMediaCacheService>();

    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddScoped<ISummarizationStrategyFactory, SummarizationStrategyFactory>();
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
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", builder.Configuration["OpenRouter:ApiToken"]);

        client.DefaultRequestHeaders.Add("X-Title", "SummyAI");
    });

    // Добавляем Hangfire + PostgreSQL
    builder.Services.AddHangfire(config =>
    {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
              .UseSimpleAssemblyNameTypeSerializer()
              .UseRecommendedSerializerSettings()
              .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    builder.Services.AddHangfireServer(); // Добавляет фоновые процессы
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(ProcessTelegramChannelPostCommandHandler).Assembly);
    });
    builder.Services.AddHostedService<ChannelMonitoringService>();
    builder.Services.AddSingleton<IUserCommandCache, UserCommandCache>();
    builder.Services.AddSingleton<WTelegram.Client>(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();

        string Config(string what) => what switch
        {
            "api_id" => configuration["Telegram:ApiId"],
            "api_hash" => configuration["Telegram:ApiHash"],
            "phone_number" => configuration["Telegram:PhoneNumber"],
            _ => null
        };

        return new WTelegram.Client(Config);
    });

    builder.Services.AddScoped<IPostService, PostService>();
    builder.Services.AddScoped<ITelegramSenderService, TelegramSenderService>();
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });    
}

var app = builder.Build();
{
    app.UseHttpsRedirection();

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

    app.UseForwardedHeaders();
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
        KnownNetworks = { }, // разрешить все сети
        KnownProxies = { }   // разрешить всех прокси
    });

    app.UseAuthorization();
    app.UseSerilogRequestLogging();

    app.MapControllers();
    app.UseExceptionHandler();
    app.Run();
}