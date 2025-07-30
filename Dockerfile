# BASE — исполняемая среда
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 8080
EXPOSE 8081
VOLUME /app/logs

# BUILD — сборка проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["SummyAITelegramBot.Core/SummyAITelegramBot.Core.csproj", "SummyAITelegramBot.Core/"]
COPY ["SummyAITelegramBot.API/SummyAITelegramBot.API.csproj", "SummyAITelegramBot.API/"]

RUN dotnet restore "SummyAITelegramBot.API/SummyAITelegramBot.API.csproj"

COPY . .
WORKDIR "/src/SummyAITelegramBot.API"
RUN dotnet build "SummyAITelegramBot.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# PUBLISH — финальная публикация
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SummyAITelegramBot.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# FINAL — образ, который запускается
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./

ENTRYPOINT ["dotnet", "SummyAITelegramBot.API.dll"]