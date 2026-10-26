# Social Network Backend

Backend навчального клону Instagram: REST API на **ASP.NET Core (.NET 10)**, **PostgreSQL**,
**ASP.NET Core Identity + JWT**, Docker. Staging розгорнуто на Render.

| Середовище | Адреса |
|---|---|
| **API (staging)** | https://socialnetwork-api-staging.onrender.com |
| **Документація API** | https://socialnetwork-api-staging.onrender.com/scalar · специфікація [`/openapi/v1.json`](https://socialnetwork-api-staging.onrender.com/openapi/v1.json) |
| **Статус і доступність** | https://stats.uptimerobot.com/QX76uZtgtw |


- Деплой і налаштування сервісів: [docs/deployment.md](docs/deployment.md)
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
| Файли | S3-сумісне сховище: Supabase Storage (локально RustFS) |
| Пошта | Brevo HTTP API на staging; локально SMTP (MailKit) → smtp4dev |
| Логи | Serilog |
| Тести | xUnit, WebApplicationFactory, Testcontainers |
| Інфраструктура | Docker, GitHub Actions, Render (API), Neon (PostgreSQL), UptimeRobot (моніторинг) |

## Структура

```
src/
  SocialNetwork.Domain          сутності
  SocialNetwork.Application     бізнес-логіка (сервіси), DTO, валідатори
  SocialNetwork.Infrastructure  EF Core, репозиторії, Unit of Work, JWT, SMTP, S3-сховище, міграції
  SocialNetwork.Api             контролери, middleware, Program.cs, Dockerfile
tests/
  SocialNetwork.Tests           unit + інтеграційні тести
docs/                           архітектура, деплой, гайд, дизайн профілю
docker-compose.yml              PostgreSQL, smtp4dev, RustFS (+ API з профілем full)
render.yaml                     опис сервісу на Render
.github/workflows/              CI (тести), деплой staging, щоденний бекап БД
```

## Швидкий старт

Потрібні: [Docker Desktop](https://www.docker.com/products/docker-desktop/), .NET SDK 10, Rider або VS.

```bash
# 1. Залежності: PostgreSQL, smtp4dev (пошта), RustFS (файли)
docker compose up -d

# 2. Секрет для підпису JWT (один раз; зберігається поза репозиторієм)
dotnet user-secrets set "Jwt:Key" "будь-який-випадковий-рядок-мінімум-32-символи" --project src/SocialNetwork.Api

# 3. Запуск (або ▶ у Rider). Міграції БД застосовуються автоматично при старті в Development.
dotnet run --project src/SocialNetwork.Api
```

Крок 2 — **один раз на комп'ютер**. Щодня достатньо `docker compose up -d` і запуску API.

Усе в Docker, без .NET (зручно для фронтенду): `cp .env.example .env`, потім
`docker compose --profile full up -d --build` → http://localhost:8080/scalar.

| URL | Що це |
|---|---|
| http://localhost:5298/scalar | документація API (кнопка Auth → вставити `accessToken`) |
| http://localhost:5298/health | перевірка стану (API + БД) |
| http://localhost:5080 | smtp4dev — усі листи (підтвердження email, скидання пароля) |
| http://localhost:9001 | консоль RustFS — завантажені файли (логін/пароль з `.env.example`: `devaccess` / `devsecret123`) |

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
| GET | `/health/live` | — | лише «API живий», без БД (для пінгу UptimeRobot) |
| GET | `/` | — | перенаправляє на `/scalar` (де документація увімкнена) |

Rate limiting: 5 запитів/хв з однієї IP на `register`, `login`, `forgot-password`, `reset-password` (відповідь `429`).

**Для фронтенду:**
- Авторизація: після `login`/`register` надсилати `Authorization: Bearer <accessToken>`; на `401` — `POST /auth/refresh`
  з `refreshToken` і повторити запит; refresh теж `401` → на сторінку входу.
- Access-токен живе 15 хв, refresh — 7 днів і **одноразовий** (після refresh використовувати новий).
- Адресу фронту треба додати в CORS (`Cors__AllowedOrigins__0` на Render), інакше браузер блокуватиме запити.
- Типи для TypeScript можна згенерувати з `/openapi/v1.json` (наприклад, `openapi-typescript`).

## Конфігурація

| Ключ | Опис | Де задається |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | рядок підключення PostgreSQL | Dev: `appsettings.Development.json` (локальний Docker); staging: Render (Neon) |
| `Jwt:Key` | секрет підпису JWT (≥ 32 символи) | Dev: user-secrets; staging: Render (генерується) |
| `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | терміни дії токенів | `appsettings.json` |
| `Smtp:*` | Host, Port, Security (None/StartTls/...), Username, Password, FromEmail — відправка через SMTP | Dev: smtp4dev |
| `Brevo:ApiKey`, `Brevo:FromEmail` | відправка через HTTP API Brevo; якщо `ApiKey` задано — замість SMTP (Render free блокує SMTP) | staging: Render |
| `Storage:*` | ServiceUrl, Region, AccessKey, SecretKey, BucketName, PublicBaseUrl — S3-сховище | Dev: RustFS; staging: Render (Supabase) |
| `Frontend:BaseUrl` | адреса фронту для посилань у листах | Render |
| `Cors:AllowedOrigins` | дозволені адреси фронту | Render (`Cors__AllowedOrigins__0=...`) |
| `OpenApi:Enabled` | показувати `/scalar` поза Development | staging: `true` |

У змінних середовища (Render, Docker) двокрапка замінюється на `__`: `Jwt__Key`, `ConnectionStrings__DefaultConnection`.

## Деплой

Push у `dev` → GitHub Actions ([deploy-staging.yml](.github/workflows/deploy-staging.yml)):
тести → міграції БД (Neon) → деплой на Render → перевірка `/health`. Кожен PR перевіряється `ci.yml` (збірка + тести).
Щоденний бекап БД — [backup.yml](.github/workflows/backup.yml).

Налаштування сервісів з нуля, бекап і відновлення, діагностика — [docs/deployment.md](docs/deployment.md).

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
