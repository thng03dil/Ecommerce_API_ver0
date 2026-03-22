# Báo cáo kỹ thuật chi tiết: Ecommerce_API_ver0

---

## 1. Tổng quan kiến trúc

### 1.1 Mô hình Clean Architecture

Dự án áp dụng **Clean Architecture** (hay còn gọi là Onion Architecture) với bốn layer chính, phụ thuộc hướng vào trong (dependencies point inward):

```
┌─────────────────────────────────────────────────────────────────┐
│                      Ecommerce.API (Presentation)                │
│  Controllers, Middleware, Program.cs                             │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Ecommerce.Application (Use Cases)             │
│  Services, DTOs, Exceptions, Pagination, Caching, Authorization   │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Ecommerce.Domain (Core)                     │
│  Entities, Interfaces (I*Repo, IJwtService), Domain Logic          │
└─────────────────────────────────────────────────────────────────┘
                                    ▲
                                    │
┌─────────────────────────────────────────────────────────────────┐
│                  Ecommerce.Infrastructure (Details)              │
│  DbContext, Repositories, Redis, JWT, Security Helpers            │
└─────────────────────────────────────────────────────────────────┘
```

| Layer | Mục đích | Đặc điểm |
|-------|---------|----------|
| **Domain** | Định nghĩa entities, interface (contracts). Không phụ thuộc layer khác. | `BaseEntity`, `User`, `Product`, `IUserRepo`, `IJwtService` |
| **Application** | Business logic, orchestration, DTOs, validation. Phụ thuộc Domain. | `AuthService`, `ProductService`, `ICacheService`, `CacheKeyGenerator` |
| **Infrastructure** | Implement interfaces: DB, Redis, JWT, Security. Phụ thuộc Domain và Application. | `UserRepo`, `RedisCacheService`, `JwtService`, `UnitOfWork` |
| **API** | HTTP endpoints, middleware, DI. Phụ thuộc tất cả layer. | `Controllers`, `GlobalExceptionMiddleware`, `SessionValidationMiddleware` |

### 1.2 Cấu trúc thư mục

```
src/
├── Ecommerce.API/
│   ├── Controllers/         # ProductController, CategoryController, AuthController...
│   ├── Middleware/          # GlobalExceptionMiddleware, SessionValidationMiddleware
│   └── Program.cs           # DI, pipeline
├── Ecommerce.Application/
│   ├── Authorization/       # PermissionAuthorizationHandler, PolicyProvider
│   ├── Common/
│   │   ├── Auth/            # UserSessionState
│   │   ├── Caching/         # CacheKeyGenerator
│   │   ├── Pagination/      # PaginationDto, PagedResponse
│   │   └── Responses/       # ApiResponse, ErrorResponseDto
│   ├── DTOs/                # Auth, Category, Product, Role, User, Permission
│   ├── Exceptions/          # BaseException, BadRequestException, NotFoundException...
│   └── Services/
│       ├── Interfaces/      # IAuthService, ICacheService, IProductService...
│       └── Implementations/
├── Ecommerce.Domain/
│   ├── Common/              # Settings (JwtSettings, AuthSecuritySettings), Filters
│   ├── Entities/            # User, Product, Category, Role, Permission, RefreshToken
│   └── Interfaces/          # IUserRepo, IJwtService, IUnitOfWork...
└── Ecommerce.Infrastructure/
    ├── Data/                # AppDbContext, UnitOfWork, Migrations, Seed
    ├── RedisCaching/        # RedisCacheService
    ├── Repositories/        # UserRepo, ProductRepo, CategoryRepo...
    ├── SecurityHelpers/     # JwtService, PasswordHasher, SecurityFingerprintHelper
    └── Services/            # DeviceService, TokenBlacklistService, SessionValidationService
```

---

## 2. Cơ chế xác thực và bảo mật (Security)

### 2.1 JWT Stateful: SessionId và SessionVersion

Thay vì JWT stateless thuần túy, project dùng **JWT Stateful** với trạng thái session lưu trên server:

| Thành phần | Mô tả |
|------------|-------|
| **SessionId** (`sid`) | `Guid` duy nhất cho mỗi phiên đăng nhập. Được lưu trong `User.CurrentSessionId` và claim `sid` của access token. |
| **SessionVersion** (`sv`) | Số nguyên tăng mỗi khi logout/refresh. Dùng để vô hiệu hóa token cũ sau khi cấp token mới. |

**Luồng đăng nhập:**

