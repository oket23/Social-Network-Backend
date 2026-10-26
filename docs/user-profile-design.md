# Особистий кабінет користувача — дизайн

## Поля профілю

| Поле (API) | Колонка БД | Тип | Обов'язкове | Редагується через PUT /users/me | Примітка |
|---|---|---|---|---|---|
| id | id | uuid | так (генерується сервером) | ні | |
| username | user_name | string(30) | так | так | унікальне (без урахування регістру), `^[a-zA-Z0-9_.]+$`, 3–30 символів |
| email | email | string(256) | так | ні (зміна email — окрема функція з підтвердженням) | унікальне |
| emailConfirmed | email_confirmed | bool | так | ні (через посилання з листа) | |
| — | password_hash | string | так | ні (`PUT /users/me/password`) | ніколи не повертається в API |
| displayName (ім'я) | display_name | string(50) | так | так | |
| bio | bio | string(160) | ні | так | |
| websiteUrl | website_url | string(200) | ні | так | лише http/https URL |
| avatarUrl | avatar_url | string(500) | ні | ні (`POST /users/me/avatar`) | URL у S3-сховищі (Supabase Storage / локально RustFS) |
| isPrivate | is_private | bool | так (default `false`) | так | |
| createdAt | created_at | timestamptz | так (ставиться сервером) | ні | UTC |
| — | deleted_at | timestamptz | ні | ні | soft delete |

## Рішення: реєстрація одно- чи багатокрокова

**Прийнято: одностадійна реєстрація** (email + username + displayName + password в одному запиті).
Причина: менше проміжних станів користувача в БД (немає "недореєстрованих" акаунтів),
простіше фронту й бекенду. Після реєстрації користувач одразу отримує токени; email підтверджується
окремо за посиланням з листа (вхід без підтвердження дозволений, поле `emailConfirmed` показує стан).

## Схема БД

Один клас `User` (наслідує `IdentityUser<Guid>`), у якому суміщені обліковий запис і профіль (таблиця `users`).
Повна схема й заплановані зв'язки (Follow, Post тощо) — у [architecture.md](architecture.md#схема-бд).

## Автентифікація

Реалізована (ASP.NET Core Identity + JWT). Id поточного користувача береться з claim `sub` токена
(`HttpContextCurrentUserService`). Тимчасова заглушка `DevCurrentUserService` і seed-користувач `dev_user` видалені.

## API-контракт (для фронту)

Усі відповіді обгорнуті: `{ "success": true, "message": "...", "data": ... }`.

- `GET /users/me` → `data: UserProfileResponse`
- `PUT /users/me` ← `{ username, displayName, bio?, websiteUrl?, isPrivate }` → `data: UserProfileResponse`
- `PUT /users/me/password` ← `{ currentPassword, newPassword }` → `{ success, message }`; усі інші сесії завершуються
- `POST /users/me/avatar` (multipart/form-data, поле `file`, JPEG/PNG/WEBP ≤ 5 МБ) → `data: UserProfileResponse`

Помилки: `400` (валідація, `errors` по полях), `401` (немає/прострочений токен), `409` (username зайнятий).

Форми DTO — у `src/SocialNetwork.Application/Users/Requests` і `Responses`.
