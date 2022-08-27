#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
# Install NodeJS 10
RUN apt-get update -y && apt-get install -y gnupg2 && apt-get install -y curl && curl -sL https://deb.nodesource.com/setup_18.x | bash - && apt-get update -y && apt-get install -y nodejs
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["packages/", "packages/"]
RUN ls packages/
COPY ["Cosmos.Cms.Website/nuget.config", "Cosmos.Cms.Website/"]
COPY ["nuget.config", "."]
COPY ["Cosmos.Cms.Website/Cosmos.Cms.Website.csproj", "Cosmos.Cms.Website/"]
COPY ["Cosmos.Cms.Common/Cosmos.Cms.Common.csproj", "Cosmos.Cms.Common/"]
RUN dotnet restore "Cosmos.Cms.Website/Cosmos.Cms.Website.csproj"
COPY . .
WORKDIR "/src/Cosmos.Cms.Website"
# RUN dotnet build "Cosmos.Cms.Website.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cosmos.Cms.Website.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Cosmos.Cms.Website.dll"]
