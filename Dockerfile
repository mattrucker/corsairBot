FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview.1 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0-preview.1 AS build
WORKDIR /src
COPY ["CorsairBot.Core/CorsairBot.Core.csproj", "CorsairBot.Core/"]
RUN dotnet restore "CorsairBot.Core/CorsairBot.Core.csproj"
COPY . .
WORKDIR "/src/CorsairBot.Core"
RUN dotnet build "CorsairBot.Core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CorsairBot.Core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install Chrome and ChromeDriver
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Install WireGuard
RUN apt-get update && apt-get install -y \
    wireguard \
    && rm -rf /var/lib/apt/lists/*

# Create mount points
RUN mkdir -p /config /data

ENTRYPOINT ["dotnet", "CorsairBot.Core.dll"] 