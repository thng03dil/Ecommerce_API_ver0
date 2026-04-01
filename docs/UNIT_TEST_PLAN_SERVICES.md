# Plan Unit Test — Auth, Session, JWT, Product (Ecommerce API)

Tài liệu mô tả **plan hoàn chỉnh** viết unit test cho các service: `AuthService`, `UserSessionInvalidationService`, `SessionValidationService`, `JwtService` (Infrastructure), `ProductService`.

---

## 0. Công cụ & quy ước

| Hạng mục | Lựa chọn |
|----------|-----------|
| Framework | **xUnit** |
| Mock | **Moq** với **`MockBehavior.Loose`** (mặc định của Moq; có thể ghi rõ `new Mock<T>(MockBehavior.Loose)`) |
| Assertion | **FluentAssertions** (NuGet `FluentAssertions`) |
| SUT | Một field duy nhất: `private readonly TService _sut;` |
| Dependencies | Mỗi dependency một `private readonly Mock<IX> _x;` |
| Khởi tạo | **Toàn bộ Mock + `_sut` trong constructor** của lớp test (mỗi test method = instance mới → môi trường sạch) |
| Dữ liệu giả | **Object Mother**: class `private static` (ví dụ `TestDataMother`) với `CreateXxx(...)` và tham số tùy chọn / delegate customize |
| Cấu trúc test | Comment **`// Arrange`**, **`// Act`**, **`// Assert`** |
| Tên test | **`TenPhuongThuc_KichBan_KetQuaMongDoi`** (VD: `LoginAsync_InvalidPassword_ShouldThrowUnauthorizedException`) |
| Hành vi | Dùng **Moq `.Verify()`** cho các tác vụ quan trọng (SaveChanges, Revoke, Increment cache, không gọi DB khi cache hit, …) |

---

## 1. Chuẩn bị project

| Việc | Ghi chú |
|------|--------|
| `Ecommerce.UnitTests` reference **Application** + **Domain** | Cho các service Application. |
| Reference **Infrastructure** | Cho `JwtService` (`Microsoft.Extensions.Options` nếu cần). |
| NuGet **FluentAssertions** | Thêm nếu chưa có. |

---

## 2. Object Mother — danh sách helper khuyến nghị

| Helper | Mục đích |
|--------|-----------|
| `CreateJwtSettings(key?, issuer?, audience?, expiryMinutes?, refreshDays?)` | Key **≥ 32** ký tự để `JwtService` ctor không throw; issuer/audience khớp khi validate token. |
| `CreateUser(...)` | `Id`, `Email`, `PasswordHash`, `RoleId`, `SessionVersion`, `CurrentSessionId`, `Role` (có/không `RolePermissions`). |
| `CreateRole(name, id?, permissions?)` | Gắn vào `User` cho `GenerateAccessToken`. |
| `CreatePermission(name)` | Cho `RolePermission` → `Permission.Name` (claim `permissions`). |
| `CreateLoginDto`, `CreateRegisterDto`, `RefreshTokenRequestDto` | Auth. |
| `CreateUserSessionState(sessionId, sessionVersion, fingerprint)` | Redis path `SessionValidationService`. |
| `CreateUserAuthState(...)` | Snapshot DB — khớp `GetUserAuthStateAsync`. |
| `CreateProduct`, `CreateProductCreateDto`, `ProductFilterDto`, `PaginationDto` | Product. |

**Lý do:** một nguồn dữ liệu giả, dễ bảo trì khi entity/DTO thay đổi.

---

## 3. Rủi ro dùng chung: `UserAuthLockRegistry` + `SemaphoreSlim`

- `UserSessionInvalidationService` và `AuthService` dùng **static** lock theo `userId`.
- **Khuyến nghị:** mỗi test dùng **`userId` khác nhau** (ví dụ `Random.Shared.Next(10000, int.MaxValue)`) hoặc cố định nhưng tránh parallel hai test cùng `userId` trên cùng registry.
- Nếu test fail giữa `Wait` và `Release` (hiếm với code hiện có vì có `finally`), vẫn ưu tiên tách `userId` giữa các test.

