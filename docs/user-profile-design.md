# Особистий кабінет користувача — дизайн

## Поля профілю

| Поле | Тип | Обов'язкове | Редагується через PUT /users/me | Примітка |
|---|---|---|---|---|
| Id | Guid | так (генерується сервером) | ні | |
| Username | string(30) | так | так | унікальне, `^[a-zA-Z0-9_]+$`, 3–30 символів |
| Email | string(256) | так | ні (зміна email — окрема функція з підтвердженням) | унікальне |
| PasswordHash | string | так | ні (окремий ендпоінт `PUT /users/me/password`) | ніколи не повертається в DTO |
| DisplayName (ім'я) | string(50) | так | так | |
| Bio | string(160) | ні | так | |
| WebsiteUrl | string(200) | ні | так | валідний URL |
| AvatarUrl | string | ні | ні (через `POST /users/me/avatar`) | |
| IsPrivate | bool | так (default `false`) | так | |
| CreatedAt | DateTime | так (ставиться сервером) | ні | |

## Рішення: реєстрація одно- чи багатокрокова

**Прийнято: одностадійна реєстрація** (email + username + password в одному запиті).
Причина: менше проміжних станів користувача в БД (немає "недореєстрованих" акаунтів),
простіше фронту й бекенду на старті проєкту. Реалізація реєстрації/логіну (JWT) —
окрема картка; наразі `GET/PUT /users/me` використовують тимчасову заглушку
`DevCurrentUserService` (див. нижче).

## Схема БД

Один проєкт — один клас `User`, у якому суміщені обліковий запис і профіль
(без окремої таблиці `Profile`), щоб не ускладнювати на старті:

```
Users
├── Id (PK, uniqueidentifier)
├── Username (unique)
├── Email (unique)
├── PasswordHash
├── DisplayName
├── Bio (nullable)
├── WebsiteUrl (nullable)
├── AvatarUrl (nullable)
├── IsPrivate
└── CreatedAt
```

Заплановані зв'язки на майбутнє (не реалізовані в цій картці):
- **Follow**: таблиця `Follows(FollowerId → Users.Id, FollowingId → Users.Id)`, складений PK/унікальний індекс на парі, self-referencing many-to-many.
- **Post**: таблиця `Posts(Id, AuthorId → Users.Id, ...)`, one-to-many `User → Posts`.

## Тимчасова автентифікація

Реальної реєстрації/логіну ще немає. `ICurrentUserService` (інтерфейс) /
`DevCurrentUserService` (реалізація) завжди повертають Id seed-користувача
`dev_user` (`11111111-1111-1111-1111-111111111111`). Коли буде готовий
логін на JWT — замінити реєстрацію DI в `Program.cs` на реалізацію, що бере
`userId` з claims токена; контролер і DTO міняти не потрібно.

## API-контракт (для фронту)

- `GET /users/me` → `UserProfileDto`
- `PUT /users/me` ← `UpdateUserProfileRequest` → `UserProfileDto`
- `PUT /users/me/password` ← `ChangePasswordRequest` → `204 No Content`
- `POST /users/me/avatar` (multipart/form-data, поле `file`) → `UserProfileDto`

Форми DTO — у `SocialNetwork.Api/Dtos/`.
