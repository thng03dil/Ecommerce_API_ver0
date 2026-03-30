# Kiến thức cần có để làm việc với dự án Ecommerce API

Tài liệu này liệt kê các chủ đề bạn nên nắm để đọc code, chạy dự án, và phát triển tiếp. Thứ tự gần đúng từ nền tảng → chuyên sâu theo stack hiện tại (`.NET 8`, kiến trúc nhiều lớp, SQL Server, Redis, Stripe).

---

## 1. Nền tảng lập trình

- **C# hiện đại** (nullable reference types, `async`/`await`, LINQ, records khi dùng DTO, pattern matching cơ bản).
- **.NET 8** và **ASP.NET Core**: middleware pipeline, `Program.cs` minimal hosting, configuration (`appsettings`, options pattern).
- **REST API**: HTTP methods, status codes, JSON, versioning API (nếu sau này mở rộng).

---

## 2. Kiến trúc phần mềm (rất quan trọng với repo này)

Dự án chia **Clean Architecture / layered**:

| Layer | Vai trò |
|--------|--------|
| `Ecommerce.API` | HTTP, controllers, Swagger, auth middleware, đăng ký DI |
| `Ecommerce.Application` | Use cases, DTO, services, exceptions ứng dụng |
| `Ecommerce.Domain` | Entities, enums, interfaces repository, settings domain |
| `Ecommerce.Infrastructure` | EF Core, migrations, repository thực thi, Redis, seed |

Cần hiểu:

- **Phụ thuộc hướng vào trong**: Domain không phụ thuộc Infrastructure; API phụ thuộc Application + Infrastructure.
- **Dependency Injection** trong ASP.NET Core: đăng ký interface → implementation, lifetime (`Scoped`/`Singleton`/`Transient`).
- **Repository pattern** và **Unit of Work** (nếu có trong code — thường qua `DbContext`).
- **DTO** vs entity: tách model API khỏi domain để tránh lộ schema và kiểm soát contract.

---

## 3. ASP.NET Core Web API

- **Controllers** và model binding, validation (`ModelState`, data annotations / FluentValidation nếu dùng).
- **Global exception handling** / middleware xử lý lỗi thống nhất (nếu project có).
- **Swagger / OpenAPI** (Swashbuckle): đọc và thử API trên UI.

---

## 4. Cơ sở dữ liệu & Entity Framework Core

- **SQL Server** cơ bản: bảng, khóa, quan hệ, transaction.
- **EF Core 8**: `DbContext`, `DbSet`, fluent configuration (`IEntityTypeConfiguration`), migrations.
- Lệnh thường dùng: `dotnet ef migrations add`, `dotnet ef database update` (cần cấu hình connection string đúng project startup).

---

## 5. Xác thực & phân quyền

- **JWT Bearer**: access token, cấu hình `JwtBearerOptions`, claims.
- **ASP.NET Core Authorization**: policies, `[Authorize]`, roles/permissions tùy custom (project có authorization tùy chỉnh trong Application).
- **Băm mật khẩu**: **BCrypt** (package `BCrypt.Net-Next` trong Infrastructure).
- **Bảo mật cấu hình**: User Secrets, biến môi trường, file `.env` (project dùng **DotNetEnv** — bootstrap tải `.env` nếu có).

---

## 6. Thanh toán (Stripe)

- Luồng thanh toán **Stripe** (Checkout / PaymentIntent tùy implementation hiện tại trong `OrderPaymentService` và cấu hình `StripeSettings`).
- Webhook (nếu có): chữ ký, idempotency, xử lý bất đồng bộ.
- Đọc tài liệu Stripe phù hợp với flow API đang dùng.

---

## 7. Cache & hiệu năng

- **Redis** với **StackExchange.Redis** / `Microsoft.Extensions.Caching.StackExchangeRedis`: distributed cache, invalidation cơ bản.
- Khi nào cache vs đọc trực tiếp DB.

---

## 8. Logging & quan sát

- **Serilog**: sinks Console, File, Seq; cấu hình trong `appsettings` / code.
- Correlation ID / structured logging (nếu project áp dụng — xem `docs/LOGGING.md` nếu có).

---

## 9. Container & triển khai cục bộ

- **Docker**: image, `Dockerfile`, build context.
- **Docker Compose**: orchestrate SQL Server, Redis, API; `depends_on`, healthcheck, biến môi trường cho connection strings.

---

## 10. Kiểm thử

- **xUnit**: viết và chạy test (`dotnet test`).
- **Moq**: mock interface (services, repositories).
- **FluentAssertions**: assert dễ đọc.
- **EF Core InMemory** (hoặc test DB): test tầng data/repository khi cần.

---

## 11. Công cụ phát triển

- **Visual Studio** hoặc **VS Code** + C# Dev Kit / OmniSharp.
- **Git**: branch, merge, đọc diff.
- **dotnet CLI**: `build`, `run`, `test`, `user-secrets`.

---

## Gợi ý lộ trình học ngắn gọn

1. C# + .NET 8 + ASP.NET Core Web API (REST, DI, config).  
2. EF Core + SQL Server + migrations.  
3. JWT + authorization policies trong ASP.NET Core.  
4. Đọc luồng nghiệp vụ trong `Application` (order, auth, admin) rồi mapping sang `Infrastructure` và `API`.  
5. Redis cache khi chạm phần listing/cache.  
6. Stripe theo đúng flow trong code.  
7. Docker Compose để chạy full stack local.  
8. Viết/mở rộng unit test với xUnit + Moq.

---

*Tài liệu được tạo dựa trên cấu trúc và package của repository tại thời điểm chỉnh sửa; khi thêm công nghệ mới, nên cập nhật mục tương ứng.*
