# Деплой: Render + Neon + Supabase Storage

Як розгорнути **staging** (гілка `dev`) на безкоштовних тарифах — без банківської карти.
Production (гілка `main`) додається за тим самим шаблоном (див. останній розділ).

## Хто за що відповідає

| Сервіс | Роль | Локальний замінник |
|---|---|---|
| **Render** | запускає наш Docker-образ API, дає HTTPS-адресу `*.onrender.com` | Rider / `docker compose --profile full` |
| **Neon** | PostgreSQL | `postgres` у Docker |
| **Supabase Storage** | файли (аватари, далі фото/відео) через S3 API | `rustfs` у Docker |
| **Brevo** | відправка листів (SMTP) | `smtp4dev` у Docker |
| **UptimeRobot** | пінгує `/health/live` кожні 5 хв, щоб Render не засинав + повідомляє, якщо API впав | — |
| **GitHub Actions** | тести → міграції БД → деплой → щоденний бекап | — |

```
push у dev ──► GitHub Actions: тести ──► міграції (Neon) ──► Render Deploy Hook ──► Render збирає Dockerfile
                                                                                     │
фронтенд ──HTTPS──► https://<сервіс>.onrender.com ──► PostgreSQL (Neon) + файли (Supabase) + листи (Brevo)
UptimeRobot ──кожні 5 хв──► /health/live
```

## Обмеження безкоштовних тарифів (станом на жовтень 2026, перевіряйте на сайтах)

- **Render free:** засинає після 15 хв без запитів (перший запит після сну ~30–60 с) — тому UptimeRobot.
  750 год/міс на workspace — вистачає на **один** сервіс 24/7. Диск контейнера тимчасовий (нам це не заважає: файли в Supabase, ключі Data Protection — у БД).
- **Neon free:** ~0.5–1 GB на проєкт, обмежені години обчислень; засинає після 5 хв без запитів і прокидається за секунди.
  Тому UptimeRobot пінгує `/health/live` (без БД), а не `/health`.
- **Supabase free:** 1 GB файлів. **Проєкт ставиться на паузу після тижня неактивності** — тоді в дашборді Supabase натиснути «Restore».
- **Brevo free:** 300 листів на день.

---

## Крок 1. Neon (база даних)

1. https://neon.tech → Sign up (через GitHub).
2. **Create project:** назва `socialnetwork`, Postgres **17**, регіон **AWS Europe Central 1 (Frankfurt)** (поруч із Render Frankfurt).
3. Dashboard → **Connect**. Знадобляться два формати одного рядка підключення (вимкніть «Connection pooling», щоб узяти пряму адресу):
   - **URL** (для бекапів):
     `postgresql://neondb_owner:<пароль>@ep-xxx.eu-central-1.aws.neon.tech/neondb?sslmode=require`
   - **.NET (Npgsql)** (для API і міграцій) — зібрати з тих самих частин:
     `Host=ep-xxx.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=<пароль>;SSL Mode=Require`

## Крок 2. Supabase Storage (файли)

1. https://supabase.com → Sign up (через GitHub) → **New project**, регіон **Central EU (Frankfurt)**.
   Пароль БД Supabase нам не знадобиться (базу беремо з Neon), але збережіть його.
2. **Storage → New bucket:** назва `media`, увімкнути **Public bucket**.
3. **Storage → Settings (S3 Connection):**
   - **Endpoint** → це `Storage__ServiceUrl` (вигляд `https://<project-ref>.supabase.co/storage/v1/s3`);
   - **Region** → `Storage__Region`;
   - **New access key** → `Storage__AccessKey` і `Storage__SecretKey` (секрет показується один раз!).
4. `Storage__PublicBaseUrl` = `https://<project-ref>.supabase.co/storage/v1/object/public/media`
   (`<project-ref>` — той самий, що в Endpoint).

## Крок 3. Brevo (пошта, можна пізніше)

Без цього кроку API працює, просто листи не відправляються (помилка лише пишеться в лог).

1. https://www.brevo.com → Sign up.
2. **Senders, Domains & Dedicated IPs → Senders → Add a sender** — ваш email, підтвердити за листом.
3. **SMTP & API → SMTP → Generate a new SMTP key**.
4. Значення: `Smtp__Host` = `smtp-relay.brevo.com`, `Smtp__Port` = `587`,
   `Smtp__Username` = SMTP login (на тій самій сторінці), `Smtp__Password` = SMTP key, `Smtp__FromEmail` = підтверджений sender.

## Крок 4. Render (API)

1. https://render.com → Sign up **через GitHub** і дати доступ до репозиторію `Social-Network-Backend`.
2. **New → Blueprint** → вибрати репозиторій, гілку **`dev`**. Render прочитає [`render.yaml`](../render.yaml)
   і запропонує створити сервіс `socialnetwork-api-staging`.
3. Заповнити змінні з `sync: false`:

   | Змінна | Значення |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | рядок Neon у форматі **.NET** (крок 1) |
   | `Storage__ServiceUrl`, `Storage__Region`, `Storage__AccessKey`, `Storage__SecretKey`, `Storage__PublicBaseUrl` | з кроку 2 |
   | `Cors__AllowedOrigins__0` | адреса фронтенду (напр. `https://our-front.vercel.app`); поки немає — `http://localhost:3000` |
   | `Frontend__BaseUrl` | та сама адреса фронтенду (для посилань у листах) |
   | `Smtp__*` | з кроку 3 (якщо поле не можна лишити порожнім — тимчасово `-`) |

   `Jwt__Key` Render згенерує сам; `PORT`, `ASPNETCORE_ENVIRONMENT`, `OpenApi__Enabled` уже задані в `render.yaml`.