---

## 4. `SessionValidationService`

**Dependencies mock:** `ICacheService` `_cache`, `IUserRepo` `_userRepo`, `ISecurityFingerprintHelper` `_fingerprint`  
*(Trong implementation hiện tại `_fingerprint` có thể không được gọi trong method — Loose không bắt buộc Setup.)*

### Kịch bản

| Test | Arrange gợi ý | Assert / Verify |
|------|----------------|-----------------|
| `EnsureAccessTokenSessionValidAsync_InvalidSid_ShouldThrowUnauthorizedException` | `sidClaim` không parse Guid | `ThrowAsync<UnauthorizedException>`, message khớp "Invalid token session" |
| `EnsureAccessTokenSessionValidAsync_InvalidSv_ShouldThrowUnauthorizedException` | `svClaim` null hoặc không int | Tương tự |
| `EnsureAccessTokenSessionValidAsync_EmptyFingerprintClaim_ShouldThrowUnauthorizedException` | `fpClaim` null/empty | Tương tự |
| `EnsureAccessTokenSessionValidAsync_FingerprintMismatch_ShouldThrowUnauthorizedException` | `currentFingerprint != fpClaim` | Message fingerprint mismatch |
| `EnsureAccessTokenSessionValidAsync_CacheHit_Valid_ShouldComplete` | `GetAsync<UserSessionState>` trả state khớp sid/sv/fp | `GetUserAuthStateAsync` **Verify Never** |
| `EnsureAccessTokenSessionValidAsync_CacheHit_StaleSessionId_ShouldThrowUnauthorizedException` | Cached `SessionId` ≠ sid | Throw |
| `EnsureAccessTokenSessionValidAsync_CacheHit_StaleSessionVersion_ShouldThrowUnauthorizedException` | Cached `SessionVersion` ≠ sv | Throw |
| `EnsureAccessTokenSessionValidAsync_CacheHit_StaleFingerprint_ShouldThrowUnauthorizedException` | Cached `FingerprintHash` ≠ fpClaim | Throw |
| `EnsureAccessTokenSessionValidAsync_CacheMiss_DbValid_ShouldComplete` | Cache null; `GetUserAuthStateAsync` khớp sid/sv/fp | Verify `GetAsync` + `GetUserAuthStateAsync` Once |
| `EnsureAccessTokenSessionValidAsync_CacheMiss_DbNull_ShouldThrowUnauthorizedException` | Cache null; DB null | Throw |
| `EnsureAccessTokenSessionValidAsync_CacheMiss_DbMismatch_ShouldThrowUnauthorizedException` | DB không khớp từng field | Throw |

**Lý do mock:** cô lập thứ tự parse → fingerprint → Redis → DB, không cần HTTP/JWT thật.

---

## 5. `UserSessionInvalidationService`

**Dependencies:** `IRefreshTokenRepo` `_refreshTokenRepo`, `IUserRepo` `_userRepo`, `ICacheService` `_cacheService`.

### Kịch bản

| Test | Assert / Verify |
|------|-----------------|
| `InvalidateAsync_UserNotFound_ShouldRevokeWithoutSaveChanges` | `GetByIdForUpdateAsync` null → **không** `SaveChangesAsync` (Verify Never); vẫn **RevokeAllForUserAsync** Once |
| `InvalidateAsync_UserExists_ShouldRevokeSaveAndRemoveCache` | `SessionVersion` user +1; Verify `SaveChangesAsync` Once; `RemoveAsync` đúng key `AuthSession(userId, oldSessionVersion)` |
| `InvalidateAsync_UserExists_ShouldClearSessionFields` | Kiểm tra user sau xử lý: `CurrentSessionId`, `LastDeviceIdHash`, `LastFingerprintHash`, refresh fields null (nếu giữ reference mutable từ Setup) |

