#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TheThinker.DiscordBot.WorkerService/TheThinker.DiscordBot.WorkerService.csproj", "TheThinker.DiscordBot.WorkerService/"]
RUN dotnet restore "TheThinker.DiscordBot.WorkerService/TheThinker.DiscordBot.WorkerService.csproj"
COPY . .
WORKDIR "/src/TheThinker.DiscordBot.WorkerService"
RUN dotnet build "TheThinker.DiscordBot.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TheThinker.DiscordBot.WorkerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TheThinker.DiscordBot.WorkerService.dll"]