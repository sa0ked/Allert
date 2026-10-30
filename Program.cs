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
        ["Вінницька область"] = new[] { "Вінницький район", "Гайсинський район", "Жмеринський район", "Могилів-Подільський район" },
        ["Волинська область"] = new[] { "Володимирський район", "Камінь-Каширський район", "Ковельський район", "Луцький район" },
        ["Дніпропетровська область"] = new[] { "Дніпровський район", "Кам'янський район", "Криворізький район", "Нікопольський район", "Павлоградський район", "Синельниківський район" },
        ["Донецька область"] = new[] { "Бахмутський район", "Волноваський район", "Краматорський район", "Маріупольський район", "Покровський район" },
        ["Житомирська область"] = new[] { "Бердичівський район", "Житомирський район", "Коростенський район", "Звягельський район" },
        ["Закарпатська область"] = new[] { "Берегівський район", "Мукачівський район", "Рахівський район", "Тячівський район", "Ужгородський район", "Хустський район" },
        ["Запорізька область"] = new[] { "Бердянський район", "Василівський район", "Запорізький район", "Мелітопольський район", "Пологівський район" },
        ["Івано-Франківська область"] = new[] { "Верховинський район", "Івано-Франківський район", "Калуський район", "Коломийський район", "Косівський район", "Надвірнянський район" },
        ["Київська область"] = new[] { "Білоцерківський район", "Бориспільський район", "Броварський район", "Бучанський район", "Вишгородський район", "Обухівський район", "Фастівський район" },
        ["Кіровоградська область"] = new[] { "Голованівський район", "Кропивницький район", "Новоукраїнський район", "Олександрійський район" },
        ["Львівська область"] = new[] { "Дрогобицький район", "Золочівський район", "Львівський район", "Самбірський район", "Стрийський район", "Червоноградський район", "Яворівський район" },
        ["Миколаївська область"] = new[] { "Баштанський район", "Вознесенський район", "Миколаївський район", "Первомайський район" },
        ["Одеська область"] = new[] { "Березівський район", "Білгород-Дністровський район", "Болградський район", "Ізмаїльський район", "Одеський район", "Подільський район", "Роздільнянський район" },
        ["Полтавська область"] = new[] { "Кременчуцький район", "Лубенський район", "Миргородський район", "Полтавський район" },
        ["Рівненська область"] = new[] { "Вараський район", "Дубенський район", "Рівненський район", "Сарненський район" },
        ["Сумська область"] = new[] { "Конотопський район", "Охтирський район", "Роменський район", "Сумський район", "Шосткинський район" },
        ["Тернопільська область"] = new[] { "Кременецький район", "Тернопільський район", "Чортківський район" },
        ["Херсонська область"] = new[] { "Бериславський район", "Генічеський район", "Каховський район", "Скадовський район", "Херсонський район" },
        ["Хмельницька область"] = new[] { "Кам'янець-Подільський район", "Хмельницький район", "Шепетівський район" },
        ["Черкаська область"] = new[] { "Звенигородський район", "Золотоніський район", "Уманський район", "Черкаський район" },
        ["Чернівецька область"] = new[] { "Вижницький район", "Дністровський район", "Чернівецький район" },
        ["Чернігівська область"] = new[] { "Корюківський район", "Ніжинський район", "Новгород-Сіверський район", "Прилуцький район", "Чернігівський район" },
        ["Харківська область"] = new[]
        {
            "Богодухівський район", "Ізюмський район", "Куп'янський район", "Лозівський район",
            "Харківський район", "Чугуївський район"
        }
    };

    private static readonly Dictionary<string, string[]> CitiesByDistrict = new()
    {
        ["Вінницький район"] = new[] { "Вінниця", "Жмеринка", "Літин" },
        ["Гайсинський район"] = new[] { "Гайсин", "Бершадь", "Ладижин" },
        ["Жмеринський район"] = new[] { "Жмеринка", "Бар", "Шаргород" },
        ["Могилів-Подільський район"] = new[] { "Могилів-Подільський", "Ямпіль", "Муровані Курилівці" },
        ["Володимирський район"] = new[] { "Володимир", "Нововолинськ", "Любомль" },
        ["Камінь-Каширський район"] = new[] { "Камінь-Каширський", "Любешів", "Маневичі" },
        ["Ковельський район"] = new[] { "Ковель", "Ратне", "Шацьк" },
        ["Луцький район"] = new[] { "Луцьк", "Горохів", "Ківерці" },
        ["Дніпровський район"] = new[] { "Дніпро", "Самар", "Підгородне" },
        ["Кам'янський район"] = new[] { "Кам'янське", "Верхньодніпровськ", "Жовті Води" },
        ["Криворізький район"] = new[] { "Кривий Ріг", "Апостолове", "Зеленодольськ" },
        ["Нікопольський район"] = new[] { "Нікополь", "Марганець", "Покров" },
        ["Павлоградський район"] = new[] { "Павлоград", "Тернівка", "Першотравенськ" },
        ["Синельниківський район"] = new[] { "Синельникове", "П'ятихатки", "Верхньодніпровськ" },
        ["Бахмутський район"] = new[] { "Бахмут", "Сіверськ", "Часів Яр" },
        ["Волноваський район"] = new[] { "Волноваха", "Вугледар", "Комишуваха" },
        ["Краматорський район"] = new[] { "Краматорськ", "Слов'янськ", "Дружківка" },
        ["Маріупольський район"] = new[] { "Маріуполь", "Нікольське", "Мангуш" },
        ["Покровський район"] = new[] { "Покровськ", "Мирноград", "Добропілля" },
        ["Бердичівський район"] = new[] { "Бердичів", "Андрушівка", "Чуднів" },
        ["Житомирський район"] = new[] { "Житомир", "Коростишів", "Черняхів" },
        ["Коростенський район"] = new[] { "Коростень", "Овруч", "Малин" },
        ["Звягельський район"] = new[] { "Звягель", "Баранівка", "Ярун" },
        ["Берегівський район"] = new[] { "Берегове", "Виноградів", "Велика Бийгань" },
        ["Мукачівський район"] = new[] { "Мукачево", "Свалява", "Іршава" },
        ["Рахівський район"] = new[] { "Рахів", "Ясіня", "Великий Бичків" },
        ["Тячівський район"] = new[] { "Тячів", "Буштино", "Дубове" },
        ["Ужгородський район"] = new[] { "Ужгород", "Чоп", "Перечин" },
        ["Хустський район"] = new[] { "Хуст", "Міжгір'я", "Воловець" },
        ["Бердянський район"] = new[] { "Бердянськ", "Приморськ", "Чернігівка" },
        ["Василівський район"] = new[] { "Василівка", "Дніпрорудне", "Степногірськ" },
        ["Запорізький район"] = new[] { "Запоріжжя", "Вільнянськ", "Оріхів" },
        ["Мелітопольський район"] = new[] { "Мелітополь", "Токмак", "Молочанськ" },
        ["Пологівський район"] = new[] { "Пологи", "Гуляйполе", "Кам'янка-Дніпровська" },
        ["Верховинський район"] = new[] { "Верховина", "Криворівня", "Білоберізка" },
        ["Івано-Франківський район"] = new[] { "Івано-Франківськ", "Тисмениця", "Богородчани" },
        ["Калуський район"] = new[] { "Калуш", "Долина", "Болехів" },
        ["Коломийський район"] = new[] { "Коломия", "Городенка", "Снятин" },
        ["Косівський район"] = new[] { "Косів", "Кути", "Рожнів" },
        ["Надвірнянський район"] = new[] { "Надвірна", "Яремче", "Битків" },
        ["Білоцерківський район"] = new[] { "Біла Церква", "Сквира", "Тараща" },
        ["Бориспільський район"] = new[] { "Бориспіль", "Баришівка", "Переяслав" },
        ["Броварський район"] = new[] { "Бровари", "Березань", "Згурівка" },
        ["Бучанський район"] = new[] { "Буча", "Ірпінь", "Вишневе" },
        ["Вишгородський район"] = new[] { "Вишгород", "Славутич", "Димер" },
        ["Обухівський район"] = new[] { "Обухів", "Українка", "Васильків" },
        ["Фастівський район"] = new[] { "Фастів", "Боярка", "Кагарлик" },
        ["Ізюмський район"] = new[] { "Ізюм", "Барвінкове", "Борова", "Донецьке" },
        ["Харківський район"] = new[] { "Харків", "Дергачі", "Люботин", "Мерефа", "Південне" },
        ["Богодухівський район"] = new[] { "Богодухів", "Валки", "Краснокутськ" },
        ["Куп'янський район"] = new[] { "Куп'янськ", "Дворічна", "Шевченкове" },
        ["Лозівський район"] = new[] { "Лозова", "Первомайський", "Близнюки" },
        ["Чугуївський район"] = new[] { "Чугуїв", "Вовчанськ", "Зміїв", "Печеніги" }
    };

    static Program()
    {
        foreach (string district in DistrictsByRegion.Values.SelectMany(items => items))
        {
            if (!CitiesByDistrict.ContainsKey(district))
            {
                string city = district.Replace(" район", "", StringComparison.OrdinalIgnoreCase);
                CitiesByDistrict[district] = new[] { city };
            }
        }
    }

    private static readonly Dictionary<long, Subscription> Subscriptions = new();
    private static readonly Dictionary<long, string> PendingRegions = new();
    private static readonly Dictionary<long, string> PendingDistricts = new();
    private static readonly HashSet<long> AwaitingDistrictText = new();
    private static readonly ConcurrentDictionary<string, AlertState> LastStates = new();
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private const string DefaultAlertsUrl = "https://ubilling.net.ua/aerialalerts/";
    private const string AdminUsername = "sa0ked";
    private static readonly string DataFilePath = Path.Combine(AppContext.BaseDirectory, "bot-data.json");
    private static readonly string UpdateFilePath = Path.Combine(AppContext.BaseDirectory, "bot-update.json");
    private static readonly string UpdateVersionPath = Path.Combine(AppContext.BaseDirectory, "bot-update-version.txt");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private static readonly object DataLock = new();
    private static readonly TimeZoneInfo KyivTimeZone = GetKyivTimeZone();
    private static readonly string AlertsUrl = Environment.GetEnvironmentVariable("ALERTS_API_URL") ?? DefaultAlertsUrl;
    private static readonly string? UpdatesUrl = Environment.GetEnvironmentVariable("UPDATES_URL");
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

        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        ReceiverOptions options = new() { AllowedUpdates = Array.Empty<UpdateType>() };
        Task polling = Bot.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, options, cts.Token);
        Task alerts = AlertPollingLoopAsync(cts.Token);
        Task updates = UpdatePollingLoopAsync(cts.Token);

        try
        {
            await Task.WhenAll(polling, alerts, updates);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
        finally
        {
            await SendMaintenanceMessageAsync();
        }
    }

    private static bool IsAdmin(Message message)
    {
        return string.Equals(message.From?.Username, AdminUsername, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SendMaintenanceMessageAsync(string? customText = null)
    {
        const string defaultText =
            "🔧 Бот тимчасово вимикається на технічні роботи.\n" +
            "Сповіщення будуть недоступні до завершення оновлення.\n\n" +
            "Будь ласка, перевіряй офіційні джерела цивільного захисту.";
        string text = string.IsNullOrWhiteSpace(customText) ? defaultText : $"🔧 {customText}";

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

    private static async Task SendBotUpdateAsync(CancellationToken ct, long? onlyChatId = null)
    {
        BotUpdate? update = await LoadBotUpdateAsync(ct);
        if (update is null)
        {
            return;
        }

        List<string> updateChanges = update.Changes ?? new List<string>();
        string changes = updateChanges.Count == 0
            ? "Змін немає."
            : string.Join("\n", updateChanges.Select(change => $"• {change}"));

        string text = $"📢 {update.Title}\n" +
                      $"Версія: {update.Version}\n" +
                      $"Дата: {FormatTime(update.UpdatedAt)}\n\n" +
                      changes;

        IEnumerable<long> recipients = onlyChatId.HasValue
            ? new[] { onlyChatId.Value }
            : Subscriptions.Keys.ToArray();

        foreach (long chatId in recipients)
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

    private static async Task UpdatePollingLoopAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(UpdatesUrl))
        {
            await Task.Delay(Timeout.Infinite, ct);
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                BotUpdate? update = await LoadBotUpdateAsync(ct);
                if (update is not null && !string.IsNullOrWhiteSpace(update.Version))
                {
                    string? lastVersion = LoadLastUpdateVersion();

                    if (lastVersion is null)
                    {
                        SaveLastUpdateVersion(update.Version);
                    }
                    else if (!string.Equals(lastVersion, update.Version, StringComparison.Ordinal))
                    {
                        SaveLastUpdateVersion(update.Version);
                        await SendBotUpdateAsync(ct);
                        Console.WriteLine($"Обновление {update.Version} отправлено подписчикам.");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося перевірити оновлення: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private static async Task<BotUpdate?> LoadBotUpdateAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(UpdatesUrl))
        {
            try
            {
                string remoteJson = await Http.GetStringAsync(UpdatesUrl, ct);
                return JsonSerializer.Deserialize<BotUpdate>(remoteJson, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося завантажити оновлення з GitHub: {ex.Message}");
            }
        }

        if (!File.Exists(UpdateFilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BotUpdate>(File.ReadAllText(UpdateFilePath), JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося завантажити bot-update.json: {ex.Message}");
            return null;
        }
    }

    private static string? LoadLastUpdateVersion()
    {
        return File.Exists(UpdateVersionPath)
            ? File.ReadAllText(UpdateVersionPath).Trim()
            : null;
    }

    private static void SaveLastUpdateVersion(string version)
    {
        File.WriteAllText(UpdateVersionPath, version);
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
            else if (text.Equals("/updates", StringComparison.OrdinalIgnoreCase))
            {
                await SendBotUpdateAsync(ct, chatId);
            }
            else if (text.StartsWith("/maintenance", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsAdmin(message))
                {
                    await bot.SendMessage(chatId, "Ця команда доступна лише адміністратору.", cancellationToken: ct);
                    return;
                }

                string customText = text["/maintenance".Length..].Trim();
                await SendMaintenanceMessageAsync(string.IsNullOrWhiteSpace(customText) ? null : customText);
                await bot.SendMessage(chatId, "Повідомлення про технічні роботи надіслано підписникам.", cancellationToken: ct);
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
        "/start — вибрати область і місто\n/status — поточний статус\n/alarm — є тривога?\n/updates — останні оновлення\n/help — допомога\n\n" +
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
                string level = ReadString(property.Value, "level", "alert_level", "danger_level", "color", "severity");
                return new AlertState(isAlert, changed, level);
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
            string level = ReadString(item, "level", "alert_level", "color");
            return new AlertState(true, updatedAt, level);
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
            ? $"🚨 ТРИВОГА\n{FormatAlertLevel(state.Level)}\nРегіон: {region}\nПочаток: {FormatTime(state.UpdatedAt)}\nТриває: {FormatDuration(state.UpdatedAt)}\n\nНегайно пройди до укриття та стеж за офіційними повідомленнями."
            : $"✅ Відбій повітряної тривоги\nРегіон: {region}\nЧас відбою: {FormatTime(state.UpdatedAt)}\nВідбій триває: {FormatDuration(state.UpdatedAt)}\n\nВсе одно перевіряй офіційні джерела.";
    }

    private static string FormatAlarmStatus(AlertState state, string region)
    {
        if (!state.HasData)
        {
            return $"⚠️ Немає актуальних даних про статус тривоги.\nРегіон: {region}\n\nПеревір офіційні джерела цивільного захисту.";
        }

        return state.IsAlert
            ? $"🚨 Так, є повітряна тривога!\n{FormatAlertLevel(state.Level)}\nРегіон: {region}\nПочаток: {FormatTime(state.UpdatedAt)}\nТриває: {FormatDuration(state.UpdatedAt)}\n\nПройди до укриття та стеж за офіційними повідомленнями."
            : $"✅ Зараз активної повітряної тривоги не зафіксовано.\nРегіон: {region}\nЧас відбою: {FormatTime(state.UpdatedAt)}\nВідбій триває: {FormatDuration(state.UpdatedAt)}\n\nІнформація може змінитися — перевіряй офіційні джерела.";
    }

    private static string FormatAlertLevel(string level)
    {
        if (level.Contains("помаранч", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("оранж", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("orange", StringComparison.OrdinalIgnoreCase))
        {
            return "🟠 ПОМАРАНЧЕВИЙ РІВЕНЬ НЕБЕЗПЕКИ";
        }

        if (level.Contains("жовт", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("yellow", StringComparison.OrdinalIgnoreCase))
        {
            return "🟡 ЖОВТИЙ РІВЕНЬ НЕБЕЗПЕКИ";
        }

        if (level.Contains("червон", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("red", StringComparison.OrdinalIgnoreCase))
        {
            return "🔴 ЧЕРВОНИЙ РІВЕНЬ НЕБЕЗПЕКИ";
        }

        if (level.Contains("зел", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("green", StringComparison.OrdinalIgnoreCase))
        {
            return "🟢 ЗЕЛЕНИЙ РІВЕНЬ НЕБЕЗПЕКИ";
        }

        if (level.Contains("висок", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("high", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("critical", StringComparison.OrdinalIgnoreCase))
        {
            return "🔴 ВИСОКИЙ РІВЕНЬ НЕБЕЗПЕКИ";
        }

        return string.IsNullOrWhiteSpace(level)
            ? "⚠️ РІВЕНЬ НЕ ВКАЗАНО ДЖЕРЕЛОМ"
            : $"⚠️ РІВЕНЬ НЕБЕЗПЕКИ: {level}";
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

    private static string FormatDuration(string value)
    {
        if (!TryParseAlertTime(value, out DateTimeOffset startedAt) || startedAt.Year <= 1971)
        {
            return "невідомо (джерело не передало коректний час)";
        }

        TimeSpan duration = DateTimeOffset.UtcNow - startedAt.ToUniversalTime();
        if (duration < TimeSpan.Zero)
        {
            return "невідомо (час джерела попереду поточного часу)";
        }

        return duration.TotalDays >= 1
            ? $"{(int)duration.TotalDays} дн. {duration.Hours} год. {duration.Minutes} хв."
            : duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours} год. {duration.Minutes} хв."
                : $"{duration.Minutes} хв. {duration.Seconds} сек.";
    }

    private static bool TryParseAlertTime(string value, out DateTimeOffset result)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out result))
        {
            return true;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out result);
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
    private sealed record BotUpdate(string Version, string UpdatedAt, string Title, List<string>? Changes);
    private sealed record AlertState(bool IsAlert, string UpdatedAt, string Level = "", bool HasData = true);
}
