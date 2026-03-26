# Cấu trúc solution và vai trò các nhóm file

Tài liệu mô tả kiến trúc solution **Ecommerce API** và **mục đích** của từng project / thư mục. Các thư mục build (`bin/`, `obj/`), file sinh tự động (`*.AssemblyInfo.cs`) và cache IDE (`.vs/`) không được liệt kê vì không phải mã nguồn bảo trì thủ công.

---

## Tổng quan kiến trúc

Solution theo hướng **Clean Architecture** (gần):

- **Domain** — không phụ thuộc EF hay HTTP.
- **Application** — use case, contract, authorization ứng dụng.
- **Infrastructure** — triển khai EF Core, Redis, JWT, repository.
- **API** — host ASP.NET Core, HTTP surface.
- **UnitTests** — kiểm thử Application/Infrastructure.

| Project | Vai trò |
|--------|---------|
| **Ecommerce.Domain** | Entity, interface repository, filter/settings/pagination dùng chung ở tầng domain. |
| **Ecommerce.Application** | Service, DTO, policy/handler authorization, exception, helper ứng dụng. |
| **Ecommerce.Infrastructure** | DbContext, migration, repository, Redis, JWT, hash mật khẩu, dịch vụ hạ tầng. |
| **Ecommerce.API** | `Program`, controller, middleware, đăng ký DI/Swagger/JWT. |
| **Ecommerce.UnitTests** | Unit test service, JWT, policy, session, cache key, v.v. |

File **`Ecommerce.sln`** (ở root repo): mở và build toàn bộ project.

---

## Ecommerce.API

| Nhóm / file | Tác dụng |
|-------------|----------|
| **`Program.cs`** | Khởi tạo app, pipeline HTTP, gọi extension đăng ký dịch vụ. |
| **`Controllers/*.cs`** | Map HTTP → service: Auth, User, Role, Permission, Product, Category. |
| **`BaseController.cs`** | Phần dùng chung cho controller (format response, claim user, v.v.). |
| **`Middleware/GlobalExceptionMiddleware.cs`** | Bắt exception, trả response lỗi thống nhất. |
| **`Middleware/SessionValidationMiddleware.cs`** | Kiểm tra phiên/JWT theo luồng session (invalidation, version). |
| **`Extensions/*Extensions.cs`** | Gom cấu hình: JWT (`AutheticationExtensions.cs`), Swagger, Infrastructure, Application, Authorization. |
| **`appsettings.json`**, **`appsettings.Development.json`** | Connection string, JWT, Redis, logging, biến môi trường. |
| **`Properties/launchSettings.json`** | Profile chạy debug (URL, env). |

---

## Ecommerce.Application

| Nhóm | Tác dụng |
|------|----------|
| **`Services/Interfaces/*.cs`** | Hợp đồng use case: Auth, User, Product, Category, Role, Permission, Order, cache, session invalidation, token blacklist, device, session validation, v.v. |
| **`Services/Implementations/*.cs`** | Logic nghiệp vụ, gọi repo/cache; map entity ↔ DTO. |
| **`DTOs/**`** | Model vào/ra API (Login, User, Role, Permission, Product, Category, Order, refresh token…). |
| **`Authorization/*`** | Policy động theo permission: `PermissionPolicyProvider`, `PermissionAttribute`, `PermissionRequirement`, `PermissionAuthorizationHandler`. |
| **`Common/Auth/*`** | Khóa đăng nhập theo user, trạng thái session (hỗ trợ invalidate, tránh race). |
| **`Common/Caching/CacheKeyGenerator.cs`** | Chuẩn hóa key Redis (category/role/permission, blacklist token, session). |
| **`Common/Pagination/*`**, **`Common/Responses/*`** | Phân trang, envelope API (`ApiResponse`, lỗi). |
| **`Exceptions/*.cs`** | Exception ứng dụng (NotFound, BadRequest, Business, Conflict, Forbidden, TooManyRequests…). |
| **`Extensions/ClaimsPrincipalExtensions.cs`** | Đọc `userId` (và claim liên quan) từ `ClaimsPrincipal`. |

---

## Ecommerce.Domain

