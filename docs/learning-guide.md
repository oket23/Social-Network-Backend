# Гайд: що зроблено, навіщо, і як повторити самому

Цей документ пояснює кожне рішення в проєкті простими словами. Читай по порядку — розділи йдуть так,
як будувався проєкт. Наприкінці — розділ «Повтори сам» з усіма командами від нуля.

Корисно паралельно дивитися на коміти: `git log --oneline`, а потім `git show <hash>` — видно, що змінилося на кожному етапі.

---

## 1. Навіщо взагалі шари (Clean Architecture)

**Як було:** `UsersController` сам ходив у `AppDbContext`, сам хешував паролі, сам писав файли на диск.
Поки ендпоінтів 4 — нормально. Коли їх стане 60 (пости, сторіз, чат...) — контролери перетворяться на «кашу»:
логіку не перевикористаєш, не протестуєш без БД, а зміна БД зачепить усе.

**Як стало:** кожен шар відповідає за одне.

| Шар | Аналогія з рестораном | Що в нас |
|---|---|---|
| **Api** | офіціант: приймає замовлення, віддає страву | контролери — лише приймають HTTP і викликають сервіс |
| **Application** | шеф-кухар: знає рецепти | сервіси з бізнес-правилами («username має бути унікальним») |
| **Infrastructure** | постачальники: продукти, посуд | БД, пошта, файли, JWT — конкретні технології |
| **Domain** | самі продукти | сутності `User`, `RefreshToken` |

**Ключова ідея — інверсія залежностей.** Application каже: «мені потрібен хтось, хто вміє `SendAsync(email)`» —
це інтерфейс `IEmailSender` (лежить в Application). Infrastructure реалізує його через SMTP (`SmtpEmailSender`).
У тестах ми підставляємо `FakeEmailSender`. Сервіс навіть не знає, що є різні реалізації.

Хто з ким з'єднує інтерфейс і реалізацію? **DI-контейнер** (Dependency Injection):

```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped<IEmailSender, SmtpEmailSender>();
```

і далі будь-який клас, що просить `IEmailSender` у конструкторі, отримає `SmtpEmailSender`.

**Час життя (lifetime):**
- `AddScoped` — один екземпляр на HTTP-запит (DbContext, репозиторії, сервіси).
- `AddSingleton` — один на весь застосунок (`JwtTokenService`, `BlobFileStorage` — не мають стану запиту).
- `AddTransient` — новий щоразу (у нас не використовується).

## 2. Repository і Unit of Work

**Repository** ховає «як дістати дані». Сервіс пише `_userRepository.GetById(id)` і не знає про SQL чи EF Core.
Базові операції — у `RepositoryBase<T>` (GetById, Add, Update, Delete), специфічні — у конкретному репозиторії
(`IsUsernameTaken`).

**Unit of Work** — «кошик змін». Репозиторії тільки кладуть зміни в кошик (`Add`, змінені поля),
а `_unitOfWork.SaveChangesAsync()` зберігає все **однією транзакцією**. Приклад з `AuthService.IssueTokens` при
ротації: відкликати старий refresh-токен **і** додати новий — або обидва, або жоден.

Коли змін кілька і вони зберігаються різними способами — `ExecuteInTransactionAsync` (див. `ResetPassword`:
UserManager зберігає пароль сам, а ми ще відкликаємо токени окремим UPDATE).

> **Пастка (була в JobBoard):** якщо завантажити сутність з `AsNoTracking()`, EF її «не бачить», і зміни
> не збережуться. Тому в `RepositoryBase` є `Query` (для читання) і `Set` (для змін).

## 3. DTO: Request / Response

Ніколи не повертаємо сутність `User` напряму — в ній `PasswordHash`, `SecurityStamp` тощо.
- `*Request` — що приходить від клієнта (`UpdateUserProfileRequest`).
- `*Response` — що віддаємо (`UserProfileResponse`).
- Перетворення — вручну в `UserMapper.ToProfileResponse()` (простіше й прозоріше за AutoMapper).

## 4. Валідація (FluentValidation)

Правила в окремих класах: `UpdateUserProfileRequestValidator`. Спільні правила (пароль, username) —
`Common/Validation/ValidationRules.cs`, щоб не дублювати.

Хто їх запускає? `Api/Filters/ValidationFilter.cs` — перед кожним action шукає валідатор для аргументу
і кидає `ValidationException`, якщо дані неправильні. Тому в контролерах немає жодного `if (!ModelState.IsValid)`.

## 5. Обробка помилок (middleware)

**Middleware** — ланцюжок обробників, через який проходить кожен запит (`Program.cs`, порядок важливий).

`GlobalExceptionHandlingMiddleware` обгортає все в `try/catch`. Сервіс просто пише
`throw new NotFoundException("Користувача не знайдено.")`, а middleware перетворює це на `404` з JSON.
Переваги: один формат помилок для фронту, немає try/catch у кожному методі, внутрішні деталі не «витікають» у production.

## 6. Identity, паролі, JWT

**ASP.NET Core Identity** — готова бібліотека: хешування паролів (PBKDF2), перевірка унікальності,
блокування після невдалих спроб, токени для email і скидання пароля. Через `UserManager<User>`.

