# CodeHub — ASP.NET Core 10 Minimal API (بازنویسی از FastAPI)

بازنویسی پروژه CodeHub (پایتون / FastAPI) با ASP.NET Core 10، Minimal API، معماری Clean و CQRS.

## ساختار Solution

```
CodeHub.slnx
├── CodeHub.Domain           # Entity ها، Enum ها، Value Object ها (بدون هیچ وابستگی)
├── CodeHub.Application      # CQRS (Command/Query/Handler)، Abstraction ها، Contract ها، Validation
├── CodeHub.Infrastructure   # دو DbContext (خواندن/نوشتن)، Repository ها، JWT، BCrypt، AI Client، Storage
└── CodeHub.Api              # Minimal API Endpoints، Middleware، Exception Handler، DI
```

- **CQRS**: کلاس‌های `ICommand` / `IQuery` + `IDispatcher` در `Application/Common/Cqrs.cs`. Command ها از `IUnitOfWork` (DbContext نوشتن) و Query ها از `CodeHubReadDbContext` (DbContext خواندن) استفاده می‌کنند.
- **تفکیک آینده دیتابیس**: هر دو DbContext از یک رشته اتصال استفاده می‌کنند. برای جدا کردن، فقط `ConnectionStrings:Read` را در تنظیمات عوض کنید؛ مدل و Repository ها بدون تغییر باقی می‌مانند.
- **جهت وابستگی‌ها**: `Api → Application + Infrastructure`، `Infrastructure → Application`، `Application → Domain`، `Domain → هیچ‌چیز`

## اجرا

### لوکال (نیازمند PostgreSQL روی 5432)
```bash
dotnet run --project CodeHub.Api
# Swagger UI:   http://localhost:xxxx/swagger
# OpenAPI JSON: /swagger/v1/swagger.json (و /openapi/v1.json در Development)
```

### داکر (آماده سرور)
```bash
docker compose up -d --build
# API:        http://localhost:2002/api/v1/...
# Swagger UI: http://localhost:2002/swagger
# pgAdmin:    http://localhost:5053
```
- Migrat ها هنگام استارتاپ به‌صورت خودکار اعمال می‌شوند (`Database:MigrateOnStartup`).
- فایل‌های آپلودی روی volume ی `storage_data` نگه داشته می‌شوند.
- کلیدهای SambaNova و JWT Secret با متغیرهای محیطی قابل override هستند (`.env` کنار compose یا environment).

## نکات سازگاری با کلاینت

- همه مسیرها، متدها، ورودی‌ها و خروجی‌ها عیناً مطابق نسخه FastAPI است.
- JSON کاملاً snake_case است (`is_success`، `access_token`، `repository_id`، …) و enum ها به‌صورت `"public"` / `"private"` سریالایز می‌شوند.
- تاریخ‌های ریپازیتوری/فایل با همان فرمت `YYYY-MM-DD | HH:mm:ss` برگردانده می‌شوند؛ پاسخ‌های خطا همان کدها و تقریباً همان پیام‌ها را دارند.
- هش پسورد دقیقاً مانند نسخه پایتون است (SHA-256 → Base64 → bcrypt)؛ پسورد کاربران فعلی دیتابیس بدون تغییر کار می‌کند.

## تغییرات عمدی نسبت به نسخه پایتون (رفع باگ‌ها)

1. **جستجوی AI فقط روی ریپازیتوری‌های public** انجام می‌شود (در پایتون ریپازیتوری‌های private هم به مدل داده می‌شد و در نتایج برمی‌گشت).
2. **شمارش نتایج جستجو** (`total_count`) با همان شرطی که آیتم‌ها فیلتر می‌شوند محاسبه می‌شود (در پایتون فقط نام جستجو می‌شد ولی آیتم‌ها روی نام + توضیحات).
3. توکن‌های نادرست/منقضی همیشه 401 با پاکت `ApiResponse` برمی‌گردانند.
4. آپلود مسیرهای شامل `..` قبل از ذخیره‌سازی فایل رد می‌شود و ورودی‌های zip از zip-slip محافظت می‌شوند.

## وضعیت فازها

- [x] فاز ۰ — زیرساخت پایه (Solution، لایه‌ها، پکیج‌ها)
- [x] فاز ۱ — Domain (Entity ها، Value Object ها، Enum)
- [x] فاز ۲ — Application (CQRS، Abstractions، Contract ها، Handler ها، Validation، Mapping)
- [x] فاز ۳ — Infrastructure (دو DbContext، Repository ها، JWT، BCrypt+SHA256، SambaNova AI، Local Storage، Migration)
- [x] فاز ۴ — Api (همه ۲۶ اندپوینت، Exception Handler، Validation Filter، JWT Bearer، CORS)
- [x] فاز ۵ — داکرایز (Dockerfile چندمرحله‌ای، docker-compose با db/api/pgadmin، migration خودکار)
- [x] Swagger UI با پشتیبانی JWT (دکمه Authorize برای اندپوینت‌های محافظت‌شده)