**Lý do:** đảm bảo phương án 2 (revoke + bump `SessionVersion` + xóa Redis) không regress.

**Lưu ý:** dùng `userId` khác nhau giữa các test để tránh ảnh hưởng `UserAuthLockRegistry`.

---

## 6. `JwtService` (Infrastructure)

**Dependency:** `IOptions<JwtSettings>` — nên dùng `Microsoft.Extensions.Options.Options.Create(settings)` thay vì Mock khi có thể.

### Kịch bản

| Test | Nội dung |
|------|-----------|
| `Ctor_EmptyKey_ShouldThrowException` | Key null/whitespace |
| `Ctor_ShortKey_ShouldThrowException` | Key length < 32 |
| `GenerateAccessToken_RoleNull_ShouldThrowInvalidOperationException` | `user.Role == null` |
| `GenerateAccessToken_ValidUser_ShouldReturnParsableJwt` | Decode `JwtSecurityTokenHandler.ReadJwtToken`; assert `sub`, `sid`, `sv`, `fp`, role, `permissions` nếu Mother gán |
| `GenerateRefreshToken_ShouldReturnNonEmptyString` | Không rỗng |
| `GetPrincipalFromExpiredToken_ValidSignedToken_ShouldReturnClaims` | Token ký bằng cùng key/issuer/audience (có thể tạo qua `GenerateAccessToken` + cấu hình expiry) |
| `GetPrincipalFromExpiredToken_MalformedToken_ShouldThrow` | Chuỗi không phải JWT |
| `HashToken_NullOrEmpty_ShouldReturnEmpty` | Theo implementation |
| `GetAccessTokenRemainingLifetime_InvalidToken_ShouldReturnNull` | Chuỗi không hợp lệ |

**Lý do:** bảo vệ contract JWT và validation ctor mà Auth/middleware phụ thuộc.

---

## 7. `ProductService`

**Dependencies:** `IProductRepo` `_productRepo`, `ICacheService` `_cacheService`.

### Kịch bản

| Test | Ghi chú |
|------|--------|
| `GetByIdAsync_ProductExists_ShouldReturnSuccess` | Setup `GetOrSetAsync` để gọi factory: `.Returns<string, Func<Task<ProductResponseDto?>>, TimeSpan?>((_, factory, _) => factory())` + `GetByIdAsync` trả product |
| `GetByIdAsync_ProductNotFound_ShouldThrowNotFoundException` | Factory returns null |
| `CreateAsync_CategoryNotFound_ShouldThrowNotFoundException` | `CategoryExistsAsync` false |
| `CreateAsync_Valid_ShouldCallCreateAndIncrementVersion` | Verify `CreateAsync`, `LoadCategoryAsync`, `IncrementAsync(ProductVersionKey)` |
| `UpdateAsync_ProductNotFound_ShouldThrowNotFoundException` | `GetByIdAsync` null |
| `UpdateAsync_Valid_ShouldRemoveProductCacheAndIncrementListVersion` | Verify `RemoveAsync` product id + `IncrementAsync` |
| `DeleteAsync_Valid_ShouldRemoveAndIncrementVersion` | Tương tự |
| `GetAllAsync_CacheHitFirstRead_ShouldNotCallGetFilteredAsync` | `GetAsync` non-null ngay → Verify `GetFilteredAsync` Never |
| `GetAllAsync_CacheMiss_ShouldCallGetFilteredAndSetCache` | `GetAsync` null (trước và trong lock); Verify `GetFilteredAsync`, `SetAsync` |

**Lý do static `SemaphoreSlim`:** có thể flaky nếu nhiều test miss song song — ưu tiên tách Fact hit/miss; hạn chế parallel cùng nhánh miss nếu cần.

---

## 8. `AuthService`

