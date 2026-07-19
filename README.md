# TestTask — WebAPI для обработки timescale-данных

Решение тестового задания «Разработчик C#». .NET 10, EF Core, PostgreSQL, Swagger, Docker.

## Структура решения

```
TestTask.sln
src/
  TestTask.Api/              # ASP.NET Core WebAPI, контроллеры, Swagger, DI, Dockerfile
  TestTask.Core/             # Сущности, DTO, интерфейсы сервисов, доменные исключения
  TestTask.Infrastructure/   # EF Core DbContext, миграции, реализация сервисов (парсинг CSV, агрегация, фильтрация)
tests/
  TestTask.Tests/            # xUnit-тесты на валидацию CSV, агрегацию, overwrite, фильтрацию, last-10
docker-compose.yml           # postgres + api
```

## API

1. **POST `/api/values/upload`** (multipart/form-data, поле `file`) — парсит CSV,
   валидирует построчно (дата в диапазоне [2000-01-01, now], ExecutionTime и Value ≥ 0,
   1..10000 строк, все поля обязательны), сохраняет строки в `Values` и пересчитывает
   агрегаты в `Results`. При повторной загрузке файла с тем же именем — старые данные
   перезаписываются. Любая ошибка валидации откатывает транзакцию и возвращает `400`
   со списком ошибок.
2. **GET `/api/results`** — список `Results` с фильтрами по `fileName`,
   `startDateFrom/To`, `averageValueFrom/To`, `averageExecutionTimeFrom/To`.
3. **GET `/api/values/last?fileName=...&take=10`** — последние 10 значений по файлу,
   отсортированные по `Date` (сначала самые свежие).

Формат CSV (как в задании, время через дефис, не через двоеточие):
```
Date;ExecutionTime;Value
2024-01-01T10-00-00.0000Z;1.5;10
```

## Запуск локально (без Docker)

```bash
# 1. Поднять PostgreSQL (например, локально или через docker)
docker run -d --name pg -e POSTGRES_DB=testtask -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine

# 2. Применить миграции и запустить API
cd src/TestTask.Api
dotnet run
# Swagger: http://localhost:5080/swagger
```

## Запуск через Docker Compose

```bash
docker compose up --build
# API:  http://localhost:8080/swagger
```

Миграции применяются автоматически при старте API (`db.Database.Migrate()` в `Program.cs`).

## Тесты

```bash
dotnet test
```

Покрыты: успешная обработка CSV и расчёт агрегатов; overwrite при повторной загрузке
одноимённого файла; отклонение файлов с будущей/слишком старой датой, отрицательными
значениями, пропущенными полями, 0 или >10000 строк, неверным заголовком; фильтрация
`Results` по всем видам диапазонов; выборка последних N значений.
