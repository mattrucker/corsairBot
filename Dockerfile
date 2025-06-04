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

ENV APP_USER=appuser
RUN addgroup --system ${APP_USER} && adduser --system --ingroup ${APP_USER} ${APP_USER}

ENV CHROME_VERSION="121.0.6167.85-1"
ENV CHROME_DRIVER_VERSION="121.0.6167.85"

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        wget \
        gnupg \
        ca-certificates \
        unzip \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable=${CHROME_VERSION} \
    && wget -q "https://storage.googleapis.com/chrome-for-testing-public/${CHROME_DRIVER_VERSION}/linux64/chromedriver-linux64.zip" -P /tmp \
    && unzip /tmp/chromedriver-linux64.zip -d /tmp \
    && mv /tmp/chromedriver-linux64/chromedriver /usr/local/bin/chromedriver \
    && chmod +x /usr/local/bin/chromedriver \
    && apt-get install -y --no-install-recommends wireguard \
    && rm -rf /var/lib/apt/lists/* /tmp/*

WORKDIR /app
COPY --from=publish /app/publish .

# Create mount points (should already exist if WORKDIR /app is effective from base, but explicit is fine)
RUN mkdir -p /config /data
RUN chown -R ${APP_USER}:${APP_USER} /app /data /config

USER ${APP_USER}

ENTRYPOINT ["dotnet", "CorsairBot.Core.dll"] 