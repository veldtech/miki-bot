#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
COPY ["NuGet.config", "."]
COPY ["src/Miki/Miki.csproj", "src/Miki/"]
COPY ["src/Miki.Api/Miki.Api.csproj", "src/Miki.Api/"]
COPY ["submodules/miki.bot.models/Miki.Bot.Models.csproj", "submodules/miki.bot.models/"]
COPY ["submodules/retsu/src/Retsu.Consumer/Retsu.Consumer.csproj", "submodules/retsu/src/Retsu.Consumer/"]
COPY ["submodules/retsu/src/Retsu.Models/Retsu.Models.csproj", "submodules/retsu/src/Retsu.Models/"]
RUN dotnet restore "src/Miki/Miki.csproj"
COPY . .
WORKDIR "/src/Miki"
RUN dotnet build "Miki.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Miki.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Miki.dll"]