4. **Apply** → Render створить сервіс. Адреса буде у верхній частині сторінки сервісу (`https://socialnetwork-api-staging.onrender.com`
   або з суфіксом, якщо назва зайнята).
5. Сервіс → **Settings → Deploy Hook** → скопіювати URL (це секрет: хто його знає, той може запустити деплой).

> Перший деплой Render запустить сам, але **до міграцій** — API впаде на запитах до БД, доки не виконано крок 6. Це нормально.

## Крок 5. GitHub (секрети для CI)

Репозиторій → **Settings → Environments → New environment** → `staging`:

| Тип | Назва | Значення |
|---|---|---|
| Secret | `STAGING_DB_CONNECTION` | рядок Neon у форматі **.NET** |
| Secret | `STAGING_DB_URL` | рядок Neon у форматі **URL** |
| Secret | `RENDER_STAGING_DEPLOY_HOOK` | Deploy Hook з Render |
| Variable | `STAGING_API_URL` | `https://<сервіс>.onrender.com` (без `/` у кінці) |

## Крок 6. Перший деплой

Зробити push (або змерджити PR) у `dev` → **Actions → Deploy staging**:
тести → міграції в Neon → Render збирає образ → перевірка `/health`.

Перевірити: `https://<сервіс>.onrender.com/scalar` → register → login → `GET /users/me` → аватар.

> Кнопка «Run workflow» (ручний запуск) з'явиться в GitHub лише після того, як workflow потрапить у гілку за замовчуванням (`main`).
> До того деплой запускається push-ем у `dev`.

## Крок 7. UptimeRobot (щоб не засинав + моніторинг)

1. https://uptimerobot.com → Sign up.
2. **Add New Monitor:** тип **HTTP(s)**, URL `https://<сервіс>.onrender.com/health/live`, інтервал **5 minutes**.
3. Alert contact — ваш email (лист, якщо API впав).
4. **Status Pages → Create** → додати монітор → публічна сторінка доступності з історією інцидентів.
   Наша: **https://stats.uptimerobot.com/QX76uZtgtw** (посилання в README і в описі репозиторію на GitHub).

Успішні перевірки `/health*` не пишуться в логи застосунку (рівень Verbose у `Program.cs` → `GetRequestLogLevel`),
щоб у Render → Logs було видно справжні запити. Якщо health-перевірка падає з помилкою — вона в логах буде.

## Крок 8. Дати API фронтенду

- **Base URL:** `https://socialnetwork-api-staging.onrender.com` (відкриття адреси перенаправляє на документацію)
- **Документація:** `/scalar`, специфікація для генерації типів: `/openapi/v1.json`
- **Статус:** https://stats.uptimerobot.com/QX76uZtgtw
- Адресу фронтенду додати в Render → Environment → `Cors__AllowedOrigins__0` (ще адреси — `__1`, `__2`...) → Save (Render перезапустить сервіс).
- Формат відповідей і авторизація — див. розділ «API» в [README](../README.md#api).

---

## Бекап і відновлення

- Workflow [`backup.yml`](../.github/workflows/backup.yml) щодня о 03:00 UTC робить `pg_dump` і зберігає його як artifact (14 днів).
  Scheduled-запуски GitHub виконує лише з гілки `main` — автоматичні бекапи почнуться після мерджу `dev → main`.
- Neon також дозволяє відновити БД на момент у недалекому минулому (Branches → Restore; глибина історії залежить від тарифу).

**Відновити з дампу:**
1. Actions → Backup staging DB → потрібний запуск → **Artifacts** → завантажити й розпакувати `.dump`.
2. ```bash
   docker run --rm -v "$PWD:/backup" postgres:17 \
     pg_restore --clean --if-exists --no-owner -d "<STAGING_DB_URL>" /backup/staging-<дата>.dump
   ```
3. Перевірити `https://<сервіс>.onrender.com/health`.

Відновлення локально (подивитися дані): замість `<STAGING_DB_URL>` — `postgresql://postgres:postgres@host.docker.internal:5432/socialnetwork`.

## Логи й діагностика

- **Render → сервіс → Logs** — логи Serilog у реальному часі (помилки з `traceId`).
- **Render → Events** — історія деплоїв; невдалий деплой не замінює робочу версію.
- **UptimeRobot** — історія доступності.

| Проблема | Що перевірити |
|---|---|
| Деплой у GitHub Actions падає на міграціях | секрет `STAGING_DB_CONNECTION` (формат .NET, `SSL Mode=Require`) |
| `/health` = Unhealthy | `ConnectionStrings__DefaultConnection` у Render; чи не вичерпано ліміт Neon |
| Аватар: 500 або URL не відкривається | змінні `Storage__*`; bucket `media` публічний; проєкт Supabase не на паузі |
| Фронт: CORS error у браузері | адреса фронту в `Cors__AllowedOrigins__0` точно збігається (схема, домен, порт, без `/` в кінці) |
| Листи не приходять | `Smtp__*`, sender підтверджений у Brevo; помилка буде в логах Render |
| Перший запит дуже довгий | сервіс заснув — перевірити монітор UptimeRobot |

## Production пізніше

Коли знадобиться (гілка `main`): у `render.yaml` додати другий сервіс `socialnetwork-api-prod` з `branch: main`;
окремий проєкт Neon і bucket (або Supabase-проєкт) для prod; GitHub environment `production` з тими ж секретами;
workflow `deploy-production.yml` за зразком `deploy-staging.yml`.
Нюанс: безкоштовних 750 год Render вистачає на один сервіс 24/7 — другий засинатиме або знадобиться платний план.
