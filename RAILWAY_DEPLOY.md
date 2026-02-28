# 🚀 Деплой NSprob на Railway

## Крок 1 — GitHub репозиторій

1. Встанови **GitHub Desktop** або використай сайт github.com
2. Створи новий репо: `nsprob-server` (приватний!)
3. Завантаж туди всю папку `NSprob.Server/`

Структура репо має бути:
```
nsprob-server/
├── Dockerfile
├── railway.json
├── NSprob.Server.csproj
├── Program.cs
├── appsettings.json
├── Controllers/
│   ├── AuthController.cs
│   └── ChatController.cs
├── Data/
│   └── AppDbContext.cs
├── Hubs/
│   └── ChatHub.cs
└── Services/
    ├── TokenService.cs
    ├── EmailService.cs
```

## Крок 2 — Railway

1. Зайди на **railway.app** → "Start a New Project"
2. Вибери **"Deploy from GitHub repo"**
3. Підключи GitHub → вибери `nsprob-server`
4. Railway автоматично знайде `Dockerfile` і задеплоїть

## Крок 3 — Environment Variables (ВАЖЛИВО!)

В Railway → твій проєкт → **Variables** → додай:

| Variable      | Value                              |
|---------------|------------------------------------|
| `JWT_KEY`     | будь-який довгий рядом (32+ символи) |
| `EMAIL_USER`  | твій@gmail.com                     |
| `EMAIL_PASS`  | App Password (16 символів)         |
| `EMAIL_HOST`  | smtp.gmail.com                     |
| `EMAIL_PORT`  | 587                                |

### Як отримати Gmail App Password:
1. myaccount.google.com → Безпека
2. Двофакторна автентифікація → увімкни
3. Паролі додатків → створи → назви "NSprob"
4. Скопіюй 16-значний код → вставляй в `EMAIL_PASS`

## Крок 4 — Отримай URL

Після деплою Railway дасть URL типу:
```
https://nsprob-server-production.up.railway.app
```

## Крок 5 — Онови клієнт NSprob

Відкрий `ServerConfig.cs` і встав свій Railway URL:
```csharp
public static string BaseUrl { get; set; } =
    "https://nsprob-server-production.up.railway.app";
```

Rebuild → готово! 🎉

## Безкоштовний план Railway

- $5 кредитів на місяць
- Для особистого месенджера вистачить на ~500 годин роботи
- Якщо закінчиться — сервер зупиниться до наступного місяця
