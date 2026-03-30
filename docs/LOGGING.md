# Ghi log & debug (Serilog)

## File log ở đâu?

- Đường dẫn cấu hình: **`logs/log-YYYYMMDD.txt`** (rolling theo ngày), tính từ **thư mục làm việc** khi chạy API (thường là `bin/Debug/net8.0/` khi F5, hoặc thư mục bạn `dotnet run`).

## Console vs Development

- **Production / base** (`appsettings.json`): mức mặc định `Information`; framework (`Microsoft.*`) ở `Warning`.
- **Development** (`appsettings.Development.json`): chỉ nâng **`Default` → `Debug`** cho code app; không ghi đè sink → vẫn có **Console + File** như base.

## RequestId và TraceId

- Dòng **HTTP request** (Serilog): **`[RequestId: …]` đứng đầu** message (trước method/path).
- **Console / file** (template Serilog): sau timestamp và level có **`[RequestId:…]`**; ngoài HTTP pipeline có thể để trống giữa dấu ngoặc.
- Lỗi API (middleware): body thường có **`TraceId`** — cùng mục đích bám request; có thể đối chiếu với log quanh cùng thời điểm.

## Mức log theo HTTP

- **2xx/3xx**: `Information`
- **4xx**: `Warning`
- **5xx / exception**: `Error`

## JWT

- **Bearer trên request**: logger `JwtAuth` — cảnh báo kèm loại exception và `Path` (không log token).
- **Refresh** (`JwtService.GetPrincipalFromExpiredToken`): `Warning` khi validate access token hết hạn thất bại.

## Không log

- JWT đầy đủ, refresh token, mật khẩu, connection string.
