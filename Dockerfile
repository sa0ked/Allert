FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore ./ConsoleApp.csproj
RUN dotnet publish ./ConsoleApp.csproj -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/out .

# Данные бота (подписки, история, бан-лист). Смонтируй сюда volume, чтобы они не терялись при деплое.
RUN mkdir -p /data
ENV BOT_DATA_FILE=/data/bot-data.json

# Переменные окружения (задаются в панели хостинга, НЕ в коде):
#   BOT_TOKEN          - токен Telegram-бота (обязательно)
#   UKRAINEALARM_API_KEY - ключ api.ukrainealarm.com (основне джерело тривог)
#   ALERTS_API_TOKEN   - (необовʼязково) ключ alerts.in.ua, резервне джерело
#   ADMIN_IDS          - (необязательно) Telegram ID админов через запятую
#   DRONE_CHANNEL_IDS  - ID каналів-моніторингів (бот має бути адміном каналу) — звідси бере тип загрози: ракети/дрони
#   UPDATES_URL        - (необязательно) ссылка на bot-update.json на GitHub

ENTRYPOINT ["dotnet", "ConsoleApp.dll"]
