#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
# Install NodeJS 10
RUN apt-get update -y && apt-get install -y gnupg2 && apt-get install -y curl && curl -sL https://deb.nodesource.com/setup_18.x | bash - && apt-get update -y && apt-get install -y nodejs
WORKDIR /app
EXPOSE 80
EXPOSE 443

# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Cosmos.Cms/Cosmos.Cms.csproj", "Cosmos.Cms/"]
COPY ["Cosmos.Cms.Common/Cosmos.Cms.Common.csproj", "Cosmos.Cms.Common/"]
COPY ["Cosmos.BlobService/Cosmos.BlobService.csproj", "Cosmos.BlobService/"]
COPY . .

WORKDIR "/src/Cosmos.Cms"
RUN dotnet build "Cosmos.Cms.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cosmos.Cms.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Cosmos.Cms.dll"]
