#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /app

ARG private_nuget_pat
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS '{"endpointCredentials":[{"endpoint":"https://pkgs.dev.azure.com/mikibot/Miki/_packaging/mikibot/nuget/v3/index.json","username":"Miki","password":"'${private_nuget_pat}'"}]}'

RUN apt-get update
RUN apt-get install -y wget  
RUN wget -O - https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh  | bash

COPY ["NuGet.config", "."]
COPY ["src/Miki/Miki.csproj", "src/Miki/"]
COPY ["src/Miki.Api/Miki.Api.csproj", "src/Miki.Api/"]
COPY ["submodules/miki.bot.models/Miki.Bot.Models.csproj", "submodules/miki.bot.models/"]
COPY ["submodules/retsu/src/Retsu.Consumer/Retsu.Consumer.csproj", "submodules/retsu/src/Retsu.Consumer/"]
COPY ["submodules/retsu/src/Retsu.Models/Retsu.Models.csproj", "submodules/retsu/src/Retsu.Models/"]
RUN dotnet restore "src/Miki/Miki.csproj"
COPY . .

WORKDIR ./src/Miki
RUN dotnet build "Miki.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Miki.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Miki.dll"]