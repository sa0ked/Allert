using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
        "Чернівецька область", "Чернігівська область",
        "Луганська область", "Автономна Республіка Крим", "м. Київ"
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
        ["Луганська область"] = new[] { "Алчевський район", "Луганський район", "Сєвєродонецький район", "Старобільський район", "Щастинський район" },
        ["Автономна Республіка Крим"] = new[] { "Бахчисарайський район", "Джанкойський район", "Сімферопольський район", "Феодосійський район", "Ялтинський район" },
        ["м. Київ"] = new[] { "Київ" },
        ["Харківська область"] = new[]
        {
            "Богодухівський район", "Ізюмський район", "Куп'янський район",
            "Лозівський район", "Харківський район", "Чугуївський район"
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
        ["Голованівський район"] = new[] { "Голованівськ", "Гайворон", "Ульяновка" },
        ["Кропивницький район"] = new[] { "Кропивницький", "Долинська", "Знам'янка" },
        ["Новоукраїнський район"] = new[] { "Новоукраїнка", "Мала Виска", "Помічна" },
        ["Олександрійський район"] = new[] { "Олександрія", "Світловодськ", "Знам'янка" },
        ["Дрогобицький район"] = new[] { "Дрогобич", "Трускавець", "Борислав" },
        ["Золочівський район"] = new[] { "Золочів", "Броди", "Буськ" },
        ["Львівський район"] = new[] { "Львів", "Винники", "Жовква" },
        ["Самбірський район"] = new[] { "Самбір", "Турка", "Добромиль" },
        ["Стрийський район"] = new[] { "Стрий", "Моршин", "Сколе" },
        ["Червоноградський район"] = new[] { "Червоноград", "Соснівка", "Радехів" },
        ["Яворівський район"] = new[] { "Яворів", "Новояворівськ", "Мостиська" },
        ["Баштанський район"] = new[] { "Баштанка", "Новий Буг", "Снігурівка" },
        ["Вознесенський район"] = new[] { "Вознесенськ", "Южноукраїнськ", "Братське" },
        ["Миколаївський район"] = new[] { "Миколаїв", "Очаків", "Нова Одеса" },
        ["Первомайський район"] = new[] { "Первомайськ", "Южноукраїнськ", "Криве Озеро" },
        ["Березівський район"] = new[] { "Березівка", "Раухівка", "Миколаївка" },
        ["Білгород-Дністровський район"] = new[] { "Білгород-Дністровський", "Затока", "Сергіївка" },
        ["Болградський район"] = new[] { "Болград", "Арциз", "Тарутине" },
        ["Ізмаїльський район"] = new[] { "Ізмаїл", "Кілія", "Рені" },
        ["Одеський район"] = new[] { "Одеса", "Чорноморськ", "Южне" },
        ["Подільський район"] = new[] { "Подільськ", "Балта", "Кодима" },
        ["Роздільнянський район"] = new[] { "Роздільна", "Великодолинське", "Лиманське" },
        ["Кременчуцький район"] = new[] { "Кременчук", "Горішні Плавні", "Глобине" },
        ["Лубенський район"] = new[] { "Лубни", "Хорол", "Пирятин" },
        ["Миргородський район"] = new[] { "Миргород", "Гадяч", "Велика Багачка" },
        ["Полтавський район"] = new[] { "Полтава", "Карлівка", "Диканька" },
        ["Вараський район"] = new[] { "Вараш", "Зарічне", "Володимирець" },
        ["Дубенський район"] = new[] { "Дубно", "Радивилів", "Млинів" },
        ["Рівненський район"] = new[] { "Рівне", "Здолбунів", "Острог" },
        ["Сарненський район"] = new[] { "Сарни", "Дубровиця", "Рокитне" },
        ["Конотопський район"] = new[] { "Конотоп", "Буринь", "Кролевець" },
        ["Охтирський район"] = new[] { "Охтирка", "Тростянець", "Лебедин" },
        ["Роменський район"] = new[] { "Ромни", "Недригайлів", "Хмелів" },
        ["Сумський район"] = new[] { "Суми", "Білопілля", "Тростянець" },
        ["Шосткинський район"] = new[] { "Шостка", "Глухів", "Дружба" },
        ["Кременецький район"] = new[] { "Кременець", "Почаїв", "Шумськ" },
        ["Тернопільський район"] = new[] { "Тернопіль", "Збараж", "Бережани", "Зборів" },
        ["Чортківський район"] = new[] { "Чортків", "Бучач", "Борщів", "Монастириська" },
        ["Бериславський район"] = new[] { "Берислав", "Каховка", "Нова Каховка" },
        ["Генічеський район"] = new[] { "Генічеськ", "Новоолексіївка", "Щасливцеве" },
        ["Каховський район"] = new[] { "Каховка", "Нова Каховка", "Любимівка" },
        ["Скадовський район"] = new[] { "Скадовськ", "Гола Пристань", "Лазурне" },
        ["Херсонський район"] = new[] { "Херсон", "Олешки", "Білозерка" },
        ["Кам'янець-Подільський район"] = new[] { "Кам'янець-Подільський", "Дунаївці", "Стара Ушиця" },
        ["Хмельницький район"] = new[] { "Хмельницький", "Старокостянтинів", "Волочиськ" },
        ["Шепетівський район"] = new[] { "Шепетівка", "Славута", "Нетішин" },
        ["Звенигородський район"] = new[] { "Звенигородка", "Ватутіне", "Шпола" },
        ["Золотоніський район"] = new[] { "Золотоноша", "Драбів", "Чорнобай" },
        ["Уманський район"] = new[] { "Умань", "Христинівка", "Монастирище" },
        ["Черкаський район"] = new[] { "Черкаси", "Канів", "Сміла" },
        ["Вижницький район"] = new[] { "Вижниця", "Вашківці", "Путила" },
        ["Дністровський район"] = new[] { "Хотин", "Кельменці", "Сокиряни" },
        ["Чернівецький район"] = new[] { "Чернівці", "Новоселиця", "Сторожинець" },
        ["Корюківський район"] = new[] { "Корюківка", "Мена", "Сновськ" },
        ["Ніжинський район"] = new[] { "Ніжин", "Бахмач", "Борзна" },
        ["Новгород-Сіверський район"] = new[] { "Новгород-Сіверський", "Семенівка", "Короп" },
        ["Прилуцький район"] = new[] { "Прилуки", "Ічня", "Ладан" },
        ["Чернігівський район"] = new[] { "Чернігів", "Батурин", "Козелець" },

        ["Алчевський район"] = new[] { "Алчевськ", "Брянка", "Кадіївка" },
        ["Луганський район"] = new[] { "Луганськ", "Олександрівськ", "Хрящувате" },
        ["Сєвєродонецький район"] = new[] { "Сєвєродонецьк", "Лисичанськ", "Рубіжне" },
        ["Старобільський район"] = new[] { "Старобільськ", "Мілове", "Біловодськ" },
        ["Щастинський район"] = new[] { "Щастя", "Новоайдар", "Станиця Луганська" },
        ["Бахчисарайський район"] = new[] { "Бахчисарай", "Куйбишеве" },
        ["Джанкойський район"] = new[] { "Джанкой", "Армянськ", "Красноперекопськ" },
        ["Сімферопольський район"] = new[] { "Сімферополь", "Алушта", "Євпаторія" },
        ["Феодосійський район"] = new[] { "Феодосія", "Керч", "Судак" },
        ["Ялтинський район"] = new[] { "Ялта", "Севастополь", "Алупка" },
        ["Київ"] = new[] { "Київ" },
        ["Ізюмський район"] = new[] { "Ізюм", "Барвінкове", "Борова", "Донецьке", "Балаклія" },
        ["Харківський район"] = new[] { "Харків", "Дергачі", "Люботин", "Мерефа", "Південне" },
        ["Богодухівський район"] = new[] { "Богодухів", "Валки", "Краснокутськ" },
        ["Куп'янський район"] = new[] { "Куп'янськ", "Дворічна", "Шевченкове" },
        ["Лозівський район"] = new[] { "Лозова", "Первомайський", "Близнюки" },
        ["Чугуївський район"] = new[] { "Чугуїв", "Вовчанськ", "Зміїв", "Печеніги" }
    };

    private static readonly Dictionary<string, string> RegionalCenters = new()
    {
        ["Вінницька область"] = "Вінниця",
        ["Волинська область"] = "Луцьк",
        ["Дніпропетровська область"] = "Дніпро",
        ["Донецька область"] = "Донецьк",
        ["Житомирська область"] = "Житомир",
        ["Закарпатська область"] = "Ужгород",
        ["Запорізька область"] = "Запоріжжя",
        ["Івано-Франківська область"] = "Івано-Франківськ",
        ["Київська область"] = "Київ",
        ["Кіровоградська область"] = "Кропивницький",
        ["Львівська область"] = "Львів",
        ["Миколаївська область"] = "Миколаїв",
        ["Одеська область"] = "Одеса",
        ["Полтавська область"] = "Полтава",
        ["Рівненська область"] = "Рівне",
        ["Сумська область"] = "Суми",
        ["Тернопільська область"] = "Тернопіль",
        ["Харківська область"] = "Харків",
        ["Херсонська область"] = "Херсон",
        ["Хмельницька область"] = "Хмельницький",
        ["Черкаська область"] = "Черкаси",
        ["Чернівецька область"] = "Чернівці",
        ["Чернігівська область"] = "Чернігів"
    };

    private static readonly Dictionary<string, int> RegionUids = new()
    {
        ["Вінницька область"] = 4,
        ["Волинська область"] = 8,
        ["Дніпропетровська область"] = 9,
        ["Донецька область"] = 28,
        ["Житомирська область"] = 10,
        ["Закарпатська область"] = 11,
        ["Запорізька область"] = 12,
        ["Івано-Франківська область"] = 13,
        ["Київська область"] = 14,
        ["Кіровоградська область"] = 15,
        ["Львівська область"] = 27,
        ["Миколаївська область"] = 17,
        ["Одеська область"] = 18,
        ["Полтавська область"] = 19,
        ["Рівненська область"] = 5,
        ["Сумська область"] = 20,
        ["Тернопільська область"] = 21,
        ["Харківська область"] = 22,
        ["Херсонська область"] = 23,
        ["Хмельницька область"] = 3,
        ["Черкаська область"] = 24,
        ["Чернівецька область"] = 26,
        ["Чернігівська область"] = 25
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

        foreach (KeyValuePair<string, string> item in RegionalCenters)
        {
            if (!DistrictsByRegion.TryGetValue(item.Key, out string[]? districts))
                continue;

            string? district = districts.FirstOrDefault(value =>
                value.Contains(item.Value.Replace("і", "и", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase) ||
                item.Value.Contains(value.Replace(" район", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));

            district ??= districts[0];
            string[] cities = CitiesByDistrict[district];

            if (!cities.Contains(item.Value, StringComparer.OrdinalIgnoreCase))
            {
                CitiesByDistrict[district] = cities
                    .Append(item.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    private static readonly ConcurrentDictionary<long, Subscription> Subscriptions = new();
    private static readonly ConcurrentDictionary<long, int> StatusPresses = new();
    private static readonly ConcurrentDictionary<long, string> PendingRegions = new();
    private static readonly ConcurrentDictionary<long, string> PendingDistricts = new();
    private static readonly ConcurrentDictionary<long, byte> AwaitingDistrictText = new();
    private static readonly ConcurrentDictionary<long, byte> BannedUsers = new();
    private static readonly ConcurrentDictionary<long, byte> DroneOptOut = new();
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastDroneNotify = new();
    private static readonly List<DroneReport> DroneReports = new();
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;
    private static readonly ConcurrentDictionary<string, AlertState> LastStates = new();
    private static readonly List<AlertHistoryEntry> AlertHistory = new();
    private static readonly SemaphoreSlim AlertsJsonLock = new(1, 1);
    private static string? CachedAlertsJson;
    private static DateTimeOffset AlertsJsonFetchedAt;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private const string DefaultAlertsUrl = "https://ubilling.net.ua/aerialalerts/";
    
    // Список администраторов бота
    private static readonly string[] AdminUsernames = { "sa0ked", "kandes00" };

    private const string KeyedAlertsUrl = "https://api.alerts.in.ua/v1/alerts/active.json";

    // Ukraine Alarm API (api.ukrainealarm.com). Ключ — ТІЛЬКИ зі змінної оточення UKRAINEALARM_API_KEY.
    private const string UkraineAlarmUrl = "https://api.ukrainealarm.com/api/v3/alerts";

    private static readonly string? UkraineAlarmToken =
        Environment.GetEnvironmentVariable("UKRAINEALARM_API_KEY")?.Trim();

    private static readonly string? AlertsHistoryToken =
        Environment.GetEnvironmentVariable("ALERTS_HISTORY_TOKEN")?.Trim();

    // Ключ API берём ТОЛЬКО из переменной окружения (не хардкодим в коде/репозитории).
    private static readonly string? AlertsApiToken =
        Environment.GetEnvironmentVariable("ALERTS_API_TOKEN")?.Trim() ?? AlertsHistoryToken;

    private static readonly HashSet<long> AdminIds =
        ParseLongList(Environment.GetEnvironmentVariable("ADMIN_IDS"));

    // Каналы-мониторинги, из которых бот принимает посты про БпЛА (бот должен быть админом канала).
    private static readonly HashSet<long> DroneChannelIds =
        ParseLongList(Environment.GetEnvironmentVariable("DRONE_CHANNEL_IDS"));

    // 🟡 ЖОВТИЙ рівень: дрони (ударні шахеди, реактивні, FPV, будь-які БпЛА).
    private static readonly string[] DroneKeywords =
    {
        "шахед", "бпла", "дрон", "герань", "геран", "безпілот", "мопед", "гербера",
        "фпв", "fpv", "камікадзе", "shahed", "geran", "gerbera", "uav"
    };

    // 🔴 ЧЕРВОНИЙ рівень: ракети та балістика (перевіряється ПЕРШИМ — має пріоритет над дронами).
    private static readonly string[] MissileKeywords =
    {
        "баліст", "ракет", "крилат", "кинджал", "кінжал", "іскандер", "калібр", "циркон",
        "х-101", "х-22", "х-47", "х-59", "х-31", "x-101", "x-22", "x-47", "x-59", "x-31",
        "кн-23", "kn-23", "iskander", "kinzhal", "kalibr", "zircon", "ballistic", "cruise missile"
    };

    // Реактивні дрони: "реактивний шахед", "реактивні дрони", "Герань-3" тощо.
    private static readonly Regex JetDroneRegex = new(
        @"реактивн\w*\s+(шахед|дрон|бпла|герань|геран|безпілот)|геран[ьи]?-?\s?3|geran-?\s?3|шахед-?\s?238",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] DroneClearKeywords = { "відбій", "відбой" };

    private static readonly Regex ExplicitOffsetRegex =
        new(@"(Z|[+\-]\d{2}(:?\d{2})?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string DataFilePath = ResolveDataFilePath();

    private static readonly string UpdateFilePath =
        Path.Combine(AppContext.BaseDirectory, "bot-update.json");

    private static readonly string UpdateVersionPath =
        Path.Combine(
            Path.GetDirectoryName(DataFilePath) ?? AppContext.BaseDirectory,
            "bot-update-version.txt");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly object DataLock = new();
    private static readonly TimeZoneInfo KyivTimeZone = GetKyivTimeZone();

    private static readonly string AlertsUrl = ResolveAlertsUrl();

    private static string ActiveSourceUrl = AlertsUrl;

    private static readonly string? UpdatesUrl =
        Environment.GetEnvironmentVariable("UPDATES_URL");

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
        Console.WriteLine($"Джерело статусів: {AlertsUrl}");
        Console.WriteLine($"API ключ alerts.in.ua: {(string.IsNullOrWhiteSpace(AlertsApiToken) ? "не налаштований" : "налаштований")}");
        Console.WriteLine($"API ключ ukrainealarm.com: {(string.IsNullOrWhiteSpace(UkraineAlarmToken) ? "не налаштований" : "налаштований")}");
        Console.WriteLine($"Канали БпЛА: {DroneChannelIds.Count}; ID адмінів: {AdminIds.Count}");

        using CancellationTokenSource cts = new();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        ReceiverOptions options = new()
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.ChannelPost }
        };

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
        if (message.From is null)
            return false;

        if (AdminIds.Contains(message.From.Id))
            return true;

        if (message.From.Username == null)
            return false;

        return AdminUsernames.Contains(message.From.Username, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SendAdminInfoAsync(long chatId, CancellationToken ct)
    {
        string adminsList = string.Join("\n", AdminUsernames.Select(username => $"• @{WebUtility.HtmlEncode(username)}"));

        string text =
            "👑 <b>Інформація про адміністраторів бота:</b>\n\n" +
            adminsList + "\n\n" +
            "З усіх питань роботи бота або пропозицій ви можете звертатися до них! 🫶";

        await Bot.SendMessage(chatId, text, parseMode: ParseMode.Html, cancellationToken: ct);
    }

    private static async Task SendMaintenanceMessageAsync(string? customText = null)
    {
        const string defaultText =
            "🔧 Бот тимчасово вимикається на технічні роботи.\n" +
            "Сповіщення будуть недоступні до завершення оновлення.\n\n" +
            "Будь ласка, перевіряй офіційні джерела цивільного захисту.";

        string text = string.IsNullOrWhiteSpace(customText)
            ? defaultText
            : $"🔧 {customText}";

        await SendToAllAsync(text, CancellationToken.None);
    }

    private static async Task<int> SendToAllAsync(string text, CancellationToken ct)
    {
        int sent = 0;

        foreach (long chatId in Subscriptions.Keys.ToArray())
        {
            if (BannedUsers.ContainsKey(chatId))
                continue;

            if (await SafeSendAsync(chatId, text, ct))
                sent++;

            await Task.Delay(40, CancellationToken.None);
        }

        return sent;
    }

    /// <summary>Безпечна відправка: не падає, а блокування бота користувачем (403) прибирає підписку.</summary>
    private static async Task<bool> SafeSendAsync(
        long chatId,
        string text,
        CancellationToken ct,
        ParseMode parseMode = default)
    {
        try
        {
            await Bot.SendMessage(chatId, text, parseMode: parseMode, cancellationToken: ct);
            return true;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
        {
            if (Subscriptions.TryRemove(chatId, out _))
            {
                Console.WriteLine($"Користувач {chatId} заблокував бота — підписку видалено.");
                SaveSubscriptions();
            }

            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося надіслати повідомлення в чат {chatId}: {ex.Message}");
            return false;
        }
    }

    private static async Task SendBotUpdateAsync(CancellationToken ct, long? onlyChatId = null)
    {
        BotUpdate? update = await LoadBotUpdateAsync(ct);
        await SendBotUpdateAsync(update, ct, onlyChatId);
    }

    private static async Task SendBotUpdateAsync(
        BotUpdate? update,
        CancellationToken ct,
        long? onlyChatId = null)
    {
        if (update is null)
            return;

        List<string> updateChanges = update.Changes ?? new List<string>();

        string changes = updateChanges.Count == 0
            ? "Змін немає."
            : string.Join("\n", updateChanges.Select(change => $"• {change}"));

        string text =
            $"📢 {update.Title}\n" +
            $"Версія: {update.Version}\n" +
            $"Опубліковано: {FormatUpdateTime(update.UpdatedAt)}\n\n" +
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

                    Console.WriteLine(
                        $"Перевірка оновлення: GitHub={update.Version}, збережена версія={lastVersion ?? "відсутня"}.");

                    if (lastVersion is null)
                    {
                        SaveLastUpdateVersion(update.Version);
                    }
                    else if (!string.Equals(lastVersion, update.Version, StringComparison.Ordinal))
                    {
                        SaveLastUpdateVersion(update.Version);
                        await SendBotUpdateAsync(update, ct);

                        Console.WriteLine($"Оновлення {update.Version} надіслано підписникам.");
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
                string separator = UpdatesUrl.Contains('?') ? "&" : "?";

                string remoteJson = await Http.GetStringAsync(
                    $"{UpdatesUrl}{separator}cacheBust={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                    ct);

                return JsonSerializer.Deserialize<BotUpdate>(remoteJson, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося завантажити оновлення з GitHub: {ex.Message}");
            }
        }

        if (!File.Exists(UpdateFilePath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BotUpdate>(
                File.ReadAllText(UpdateFilePath),
                JsonOptions);
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

    private static string ResolveAlertsUrl()
    {
        string? custom = Environment.GetEnvironmentVariable("ALERTS_URL")?.Trim();
        if (!string.IsNullOrWhiteSpace(custom))
            return custom;

        if (!string.IsNullOrWhiteSpace(UkraineAlarmToken))
            return UkraineAlarmUrl;

        return string.IsNullOrWhiteSpace(AlertsApiToken) ? DefaultAlertsUrl : KeyedAlertsUrl;
    }

    private static HashSet<long> ParseLongList(string? value)
    {
        HashSet<long> result = new();
        if (string.IsNullOrWhiteSpace(value))
            return result;

        foreach (string part in value.Split(new[] { ',', ';', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (long.TryParse(part.Trim(), out long id))
                result.Add(id);
        }

        return result;
    }

    private static string ResolveDataFilePath()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("BOT_DATA_FILE");
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        string workingDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "bot-data.json");
        if (File.Exists(workingDirectoryPath))
            return workingDirectoryPath;

        string executableDirectoryPath = Path.Combine(AppContext.BaseDirectory, "bot-data.json");
        return File.Exists(executableDirectoryPath) ? executableDirectoryPath : workingDirectoryPath;
    }

    private static void LoadSubscriptions()
    {
        if (!File.Exists(DataFilePath))
            return;

        try
        {
            string json = File.ReadAllText(DataFilePath);

            if (string.IsNullOrWhiteSpace(json))
                return;

            using JsonDocument document = JsonDocument.Parse(json);
            List<PersistedSubscription>? saved;

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Backward compatibility with the original bot-data.json array.
                saved = JsonSerializer.Deserialize<List<PersistedSubscription>>(json, JsonOptions);
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                JsonElement root = document.RootElement;
                saved = root.TryGetProperty("subscriptions", out JsonElement subscriptions)
                    ? JsonSerializer.Deserialize<List<PersistedSubscription>>(subscriptions.GetRawText(), JsonOptions)
                    : new List<PersistedSubscription>();

                LoadIdSet(root, "bannedUsers", BannedUsers);
                LoadIdSet(root, "droneOptOut", DroneOptOut);

                if (root.TryGetProperty("alertHistory", out JsonElement history) &&
                    history.ValueKind == JsonValueKind.Array)
                {
                    AlertHistory.AddRange(
                        JsonSerializer.Deserialize<List<AlertHistoryEntry>>(history.GetRawText(), JsonOptions)
                        ?? new List<AlertHistoryEntry>());
                }
            }
            else
            {
                return;
            }

            foreach (PersistedSubscription item in saved ?? new List<PersistedSubscription>())
            {
                if (!string.IsNullOrWhiteSpace(item.Region) &&
                    !string.IsNullOrWhiteSpace(item.City))
                {
                    Subscriptions[item.ChatId] = new Subscription(
                        item.Region,
                        item.City,
                        item.District ?? "Район не вказано");
                }
            }

            Console.WriteLine(
                $"Завантажено підписок: {Subscriptions.Count}; записів історії: {AlertHistory.Count}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося завантажити bot-data.json: {ex.Message}");
        }
    }

    private static void LoadIdSet(JsonElement root, string property, ConcurrentDictionary<long, byte> target)
    {
        if (!root.TryGetProperty(property, out JsonElement array) ||
            array.ValueKind != JsonValueKind.Array)
            return;

        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out long id))
                target[id] = 0;
        }
    }

    private static void SaveSubscriptions()
    {
        try
        {
            lock (DataLock)
            {
                PersistedBotData saved = new(
                    Subscriptions.Select(item => new PersistedSubscription(
                        item.Key,
                        item.Value.Region,
                        item.Value.City,
                        item.Value.District)).ToList(),
                    AlertHistory.ToList(),
                    BannedUsers.Keys.ToList(),
                    DroneOptOut.Keys.ToList());

                string temporaryPath = DataFilePath + ".tmp";

                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(saved, JsonOptions));

                File.Move(temporaryPath, DataFilePath, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не вдалося зберегти bot-data.json: {ex.Message}");
        }
    }

    private static async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.CallbackQuery &&
                update.CallbackQuery is not null)
            {
                await HandleCallbackAsync(update.CallbackQuery, ct);
                return;
            }

            if (update.Type == UpdateType.ChannelPost &&
                update.ChannelPost is not null)
            {
                await HandleChannelPostAsync(update.ChannelPost, ct);
                return;
            }

            if (update.Type != UpdateType.Message ||
                update.Message?.Text is null)
            {
                return;
            }

            Message message = update.Message;
            long chatId = message.Chat.Id;
            string text = NormalizeCommand(message.Text.Trim());

            if (BannedUsers.ContainsKey(chatId) && !IsAdmin(message))
                return;

            if (await TryHandleAdminCommandAsync(message, text, ct))
                return;

            if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                ClearPendingSetup(chatId);
                await SendRegionsAsync(chatId, ct);
            }
            else if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                await SendHelpAsync(chatId, IsAdmin(message), ct);
            }
            else if (text.Equals("ℹ️ Інфо", StringComparison.OrdinalIgnoreCase) || text.Equals("/admin", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await SendAdminInfoAsync(chatId, ct);
            }
            else if (text.Equals("/status", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.AddOrUpdate(chatId, 1, (_, value) => value + 1);
                await SendStatusAsync(chatId, ct);
            }
            else if (text.Equals("🔄 Оновити", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/refresh", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await SendAlarmStatusAsync(chatId, ct);
            }
            else if (text.Equals("📍 Моя підписка", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/subscription", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await SendSubscriptionInfoAsync(chatId, ct);
            }
            else if (text.Equals("/alarm", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("Є тривога?", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await SendAlarmStatusAsync(chatId, ct);
            }
            else if (text.Equals("Історія", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("/history", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await SendCityHistoryAsync(chatId, ct);
            }
            else if (text.Equals("/updates", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await SendBotUpdateAsync(ct, chatId);
            }
            else if (text.StartsWith("/drones", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("🛩 БпЛА поруч", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                await HandleDronesCommandAsync(chatId, text, ct);
            }
            else if (text.Equals("/cancel", StringComparison.OrdinalIgnoreCase) ||
                     text.Equals("Скасувати", StringComparison.OrdinalIgnoreCase))
            {
                ClearPendingSetup(chatId);
                await bot.SendMessage(
                    chatId,
                    "Налаштування скасовано. Поточна підписка не змінена.",
                    replyMarkup: GetStatusKeyboard(),
                    cancellationToken: ct);
            }
            else if (text.Equals("/stop", StringComparison.OrdinalIgnoreCase))
            {
                StatusPresses.TryRemove(chatId, out _);
                Subscriptions.TryRemove(chatId, out _);
                PendingRegions.TryRemove(chatId, out _);
                PendingDistricts.TryRemove(chatId, out _);
                AwaitingDistrictText.TryRemove(chatId, out _);

                SaveSubscriptions();

                await bot.SendMessage(
                    chatId,
                    "Підписку вимкнено. Щоб підписатися знову — /start",
                    replyMarkup: GetStatusKeyboard(),
                    cancellationToken: ct);
            }
            else if (AwaitingDistrictText.TryRemove(chatId, out _))
            {
                PendingDistricts[chatId] = text;

                await bot.SendMessage(
                    chatId,
                    "Район збережено. Тепер напиши назву свого міста:",
                    cancellationToken: ct);
            }
            else if (PendingDistricts.ContainsKey(chatId))
            {
                string city = text.Trim();
                string region = PendingRegions[chatId];
                string district = PendingDistricts[chatId];

                Subscriptions[chatId] = new Subscription(region, city, district);

                PendingRegions.TryRemove(chatId, out _);
                PendingDistricts.TryRemove(chatId, out _);

                SaveSubscriptions();

                await bot.SendMessage(
                    chatId,
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
                    await bot.SendMessage(
                        chatId,
                        "Напиши назву свого району одним повідомленням:",
                        cancellationToken: ct);

                    AwaitingDistrictText[chatId] = 0;
                }
            }
            else
            {
                await bot.SendMessage(
                    chatId,
                    "Напиши /start для вибору області або /help для списку команд.",
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка обробки оновлення: {ex.Message}");
        }
    }

    private const string ManualCityButton = "Введу місто вручну";

    private static string NormalizeCommand(string text)
    {
        if (!text.StartsWith('/'))
            return text;

        int split = text.IndexOfAny(new[] { ' ', '\n', '\t' });
        string command = split < 0 ? text : text[..split];
        string rest = split < 0 ? "" : text[split..];

        int at = command.IndexOf('@');
        if (at > 0)
            command = command[..at];

        return command + rest;
    }

    private static void ClearPendingSetup(long chatId)
    {
        PendingRegions.TryRemove(chatId, out _);
        PendingDistricts.TryRemove(chatId, out _);
        AwaitingDistrictText.TryRemove(chatId, out _);
    }

    private static async Task SendSubscriptionInfoAsync(long chatId, CancellationToken ct)
    {
        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(
                chatId,
                "Підписки ще немає. Натисни /start, щоб обрати область і місто.",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);
            return;
        }

        await Bot.SendMessage(
            chatId,
            $"📍 Твоя підписка\nОбласть: {subscription.Region}\nРайон: {subscription.District}\nМісто: {subscription.City}\n\n" +
            "Щоб змінити локацію, натисни /start. Щоб вимкнути сповіщення — /stop.",
            replyMarkup: GetStatusKeyboard(),
            cancellationToken: ct);
    }

    private static async Task SendRegisteredUsersAsync(long chatId, string filter, CancellationToken ct)
    {
        KeyValuePair<long, Subscription>[] users = Subscriptions
            .Where(item => string.IsNullOrWhiteSpace(filter) ||
                           item.Value.Region.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                           item.Value.District.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                           item.Value.City.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Value.Region, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Value.City, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (users.Length == 0)
        {
            await Bot.SendMessage(chatId, "Підписок за цим запитом немає.", cancellationToken: ct);
            return;
        }

        const int maxMessageLength = 3500;
        string header = $"👥 Зареєстровані підписки: {users.Length}" +
                        (string.IsNullOrWhiteSpace(filter) ? "" : $" (фільтр: {filter})") + "\n\n";

        string chunk = header;
        foreach ((long id, Subscription subscription) in users)
        {
            string line = $"• ID {id} — {subscription.City}, {subscription.District}, {subscription.Region}" +
                          (DroneOptOut.ContainsKey(id) ? " 🔕" : "");

            if (chunk.Length + line.Length + 1 > maxMessageLength)
            {
                await Bot.SendMessage(chatId, chunk, cancellationToken: ct);
                chunk = "👥 Продовження списку\n\n";
            }

            chunk += line + "\n";
        }

        if (!string.IsNullOrWhiteSpace(chunk))
            await Bot.SendMessage(chatId, chunk, cancellationToken: ct);
    }

    private static async Task HandleCallbackAsync(
        CallbackQuery query,
        CancellationToken ct)
    {
        await Bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        if (query.Message is null || string.IsNullOrWhiteSpace(query.Data))
            return;

        long chatId = query.Message.Chat.Id;
        string data = query.Data;

        if (BannedUsers.ContainsKey(chatId))
            return;

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
                await Bot.SendMessage(
                    chatId,
                    $"Обрана область: {region}. Напиши назву свого району:",
                    cancellationToken: ct);

                AwaitingDistrictText[chatId] = 0;
            }
        }
        else if (data.StartsWith("district:", StringComparison.Ordinal))
        {
            if (!PendingRegions.TryGetValue(chatId, out string? region))
            {
                await Bot.SendMessage(chatId, "Сесія вибору застаріла. Натисни /start і обери область ще раз.", cancellationToken: ct);
                return;
            }

            string district = data["district:".Length..];
            PendingDistricts[chatId] = district;

            await SendCitiesAsync(chatId, region, district, ct);
        }
        else if (data.StartsWith("city:", StringComparison.Ordinal))
        {
            if (!PendingRegions.TryGetValue(chatId, out string? region) ||
                !PendingDistricts.TryGetValue(chatId, out string? district))
            {
                await Bot.SendMessage(chatId, "Сесія вибору застаріла. Натисни /start і обери область ще раз.", cancellationToken: ct);
                return;
            }

            string city = data["city:".Length..];

            if (city == ManualCityButton)
            {
                await Bot.SendMessage(chatId, "Напиши назву свого міста одним повідомленням:", cancellationToken: ct);
                return;
            }

            Subscriptions[chatId] = new Subscription(region, city, district);

            PendingRegions.TryRemove(chatId, out _);
            PendingDistricts.TryRemove(chatId, out _);

            SaveSubscriptions();

            await Bot.SendMessage(
                chatId,
                $"Готово. Ти підписаний на {city}, {district}, {region}.\n\n" +
                "Я повідомлю про зміну статусу повітряної тривоги. Перевіряй також офіційні джерела.",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);
        }
    }

    private static async Task SendDistrictsAsync(
        long chatId,
        string region,
        string[] districts,
        CancellationToken ct)
    {
        InlineKeyboardButton[][] rows = districts
            .Select(district => new[]
            {
                InlineKeyboardButton.WithCallbackData(district, $"district:{district}")
            })
            .ToArray();

        await Bot.SendMessage(
            chatId,
            $"Обрана область: {region}. Обери свій район:",
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    private static async Task SendCitiesAsync(
        long chatId,
        string region,
        string district,
        CancellationToken ct)
    {
        string[] cities = CitiesByDistrict.TryGetValue(district, out string[]? knownCities)
            ? knownCities
            : new[] { ManualCityButton };

        InlineKeyboardButton[][] rows = cities
            .Select(city => new[]
            {
                InlineKeyboardButton.WithCallbackData(city, $"city:{city}")
            })
            .ToArray();

        await Bot.SendMessage(
            chatId,
            $"Район: {district}. Обери своє місто або введи його вручну:",
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    private static async Task SendRegionsAsync(long chatId, CancellationToken ct)
    {
        InlineKeyboardButton[][] rows = Regions
            .Select((region, index) => new { region, index })
            .GroupBy(x => x.index / 2)
            .Select(group => group
                .Select(x => InlineKeyboardButton.WithCallbackData(
                    x.region,
                    $"region:{x.region}"))
                .ToArray())
            .ToArray();

        await Bot.SendMessage(
            chatId,
            "Привіт! Це бот цивільних сповіщень про повітряні тривоги.\n\nОбери область:",
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    private static Task SendHelpAsync(long chatId, bool isAdmin, CancellationToken ct)
    {
        string text =
            "/start — вибрати область і місто\n" +
            "/status — поточний статус\n" +
            "/refresh — оновити статус зараз\n" +
            "/alarm — є тривога?\n" +
            "/drones — БпЛА поруч (on/off — сповіщення)\n" +
            "/subscription — моя підписка\n" +
            "/cancel — скасувати налаштування\n" +
            "/history — історія тривог\n" +
            "/updates — останні оновлення\n" +
            "ℹ️ Інфо — інформація про адмінів\n" +
            "/help — допомога\n\n" +
            "Рівні небезпеки: 🔴 червоний — ракети / балістика; 🟡 жовтий — дрони (ударні, реактивні, FPV); 🟢 зелений — спокійно.\n\n" +
            "Повідомлення можуть затримуватися. У небезпеці користуйся офіційними каналами цивільного захисту.";

        if (isAdmin)
            text += "\n\n" + AdminHelpText;

        return Bot.SendMessage(chatId, text, cancellationToken: ct);
    }

    private const string AdminHelpText =
        "👑 Команди адміністратора:\n" +
        "/stats — статистика бота\n" +
        "/users [фільтр] — список підписок (за областю/містом)\n" +
        "/broadcast <текст> — розсилка всім підписникам\n" +
        "/maintenance [текст] — повідомлення про техроботи\n" +
        "/sendto <id> <текст> — написати користувачу\n" +
        "/userinfo <id> — інформація про користувача\n" +
        "/ban <id> · /unban <id> · /banned — блокування\n" +
        "/drone <текст> — ручне повідомлення про БпЛА (піде тим, хто поруч)\n" +
        "/export — вивантажити bot-data.json\n" +
        "/adminhelp — ця довідка";

    private static ReplyKeyboardMarkup GetStatusKeyboard() => new(new[]
    {
        new KeyboardButton[] { "🔄 Оновити", "Є тривога?", "Історія" },
        new KeyboardButton[] { "📍 Моя підписка", "/status", "ℹ️ Інфо" },
        new KeyboardButton[] { "🛩 БпЛА поруч", "/stop" }
    })
    {
        ResizeKeyboard = true,
        IsPersistent = true
    };

    private static async Task SendCityHistoryAsync(long chatId, CancellationToken ct)
    {
        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(
                chatId,
                "Спочатку обери область і місто через /start.",
                cancellationToken: ct);
            return;
        }

        List<AlertHistoryEntry> entries;
        lock (DataLock)
        {
            entries = AlertHistory
                .Where(item => HistoryEntryMatches(item, subscription))
                .OrderByDescending(item => item.StartedAt)
                .Take(10)
                .ToList();
        }

        if (entries.Count == 0)
        {
            await Bot.SendMessage(
                chatId,
                $"📚 Історія для {subscription.City} поки порожня.\n\n" +
                "Бот почне записувати тривоги й відбої, щойно побачить зміну статусу. " +
                "Записи зберігаються локально у bot-data.json.",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);
            return;
        }

        List<string> lines = new()
        {
            $"📚 ІСТОРІЯ ТРИВОГ — {subscription.City}",
            $"{subscription.District}, {subscription.Region}",
            "Останні 10 подій, зафіксованих ботом:",
            ""
        };

        foreach (AlertHistoryEntry entry in entries)
        {
            DateTimeOffset finishedAt = entry.FinishedAt ?? DateTimeOffset.UtcNow;
            TimeSpan duration = finishedAt - entry.StartedAt;
            string durationText = FormatHistoryDuration(duration);
            string scopeLabel = entry.Scope switch
            {
                "місто" => "місто/громада",
                "район" => "район",
                _ => "область"
            };

            lines.Add(
                $"{(entry.FinishedAt.HasValue ? "✅" : "🚨")} {FormatUpdateTime(entry.StartedAt.ToString("O"))}\n" +
                $"{(entry.FinishedAt.HasValue ? "Відбій" : "Триває зараз")} · {durationText}\n" +
                $"Рівень запису: {scopeLabel}\n");
        }

        lines.Add("Дані накопичуються ботом із моменту запуску цієї версії.");

        await Bot.SendMessage(
            chatId,
            string.Join("\n", lines),
            replyMarkup: GetStatusKeyboard(),
            cancellationToken: ct);
    }

    private static bool HistoryEntryMatches(AlertHistoryEntry entry, Subscription subscription)
    {
        if (!IsSameRegion(entry.Region, subscription.Region))
            return false;

        return entry.Scope switch
        {
            "місто" => string.Equals(entry.City, subscription.City, StringComparison.OrdinalIgnoreCase),
            "район" => string.Equals(entry.District, subscription.District, StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string FormatHistoryDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            return "невідомо";
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays} дн. {duration.Hours} год.";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} год. {duration.Minutes} хв.";
        return $"{Math.Max(0, duration.Minutes)} хв.";
    }
    private static async Task SendAlarmStatusAsync(long chatId, CancellationToken ct)
    {
        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(
                chatId,
                "Спочатку обери область командою /start.",
                cancellationToken: ct);
            return;
        }

        AlertState? state = await TryGetAlertStateAsync(chatId, subscription, ct);
        if (state is null)
            return;

        await Bot.SendMessage(
            chatId,
            FormatAlarmStatus(state, subscription) + GetDroneSuffix(subscription),
            replyMarkup: GetStatusKeyboard(),
            cancellationToken: ct);
    }

    private static async Task<AlertState?> TryGetAlertStateAsync(
        long chatId,
        Subscription subscription,
        CancellationToken ct)
    {
        try
        {
            return await GetAlertStateAsync(subscription, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Console.WriteLine($"Не вдалося отримати статус: {ex.Message}");

            await Bot.SendMessage(
                chatId,
                "⚠️ Джерело даних про тривоги тимчасово недоступне. Спробуй ще раз за хвилину та перевір офіційні джерела.",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);

            return null;
        }
    }

    private static async Task SendStatusAsync(long chatId, CancellationToken ct)
    {
        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(
                chatId,
                "Спочатку обери область командою /start.",
                cancellationToken: ct);
            return;
        }

        AlertState? state = await TryGetAlertStateAsync(chatId, subscription, ct);
        if (state is null)
            return;

        StatusPresses.TryGetValue(chatId, out int presses);

        if (presses >= 10)
        {
            StatusPresses.TryRemove(chatId, out _);

            await Bot.SendMessage(
                chatId,
                "🏆 Секретне досягнення!\nТи як Данило — тобі нічого робити?.. 😄",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);
            return;
        }

        await Bot.SendMessage(
            chatId,
            FormatAlert(state, subscription) + GetDroneSuffix(subscription),
            replyMarkup: GetStatusKeyboard(),
            cancellationToken: ct);
    }

    private static async Task AlertPollingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                foreach (IGrouping<string, KeyValuePair<long, Subscription>> group
                         in Subscriptions.ToArray().GroupBy(x => GetSubscriptionStateKey(x.Value)))
                {
                    try
                    {
                        Subscription subscription = group.First().Value;
                        AlertState state = await GetAlertStateAsync(subscription, ct);
                        string stateKey = GetSubscriptionStateKey(subscription);

                        if (!state.HasData)
                            continue;

                        if (!LastStates.TryGetValue(stateKey, out AlertState? previous))
                        {
                            LastStates[stateKey] = state;
                            ReconcileHistoryOnStartup(stateKey, subscription, state);
                            continue;
                        }

                        bool changed = previous.IsAlert != state.IsAlert;
                        bool escalated = !changed && state.IsAlert &&
                                         !string.Equals(previous.Scope, state.Scope, StringComparison.Ordinal);

                        LastStates[stateKey] = state;

                        if (changed)
                            RecordAlertTransition(stateKey, subscription, state);

                        if (!changed && !escalated)
                            continue;

                        string prefix = escalated ? "⬆️ Рівень небезпеки змінився\n" : "";

                        foreach (KeyValuePair<long, Subscription> item in group.ToArray())
                        {
                            if (BannedUsers.ContainsKey(item.Key))
                                continue;

                            await SafeSendAsync(
                                item.Key,
                                prefix + FormatAlert(state, item.Value) + GetDroneSuffix(item.Value),
                                ct);

                            await Task.Delay(40, ct);
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Джерело тривог недоступне: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка циклу тривог: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static void ReconcileHistoryOnStartup(
        string stateKey,
        Subscription subscription,
        AlertState state)
    {
        bool changed = false;
        lock (DataLock)
        {
            if (state.IsAlert)
            {
                if (!AlertHistory.Any(item => item.StateKey == stateKey && !item.FinishedAt.HasValue))
                {
                    AlertHistory.Add(new AlertHistoryEntry(
                        stateKey,
                        subscription.Region,
                        subscription.District,
                        subscription.City,
                        GetHistoryTimestamp(state.UpdatedAt),
                        null,
                        state.Scope));
                    changed = true;
                }
            }
            else
            {
                changed = CloseOpenHistoryEntry(stateKey, state.UpdatedAt);
            }

            TrimAlertHistory();
        }

        if (changed)
            SaveSubscriptions();
    }

    private static void RecordAlertTransition(
        string stateKey,
        Subscription subscription,
        AlertState state)
    {
        lock (DataLock)
        {
            if (state.IsAlert)
            {
                AlertHistory.Add(new AlertHistoryEntry(
                    stateKey,
                    subscription.Region,
                    subscription.District,
                    subscription.City,
                    GetHistoryTimestamp(state.UpdatedAt),
                    null,
                    state.Scope));
            }
            else
            {
                CloseOpenHistoryEntry(stateKey, state.UpdatedAt);
            }

            TrimAlertHistory();
        }

        SaveSubscriptions();
    }

    private static bool CloseOpenHistoryEntry(string stateKey, string sourceTime)
    {
        int index = AlertHistory.FindLastIndex(item =>
            item.StateKey == stateKey && !item.FinishedAt.HasValue);

        if (index < 0)
            return false;

        AlertHistoryEntry entry = AlertHistory[index];
        DateTimeOffset finishedAt = GetHistoryTimestamp(sourceTime);
        if (finishedAt < entry.StartedAt)
            finishedAt = DateTimeOffset.UtcNow;

        AlertHistory[index] = entry with { FinishedAt = finishedAt };
        return true;
    }

    private static DateTimeOffset GetHistoryTimestamp(string sourceTime)
    {
        return TryParseSourceTime(sourceTime, out DateTimeOffset timestamp)
            ? timestamp
            : DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Єдиний парсер часу джерела: якщо є зсув (Z, +02:00) — використовуємо його,
    /// якщо часу без зони — вважаємо, що це київський час. Повертає UTC.
    /// </summary>
    private static bool TryParseSourceTime(string? value, out DateTimeOffset result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();

        if (value.Contains(':') && ExplicitOffsetRegex.IsMatch(value) &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset withOffset))
        {
            result = withOffset.ToUniversalTime();
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime local))
        {
            DateTime unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            result = new DateTimeOffset(unspecified, KyivTimeZone.GetUtcOffset(unspecified)).ToUniversalTime();
            return true;
        }

        return false;
    }

    private static void TrimAlertHistory()
    {
        const int maxEntries = 5000;
        if (AlertHistory.Count <= maxEntries)
            return;

        int excess = AlertHistory.Count - maxEntries;
        List<AlertHistoryEntry> oldestFinished = AlertHistory
            .Where(item => item.FinishedAt.HasValue)
            .OrderBy(item => item.FinishedAt)
            .Take(excess)
            .ToList();
        foreach (AlertHistoryEntry item in oldestFinished)
            AlertHistory.Remove(item);

        if (AlertHistory.Count > maxEntries)
        {
            AlertHistory.Sort((left, right) => left.StartedAt.CompareTo(right.StartedAt));
            AlertHistory.RemoveRange(0, AlertHistory.Count - maxEntries);
        }
    }

    private static string GetSubscriptionStateKey(Subscription subscription)
    {
        if (IsAlertsInUa(ActiveSourceUrl) || IsUkraineAlarm(ActiveSourceUrl))
            return $"{subscription.Region}|{subscription.District}|{subscription.City}";

        return subscription.Region;
    }

    private static async Task<AlertState> GetAlertStateAsync(
        Subscription subscription,
        CancellationToken ct)
    {
        string region = subscription.Region;
        string json = await GetAlertsJsonAsync(ct);
        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        if (IsUkraineAlarm(ActiveSourceUrl) && root.ValueKind == JsonValueKind.Array)
            return ParseUkraineAlarm(root, subscription);

        JsonElement records =
            root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("states", out JsonElement states)
                ? states
                : root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.ValueKind == JsonValueKind.Object &&
                      root.TryGetProperty("alerts", out JsonElement alerts)
                        ? alerts
                        : default;
        bool isActiveAlertsFeed =
            root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("alerts", out JsonElement activeAlerts) &&
            activeAlerts.ValueKind == JsonValueKind.Array;

        if (records.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in records.EnumerateObject())
            {
                if (!IsSameRegion(property.Name, region))
                    continue;

                if (property.Value.ValueKind == JsonValueKind.True ||
                    property.Value.ValueKind == JsonValueKind.False)
                {
                    return new AlertState(property.Value.GetBoolean(), "", HasData: true, Scope: "область");
                }

                if (property.Value.ValueKind != JsonValueKind.Object)
                    continue;

                bool? alertStatus = ReadBoolean(property.Value, "enabled", "alertnow", "alert");
                if (alertStatus is null)
                    continue;

                string changed = alertStatus.Value
                    ? ReadString(property.Value, "enabled_at", "changed", "updated_at", "updatedAt")
                    : ReadString(property.Value, "disabled_at", "changed", "updated_at", "updatedAt");

                string level = ReadString(
                    property.Value,
                    "level",
                    "alert_level",
                    "danger_level",
                    "color",
                    "severity");

                return new AlertState(alertStatus.Value, changed, level, Scope: "область");
            }

            return new AlertState(false, "", HasData: false);
        }

        if (records.ValueKind != JsonValueKind.Array)
            return new AlertState(false, "", HasData: false);

        foreach (JsonElement item in records.EnumerateArray())
        {
            string alertType = ReadString(item, "alert_type");
            if (isActiveAlertsFeed &&
                !string.IsNullOrWhiteSpace(alertType) &&
                !alertType.Equals("air_raid", StringComparison.OrdinalIgnoreCase))
                continue;

            string title = ReadString(
                item,
                "location_title",
                "region",
                "location",
                "name");

            string oblast = ReadString(item, "location_oblast");
            if (isActiveAlertsFeed)
            {
                if (!TryMatchActiveAlert(item, subscription, out string scope))
                    continue;

                string activeStart = ReadString(item, "started_at", "updated_at", "updatedAt");
                string activeLevel = ReadString(item, "alert_level", "level", "color");
                return new AlertState(true, activeStart, activeLevel, Scope: scope);
            }

            if (!IsSameRegion(title, region) && !IsSameRegion(oblast, region))
                continue;

            string updatedAt = ReadString(
                item,
                "last_update",
                "updated_at",
                "updatedAt",
                "started_at");

            string level = ReadString(
                item,
                "level",
                "alert_level",
                "color");

            return new AlertState(true, updatedAt, level, Scope: "область");
        }

        return new AlertState(
            false,
            "",
            HasData: isActiveAlertsFeed,
            Scope: isActiveAlertsFeed ? "місто" : "область");
    }

    private static bool TryMatchActiveAlert(
        JsonElement alert,
        Subscription subscription,
        out string scope) =>
        TryMatchLocation(
            ReadString(alert, "location_title", "location", "name"),
            ReadString(alert, "location_type"),
            ReadString(alert, "location_oblast"),
            ReadString(alert, "location_raion"),
            subscription,
            out scope);

    /// <summary>Розбір відповіді Ukraine Alarm API v3: масив регіонів з activeAlerts (State / District / Community).</summary>
    private static AlertState ParseUkraineAlarm(JsonElement root, Subscription subscription)
    {
        string? bestScope = null;
        string bestTime = "";

        foreach (JsonElement region in root.EnumerateArray())
        {
            if (region.ValueKind != JsonValueKind.Object)
                continue;

            // Беремо лише повітряні тривоги (AIR); артилерія / вуличні бої / хімія не рахуються.
            string since = "";
            bool hasAir = false;

            if (region.TryGetProperty("activeAlerts", out JsonElement active) &&
                active.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement alert in active.EnumerateArray())
                {
                    if (!ReadString(alert, "type").Equals("AIR", StringComparison.OrdinalIgnoreCase))
                        continue;

                    hasAir = true;
                    since = ReadString(alert, "lastUpdate");
                    break;
                }
            }

            if (!hasAir)
                continue;

            string name = ReadString(region, "regionName");
            string kind = ReadString(region, "regionType");

            string type = kind.ToLowerInvariant() switch
            {
                "state" => "oblast",
                "district" => "raion",
                "community" => "hromada",
                _ => ""
            };

            // Для областей передаємо назву як oblast, щоб тривога чужої області не збігалась з нашою.
            string oblast = type == "oblast" ? name : "";

            if (!TryMatchLocation(name, type, oblast, "", subscription, out string scope))
                continue;

            if (scope == "область" && subscription.Region.StartsWith("м. Київ", StringComparison.OrdinalIgnoreCase))
                scope = "місто";

            if (bestScope is null || ScopeRank(scope) > ScopeRank(bestScope))
            {
                bestScope = scope;
                bestTime = since;
            }
        }

        return bestScope is null
            ? new AlertState(false, "", HasData: true, Scope: "місто")
            : new AlertState(true, bestTime, "", Scope: bestScope);
    }

    private static int ScopeRank(string scope) => scope switch
    {
        "місто" => 3,
        "район" => 2,
        _ => 1
    };

    private static bool TryMatchLocation(
        string title,
        string type,
        string oblast,
        string raion,
        Subscription subscription,
        out string scope)
    {

        if (!string.IsNullOrWhiteSpace(oblast) &&
            !IsSameRegion(oblast, subscription.Region))
        {
            scope = "місто";
            return false;
        }

        string normalizedTitle = NormalizeLocationName(title);
        string normalizedCity = NormalizeLocationName(subscription.City);
        string normalizedDistrict = NormalizeLocationName(subscription.District);
        string normalizedRaion = NormalizeLocationName(raion);

        bool exactCityMatch =
            normalizedTitle == normalizedCity ||
            normalizedTitle.StartsWith("місто" + normalizedCity, StringComparison.Ordinal) ||
            normalizedTitle.StartsWith("м" + normalizedCity, StringComparison.Ordinal);

        bool communityMatch =
            type.Equals("hromada", StringComparison.OrdinalIgnoreCase) &&
            GetCityNameRoots(normalizedCity).Any(root => normalizedTitle.StartsWith(root, StringComparison.Ordinal));

        if (exactCityMatch || communityMatch)
        {
            scope = "місто";
            return true;
        }

        bool districtMatch =
            (type.Equals("raion", StringComparison.OrdinalIgnoreCase) ||
             type.Equals("district", StringComparison.OrdinalIgnoreCase)) &&
            (normalizedTitle == normalizedDistrict || normalizedRaion == normalizedDistrict);

        if (districtMatch)
        {
            scope = "район";
            return true;
        }

        bool oblastMatch =
            type.Equals("oblast", StringComparison.OrdinalIgnoreCase) ||
            IsSameRegion(title, subscription.Region);

        if (oblastMatch &&
            (string.IsNullOrWhiteSpace(oblast) || IsSameRegion(oblast, subscription.Region)))
        {
            scope = "область";
            return true;
        }

        scope = "місто";
        return false;
    }

    private static IEnumerable<string> GetCityNameRoots(string city)
    {
        yield return city;

        string[] suffixes = { "ка", "ія", "я", "а", "о", "е", "ий", "ій", "ь" };
        foreach (string suffix in suffixes)
        {
            if (!city.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            string root = city[..^suffix.Length];
            if (root.Length >= 4)
                yield return root;
        }
    }

    private static string NormalizeLocationName(string value)
    {
        string normalized = value
            .Replace("'", "")
            .Replace("’", "")
            .Replace("ʼ", "")
            .Replace("територіальна громада", "", StringComparison.OrdinalIgnoreCase)
            .Replace("громада", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" район", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" область", "", StringComparison.OrdinalIgnoreCase)
            .Replace("місто", "", StringComparison.OrdinalIgnoreCase)
            .Replace("м. ", "", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    private static bool IsUkraineAlarm(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               uri.Host.Equals("api.ukrainealarm.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAlertsInUa(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               uri.Host.Equals("api.alerts.in.ua", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetAlertsJsonAsync(CancellationToken ct)
    {
        await AlertsJsonLock.WaitAsync(ct);
        try
        {
            if (CachedAlertsJson is not null &&
                DateTimeOffset.UtcNow - AlertsJsonFetchedAt < TimeSpan.FromSeconds(30))
            {
                return CachedAlertsJson;
            }

            // Основне джерело (з ключем), а якщо воно лягло / ключ не підійшов — резервне без ключа.
            List<string> urls = new() { AlertsUrl };
            if (!AlertsUrl.Equals(DefaultAlertsUrl, StringComparison.OrdinalIgnoreCase))
                urls.Add(DefaultAlertsUrl);

            Exception? lastError = null;

            foreach (string url in urls)
            {
                try
                {
                    using HttpRequestMessage request = new(HttpMethod.Get, url);

                    if (!string.IsNullOrWhiteSpace(AlertsApiToken) && IsAlertsInUa(url))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AlertsApiToken);

                    // Ukraine Alarm: ключ передається в заголовку Authorization БЕЗ слова "Bearer".
                    if (!string.IsNullOrWhiteSpace(UkraineAlarmToken) && IsUkraineAlarm(url))
                        request.Headers.TryAddWithoutValidation("Authorization", UkraineAlarmToken);

                    using HttpResponseMessage response = await Http.SendAsync(request, ct);
                    response.EnsureSuccessStatusCode();

                    CachedAlertsJson = await response.Content.ReadAsStringAsync(ct);
                    AlertsJsonFetchedAt = DateTimeOffset.UtcNow;
                    ActiveSourceUrl = url;
                    return CachedAlertsJson;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    lastError = ex;
                    string host = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.Host : url;
                    Console.WriteLine($"Джерело {host} недоступне: {ex.Message}");
                }
            }

            throw lastError ?? new InvalidOperationException("Немає доступного джерела тривог.");
        }
        finally
        {
            AlertsJsonLock.Release();
        }
    }

    private static bool? ReadBoolean(JsonElement item, params string[] names)
    {
        foreach (string name in names)
        {
            if (item.TryGetProperty(name, out JsonElement value))
            {
                if (value.ValueKind == JsonValueKind.True)
                    return true;
                if (value.ValueKind == JsonValueKind.False)
                    return false;
            }
        }

        return null;
    }

    private static string ReadString(JsonElement item, params string[] names)
    {
        foreach (string name in names)
        {
            if (item.TryGetProperty(name, out JsonElement value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }
        }

        return "";
    }

    private static bool IsSameRegion(string title, string region)
    {
        if (string.Equals(title, region, StringComparison.OrdinalIgnoreCase))
            return true;

        string normalizedTitle = title
            .Replace(" область", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        string normalizedRegion = region
            .Replace(" область", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return string.Equals(
            normalizedTitle,
            normalizedRegion,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAlert(AlertState state, Subscription subscription)
    {
        string region = subscription.Region;
        if (!state.HasData)
        {
            return $"⚠️ Немає актуальних даних про статус тривоги.\n" +
                   $"Регіон: {region}\n\n" +
                   "Перевір офіційні джерела цивільного захисту.";
        }

        string place = GetAlertScopeText(state, subscription);

        return state.IsAlert
            ? $"{(state.Scope == "область" ? "⚠️ ТРИВОГА В ОБЛАСТІ" : "🚨 ТРИВОГА")}\n{FormatAlertLevel(state, subscription)}\n" +
              $"{place}\n" +
              $"Початок: {FormatTime(state.UpdatedAt)}\n" +
              $"Триває: {FormatDuration(state.UpdatedAt)}\n\n" +
              (state.Scope == "область"
                  ? "Джерело не підтверджує окремо статус твого міста. Перевір місцеві офіційні повідомлення.\n"
                  : "Негайно пройди до укриття та стеж за офіційними повідомленнями.\n") +
              GetAlertsSourceText()
            : $"✅ Відбій повітряної тривоги\n" +
              $"{FormatAlertLevel(state, subscription)}\n" +
              $"{place}\n" +
              $"Час відбою: {FormatTime(state.UpdatedAt)}\n" +
              $"Відбій триває: {FormatDuration(state.UpdatedAt)}\n\n" +
              (state.Scope == "область"
                  ? "API не надає окремий статус твого міста; перевір місцеві офіційні повідомлення.\n"
                  : "Все одно перевіряй офіційні джерела.\n") +
              GetAlertsSourceText();
    }

    private static string FormatAlarmStatus(AlertState state, Subscription subscription)
    {
        string region = subscription.Region;
        if (!state.HasData)
        {
            return $"⚠️ Немає актуальних даних про статус тривоги.\n" +
                   $"Регіон: {region}\n\n" +
                   "Перевір офіційні джерела цивільного захисту.\n" +
                   GetAlertsSourceText();
        }

        string place = GetAlertScopeText(state, subscription);

        return state.IsAlert
            ? $"{(state.Scope == "область" ? "⚠️ В області зафіксована тривога" : "🚨 Так, на локації є тривога!")}\n{FormatAlertLevel(state, subscription)}\n" +
              $"{place}\n" +
              $"Початок: {FormatTime(state.UpdatedAt)}\n" +
              $"Триває: {FormatDuration(state.UpdatedAt)}\n\n" +
              (state.Scope == "область"
                  ? "Статус міста окремо не підтверджений джерелом. Перевір місцеві офіційні повідомлення.\n"
                  : "Пройди до укриття та стеж за офіційними повідомленнями.\n") +
              GetAlertsSourceText()
            : $"✅ Зараз активної повітряної тривоги не зафіксовано.\n" +
              $"{FormatAlertLevel(state, subscription)}\n" +
              $"{place}\n" +
              $"Час відбою: {FormatTime(state.UpdatedAt)}\n" +
              $"Відбій триває: {FormatDuration(state.UpdatedAt)}\n\n" +
              (state.Scope == "область"
                  ? "Джерело не передає статус міста окремо; перевір місцеві офіційні повідомлення.\n"
                  : "Інформація може змінитися — перевіряй офіційні джерела.\n") +
              GetAlertsSourceText();
    }

    private static string GetAlertScopeText(AlertState state, Subscription subscription)
    {
        return state.Scope switch
        {
            "місто" => $"Місто/громада: {subscription.City}, {subscription.Region}",
            "район" => $"Район: {subscription.District}, {subscription.Region}",
            _ => $"Область: {subscription.Region} (місто окремо не перевірено)"
        };
    }

    private static string GetAlertsSourceText()
    {
        if (!Uri.TryCreate(ActiveSourceUrl, UriKind.Absolute, out Uri? uri))
            return "Джерело: API статусів тривог";

        string source = uri.Host.Equals("api.ukrainealarm.com", StringComparison.OrdinalIgnoreCase)
            ? "Ukraine Alarm (ukrainealarm.com)"
            : uri.Host.Equals("api.alerts.in.ua", StringComparison.OrdinalIgnoreCase)
            ? "alerts.in.ua"
            : uri.Host.Equals("ubilling.net.ua", StringComparison.OrdinalIgnoreCase)
                ? "UBilling (статуси по областях)"
            : uri.Host.Equals("jaam.net.ua", StringComparison.OrdinalIgnoreCase)
                ? "JAAM (jaam.net.ua)"
                : uri.Host;

        return $"Дані: {source}";
    }

    /// <summary>
    /// Рівні: 🔴 червоний — ракети / балістика; 🟡 жовтий — дрони (ударні, реактивні, FPV);
    /// 🟢 зелений — тривоги немає. Тип загрози береться зі свіжих повідомлень каналів моніторингу.
    /// </summary>
    private static string FormatAlertLevel(AlertState state, Subscription subscription)
    {
        if (!state.IsAlert)
            return "🟢 ЗЕЛЕНИЙ РІВЕНЬ — тривоги немає";

        return GetRecentThreat(subscription, 30) switch
        {
            ThreatKind.Missile => "🔴 ЧЕРВОНИЙ РІВЕНЬ — ракетна загроза / балістика",
            ThreatKind.Drone => "🟡 ЖОВТИЙ РІВЕНЬ — загроза БпЛА (дрони, реактивні, FPV)",
            _ => "🚨 Повітряна тривога — тип загрози не уточнено (немає свіжих повідомлень моніторингу)"
        };
    }

    private static string? FormatSourceLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return null;

        if (level.Contains("помаранч", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("оранж", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("orange", StringComparison.OrdinalIgnoreCase))
            return "🟠 ПОМАРАНЧЕВИЙ РІВЕНЬ НЕБЕЗПЕКИ";

        if (level.Contains("жовт", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("yellow", StringComparison.OrdinalIgnoreCase))
            return "🟡 ЖОВТИЙ РІВЕНЬ НЕБЕЗПЕКИ";

        if (level.Contains("червон", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("red", StringComparison.OrdinalIgnoreCase))
            return "🔴 ЧЕРВОНИЙ РІВЕНЬ НЕБЕЗПЕКИ";

        if (level.Contains("зел", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("green", StringComparison.OrdinalIgnoreCase))
            return "🟢 ЗЕЛЕНИЙ РІВЕНЬ НЕБЕЗПЕКИ";

        if (level.Contains("висок", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("high", StringComparison.OrdinalIgnoreCase) ||
            level.Contains("critical", StringComparison.OrdinalIgnoreCase))
            return "🔴 ВИСОКИЙ РІВЕНЬ НЕБЕЗПЕКИ";

        return null;
    }

    private static string FormatTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "невідомий (джерело не передало час)";

        if (!TryParseSourceTime(value, out DateTimeOffset moment))
            return value;

        if (moment.Year <= 1971)
            return "невідомий (джерело передало технічну дату)";

        return $"{TimeZoneInfo.ConvertTime(moment, KyivTimeZone):dd.MM.yyyy HH:mm:ss} (Київ)";
    }

    private static string FormatUpdateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "невідомо";

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out DateTimeOffset date))
        {
            return $"{TimeZoneInfo.ConvertTime(date, KyivTimeZone):dd.MM.yyyy HH:mm:ss} (Київ)";
        }

        return value;
    }

    private static string FormatDuration(string value)
    {
        if (!TryParseSourceTime(value, out DateTimeOffset startedAt) ||
            startedAt.Year <= 1971)
        {
            return "невідомо (джерело не передало коректний час)";
        }

        TimeSpan duration = DateTimeOffset.UtcNow - startedAt.ToUniversalTime();

        if (duration < TimeSpan.Zero)
            return "невідомо (час джерела попереду поточного часу)";

        return duration.TotalDays >= 1
            ? $"{(int)duration.TotalDays} дн. {duration.Hours} год. {duration.Minutes} хв."
            : duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours} год. {duration.Minutes} хв."
                : $"{duration.Minutes} хв. {duration.Seconds} сек.";
    }

    private static bool TryParseAlertTime(
        string value,
        out DateTimeOffset result)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out result))
        {
            return true;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.CurrentCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
            out result);
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

    // ======================= БпЛА / шахеди поруч =======================

    private static string Truncate(string value, int max)
    {
        return value.Length <= max ? value : value[..max].TrimEnd() + "…";
    }

    private static string FormatClock(DateTimeOffset moment)
    {
        return TimeZoneInfo.ConvertTime(moment, KyivTimeZone).ToString("HH:mm");
    }

    private static async Task HandleChannelPostAsync(Message post, CancellationToken ct)
    {
        string? text = post.Text ?? post.Caption;
        if (string.IsNullOrWhiteSpace(text))
            return;

        long channelId = post.Chat.Id;

        // Тільки канали зі списку: інакше будь-хто зміг би додати бота у свій канал і розсилати фейки.
        if (!DroneChannelIds.Contains(channelId))
        {
            Console.WriteLine(
                $"Пост із каналу {channelId} ({post.Chat.Title}) проігноровано. " +
                "Щоб приймати такі пости — додай цей ID у змінну DRONE_CHANNEL_IDS.");
            return;
        }

        await ProcessDroneReportAsync(text, post.Chat.Title ?? "канал моніторингу", ct);
    }

    private enum ThreatKind { None = 0, Drone = 1, Missile = 2 }

    private static ThreatKind ClassifyThreat(string text)
    {
        string lower = text.ToLowerInvariant();

        if (MissileKeywords.Any(keyword => lower.Contains(keyword)))
            return ThreatKind.Missile;

        if (JetDroneRegex.IsMatch(lower) || DroneKeywords.Any(keyword => lower.Contains(keyword)))
            return ThreatKind.Drone;

        return ThreatKind.None;
    }

    private static bool IsDroneText(string text) => ClassifyThreat(text) != ThreatKind.None;

    /// <summary>
    /// Чи стосується повідомлення локації підписника та який там тип загрози.
    /// Довгі пости ділимо на рядки/речення, щоб "ракети на Київщину" не давали червоний рівень харків'янам.
    /// </summary>
    private static bool TryMatchThreat(string text, Subscription subscription, out string scope, out ThreatKind threat)
    {
        scope = "";
        threat = ThreatKind.None;

        ThreatKind whole = ClassifyThreat(text);
        if (whole == ThreatKind.None)
            return false;

        string? bestScope = null;
        ThreatKind bestThreat = ThreatKind.None;

        foreach (string segment in Regex.Split(text, @"[\r\n]+|(?<=[.!?;])\s+"))
        {
            if (string.IsNullOrWhiteSpace(segment))
                continue;

            string? segmentScope = MatchDroneScope(segment, subscription);
            if (segmentScope is null)
                continue;

            ThreatKind segmentThreat = ClassifyThreat(segment);
            if (segmentThreat == ThreatKind.None)
                segmentThreat = whole;

            if (bestScope is null || ScopeRank(segmentScope) > ScopeRank(bestScope))
                bestScope = segmentScope;

            if (segmentThreat > bestThreat)
                bestThreat = segmentThreat;
        }

        if (bestScope is null)
        {
            bestScope = MatchDroneScope(text, subscription);
            if (bestScope is null)
                return false;

            bestThreat = whole;
        }

        scope = bestScope;
        threat = bestThreat;
        return true;
    }

    private static ThreatKind GetRecentThreat(Subscription subscription, int minutes)
    {
        List<DroneMatch> recent = GetRecentDroneReports(subscription, minutes);
        return recent.Count == 0 ? ThreatKind.None : recent.Max(item => item.Threat);
    }

    private static async Task<int> ProcessDroneReportAsync(string text, string source, CancellationToken ct)
    {
        if (!IsDroneText(text))
            return 0;

        string lower = text.ToLowerInvariant();
        if (DroneClearKeywords.Any(keyword => lower.Contains(keyword)))
            return 0;

        lock (DataLock)
        {
            DroneReports.Add(new DroneReport(DateTimeOffset.UtcNow, Truncate(text.Trim(), 400), source));
            DroneReports.RemoveAll(item => DateTimeOffset.UtcNow - item.At > TimeSpan.FromHours(3));

            if (DroneReports.Count > 200)
                DroneReports.RemoveRange(0, DroneReports.Count - 200);
        }

        int sent = 0;

        foreach (KeyValuePair<long, Subscription> item in Subscriptions.ToArray())
        {
            ct.ThrowIfCancellationRequested();

            long chatId = item.Key;
            if (BannedUsers.ContainsKey(chatId) || DroneOptOut.ContainsKey(chatId))
                continue;

            if (!TryMatchThreat(text, item.Value, out string scope, out ThreatKind threat))
                continue;

            // Ліміт 2 хв окремо для жовтих і червоних, щоб червоний не губився за щойно надісланим жовтим.
            string notifyKey = $"{chatId}:{(int)threat}";

            if (LastDroneNotify.TryGetValue(notifyKey, out DateTimeOffset last) &&
                DateTimeOffset.UtcNow - last < TimeSpan.FromMinutes(2))
                continue;

            LastDroneNotify[notifyKey] = DateTimeOffset.UtcNow;

            if (await SafeSendAsync(chatId, FormatDroneAlert(scope, threat, item.Value, text, source), ct))
                sent++;

            await Task.Delay(40, ct);
        }

        return sent;
    }

    private static string FormatDroneAlert(
        string scope,
        ThreatKind threat,
        Subscription subscription,
        string text,
        string source)
    {
        string place = scope switch
        {
            "місто" => $"поруч із містом {subscription.City}",
            "район" => $"у районі: {subscription.District}",
            _ => $"в області: {subscription.Region}"
        };

        string header = threat == ThreatKind.Missile
            ? $"🔴 ЧЕРВОНИЙ РІВЕНЬ — ракетна загроза / балістика {place}!"
            : $"🟡 ЖОВТИЙ РІВЕНЬ — загроза БпЛА (дрони, реактивні, FPV) {place}";

        return header + "\n\n" +
               $"{(threat == ThreatKind.Missile ? "🚀" : "🛩")} «{Truncate(text.Trim(), 350)}»\n\n" +
               $"Джерело: {source} (неофіційні дані, можливі неточності).\n" +
               $"Твоя локація: {subscription.City}, {subscription.District}.\n" +
               "Слідкуй за офіційними повідомленнями; при оголошенні тривоги — негайно в укриття.\n\n" +
               "Вимкнути такі сповіщення: /drones off";
    }

    private static string[] SplitWords(string text)
    {
        string cleaned = text.ToLowerInvariant()
            .Replace("'", "")
            .Replace("’", "")
            .Replace("ʼ", "");

        return Regex.Split(cleaned, @"[^\p{L}\p{N}]+")
            .Where(word => word.Length > 0)
            .ToArray();
    }

    private static bool ContainsPlace(string[] words, IEnumerable<string> roots, int maxTail)
    {
        string[] rootList = roots.Where(root => root.Length >= 3).Distinct().ToArray();

        for (int i = 0; i < words.Length; i++)
        {
            string candidate = "";

            for (int span = 0; span < 3 && i + span < words.Length; span++)
            {
                candidate += words[i + span];

                foreach (string root in rootList)
                {
                    if (candidate.StartsWith(root, StringComparison.Ordinal) &&
                        candidate.Length - root.Length <= maxTail)
                        return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> GetDroneCityRoots(string city)
    {
        if (city.Length < 4)
            yield break;

        foreach (string root in GetCityNameRoots(city))
            yield return root;

        if (city.EndsWith("ів", StringComparison.Ordinal))
            yield return city[..^2] + "ов";

        if (city == "київ")
            yield return "києв";
    }

    /// <summary>Повертає "місто" / "район" / "область", якщо текст про БпЛА стосується локації підписника.</summary>
    private static string? MatchDroneScope(string text, Subscription subscription)
    {
        string[] words = SplitWords(text);
        if (words.Length == 0)
            return null;

        // 1) місто / громада. "Харківщина" або "Харківська" не вважаються містом: хвіст слова ≤ 3 літер.
        string city = NormalizeLocationName(subscription.City);
        if (ContainsPlace(words, GetDroneCityRoots(city), 3))
            return "місто";

        // 2) район
        string district = NormalizeLocationName(subscription.District);
        if (district.Length >= 6 && district.EndsWith("ий", StringComparison.Ordinal))
        {
            string baseStem = district[..^2];
            string[] forms = { baseStem + "ий", baseStem + "ого", baseStem + "ому", baseStem + "им", baseStem + "ім" };

            if (words.Any(word => forms.Contains(word)))
                return "район";

            if (district.EndsWith("ький", StringComparison.Ordinal) && district.Length > 8)
            {
                string core = district[..^5];
                if (core.Length >= 3 && words.Any(word => word.StartsWith(core + "щин", StringComparison.Ordinal)))
                    return "район";
            }
        }

        // 3) область ("Харківщина", "Харківська область", "на Харківщині")
        if (subscription.Region.EndsWith("область", StringComparison.OrdinalIgnoreCase))
        {
            string adjective = subscription.Region
                .Split(' ')[0]
                .Split('-')
                .Last()
                .ToLowerInvariant()
                .Replace("'", "");

            string stem = adjective;
            foreach (string ending in new[] { "ська", "цька", "зька" })
            {
                if (adjective.EndsWith(ending, StringComparison.Ordinal))
                {
                    stem = adjective[..^ending.Length];
                    break;
                }
            }

            if (stem.Length >= 3 &&
                words.Any(word => word.StartsWith(stem, StringComparison.Ordinal) && word.Length - stem.Length >= 4))
                return "область";
        }

        return null;
    }

    private static List<DroneMatch> GetRecentDroneReports(Subscription subscription, int minutes)
    {
        List<DroneReport> snapshot;
        lock (DataLock)
        {
            snapshot = DroneReports.ToList();
        }

        DateTimeOffset threshold = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(minutes);
        List<DroneMatch> result = new();

        foreach (DroneReport report in snapshot.Where(item => item.At >= threshold).OrderByDescending(item => item.At))
        {
            if (TryMatchThreat(report.Text, subscription, out string scope, out ThreatKind threat))
                result.Add(new DroneMatch(report.At, scope, report.Text, threat));
        }

        return result;
    }

    private static string GetDroneSuffix(Subscription subscription)
    {
        List<DroneMatch> recent = GetRecentDroneReports(subscription, 30);
        if (recent.Count == 0)
            return "";

        string level = recent.Any(item => item.Threat == ThreatKind.Missile) ? "🔴 червоний — ракети/балістика" : "🟡 жовтий — дрони";

        StringBuilder builder = new();
        builder.Append($"\n\n🛩 За останні 30 хв були повідомлення про загрози поруч ({level}):");

        foreach (DroneMatch item in recent.Take(2))
            builder.Append($"\n• {FormatClock(item.At)} — {Truncate(item.Text.Replace('\n', ' '), 120)}");

        builder.Append("\n(неофіційні дані)");
        return builder.ToString();
    }

    private static async Task HandleDronesCommandAsync(long chatId, string text, CancellationToken ct)
    {
        string arg = text.StartsWith('/')
            ? text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()?.Trim().ToLowerInvariant() ?? ""
            : "";

        if (arg is "off" or "вимк")
        {
            DroneOptOut[chatId] = 0;
            SaveSubscriptions();

            await Bot.SendMessage(
                chatId,
                "🔕 Сповіщення про БпЛА поруч вимкнено. Звичайні сповіщення про тривоги працюють як раніше. Увімкнути: /drones on",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);
            return;
        }

        if (arg is "on" or "увімк")
        {
            DroneOptOut.TryRemove(chatId, out _);
            SaveSubscriptions();

            await Bot.SendMessage(
                chatId,
                "🛩 Сповіщення про БпЛА поруч із твоєю локацією увімкнено.",
                replyMarkup: GetStatusKeyboard(),
                cancellationToken: ct);
            return;
        }

        if (!Subscriptions.TryGetValue(chatId, out Subscription? subscription))
        {
            await Bot.SendMessage(
                chatId,
                "Спочатку обери область і місто через /start.",
                cancellationToken: ct);
            return;
        }

        List<DroneMatch> recent = GetRecentDroneReports(subscription, 60);
        bool enabled = !DroneOptOut.ContainsKey(chatId);

        StringBuilder builder = new();
        builder.AppendLine($"🛩 БпЛА поруч — {subscription.City}");
        builder.AppendLine($"Сповіщення: {(enabled ? "увімкнені ✅" : "вимкнені 🔕")} (змінити: /drones {(enabled ? "off" : "on")})");
        builder.AppendLine();

        if (recent.Count == 0)
        {
            builder.AppendLine("🟢 За останню годину повідомлень про ракети чи дрони поруч із твоєю локацією немає.");
        }
        else
        {
            builder.AppendLine(recent.Any(item => item.Threat == ThreatKind.Missile)
                ? "🔴 ЧЕРВОНИЙ РІВЕНЬ — є повідомлення про ракети / балістику поруч:"
                : "🟡 ЖОВТИЙ РІВЕНЬ — є повідомлення про дрони (ударні, реактивні, FPV) поруч:");

            foreach (DroneMatch item in recent.Take(5))
            {
                string label = item.Scope switch
                {
                    "місто" => "місто",
                    "район" => "район",
                    _ => "область"
                };

                builder.AppendLine($"• {FormatClock(item.At)} {(item.Threat == ThreatKind.Missile ? "🔴" : "🟡")} [{label}] {Truncate(item.Text.Replace('\n', ' '), 160)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine(DroneChannelIds.Count == 0
            ? "ℹ️ Автоматичний моніторинг каналів не підключений — дані з'являються з повідомлень адміністраторів."
            : "Дані беруться з каналів моніторингу.");
        builder.Append("Це неофіційна інформація — завжди слідкуй за офіційними джерелами.");

        await Bot.SendMessage(chatId, builder.ToString(), replyMarkup: GetStatusKeyboard(), cancellationToken: ct);
    }

    // ======================= Адмін-команди =======================

    private static readonly HashSet<string> AdminCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "/adminhelp", "/stats", "/users", "/dadadaf", "/broadcast", "/maintenance",
        "/sendto", "/ban", "/unban", "/banned", "/userinfo", "/drone", "/export"
    };

    private static async Task<bool> TryHandleAdminCommandAsync(Message message, string text, CancellationToken ct)
    {
        if (!text.StartsWith('/'))
            return false;

        int split = text.IndexOfAny(new[] { ' ', '\n', '\t' });
        string command = (split < 0 ? text : text[..split]).ToLowerInvariant();
        string args = split < 0 ? "" : text[(split + 1)..].Trim();

        if (!AdminCommands.Contains(command))
            return false;

        long chatId = message.Chat.Id;
        StatusPresses.TryRemove(chatId, out _);

        if (!IsAdmin(message))
        {
            await Bot.SendMessage(chatId, "⛔ Ця команда доступна лише адміністратору.", cancellationToken: ct);
            return true;
        }

        switch (command)
        {
            case "/adminhelp":
                await Bot.SendMessage(chatId, AdminHelpText, cancellationToken: ct);
                break;

            case "/stats":
                await SendAdminStatsAsync(chatId, ct);
                break;

            case "/users":
            case "/dadadaf":
                await SendRegisteredUsersAsync(chatId, args, ct);
                break;

            case "/broadcast":
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    await Bot.SendMessage(chatId, "Використання: /broadcast <текст>", cancellationToken: ct);
                    break;
                }

                await Bot.SendMessage(chatId, $"⏳ Розсилку запущено ({Subscriptions.Count} підписок).", cancellationToken: ct);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        int sent = await SendToAllAsync($"📢 {args}", ct);
                        await Bot.SendMessage(chatId, $"✅ Розсилку завершено. Доставлено: {sent}.", cancellationToken: ct);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка розсилки: {ex.Message}");
                    }
                });
                break;
            }

            case "/maintenance":
            {
                await Bot.SendMessage(chatId, "⏳ Повідомлення про технічні роботи розсилається підписникам.", cancellationToken: ct);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendMaintenanceMessageAsync(string.IsNullOrWhiteSpace(args) ? null : args);
                        await Bot.SendMessage(chatId, "✅ Повідомлення про технічні роботи надіслано.", cancellationToken: ct);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка розсилки: {ex.Message}");
                    }
                });
                break;
            }

            case "/sendto":
            {
                string[] parts = args.Split(new[] { ' ', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2 || !long.TryParse(parts[0], out long targetId))
                {
                    await Bot.SendMessage(chatId, "Використання: /sendto <id> <текст>", cancellationToken: ct);
                    break;
                }

                bool ok = await SafeSendAsync(targetId, $"💬 Повідомлення від адміністратора:\n\n{parts[1]}", ct);

                await Bot.SendMessage(
                    chatId,
                    ok ? "✅ Надіслано." : "❌ Не вдалося надіслати (користувач заблокував бота або ID невірний).",
                    cancellationToken: ct);
                break;
            }

            case "/ban":
            case "/unban":
            {
                if (!long.TryParse(args.Split(' ')[0], out long targetId))
                {
                    await Bot.SendMessage(chatId, $"Використання: {command} <id>", cancellationToken: ct);
                    break;
                }

                if (command == "/ban")
                {
                    if (targetId == chatId)
                    {
                        await Bot.SendMessage(chatId, "Себе банити не можна 🙂", cancellationToken: ct);
                        break;
                    }

                    BannedUsers[targetId] = 0;
                    Subscriptions.TryRemove(targetId, out _);
                    SaveSubscriptions();

                    await Bot.SendMessage(chatId, $"⛔ Користувача {targetId} заблоковано, підписку видалено.", cancellationToken: ct);
                }
                else
                {
                    BannedUsers.TryRemove(targetId, out _);
                    SaveSubscriptions();

                    await Bot.SendMessage(chatId, $"✅ Користувача {targetId} розблоковано.", cancellationToken: ct);
                }

                break;
            }

            case "/banned":
            {
                long[] banned = BannedUsers.Keys.ToArray();

                await Bot.SendMessage(
                    chatId,
                    banned.Length == 0
                        ? "Заблокованих користувачів немає."
                        : "⛔ Заблоковані:\n" + string.Join("\n", banned.Select(id => $"• {id}")),
                    cancellationToken: ct);
                break;
            }

            case "/userinfo":
            {
                if (!long.TryParse(args, out long targetId))
                {
                    await Bot.SendMessage(chatId, "Використання: /userinfo <id>", cancellationToken: ct);
                    break;
                }

                string subscriptionText = Subscriptions.TryGetValue(targetId, out Subscription? target)
                    ? $"{target.City}, {target.District}, {target.Region}"
                    : "немає підписки";

                await Bot.SendMessage(
                    chatId,
                    $"👤 ID {targetId}\n" +
                    $"Підписка: {subscriptionText}\n" +
                    $"Сповіщення про БпЛА: {(DroneOptOut.ContainsKey(targetId) ? "вимкнені" : "увімкнені")}\n" +
                    $"Заблокований: {(BannedUsers.ContainsKey(targetId) ? "так" : "ні")}",
                    cancellationToken: ct);
                break;
            }

            case "/drone":
            {
                if (string.IsNullOrWhiteSpace(args))
                {
                    await Bot.SendMessage(
                        chatId,
                        "Використання: /drone <текст>\nЖовтий: /drone Шахед курсом на Балаклію, Харківщина\nЧервоний: /drone Балістика на Харківщину",
                        cancellationToken: ct);
                    break;
                }

                if (!IsDroneText(args))
                {
                    await Bot.SendMessage(
                        chatId,
                        "Текст має містити слово про загрозу: «шахед», «БпЛА», «дрон», «ракета», «балістика» тощо.",
                        cancellationToken: ct);
                    break;
                }

                int notified = await ProcessDroneReportAsync(args, "адміністратор", ct);

                await Bot.SendMessage(
                    chatId,
                    $"🛩 Повідомлення збережено. Надіслано підписникам поруч: {notified}.",
                    cancellationToken: ct);
                break;
            }

            case "/export":
            {
                SaveSubscriptions();

                if (!File.Exists(DataFilePath))
                {
                    await Bot.SendMessage(chatId, "Файл даних ще не створено.", cancellationToken: ct);
                    break;
                }

                await using FileStream stream = new(DataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                await Bot.SendDocument(
                    chatId,
                    InputFile.FromStream(stream, "bot-data.json"),
                    caption: "📦 Резервна копія bot-data.json",
                    cancellationToken: ct);
                break;
            }
        }

        return true;
    }

    private static async Task SendAdminStatsAsync(long chatId, CancellationToken ct)
    {
        KeyValuePair<long, Subscription>[] users = Subscriptions.ToArray();

        int historyCount;
        int openAlerts;
        int droneReports;

        lock (DataLock)
        {
            historyCount = AlertHistory.Count;
            openAlerts = AlertHistory.Count(item => !item.FinishedAt.HasValue);
            droneReports = DroneReports.Count;
        }

        string topRegions = string.Join("\n", users
            .GroupBy(item => item.Value.Region)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .Select(group => $"• {group.Key}: {group.Count()}"));

        string topCities = string.Join("\n", users
            .GroupBy(item => item.Value.City)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .Select(group => $"• {group.Key}: {group.Count()}"));

        StringBuilder builder = new();
        builder.AppendLine("📊 Статистика бота");
        builder.AppendLine($"⏱ Працює: {FormatHistoryDuration(DateTimeOffset.UtcNow - StartedAt)}");
        builder.AppendLine($"👥 Підписок: {users.Length} (міст: {users.Select(item => item.Value.City).Distinct().Count()})");
        builder.AppendLine($"🔕 БпЛА-сповіщення вимкнено: {DroneOptOut.Count}");
        builder.AppendLine($"⛔ Заблоковано: {BannedUsers.Count}");
        builder.AppendLine($"📚 Записів історії: {historyCount} (відкритих тривог: {openAlerts})");
        builder.AppendLine($"🛩 Повідомлень про БпЛА (за 3 год): {droneReports}; каналів: {DroneChannelIds.Count}");
        builder.AppendLine($"🌐 {GetAlertsSourceText()}");

        if (users.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Топ областей:");
            builder.AppendLine(topRegions);
            builder.AppendLine();
            builder.AppendLine("Топ міст:");
            builder.Append(topCities);
        }

        await Bot.SendMessage(chatId, builder.ToString(), cancellationToken: ct);
    }

    private static Task HandleErrorAsync(
        ITelegramBotClient _,
        Exception exception,
        CancellationToken __)
    {
        Console.WriteLine($"Telegram error: {exception.Message}");
        return Task.CompletedTask;
    }

    private sealed record Subscription(
        string Region,
        string City,
        string District);

    private sealed record PersistedSubscription(
        long ChatId,
        string Region,
        string City,
        string? District);

    private sealed record PersistedBotData(
        List<PersistedSubscription> Subscriptions,
        List<AlertHistoryEntry> AlertHistory,
        List<long>? BannedUsers = null,
        List<long>? DroneOptOut = null);

    private sealed record DroneReport(DateTimeOffset At, string Text, string Source);

    private sealed record DroneMatch(DateTimeOffset At, string Scope, string Text, ThreatKind Threat);

    private sealed record AlertHistoryEntry(
        string StateKey,
        string Region,
        string District,
        string City,
        DateTimeOffset StartedAt,
        DateTimeOffset? FinishedAt,
        string Scope);

    private sealed record BotUpdate(
        string Version,
        string UpdatedAt,
        string Title,
        List<string>? Changes);

    private sealed record AlertState(
        bool IsAlert,
        String UpdatedAt,
        string Level = "",
        bool HasData = true,
        string Scope = "область");
}
