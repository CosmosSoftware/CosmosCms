#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

# Install NodeJS 10
RUN apt-get update -y && \
   apt-get install -y gnupg2 && \
   apt-get install -y curl && \
   curl -sL https://deb.nodesource.com/setup_18.x | bash - && \
   apt-get update -y && \
   apt-get install -y nodejs && \
   apt-get install -y --no-install-recommends openssh-server && \
   mkdir -p /run/sshd && \
   echo "root:Docker!" | chpasswd

# Copy the sshd_config file to the /etc/ssh/ directory
WORKDIR /src
COPY ["Cosmos.Cms/sshd_config", "/etc/ssh/"]
COPY ["Cosmos.Cms/init.sh", "/usr/local/bin/"]


# Copy and configure the ssh_setup file
RUN mkdir -p /tmp
COPY ["Cosmos.Cms/ssh_setup.sh", "/tmp/"] 
RUN chmod +x /tmp/ssh_setup.sh \
    && (sleep 1;/tmp/ssh_setup.sh 2>&1 > /dev/null)

ENV SSH_PORT 2222

WORKDIR /app
EXPOSE 80 443 2222

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Cosmos.Cms/Cosmos.Cms.csproj", "Cosmos.Cms/"]
COPY . .

WORKDIR "/src/Cosmos.Cms"
RUN dotnet build "Cosmos.Cms.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cosmos.Cms.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ENTRYPOINT ["dotnet", "Cosmos.Cms.dll"]
 ENTRYPOINT ["bash", "init.sh"]
