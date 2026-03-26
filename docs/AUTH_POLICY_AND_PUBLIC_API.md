# Phân quyền, policy và endpoint công khai (hồi quy sau giai đoạn 1–7)

Tài liệu này tóm tắt hành vi hiện tại của API để QA hoặc dev tự smoke test (Swagger / Postman).

## Policy authorization

- Dùng tên permission dạng **`entity.action`** (ví dụ `category.read`, `product.create`).
- Có thể khai báo bằng **`[Permission("category.read")]`** hoặc **`[Authorize(Policy = "category.read")]`** — tương đương nhau.
- Prefix cũ **`Permission:category.read`** vẫn được `PermissionPolicyProvider` hỗ trợ.
- Policy động chỉ áp dụng khi tên khớp mẫu chữ thường + dấu chấm (xem `PermissionPolicyNames`).

## JWT (access token)

- Claims chính: **user id** (`NameIdentifier` / `sub`), **`jti`**, phiên **`sid` / `sv` / `fp`**.
- **Không** nhúng email, tên role hay danh sách permission trong JWT.
- Email / role / permission hiện tại: dùng **`GET /api/Auth/me`** (hoặc luồng tương đương) sau khi đăng nhập.

## Kiểm tra quyền (runtime)

- `HasPermissionAsync` đọc **DB** theo user id. Role **Admin** (theo tên role trong DB) được coi là có **mọi** permission.
- Gán permission cho role **User** chỉ cho phép **`product.read`** và **`category.read`**.

## Phiên đăng nhập

- Gán lại permission cho một role: **không** gọi invalidate toàn bộ user của role đó (quyền áp dụng theo request tiếp theo).
- Xóa role: user thuộc role đó được **chuyển sang role User**; **không** invalidate session; có xóa cache key `user:{id}` nếu từng dùng (dọn dẹp).

## Endpoint công khai (không cần JWT)

- **`GET /api/Product`** (danh sách có phân trang/filter).
- **`GET /api/Product/{id}`**.
- **`GET /api/Category`**.
- **`GET /api/Category/{id}`**.

Các thao tác ghi (POST/PUT/DELETE) trên Product/Category vẫn cần policy tương ứng.

## Redis / cache (dữ liệu nghiệp vụ)

- **Có cache (version + TTL):** **Category**, **Role**, **Permission** (list/detail).
- **Không cache response:** **Product**, **User** (luôn đọc DB).
- Thay đổi product / đặt hàng thành công: bump **`category:version`** (ảnh hưởng `ProductCount` trên DTO category).

## Checklist smoke nhanh (Swagger)

1. **Không token:** gọi GET Product + GET Category (list và theo id) → **200**.
2. **User thường:** login, gửi `Authorization: Bearer` + **`X-Device-Id`**; thử **POST/PUT/DELETE** Product hoặc Category khi user **không** có permission tương ứng → **403**; GET Product/Category vẫn **200** không token.
3. **Admin:** thao tác cần quyền quản trị → **200** khi DB gán đúng role Admin.
4. **Role:** gán permission cho role không phải User với permission ngoài `product.read`/`category.read` → OK; với role **User** gán `role.delete` → **lỗi nghiệp vụ** (message rõ ràng).
5. **Permission:** xóa một permission đang gắn role → junction gỡ, không còn chặn vì “system” hay “non-admin role”.

## Kiểm tra tự động

```bash
dotnet build Ecommerce.sln
dotnet test src/Ecommerce.UnitTests/Ecommerce.UnitTests.csproj
```

Hiện solution chỉ có project unit test `Ecommerce.UnitTests`; không có bộ integration test HTTP trong repo.
