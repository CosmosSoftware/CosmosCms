#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Cosmos.Cms/Cosmos.Cms.csproj", "Cosmos.Cms/"]
COPY ["Cosmos.Cms.Common/CDT.Cosmos.Cms.Common.csproj", "Cosmos.Cms.Common/"]
COPY ["Cosmos.BlobService/Cosmos.BlobService.csproj", "Cosmos.BlobService/"]
COPY . .
WORKDIR "/src/Cosmos.Cms"
# RUN dotnet build "Cosmos.Cms.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cosmos.Cms.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CDT.Cosmos.Cms.dll"]