**Dependencies (Mock Loose):** `_userRepo`, `_roleRepo`, `_jwtService`, `_passwordHasher`, `_refreshTokenRepo`, `_cacheService`, `_deviceService`, `_fingerprint`, `_sessionValidation`, `_tokenBlacklist`, `_sessionInvalidation`; `_jwtSettings` qua `Options.Create(...)`.

### Register

| Test | Ý |
|------|---|
| `RegisterAsync_DuplicateEmail_ShouldThrowConflictException` | `GetByEmailAsync` not null |
| `RegisterAsync_MissingDefaultRole_ShouldThrowException` | `GetByNameRoleAsync` null |
| `RegisterAsync_Valid_ShouldCallAddAsync` | Verify `Hash`, `AddAsync` |

### Login

| Test | Ý |
|------|---|
| `LoginAsync_UserNotFound_ShouldThrowUnauthorizedException` | Optional Verify `GetAsync`/`SetAsync` fail counter |
| `LoginAsync_WrongPassword_ShouldThrowUnauthorizedException` | `Verify` false |
| `LoginAsync_TooManyFailures_ShouldThrowTooManyRequestsException` | `GetAsync` return ≥ Max |
| `LoginAsync_MissingDeviceId_ShouldThrowUnauthorizedException` | `GetDeviceId` "" |
| `LoginAsync_Valid_ShouldReturnAuthResponse` | Setup đầy đủ chuỗi login; Verify `RevokeAllForUserAsync`, refresh `AddAsync`, `SaveChanges`, `GenerateAccessToken` |

### Refresh

| Test | Ý |
|------|---|
| `RefreshTokenAsync_InvalidUserIdClaim_ShouldThrowUnauthorizedException` | Principal không parse sub |
| `RefreshTokenAsync_InvalidSvClaim_ShouldThrowUnauthorizedException` | |
| `RefreshTokenAsync_RefreshTokenRevoked_ShouldInvalidateAndThrow` | Verify `_sessionInvalidation.InvalidateAsync(userId)` |
| `RefreshTokenAsync_Valid_ShouldReturnNewTokens` | Setup repo refresh, user, jwt, blacklist tùy nhánh |

### Logout

| Test | Ý |
|------|---|
| `LogoutAsync_WithValidAccessToken_ShouldBlacklistWhenRemainingPositive` | Verify `BlacklistAsync` khi có jti + remaining |
| `LogoutAsync_ShouldCallInvalidateAsync` | Verify `_sessionInvalidation.InvalidateAsync(userId)` |

### Mở rộng (tùy độ phủ)

- `HasPermissionAsync_*`
- `GetMeAsync_*`

**Lý do:** AuthService là orchestrator; **Loose** giảm Setup dư thừa, nhưng **nhánh đang chạy** vẫn cần Setup đủ để tránh `NullReferenceException`.

---

## 9. FluentAssertions — ví dụ

```csharp
// Assert
var act = async () => await _sut.LoginAsync(dto);
await act.Should().ThrowAsync<UnauthorizedException>();
```

```csharp
_userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
```

---

## 10. Thứ tự triển khai file test

1. `SessionValidationServiceTests`
2. `UserSessionInvalidationServiceTests`
3. `JwtServiceTests`
4. `ProductServiceTests` (mở rộng so với test hiện có nếu có)
5. `AuthServiceTests`

---

## 11. Checklist trước khi merge

- [ ] `dotnet test` toàn solution pass
- [ ] Không phụ thuộc Redis/SQL thật trong unit test
- [ ] Tên test đúng `Method_Scenario_ExpectedResult`
- [ ] Mỗi class: constructor khởi tạo mock + `_sut`
- [ ] Moq **Loose** (mặc định hoặc explicit)
- [ ] Có **Verify** cho hành vi quan trọng trên mỗi service (tối thiểu các luồng revoke / save / cache / invalidate)

---

*Tài liệu tham chiếu mã nguồn: `AuthService`, `UserSessionInvalidationService`, `SessionValidationService`, `JwtService`, `ProductService` trong solution Ecommerce_API_ver0.*
