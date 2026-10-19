# CLAUDE.md

Інструкції для Claude Code (і для людей) щодо роботи з цим репозиторієм.
Спілкування, коментарі в коді, повідомлення помилок для користувача та коміти — **українською**.

## Проєкт

Backend навчального клону Instagram (команда: 2 дизайнери + 2 розробники; фронт — Next.js, окремий репозиторій).
ASP.NET Core Web API на .NET 10, PostgreSQL, ASP.NET Core Identity + JWT.
Детально: [docs/architecture.md](docs/architecture.md), пояснення «навіщо» — [docs/learning-guide.md](docs/learning-guide.md).

## Архітектура — модульний моноліт (Clean Architecture)

```
src/SocialNetwork.Domain          сутності (нічого не знає про інші шари)
src/SocialNetwork.Application     сервіси, інтерфейси, Request/Response, валідатори, винятки   → Domain
src/SocialNetwork.Infrastructure  EF Core, репозиторії, UnitOfWork, JWT, SMTP, Blob Storage     → Application
src/SocialNetwork.Api             контролери, middleware, фільтри, Program.cs                   → Application, Infrastructure
tests/SocialNetwork.Tests         xUnit: Unit/ і Integration/ (Testcontainers PostgreSQL)
```

Модуль = папка з однаковою назвою в кожному шарі (`Users`, `Auth`, далі `Posts`, `Stories`, `Direct`...).

### Правила (обов'язкові)

- Контролер **не** звертається до `AppDbContext`/репозиторіїв — лише до сервісу `I<Name>Service`. Контролер тонкий.
- Application **не** посилається на EF Core, ASP.NET Core, Azure SDK. Потрібна зовнішня річ → інтерфейс у
  `Application/Common/Abstractions` або в модулі, реалізація — в Infrastructure.
- Модуль не використовує **репозиторії** іншого модуля — лише його сервіс-інтерфейс.
- Зміни зберігаються через `IUnitOfWork.SaveChangesAsync`, не через репозиторій.
- Для змін завантажуй сутність через `Set` (з трекінгом), для читання — `Query` (`AsNoTracking`).
- Бізнес-помилки — винятки з `Application/Common/Exceptions` (`NotFoundException`, `BadRequestException`,
  `ConflictException`, `UnauthorizedException`, `ForbiddenException`). Не повертати `null`/коди помилок, не писати try/catch у контролерах.
- Відповіді контролерів: `Ok(new ApiSuccessResponse<T>(data, "повідомлення"))` або `Ok(new ApiSuccessResponse("повідомлення"))`.
- Час — лише `DateTime.UtcNow`.
- Секрети (ключі, паролі) — ніколи в коді чи `appsettings*.json`. Локально — `dotnet user-secrets`, в Azure — App Settings.
- Застосовані на staging/prod міграції не редагувати — лише нова міграція.
- Нові ендпоінти, що змінюють чужі дані, — перевірка власника в сервісі (403 через `ForbiddenException`).

### Конвенції

- File-scoped namespaces, `_camelCase` для приватних полів, DI через конструктор.
- Імена: `<Action><Entity>Request`, `<Entity>Response`, `<Request>Validator`, `<Entity>Configuration`,
  `I<Entity>Repository` / `<Entity>Repository`, `I<Module>Service` / `<Module>Service`.
- Методи сервісів/репозиторіїв — без суфікса `Async` (як `GetMe`, `GetById`), останній параметр `CancellationToken ct = default`.
- Маршрути: множина, kebab-case (`/users/me`, `/auth/forgot-password`). Захищені контролери — `[Authorize]`.
- Повідомлення для користувача — українською.
- Таблиці/колонки в БД — snake_case автоматично (`UseSnakeCaseNamingConvention`), у `ToTable("...")` писати snake_case.

## Рецепт: нова фіча / новий модуль

1. **Domain:** `Domain/<Module>/<Entity>.cs` (наслідувати `BaseEntity`; `ISoftDeletable`, якщо потрібне м'яке видалення).
2. **Application:**
   - `Application/<Module>/I<Entity>Repository.cs` (наслідує `IRepository<T>`);
   - `Requests/`, `Responses/`, `Validators/`, `<Entity>Mapper.cs`;
   - `I<Module>Service.cs` + `<Module>Service.cs`;
   - зареєструвати сервіс у `Application/DependencyInjection.cs`.
3. **Infrastructure:**
   - `Persistence/Configurations/<Module>/<Entity>Configuration.cs` (довжини, індекси, зв'язки, query filter);
   - `Persistence/Repositories/<Entity>Repository.cs` (наслідує `RepositoryBase<T>`);
   - зареєструвати репозиторій у `Infrastructure/DependencyInjection.cs`.
4. **Api:** `Controllers/<Module>Controller.cs` з `[ProducesResponseType]` для документації.
5. **Міграція:** `dotnet ef migrations add <Name> -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api -o Persistence/Migrations`,
   переглянути згенерований файл.
6. **Тести:** інтеграційні в `tests/SocialNetwork.Tests/Integration` (див. `UsersTests.cs` як приклад).
7. Оновити `docs/architecture.md` (модулі/схема БД), якщо змінилася архітектура.

## Команди

```bash
docker compose up -d                                   # PostgreSQL :5432, smtp4dev :5080 (UI) / :2525, Azurite :10000
dotnet build SocialNetwork.slnx
dotnet test SocialNetwork.slnx                         # потрібен запущений Docker
dotnet run --project src/SocialNetwork.Api             # http://localhost:5298/scalar
dotnet tool restore
dotnet ef migrations add <Name> -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api -o Persistence/Migrations
dotnet ef database update -p src/SocialNetwork.Infrastructure -s src/SocialNetwork.Api
```

Перед комітом: `dotnet build` без попереджень і `dotnet test` зелений.

## Git

- Гілки: `feature/<опис>` або `fix/<опис>` від актуальної `dev`; PR у `dev`; `main` — лише через PR з `dev`.
- Push у `dev` автоматично деплоїть на staging, у `main` — на production (`.github/workflows/deploy.yml`).
- Коміти — Conventional Commits українською: `feat: додано підписки`, `fix: ...`, `refactor: ...`, `test: ...`, `docs: ...`.
- Не пушити напряму в `dev`/`main`, не комітити секрети й `.env`.