**Чому JWT, а не сесії/cookie?** Фронт (Next.js) і API — різні домени, а ще буде мобільна версія.
JWT — це підписаний «пропуск»: сервер перевіряє підпис і не ходить у БД на кожен запит.

**Два токени:**
- **Access** (15 хв) — для запитів. Короткий, бо його не можна відкликати до закінчення терміну.
- **Refresh** (7 днів) — лише щоб отримати новий access. Зберігається в БД → його **можна** відкликати (logout).

**Чому хеш refresh-токена в БД?** Якщо базу вкрадуть, хеші марні — з хешу не відновити токен.
Так само, як з паролями.

**Ротація і виявлення крадіжки:** при кожному `/auth/refresh` старий токен відкликається, видається новий.
Якщо хтось прийде зі старим (вже використаним) токеном — значить, токен вкрали → відкликаємо всі сесії.

**Як фронт це використовує:**
1. `POST /auth/login` → зберегти `accessToken` і `refreshToken`.
2. Кожен запит: `Authorization: Bearer <accessToken>`.
3. Отримали `401` → `POST /auth/refresh` з `refreshToken` → нова пара → повторити запит.
4. Refresh теж `401` → на сторінку входу.

## 7. Rate limiting, CORS, секрети

- **Rate limiting** — не більше 5 запитів на хвилину з однієї IP на login/register/forgot/reset.
  Захист від перебору паролів і спаму листами. Налаштування: `RateLimiting:Auth` в `appsettings.json`.
- **CORS** — браузер не дасть сайту `https://front.com` звертатися до `https://api.com`, якщо API явно не дозволив.
  Дозволені адреси — `Cors:AllowedOrigins`. Не використовуємо `AllowAnyOrigin` — це дірка.
- **Секрети** (ключ JWT, паролі SMTP/БД) — ніколи в git:
  - локально — `dotnet user-secrets` (зберігаються у твоєму профілі Windows, поза репозиторієм);
  - в Azure — App Settings (змінні середовища) і GitHub Environment secrets.

  `appsettings.Development.json` містить лише рядок підключення до **локального** Docker-PostgreSQL
  з паролем `postgres` — це не секрет, він існує тільки на твоєму комп'ютері.

## 8. EF Core і міграції

**Міграція** — C#-файл з інструкціями «як змінити схему БД»: створити таблицю, додати колонку.
Вони зберігаються в git (`Infrastructure/Persistence/Migrations`), тож у всіх розробників і на серверах однакова схема.

```bash
# змінив сутність/конфігурацію → створити міграцію
dotnet ef migrations add AddPosts -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api -o Persistence/Migrations

# застосувати до БД
dotnet ef database update -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api

# скасувати ОСТАННЮ міграцію, якщо її ще НЕ застосовано на staging/prod
dotnet ef migrations remove -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api

# відкотити БД до попередньої міграції
dotnet ef database update <НазваПопередньоїМіграції> -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api
```

`-p` (project) — де лежать DbContext і міграції; `-s` (startup) — звідки брати конфігурацію (рядок підключення).

**Правило:** міграцію, яка вже застосована на staging/production, не редагуємо — створюємо нову.

**Локально** `database update` вручну не потрібен: у Development `Program.cs` викликає `Database.Migrate()` при старті,
тож після `git pull` з новою міграцією достатньо перезапустити API. На staging/production міграції застосовує CI
(`deploy.yml`) окремим кроком — якщо кілька копій API стартують одночасно, автоміграція при старті може конфліктувати.

Конфігурація таблиць — у класах `IEntityTypeConfiguration<T>` (`UserConfiguration`): довжини полів, індекси,
зв'язки, soft delete (`HasQueryFilter(u => u.DeletedAt == null)` — «видалені» записи автоматично не потрапляють у запити).

## 9. Docker

**Навіщо:** щоб не ставити PostgreSQL, поштовий сервер і т.д. руками, і щоб у всіх було однаково.

- **Образ (image)** — «шаблон» (PostgreSQL 17, наш API). **Контейнер** — запущений екземпляр образу.
- `docker-compose.yml` описує кілька контейнерів разом: `postgres`, `smtp4dev` (ловить листи), `azurite` (емулятор
  Azure Blob Storage). **Volume** (`postgres-data`) — дані БД переживають перезапуск контейнера.
- `src/SocialNetwork.Api/Dockerfile` — як зібрати образ нашого API. Багатоетапний: у великому образі SDK компілюємо,
  у маленький runtime-образ копіюємо лише результат. Цей самий образ запускається в Azure.

```bash
docker compose up -d          # запустити
docker compose ps             # що запущено
docker compose logs -f postgres
docker compose down           # зупинити (дані лишаються)
docker compose down -v        # зупинити і ВИДАЛИТИ дані
```

## 10. Тести

- **Unit** (`tests/.../Unit`) — перевіряють один клас без БД: валідатори, `JwtTokenService`.
- **Integration** (`tests/.../Integration`) — піднімають увесь API в пам'яті (`WebApplicationFactory`)
  і **справжній PostgreSQL у Docker** (Testcontainers), роблять HTTP-запити як фронт.
  Пошта й сховище підмінені фейками (`Fakes.cs`) — з фейкової пошти тест дістає посилання підтвердження.