```
pseudo-code:
  LoginAsync(request):
    user = GetByEmail(request.Email)
    ValidatePassword(user, request.Password)
    
    sessionId = NewGuid()
    user.SessionVersion += 1
    user.CurrentSessionId = sessionId
    user.LastFingerprintHash = ComputeFingerprint(deviceId)
    SaveChanges(user)
    
    accessToken = GenerateAccessToken(user, sessionId, user.SessionVersion, fingerprintHash)
    CacheSession(userId, sessionId, sv, fp, deviceId)
    return { accessToken, refreshToken }
```

**Các claim trong access token:**

| Claim | Ý nghĩa |
|-------|---------|
| `sub` | User ID |
| `jti` | Token ID (để blacklist) |
| `sid` | Session ID |
| `sv` | Session Version |
| `fp` | Fingerprint hash (DeviceId + IP) |
| `permissions` | Danh sách permission names |

### 2.2 Fingerprinting (IP + DeviceId)

Fingerprint giúp ràng buộc token với thiết bị và mạng:

```csharp
// SecurityFingerprintHelper.ComputeFingerprint(deviceId)
// Fingerprint = HMAC-SHA256(secret, deviceId + "|" + ipAddress)

payload = $"{deviceId ?? ""}|{ip}"
key = Encoding.UTF8.GetBytes(AuthSecurity.FingerprintSecret)
hmac = HMACSHA256(key).ComputeHash(UTF8(payload))
return Base64(hmac)
```

| Thành phần | Nguồn |
|------------|-------|
| **IP** | `X-Forwarded-For` (nếu có) hoặc `RemoteIpAddress` |
| **DeviceId** | Header `X-Device-Id` (bắt buộc khi login) |

Login yêu cầu header `X-Device-Id`. Nếu thiếu → `UnauthorizedException("X-Device-Id header is required for login")`.

### 2.3 Fail-fast và kiểm tra token sau migration

**SessionValidationMiddleware** chạy trước các endpoint `[Authorize]` và thực hiện:

1. **Kiểm tra claim bắt buộc**: `sid`, `sv`, `fp` phải có. Thiếu → `UnauthorizedException`.
2. **Blacklist JTI**: Gọi `TokenBlacklistService.IsBlacklistedAsync(jtiHash)`. Token đã revoke → `UnauthorizedException("Token has been revoked")`.
3. **Fingerprint**: So sánh fingerprint hiện tại (từ request) với `fp` trong token. Không khớp → `UnauthorizedException("Invalid session (fingerprint mismatch)")`.
4. **Session hiện tại**: Gọi `SessionValidationService.EnsureAccessTokenSessionValidAsync`:
   - Ưu tiên lấy từ Redis (`auth:session:user:{userId}`).
   - Nếu cache miss → lấy từ DB (`GetUserAuthStateAsync`).
   - So sánh `SessionId`, `SessionVersion`, `FingerprintHash` với DB/Redis.

```
pseudo-code:
  EnsureAccessTokenSessionValidAsync(userId, sid, sv, fp, currentFp):
    if currentFp != fp: throw Unauthorized
    cached = Redis.Get(auth:session:user:{userId})
    if cached:
      if cached.sid != sid OR cached.sv != sv OR cached.fp != fp:
        throw Unauthorized
      return
    db = UserRepo.GetUserAuthStateAsync(userId)
    if db.sid != sid OR db.sv != sv OR db.fp != fp:
      throw Unauthorized
```

**Khi refresh token / logout**: Token cũ được blacklist (JTI hash, TTL = thời gian còn lại của access token). `SessionVersion` tăng → token cũ không còn hợp lệ.

---

## 3. Chiến lược Caching

### 3.1 Cache-aside với Redis

Project dùng pattern **Cache-Aside** (Lazy Loading):

```
pseudo-code GetOrSetAsync:
  cached = GetAsync(key)
  if cached != null: return cached
  value = factory()  // gọi DB
  if value != null: SetAsync(key, value, ttl)
  return value
```

**ICacheService** (`RedisCacheService`):

| Method | Mô tả |
|--------|-------|
| `GetAsync<T>(key)` | Đọc từ Redis, deserialize JSON camelCase. Miss/error → `default`. |
| `SetAsync<T>(key, value, ttl)` | Serialize JSON camelCase, ghi Redis với TTL. |
| `RemoveAsync(key)` | Xóa key. |
| `GetOrSetAsync<T>(key, factory, ttl)` | Cache-aside: Get → nếu miss gọi factory → Set. |
| `IncrementAsync(key)` | Tăng atomic (atomic counter). |

### 3.2 Resilience (Soft-fail / Fallback khi Redis lỗi)

