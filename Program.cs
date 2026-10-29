using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static readonly string[] Regions =
    {
        "Вінницька область", "Волинська область", "Дніпропетровська область",
        "Донецька область", "Житомирська область", "Закарпатська область",
        "Запорізька область", "Івано-Франківська область", "Київська область",
        "Кіровоградська область", "Львівська область", "Миколаївська область",
        "Одеська область", "Полтавська область", "Рівненська область",
        "Сумська область", "Тернопільська область", "Харківська область",
        "Херсонська область", "Хмельницька область", "Черкаська область",
        "Чернівецька область", "Чернігівська область"
    };

    private static readonly Dictionary<string, string[]> DistrictsByRegion = new()
    {
        ["Харківська область"] = new[]
        {
            "Богодухівський район", "Ізюмський район", "Куп'янський район",
            "Лозівський район", "Харківський район", "Чугуївський район"
        }
    };

    private static readonly Dictionary<string, string[]> CitiesByDistrict = new()
    {
        ["Ізюмський район"] = new[] { "Ізюм", "Барвінкове", "Борова", "Донецьке" },
        ["Харківський район"] = new[] { "Харків", "Дергачі", "Люботин", "Мерефа", "Південне" },
        ["Богодухівський район"] = new[] { "Богодухів", "Валки", "Краснокутськ" },
        ["Куп'янський район"] = new[] { "Куп'янськ", "Дворічна", "Шевченкове" },
        ["Лозівський район"] = new[] { "Лозова", "Первомайський", "Близнюки" },
        ["Чугуївський район"] = new[] { "Чугуїв", "Вовчанськ", "Зміїв", "Печеніги" }
    };

    private static readonly Dictionary<long, Subscription> Subscriptions = new();
    private static readonly Dictionary<long, string> PendingRegions = new();
    private static readonly Dictionary<long, string> PendingDistricts = new();
    private static readonly HashSet<long> AwaitingDistrictText = new();
    private static readonly ConcurrentDictionary<string, AlertState> LastStates = new();
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private const string DefaultAlertsUrl = "https://ubilling.net.ua/aerialalerts/";
    private static readonly string DataFilePath = Path.Combine(AppContext.BaseDirectory, "bot-data.json");
    private static readonly string UpdateFilePath = Path.Combine(AppContext.BaseDirectory, "bot-update.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly object DataLock = new();
    private static readonly TimeZoneInfo KyivTimeZone = GetKyivTimeZone();
    private static readonly string AlertsUrl = Environment.GetEnvironmentVariable("ALERTS_API_URL") ?? DefaultAlertsUrl;
    private static TelegramBotClient Bot = null!;

    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string? token = Environment.GetEnvironmentVariable("BOT_TOKEN")?.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Токен не введён.");
            return;
        }

        LoadSubscriptions();
        Bot = new TelegramBotClient(token);
        User me = await Bot.GetMe();
        Console.WriteLine($"Бот @{me.Username} запущен.");
        Console.WriteLine($"Источник статусов: {AlertsUrl}");

        await SendBotUpdateAsync(ct: CancellationToken.None);

        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        ReceiverOptions options = new() { AllowedUpdates = Array.Empty<UpdateType>() };
        Task polling = Bot.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, options, cts.Token);
        Task alerts = AlertPollingLoopAsync(cts.Token);

        try
        {
            await Task.WhenAll(polling, alerts);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        finally
        {
            await SendMaintenanceMessageAsync();
        }
    }

    private static async Task SendMaintenanceMessageAsync()
    {
        const string text =
            "🔧 Бот тимчасово вимикається на технічні роботи.\n" +
            "Сповіщення будуть недоступні до завершення оновлення.\n\n" +
            "Будь ласка, перевіряй офіційні джерела цивільного захисту.";

        foreach (long chatId in Subscriptions.Keys.ToArray())
        {
            try
            {
                await Bot.SendMessage(chatId, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося надіслати повідомлення про технічні роботи в чат {chatId}: {ex.Message}");
            }
        }
    }

    private static async Task SendBotUpdateAsync(CancellationToken ct)
    {
        BotUpdate? update = LoadBotUpdate();
        if (update is null)
        {
            return;
        }

        string changes = update.Changes.Count == 0
            ? "Змін немає."
            : string.Join("\n", update.Changes.Select(change => $"• {change}"));

        string text = $"📢 {update.Title}\n" +
                      $"Версія: {update.Version}\n" +
                      $"Дата: {FormatTime(update.UpdatedAt)}\n\n" +
                      changes;

        foreach (long chatId in Subscriptions.Keys.ToArray())
        {
            try
            {
                await Bot.SendMessage(chatId, text, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося надіслати оновлення в чат {chatId}: {ex.Message}");
            }
        }
    }

    private static BotUpdate? LoadBotUpdate()
    {
        if (!File.Exists(UpdateFilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BotUpdate>(File.ReadAllText(UpdateFilePath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося завантажити bot-update.json: {ex.Message}");
            return null;
        }
    }

    private static void LoadSubscriptions()
    {
        if (!File.Exists(DataFilePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(DataFilePath);
            List<PersistedSubscription>? saved = JsonSerializer.Deserialize<List<PersistedSubscription>>(json);

            if (saved is null)
            {
                return;
            }

            foreach (PersistedSubscription item in saved)
            {
                if (!string.IsNullOrWhiteSpace(item.Region) && !string.IsNullOrWhiteSpace(item.City))
                {
                    Subscriptions[item.ChatId] = new Subscription(item.Region, item.City, item.District ?? "Район не вказано");
                }
            }

            Console.WriteLine($"Завантажено підписок: {Subscriptions.Count}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося завантажити bot-data.json: {ex.Message}");
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is not null)
            {
                await HandleCallbackAsync(update.CallbackQuery, ct);
                return;
            }

            if (update.Type != UpdateType.Message || update.Message?.Text is null)
            {
                return;
            }

            Message message = update.Message;
            long chatId = message.Chat.Id;
            string text = message.Text.Trim();

            if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                await SendRegionsAsync(chatId, ct);
            }
            else if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                await SendHelpAsync(chatId, ct);
            }
            else if (text.Equals("/status", StringComparison.OrdinalIgnoreCase))
            {
                await SendStatusAsync(chatId, ct);
            }
            else if (text.Equals("/alarm", StringComparison.OrdinalIgnoreCase) || text.Equals("Є тривога?", StringComparison.OrdinalIgnoreCase))
            {
                await SendAlarmStatusAsync(chatId, ct);
            }
            else if (text.Equals("/stop", StringComparison.OrdinalIgnoreCase))
            {
                Subscriptions.Remove(chatId, out _);
                PendingRegions.Remove(chatId);
                PendingDistricts.Remove(chatId);
                AwaitingDistrictText.Remove(chatId);
                SaveSubscriptions();
                await bot.SendMessage(chatId, "Підписку вимкнено. Щоб підписатися знову — /start", cancellationToken: ct);
            }
            else if (AwaitingDistrictText.Remove(chatId))
            {
                PendingDistricts[chatId] = text;
                await bot.SendMessage(chatId, "Район збережено. Тепер напиши назву свого міста:", cancellationToken: ct);
            }
            else if (PendingDistricts.ContainsKey(chatId))
            {
                string city = text.Trim();
                string region = PendingRegions[chatId];
                string district = PendingDistricts[chatId];
                Subscriptions[chatId] = new Subscription(region, city, district);
                PendingRegions.Remove(chatId);
                PendingDistricts.Remove(chatId);
                SaveSubscriptions();
                await bot.SendMessage(chatId,
                    $"Готово. Ти підписаний на {city}, {district}, {region}.\n\n" +
                    "Я повідомлю про зміну статусу повітряної тривоги. Перевіряй також офіційні джерела.",
                    replyMarkup: GetStatusKeyboard(),
                    cancellationToken: ct);
            }
            else if (PendingRegions.ContainsKey(chatId))
            {
                string region = PendingRegions[chatId];
                if (DistrictsByRegion.TryGetValue(region, out string[]? districts))
                {
                    await SendDistrictsAsync(chatId, region, districts, ct);
                }
                else
                {
                    await bot.SendMessage(chatId, "Напиши назву свого району одним повідомленням:", cancellationToken: ct);
                    AwaitingDistrictText.Add(chatId);
                }
            }

            else
            {
                await bot.SendMessage(chatId, "Напиши /start для вибору області або /help для списку команд.", cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки обновления: {ex.Message}");
        }
    }

    private static void SaveSubscriptions()
    {
        try
        {
            lock (DataLock)
            {
                List<PersistedSubscription> saved = Subscriptions
                    .Select(item => new PersistedSubscription(
                        item.Key,
                        item.Value.Region,
                        item.Value.City,
                        item.Value.District))
                    .ToList();

                string temporaryPath = DataFilePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(saved, JsonOptions));
                File.Move(temporaryPath, DataFilePath, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося зберегти bot-data.json: {ex.Message}");
        }
    }

    private static async Task HandleCallbackAsync(CallbackQuery query, CancellationToken ct)
    {
        await Bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        if (query.Message is null || string.IsNullOrWhiteSpace(query.Data)) return;

        long chatId = query.Message.Chat.Id;
        string data = query.Data;

        if (data.StartsWith("region:", StringComparison.Ordinal))
        {
            string region = data["region:".Length..];
            PendingRegions[chatId] = region;
            if (DistrictsByRegion.TryGetValue(region, out string[]? districts))
            {
                await SendDistrictsAsync(chatId, region, districts, ct);
            }
            else
            {
                await Bot.SendMessage(chatId, $"Обрана область: {region}. Напиши назву свого району:", cancellationToken: ct);
                AwaitingDistrictText.Add(chatId);
            }
        }
        else if (data.StartsWith("district:", StringComparison.Ordinal))
        {
            if (!PendingRegions.TryGetValue(chatId, out string? region)) return;

            string district = data["district:".Length..];
            PendingDistricts[chatId] = district;
            await SendCitiesAsync(chatId, region, district, ct);
        }
        else if (data.StartsWith("city:", StringComparison.Ordinal))
        {
            if (!PendingRegions.TryGetValue(chatId, out string? region) ||
                !PendingDistricts.TryGetValue(chatId, out string? district)) return;

            string city = data["city:".Length..];
            Subscriptions[chatId] = new Subscription(region, city, district);
            PendingRegions.Remove(chatId);
            PendingDistricts.Remove(chatId);
            SaveSubscriptions();
            await Bot.SendMessage(chatId,
                $"Готово. Ти підписаний на {city}, {district}, {region}.\n\n" +
                "Я повідомлю про зміну статусу повітряної тривоги. Перевіряй також офіційні джерела.",
                replyMarkup: GetStatusKeyboard(), cancellationToken: ct);
        }
    }

    private static async Task SendDistrictsAsync(long chatId, string region, string[] districts, CancellationToken ct)
    {
        InlineKeyboardButton[][] rows = districts
            .Select(district => new[] { InlineKeyboardButton.WithCallbackData(district, $"district:{district}") })
            .ToArray();

        await Bot.SendMessage(chatId, $"Обрана область: {region}. Обери свій район:",
            replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }

    private static async Task SendCitiesAsync(long chatId, string region, string district, CancellationToken ct)
    {
        string[] cities = CitiesByDistrict.TryGetValue(district, out string[]? knownCities)
            ? knownCities
            : new[] { "Введу місто вручну" };

        InlineKeyboardButton[][] rows = cities
            .Select(city => new[] { InlineKeyboardButton.WithCallbackData(city, $"city:{city}") })
            .ToArray();

        await Bot.SendMessage(chatId, $"Район: {district}. Обери своє місто або введи його вручну:",
            replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }

    private static async Task SendRegionsAsync(long chatId, CancellationToken ct)
    {
        InlineKeyboardButton[][] rows = Regions
            .Select((region, index) => new { region, index })
            .GroupBy(x => x.index / 2)
            .Select(group => group.Select(x => InlineKeyboardButton.WithCallbackData(x.region, $"region:{x.region}")).ToArray())
            .ToArray();

        await Bot.SendMessage(chatId,
            "Привіт! Це бот цивільних сповіщень про повітряні тривоги.\n\nОбери область:",
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    private static Task SendHelpAsync(long chatId, CancellationToken ct) => Bot.SendMessage(chatId,
        "/start — вибрати область і місто\n/status — поточний статус\n/alarm — є тривога?\n/stop — вимкнути сповіщення\n/help — допомога\n\n" +
        "Повідомлення можуть затримуватися. У небезпеці користуйся офіційними каналами цивільного захисту.", cancellationToken: ct);

    private static ReplyKeyboardMarkup GetStatusKeyboard() => new(new[]
    {
        new KeyboardButton[] { "Є тривога?" },
        new KeyboardButton[] { "/status", "/stop" }
    })
    {
        ResizeKeyboard = true,
        IsPersistent = true
    };

    private static async Task SendAlarmStatusAsync(long chatId, CancellationToken ct)
    {
        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(chatId, "Спочатку обери область командою /start.", cancellationToken: ct);
            return;
        }

        AlertState state = await GetAlertStateAsync(subscription.Region, ct);
        await Bot.SendMessage(chatId, FormatAlarmStatus(state, subscription.Region), replyMarkup: GetStatusKeyboard(), cancellationToken: ct);
    }

    private static async Task SendStatusAsync(long chatId, CancellationToken ct)
    {
        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(chatId, "Спочатку обери область командою /start.", cancellationToken: ct);
            return;
        }

        AlertState state = await GetAlertStateAsync(subscription.Region, ct);
        await Bot.SendMessage(chatId, FormatAlert(state, subscription.Region), cancellationToken: ct);
    }

    private static async Task AlertPollingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (IGrouping<string, KeyValuePair<long, Subscription>> group in Subscriptions.GroupBy(x => x.Value.Region))
            {
                try
                {
                    AlertState state = await GetAlertStateAsync(group.Key, ct);
                    if (!LastStates.TryGetValue(group.Key, out AlertState? previous) || previous.IsAlert != state.IsAlert)
                    {
                        LastStates[group.Key] = state;
                        foreach (KeyValuePair<long, Subscription> item in group)
                        {
                            await Bot.SendMessage(item.Key, FormatAlert(state, item.Value.Region), cancellationToken: ct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Источник тревог недоступен: {ex.Message}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }

    private static async Task<AlertState> GetAlertStateAsync(string region, CancellationToken ct)
    {
        using HttpResponseMessage response = await Http.GetAsync(AlertsUrl, ct);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(ct);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement records = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("states", out JsonElement states)
            ? states
            : root.ValueKind == JsonValueKind.Array
            ? root
            : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("alerts", out JsonElement alerts)
                ? alerts
                : default;

        if (records.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in records.EnumerateObject())
            {
                if (!IsSameRegion(property.Name, region) || property.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                bool isAlert = property.Value.TryGetProperty("alertnow", out JsonElement alertValue) &&
                               alertValue.ValueKind == JsonValueKind.True;
                string changed = ReadString(property.Value, "changed", "updated_at", "updatedAt");
                return new AlertState(isAlert, changed);
            }

            return new AlertState(false, "", HasData: false);
        }

        if (records.ValueKind != JsonValueKind.Array)
        {
            return new AlertState(false, "", HasData: false);
        }

        foreach (JsonElement item in records.EnumerateArray())
        {
            string title = ReadString(item, "location_title", "region", "location", "name");
            if (!IsSameRegion(title, region))
            {
                continue;
            }

            string updatedAt = ReadString(item, "last_update", "updated_at", "updatedAt", "started_at");
            return new AlertState(true, updatedAt);
        }

        return new AlertState(false, "", HasData: false);
    }

    private static string ReadString(JsonElement item, params string[] names)
    {
        foreach (string name in names)
        {
            if (item.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }
        }

        return "";
    }

    private static bool IsSameRegion(string title, string region)
    {
        if (string.Equals(title, region, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string normalizedTitle = title.Replace(" область", "", StringComparison.OrdinalIgnoreCase).Trim();
        string normalizedRegion = region.Replace(" область", "", StringComparison.OrdinalIgnoreCase).Trim();
        return string.Equals(normalizedTitle, normalizedRegion, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAlert(AlertState state, string region)
    {
        if (!state.HasData)
        {
            return $"⚠️ Немає актуальних даних про статус тривоги.\nРегіон: {region}\n\nПеревір офіційні джерела цивільного захисту.";
        }

        return state.IsAlert
            ? $"🚨 ТРИВОГА\nЧЕРВОНИЙ РІВЕНЬ НЕБЕЗПЕКИ\nРегіон: {region}\nЧас: {FormatTime(state.UpdatedAt)}\n\nНегайно пройди до укриття та стеж за офіційними повідомленнями."
            : $"✅ Відбій повітряної тривоги\nРегіон: {region}\nЧас: {FormatTime(state.UpdatedAt)}\n\nВсе одно перевіряй офіційні джерела.";
    }

    private static string FormatAlarmStatus(AlertState state, string region)
    {
        if (!state.HasData)
        {
            return $"⚠️ Немає актуальних даних про статус тривоги.\nРегіон: {region}\n\nПеревір офіційні джерела цивільного захисту.";
        }

        return state.IsAlert
            ? $"🚨 Так, є повітряна тривога!\nРегіон: {region}\nЧас: {FormatTime(state.UpdatedAt)}\n\nПройди до укриття та стеж за офіційними повідомленнями."
            : $"✅ Зараз активної повітряної тривоги не зафіксовано.\nРегіон: {region}\nЧас: {FormatTime(state.UpdatedAt)}\n\nІнформація може змінитися — перевіряй офіційні джерела.";
    }

    private static string FormatTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "невідомий (джерело не передало час)";
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime date))
        {
            if (date.Year <= 1971)
            {
                return "невідомий (джерело передало технічну дату)";
            }

            DateTime kyivTime = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
            TimeSpan offset = KyivTimeZone.GetUtcOffset(kyivTime);
            DateTimeOffset exactKyivTime = new(kyivTime, offset);
            return $"{exactKyivTime:dd.MM.yyyy HH:mm:ss} (Київ)";
        }

        return value;
    }

    private static TimeZoneInfo GetKyivTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken __)
    {
        Console.WriteLine($"Telegram error: {exception.Message}");
        return Task.CompletedTask;
    }

    private sealed record Subscription(string Region, string City, string District);
    private sealed record PersistedSubscription(long ChatId, string Region, string City, string? District);
    private sealed record BotUpdate(string Version, string UpdatedAt, string Title, List<string> Changes);
    private sealed record AlertState(bool IsAlert, string UpdatedAt, bool HasData = true);
}