| Nhóm | Tác dụng |
|------|----------|
| **`Entities/*.cs`** | Model nghiệp vụ map DB: User, Role, Permission, RolePermission, Product, Category, Order, OrderItem, RefreshToken, BaseEntity… |
| **`Interfaces/I*Repo.cs`** | Hợp đồng truy cập dữ liệu (User, Role, Permission, Product, Category, Order, RefreshToken). |
| **`Interfaces/IUnitOfWork.cs`** | Đơn vị công việc / commit giao dịch (nếu dùng). |
| **`Interfaces/IJwtService.cs`**, **`IPasswordHasher.cs`** | Abstraction để Application không phụ thuộc triển khai JWT/hash cụ thể. |
| **`Common/Filters/*`** | Tham số lọc query (product, category). |
| **`Common/Pagination/PaginationDto.cs`** | Tham số phân trang dùng ở repo/service. |
| **`Common/Settings/*`** | `JwtSettings`, `AuthSecuritySettings` — cấu hình strongly-typed. |
| **`Common/*`** (vd. `UserAuthState`, `OrderLineInput`) | Kiểu phụ trợ domain. |
| **`Enums/*`** | Enum nghiệp vụ (vd. trạng thái đơn hàng). |
| **`Interfaces/OrderPlaceResult.cs`** | Kiểu kết quả đặt hàng ở tầng domain. |

---

## Ecommerce.Infrastructure

| Nhóm | Tác dụng |
|------|----------|
| **`Data/AppDbContext.cs`** | DbContext EF Core, DbSet. |
| **`Data/Configurations/*.cs`** | Fluent API map entity → bảng/cột/index. |
| **`Data/UnitOfWork.cs`** | Triển khai `IUnitOfWork`. |
| **`Data/AppDbContextFactory.cs`** | Design-time factory (CLI migration). |
| **`Data/Seed/DataSeeder.cs`** | Seed dữ liệu ban đầu (nếu có). |
| **`Repositories/*.cs`** | Triển khai `I*Repo` bằng EF. |
| **`Migrations/*.cs`** + **`AppDbContextModelSnapshot.cs`** | Lịch sử thay đổi schema; snapshot model hiện tại. |
| **`SecurityHelpers/JwtService.cs`** | Tạo/validate JWT (`IJwtService`). |
| **`SecurityHelpers/PasswordHasher.cs`** | Hash/verify mật khẩu. |
| **`SecurityHelpers/SecurityFingerprintHelper.cs`** | Fingerprint thiết bị / bảo mật session. |
| **`RedisCaching/RedisCacheService.cs`** | Triển khai `ICacheService` qua Redis. |
| **`Services/DeviceService.cs`**, **`TokenBlacklistService.cs`** | Triển khai interface Application tương ứng. |

---

## Ecommerce.UnitTests

| Nhóm | Tác dụng |
|------|----------|
| **`*ServiceTests.cs`**, **`AuthServiceTests.cs`**, **`OrderServiceTests.cs`** | Kiểm thử hành vi service (mock dependency). |
| **`JwtServiceTests.cs`** | Kiểm thử JWT. |
| **`PermissionPolicyProviderTests.cs`**, **`PermissionAuthorizationHandlerTests.cs`** | Kiểm thử policy/authorize. |
| **`SessionValidationServiceTests.cs`**, **`UserSessionInvalidationServiceTests.cs`** | Kiểm thử luồng session. |
| **`CacheKeyGeneratorTests.cs`** | Kiểm thử format key cache. |
| **`Helpers/TestDataMother.cs`** | Dữ liệu mẫu / object mother cho test. |

---

## Thư mục `docs/`

Tài liệu bổ sung trong cùng folder:

- **`AUTH_POLICY_AND_PUBLIC_API.md`** — chính sách permission & API công khai.
- **`ACCESS_REFRESH_TOKEN_FLOW.md`** — luồng access/refresh token.
- **`LOGGING.md`** — ghi log.
- **`UNIT_TEST_PLAN_SERVICES.md`** — kế hoạch/kịch bản test service.
- **`BAO_CAO_KY_THUAT_Ecommerce_API.md`** — báo cáo kỹ thuật (nếu có).
- **`PROJECT_STRUCTURE_AND_FILES.md`** — tài liệu này.

---

## Ghi chú

- Mỗi file `.cs` trong `src` thường thuộc một trong: **HTTP (API)**, **nghiệp vụ + contract (Application)**, **mô hình + interface dữ liệu (Domain)**, **triển khai kỹ thuật (Infrastructure)**, hoặc **test (UnitTests)**.
- **Migration** là lịch sử schema; chỉnh sửa cần hiểu EF Core migrations.
- Cấu hình **JSON** điều khiển hành vi runtime (connection, JWT, Redis, logging).
