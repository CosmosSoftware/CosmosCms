#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["packages/", "packages/"]
RUN ls packages/
COPY ["CDT.Cosmos.Cms.Website/nuget.config", "CDT.Cosmos.Cms.Website/"]
COPY ["nuget.config", "."]
COPY ["CDT.Cosmos.Cms.Website/CDT.Cosmos.Cms.Website.csproj", "CDT.Cosmos.Cms.Website/"]
COPY ["CDT.Cosmos.Cms.Common/CDT.Cosmos.Cms.Common.csproj", "CDT.Cosmos.Cms.Common/"]
RUN dotnet restore "CDT.Cosmos.Cms.Website/CDT.Cosmos.Cms.Website.csproj"
COPY . .
WORKDIR "/src/CDT.Cosmos.Cms.Website"
# RUN dotnet build "CDT.Cosmos.Cms.Website.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CDT.Cosmos.Cms.Website.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CDT.Cosmos.Cms.Website.dll"]