Mọi thao tác Redis được bọc `try/catch` để **không throw** khi Redis lỗi:

| Method | Khi RedisConnectionException / Exception | Hành vi |
|--------|------------------------------------------|---------|
| `GetAsync` | Log warning | Return `default` → tiếp tục dùng DB |
| `SetAsync` | Log warning | Bỏ qua ghi cache |
| `RemoveAsync` | Log warning | Bỏ qua xóa cache |
| `IncrementAsync` | Log error | Fallback `DateTime.UtcNow.Ticks` |

```csharp
// RedisCacheService.GetAsync (đoạn minh họa)
catch (RedisConnectionException ex)
{
    _logger.LogWarning(ex, "Redis unavailable. Fallback to DB for key: {CacheKey}", key);
    return default;  // Không throw → Service vẫn chạy
}
```

→ Hệ thống vẫn hoạt động bình thường khi Redis down; performance giảm do mất cache nhưng không fail.

### 3.3 Invalidation trong các Service

| Service | Thao tác ghi | Xóa cache |
|---------|--------------|-----------|
| **ProductService** | Update, Delete | `product:{id}` |
| **CategoryService** | Update, Delete | `category:{id}` |
| **UserService** | Update, Delete | `user:{id}` |
| **RoleService** | Update, Delete, AssignPermissions | `role:{id}`; Delete thêm `user:{userId}` cho mỗi user bị reassign |

**TTL theo loại dữ liệu:**

| Entity | TTL |
|--------|-----|
| Product | 10 phút |
| Category | 1 giờ |
| User | 10 phút |
| Role | 30 phút |

**Mẫu key** (`CacheKeyGenerator`):

- `product:{id}`, `product:list:{page}:{size}[:{filterHash}]`
- `category:{id}`, `category:list:{page}:{size}[:{filterHash}]`
- `user:{id}`, `user:list:{page}:{size}`
- `role:{id}`, `role:list:{page}:{size}`
- `auth:session:user:{userId}`
- `auth:loginfail:{email}`
- `Blacklist:Token:{jtiHash}`

---

## 4. Xử lý nghiệp vụ & dữ liệu

### 4.1 Unit of Work và Repository

**Unit of Work** dùng để đảm bảo transaction cho một nhóm thao tác:

```csharp
// IUnitOfWork
Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);

// UnitOfWork implementation
await using var transaction = await _context.Database.BeginTransactionAsync(ct);
try
{
    var result = await action();
    await _context.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
    return result;
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

**Repository Pattern**:

- Mỗi aggregate có interface `I*Repo` trong Domain.
- Implementation trong Infrastructure: `*Repo` dùng `AppDbContext`.
- Application gọi repo qua interface; không biết chi tiết DB.

### 4.2 Soft Delete

**BaseEntity**:

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Global Query Filter** (`AppDbContext.OnModelCreating`):

```csharp
// Với mọi entity kế thừa BaseEntity
modelBuilder.Entity(entityType.ClrType)
    .HasQueryFilter(e => e.IsDeleted == false);
```

→ Mọi truy vấn mặc định chỉ trả về bản ghi chưa xóa. Xóa mềm = set `IsDeleted = true`.

### 4.3 Xử lý ràng buộc khi xóa Role

**RoleService.DeleteAsync**:

1. Không xóa role `Admin`.
2. Không xóa role mặc định `User`.
3. Lấy role mặc định `User` (`GetByNameRoleAsync("User")`).
4. Trong **một transaction**:
   - `ReassignUsersToRoleAsync(fromRoleId, toRoleId)` → chuyển tất cả user sang role mặc định.
   - Set `role.IsDeleted = true`, `role.UpdatedAt = DateTime.UtcNow`.
5. Invalidate cache: `role:{id}` và `user:{userId}` cho từng user bị reassign.

```csharp
// pseudo-code
affectedUserIds = await _unitOfWork.ExecuteInTransactionAsync(() => {
    ids = _userRepo.ReassignUsersToRoleAsync(id, defaultUserRole.Id);
    role.IsDeleted = true;
    role.UpdatedAt = DateTime.UtcNow;
    return ids;
});
await _cacheService.RemoveAsync(role:id);
foreach (userId in affectedUserIds)
    await _cacheService.RemoveAsync(user:userId);
```

### 4.4 Xử lý ràng buộc khi xóa Category

**CategoryService.DeleteAsync**:

```csharp
if (await _categoryRepo.HasActiveProductsAsync(id))
    throw new BadRequestException("Cannot delete category with linked products");