```bash
dotnet test     # потрібен запущений Docker Desktop
```

## 11. Azure

| Ресурс | Навіщо |
|---|---|
| **Resource Group** | «папка» для всіх ресурсів проєкту (видалив групу — видалив усе) |
| **Azure Database for PostgreSQL Flexible Server** | керована БД: Azure сам робить бекапи, оновлення, моніторинг |
| **Storage Account (Blob)** | файли: аватари, фото, відео. Віддає їх за прямим URL |
| **App Service Plan** | «сервер», на якому крутяться застосунки (B1 — найменший платний) |
| **Web App (App Service)** ×2 | наш Docker-контейнер: `-staging` і `-prod`, з HTTPS на `*.azurewebsites.net` |
| **App Settings** | змінні середовища для контейнера (рядок підключення, ключ JWT...). `Jwt__Key` = `Jwt:Key` |
| **Application Insights** | моніторинг: запити, помилки, доступність `/health` |

**Деплой (GitHub Actions, `.github/workflows/deploy.yml`):** push у `dev` → тести → Docker-образ у GitHub Container
Registry → тимчасово відкриваємо firewall БД для раннера → міграції → закриваємо firewall → App Service тягне новий образ →
перевірка `/health`. Вхід в Azure — через OIDC (federated credential), без збереження паролів у GitHub.

---

## Повтори сам: від нуля

### Інструменти (Windows)
```powershell
wsl --install                          # потім перезавантажити ПК
winget install Docker.DockerDesktop
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.AzureCLI
winget install GitHub.cli
```

### Solution і проєкти
```bash
dotnet new sln -n SocialNetwork --format slnx
dotnet new classlib -n SocialNetwork.Domain         -o src/SocialNetwork.Domain
dotnet new classlib -n SocialNetwork.Application    -o src/SocialNetwork.Application
dotnet new classlib -n SocialNetwork.Infrastructure -o src/SocialNetwork.Infrastructure
dotnet new webapi   -n SocialNetwork.Api            -o src/SocialNetwork.Api --use-controllers
dotnet new xunit    -n SocialNetwork.Tests          -o tests/SocialNetwork.Tests

dotnet sln add src/*/*.csproj tests/*/*.csproj

# посилання між шарами (стрілки "залежить від")
dotnet add src/SocialNetwork.Application    reference src/SocialNetwork.Domain
dotnet add src/SocialNetwork.Infrastructure reference src/SocialNetwork.Application
dotnet add src/SocialNetwork.Api            reference src/SocialNetwork.Application src/SocialNetwork.Infrastructure
dotnet add tests/SocialNetwork.Tests        reference src/SocialNetwork.Api
```

### Пакети
```bash
dotnet add src/SocialNetwork.Domain package Microsoft.Extensions.Identity.Stores

dotnet add src/SocialNetwork.Application package FluentValidation
dotnet add src/SocialNetwork.Application package FluentValidation.DependencyInjectionExtensions

dotnet add src/SocialNetwork.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/SocialNetwork.Infrastructure package EFCore.NamingConventions
dotnet add src/SocialNetwork.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/SocialNetwork.Infrastructure package Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
dotnet add src/SocialNetwork.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
dotnet add src/SocialNetwork.Infrastructure package MailKit
dotnet add src/SocialNetwork.Infrastructure package Azure.Storage.Blobs

dotnet add src/SocialNetwork.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/SocialNetwork.Api package Microsoft.EntityFrameworkCore.Design
dotnet add src/SocialNetwork.Api package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
dotnet add src/SocialNetwork.Api package Scalar.AspNetCore
dotnet add src/SocialNetwork.Api package Serilog.AspNetCore

dotnet add tests/SocialNetwork.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/SocialNetwork.Tests package Testcontainers.PostgreSql
dotnet add tests/SocialNetwork.Tests package NSubstitute

dotnet new tool-manifest && dotnet tool install dotnet-ef
```

### Порядок написання коду (так само додається будь-який новий модуль)
1. **Domain** — сутність (`Domain/Users/User.cs`).
2. **Application** — інтерфейс репозиторію, Request/Response, валідатор, інтерфейс + реалізація сервісу,
   реєстрація сервісу в `Application/DependencyInjection.cs`.
3. **Infrastructure** — `IEntityTypeConfiguration`, репозиторій, реєстрація в `Infrastructure/DependencyInjection.cs`.
4. **Api** — контролер.
5. Міграція → `database update`.
6. Тести.

### Локальний запуск
```bash
docker compose up -d
dotnet user-secrets set "Jwt:Key" "<будь-який рядок 32+ символи>" --project src/SocialNetwork.Api   # один раз
dotnet run --project src/SocialNetwork.Api      # міграції застосуються автоматично
# http://localhost:5298/scalar — документація API, http://localhost:5080 — листи
```

### Azure
Покрокові команди створення ресурсів будуть у `docs/azure-setup.md` (додається на етапі деплою).
