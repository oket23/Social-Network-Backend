# Social Network Backend

Backend навчального клону Instagram: REST API на **ASP.NET Core (.NET 10)**, **PostgreSQL**,
**ASP.NET Core Identity + JWT**, Docker, Azure.

- Архітектура (модульний моноліт, Clean Architecture): [docs/architecture.md](docs/architecture.md)
- Пояснення кожного рішення і як повторити з нуля: [docs/learning-guide.md](docs/learning-guide.md)
- Правила для Claude Code / розробників: [CLAUDE.md](CLAUDE.md)

## Стек

| | |
|---|---|
| API | ASP.NET Core Web API, контролери, OpenAPI + Scalar |
| БД | PostgreSQL 17, EF Core 10 (Npgsql), міграції |
| Авторизація | ASP.NET Core Identity, JWT access (15 хв) + refresh (7 днів, ротація) |
| Валідація | FluentValidation |
| Файли | Azure Blob Storage (локально Azurite) |
| Пошта | SMTP через MailKit (локально smtp4dev) |
| Логи | Serilog |
| Тести | xUnit, WebApplicationFactory, Testcontainers |
| Інфраструктура | Docker, GitHub Actions, Azure App Service + Azure Database for PostgreSQL |

## Структура

```
src/
  SocialNetwork.Domain          сутності
  SocialNetwork.Application     бізнес-логіка (сервіси), DTO, валідатори
  SocialNetwork.Infrastructure  EF Core, репозиторії, Unit of Work, JWT, SMTP, Blob Storage, міграції
  SocialNetwork.Api             контролери, middleware, Program.cs, Dockerfile
tests/
  SocialNetwork.Tests           unit + інтеграційні тести
docs/                           архітектура, гайд, дизайн профілю
docker-compose.yml              PostgreSQL, smtp4dev, Azurite (+ API з профілем full)
```

## Швидкий старт

Потрібні: [Docker Desktop](https://www.docker.com/products/docker-desktop/), .NET SDK 10, Rider або VS.

```bash
# 1. Залежності: PostgreSQL, smtp4dev (пошта), Azurite (файли)
docker compose up -d

# 2. Секрет для підпису JWT (один раз; зберігається поза репозиторієм)
dotnet user-secrets set "Jwt:Key" "будь-який-випадковий-рядок-мінімум-32-символи" --project src/SocialNetwork.Api

# 3. Схема БД
dotnet tool restore
dotnet ef database update -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api

# 4. Запуск
dotnet run --project src/SocialNetwork.Api
```

| URL | Що це |
|---|---|
| http://localhost:5298/scalar | документація API (кнопка Auth → вставити `accessToken`) |
| http://localhost:5298/health | перевірка стану |
| http://localhost:5080 | smtp4dev — усі листи (підтвердження email, скидання пароля) |

Тести: `dotnet test` (Docker має бути запущений — інтеграційні тести піднімають власний PostgreSQL).

## API

Усі відповіді в єдиному форматі:

```jsonc
// успіх
{ "success": true, "message": "OK", "data": { ... } }
// помилка
{ "success": false, "message": "Помилка валідації.", "errors": { "Username": ["..."] } }
```

| Метод | Маршрут | Авторизація | Опис |
|---|---|---|---|
| POST | `/auth/register` | — | реєстрація (email, username, displayName, password) → токени |
| POST | `/auth/login` | — | вхід за email або username → токени |
| POST | `/auth/refresh` | — | нова пара токенів за refresh-токеном |
| POST | `/auth/logout` | — | відкликати refresh-токен |
| GET | `/auth/confirm-email?userId&token` | — | підтвердження email (посилання з листа) |
| POST | `/auth/forgot-password` | — | лист зі скиданням пароля |
| POST | `/auth/reset-password` | — | новий пароль за токеном з листа |
| GET | `/users/me` | Bearer | мій профіль |
| PUT | `/users/me` | Bearer | редагування профілю |
| PUT | `/users/me/password` | Bearer | зміна пароля |
| POST | `/users/me/avatar` | Bearer | аватар (multipart, поле `file`, JPEG/PNG/WEBP ≤ 5 МБ) |
| GET | `/health` | — | стан API і БД |

Rate limiting: 5 запитів/хв з однієї IP на `register`, `login`, `forgot-password`, `reset-password` (відповідь `429`).

## Конфігурація

| Ключ | Опис | Де задається |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | рядок підключення PostgreSQL | Dev: `appsettings.Development.json` (локальний Docker); Azure: App Settings |
| `Jwt:Key` | секрет підпису JWT (≥ 32 символи) | Dev: user-secrets; Azure: App Settings |
| `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | терміни дії токенів | `appsettings.json` |
| `Smtp:*` | Host, Port, Security (None/StartTls/...), Username, Password, FromEmail | Dev: smtp4dev; Azure: App Settings |
| `Storage:ConnectionString` | Azure Storage (`UseDevelopmentStorage=true` для Azurite) | Azure: App Settings |
| `Frontend:BaseUrl` | адреса фронту для посилань у листах | App Settings |
| `Cors:AllowedOrigins` | дозволені адреси фронту | App Settings (`Cors__AllowedOrigins__0=...`) |
| `OpenApi:Enabled` | показувати `/scalar` поза Development | staging: `true` |

В Azure/Docker двокрапка замінюється на `__`: `Jwt__Key`, `ConnectionStrings__DefaultConnection`.

## Деплой

Автоматично через GitHub Actions ([.github/workflows/deploy.yml](.github/workflows/deploy.yml)):
push у `dev` → **staging**, push у `main` → **production**. Кожен PR перевіряється `ci.yml` (збірка + тести).

Кроки деплою: тести → Docker-образ у GitHub Container Registry → міграції БД → оновлення Azure App Service → перевірка `/health`.
Створення ресурсів Azure і відновлення з бекапу — у [docs/architecture.md](docs/architecture.md#резервне-копіювання-та-відновлення).

## Правила роботи з Git

**Гілки**
- `main`: стабільна версія, лише через Pull Request з `dev`
- `dev`: поточна розробка, лише через Pull Request
- `feature/<короткий-опис>`: нова функція, наприклад `feature/auth-register`
- `fix/<опис>`: виправлення помилок

**Процес**
1. Взяти картку в Trello, перенести в In Progress
2. Створити гілку від актуальної `dev`
3. Робити невеликі коміти
4. Відкрити Pull Request у `dev`, посилання на картку Trello в описі
5. Мінімум 1 approve від іншого розробника, картка переходить у Code Review
6. Після merge видалити гілку, картку перенести в Done

**Коміти:** `feat: додано реєстрацію`, `fix: помилка валідації email`, `docs: оновлено README`

**Заборонено:** пушити напряму в `main`/`dev`, комітити паролі й ключі.