```

`HasActiveProductsAsync` kiểm tra có sản phẩm nào còn hoạt động (`!IsDeleted`) thuộc category đó không.

---

## 5. Quản lý cấu hình và môi trường

### 5.1 User Secrets và biến môi trường

**User Secrets**:

```bash
dotnet user-secrets set "AuthSecurity:FingerprintSecret" "your-secret-32-chars-min" --project src/Ecommerce.API
```

**Biến môi trường** (ví dụ):

- `Jwt__Key` – key ký JWT (≥ 32 ký tự).
- `AuthSecurity__FingerprintSecret` – secret cho fingerprint.
- `Redis__ConnectionString`, `Redis__InstanceName`.

**Kiểm tra khi khởi động** (`Program.cs`):

```csharp
var fingerprintSecret = builder.Configuration["AuthSecurity:FingerprintSecret"];
if (string.IsNullOrWhiteSpace(fingerprintSecret))
    throw new InvalidOperationException(
        "AuthSecurity:FingerprintSecret is required. Set via User Secrets...");
```

### 5.2 Cấu hình chính

| Section | Mục đích |
|---------|----------|
| `ConnectionStrings:DefaultConnection` | Chuỗi kết nối SQL Server |
| `Jwt` | Key, Issuer, Audience, ExpiryMinutes, RefreshTokenDays |
| `AuthSecurity` | FingerprintSecret |
| `Redis` | ConnectionString, InstanceName, DefaultExpirationMinutes |

---

## 6. Xử lý lỗi (Exception Handling)

### 6.1 Exception tùy chỉnh

| Exception | StatusCode | ErrorCode | Dùng cho |
|-----------|------------|-----------|----------|
| `BadRequestException` | 400 | BAD_REQUEST | Dữ liệu sai (vd: xóa category có sản phẩm) |
| `BusinessException` | 400 | BUSINESS_ERROR | Lỗi nghiệp vụ (vd: role đã tồn tại) |
| `ConflictException` | 409 | CONFLICT_ERROR | Xung đột (vd: email đã tồn tại) |
| `ForbiddenException` | 403 | FORBIDDEN | Không đủ quyền |
| `NotFoundException` | 404 | NOT_FOUND | Tài nguyên không tồn tại |
| `UnauthorizedException` | 401 | UNAUTHORIZED | Chưa đăng nhập / token sai |
| `TooManyRequestsException` | 429 | TOO_MANY_REQUESTS | Rate limit (vd: đăng nhập sai quá nhiều) |

**BaseException**:

```csharp
public abstract class BaseException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    protected BaseException(int statusCode, string message, string errorCode)
        : base(message) { ... }
}
```

### 6.2 GlobalExceptionMiddleware

```csharp
// pseudo-code
try { await _next(context); }
catch (Exception ex)
{
    if (context.Response.HasStarted) return;
    
    statusCode = 500;
    errorCode = "INTERNAL_SERVER_ERROR";
    message = "An unexpected error occurred.";
    
    if (ex is BaseException baseEx)
    {
        statusCode = baseEx.StatusCode;
        errorCode = baseEx.ErrorCode;
        message = baseEx.Message;
    }
    
    LogError(ex, ...);
    Response = new ErrorResponseDto { StatusCode, Success=false, ErrorCode, Message, Path, TraceId, Timestamp };
    await Response.WriteAsJsonAsync(Response);
}
```

**ErrorResponseDto** (camelCase): `StatusCode`, `Success`, `ErrorCode`, `Message`, `Path`, `TraceId`, `Timestamp`, `Errors` (cho validation).

### 6.3 JWT Bearer Events

- `OnChallenge`: map `SecurityTokenExpiredException`, `SecurityTokenInvalidSignatureException`, `SecurityTokenException` → `UnauthorizedException`.
- `OnForbidden`: map → `ForbiddenException`.

---

## Tóm tắt

| Khía cạnh | Kỹ thuật |
|-----------|----------|
| **Kiến trúc** | Clean Architecture (Domain, Application, Infrastructure, API) |
| **Xác thực** | JWT Stateful với SessionId, SessionVersion, fingerprint (HMAC-SHA256) |
| **Caching** | Redis Cache-aside, soft-fail khi Redis lỗi |
| **Dữ liệu** | Unit of Work cho transaction, Repository Pattern |
| **Soft Delete** | Global query filter `IsDeleted == false` |
| **Cấu hình** | User Secrets, biến môi trường cho tham số nhạy cảm |
| **Exception** | BaseException + GlobalExceptionMiddleware |
