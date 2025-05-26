# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

RUN mkdir -p /app/logs
VOLUME /app/logs

EXPOSE 8080
EXPOSE 8081

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем и восстанавливаем библиотеки
COPY ["SummyAITelegramBot.Core/SummyAITelegramBot.Core.csproj", "SummyAITelegramBot.Core/"]
COPY ["SummyAITelegramBot.Infrastructure/SummyAITelegramBot.Infrastructure.csproj", "SummyAITelegramBot.Infrastructure/"]
COPY ["SummyAITelegramBot.API/SummyAITelegramBot.API.csproj", "SummyAITelegramBot.API/"]

# Восстанавливаем зависимости для всех проектов
RUN dotnet restore "./SummyAITelegramBot.API/SummyAITelegramBot.API.csproj"
RUN dotnet restore "./SummyAITelegramBot.Core/SummyAITelegramBot.Core.csproj"
RUN dotnet restore "./SummyAITelegramBot.Infrastructure/SummyAITelegramBot.Infrastructure.csproj"

# Копируем весь исходный код
COPY . .

WORKDIR "/src/SummyAITelegramBot.API"
RUN dotnet build "./SummyAITelegramBot.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SummyAITelegramBot.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/logs
ENTRYPOINT ["dotnet", "SummyAITelegramBot.API.dll"]