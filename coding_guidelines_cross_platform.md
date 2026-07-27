# 📋 Quy Tắc Viết Code & Quy Chuẩn Phát Triển Đa Môi Trường (.NET Core & PostgreSQL Cloud)

Tài liệu này quy định các chuẩn mực lập trình (Coding Guidelines) và quy tắc thiết kế kiến trúc chuẩn dành cho ứng dụng **ASP.NET Core** kết hợp CSDL **PostgreSQL**, đảm bảo ứng dụng vận hành tương thích 100% trên các môi trường triển khai Cloud PaaS (**Vercel Frontend**, **Render Backend Docker** và **Supabase PostgreSQL Cloud**).

---

## 📊 1. Bảng So Sánh Các Môi Trường Triển Khai

| Yếu tố | Local Development (Dev) | Linux Container (Docker) | Cloud PaaS (Vercel + Render + Supabase) |
| :--- | :--- | :--- | :--- |
| **Hệ điều hành** | Windows / macOS / Linux | Ubuntu / Debian / Alpine | Managed Linux Environment |
| **CSDL** | PostgreSQL Local / Remote | PostgreSQL Container | Managed PostgreSQL (Supabase Cloud) |
| **Phân biệt hoa/thường** | Phân biệt theo PostgreSQL Engine | Phân biệt nghiêm ngặt | Phân biệt nghiêm ngặt |
| **Đường dẫn File** | Dùng Dấu `/` hoặc `\` | Dùng Dấu `/` (`/app/wwwroot`) | Dùng Dấu `/` (Standard Linux Path) |
| **Độ trễ Mạng** | 0ms - 1ms | 1ms (Internal Container) | 2ms - 5ms (Region Singapore) |

---

## 🏗️ 2. Bộ Quy Chuẩn Thiết Kế & Lập Trình (Development Guidelines)

> [!IMPORTANT]
> **QUY TẮC CỐT LÕI (RULE #0): CHUẨN HÓA TÊN BẢNG & CỘT VỀ CHỮ THƯỜNG (LOWERCASE) 100%!**
> 
> PostgreSQL mặc định phân biệt chữ hoa/thường và tự động chuyển các định danh không bọc trong ngoặc kép về chữ thường. Để tránh lỗi sai tên cột (`column does not exist`) giữa các môi trường:
> 1. **Thiết kế CSDL & Script SQL**: Tên tất cả Bảng và Cột **100% viết chữ thường (lowercase)** (ví dụ: `users`, `id`, `username`, `passwordhash`, `displayname`, `email`, `role`, `avatarurl`, `isactive`, `createdat`, `matchid`, `userid`).
> 2. **Trong EF Core `AppDbContext.cs`**: Tự động ép `.ToLowerInvariant()` cho tên Bảng và Cột trong `OnModelCreating`:
>    ```csharp
>    foreach (var entity in modelBuilder.Model.GetEntityTypes())
>    {
>        entity.SetTableName(entity.GetTableName()?.ToLowerInvariant());
>        foreach (var property in entity.GetProperties())
>        {
>            var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
>            property.SetColumnName(property.GetColumnName(storeObject)?.ToLowerInvariant());
>        }
>    }
>    ```

---

### 1️⃣ Quy Chuẩn Quản Lý Chuỗi Kết Nối & PostgreSQL Driver Configuration
- Đọc chuỗi kết nối từ `builder.Configuration.GetConnectionString("DefaultConnection")` để tự động đè bởi biến môi trường khi triển khai trên Render Cloud.
- Luôn đính kèm cấu hình **SSL Mode** và **Retry Policy** khi cấu hình Npgsql PostgreSQL trong `Program.cs`:
  ```csharp
  if (!connectionString.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase))
  {
      connectionString = connectionString.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true;";
  }
  options.UseNpgsql(connectionString, npgsqlOptions =>
  {
      npgsqlOptions.EnableRetryOnFailure(
          maxRetryCount: 5,
          maxRetryDelay: TimeSpan.FromSeconds(5),
          errorCodesToAdd: null);
  });
  ```

### 2️⃣ Quy Chuẩn Điều Hướng API Tự Động Ở Frontend (`app.js`)
Frontend tự động nhận diện môi trường thực thi dựa trên Domain tên miền:
```javascript
const API_BASE = (window.location.hostname.includes('vercel.app'))
    ? 'https://dudoanbongda-api.onrender.com'
    : '';
```
- **Khi chạy trên Localhost**: `API_BASE` tự bằng `""` (gọi trực tiếp Backend Local).
- **Khi chạy trên Vercel Cloud**: `API_BASE` tự chọn Render Cloud Backend API.

### 3️⃣ Quy Chuẩn Ràng Buộc Dữ Liệu Tầng CSDL (Integrity & Constraints)
- Đảm bảo tất cả các bảng đều có **Khóa chính (PRIMARY KEY)**.
- Khai báo đầy đủ các **Ràng buộc Duy nhất (UNIQUE Constraint)** để bảo vệ tính toàn vẹn dữ liệu:
  ```sql
  ALTER TABLE users ADD CONSTRAINT uq_users_username UNIQUE (username);
  ALTER TABLE tournamentmembers ADD CONSTRAINT uq_tournament_member UNIQUE (tournamentid, userid);
  ALTER TABLE predictions ADD CONSTRAINT uq_predictions_user_match UNIQUE (userid, matchid);
  ```

### 4️⃣ Chuẩn Hóa Kiểu Dữ Liệu PostgreSQL (Data Types Standard)
- **Kiểu Luận lý (Boolean)**: Sử dụng kiểu `BOOLEAN` (`true`/`false`).
- **Kiểu Ngày giờ (DateTime)**: Sử dụng kiểu `TIMESTAMPTZ` (`timestamp with time zone`) để đảm bảo chính xác múi giờ quốc tế.

### 5️⃣ Cấu Hình Docker Container Chạy Trên Linux Server / Render
- Thêm `ENV DOTNET_USE_POLLING_FILE_WATCHER=1` vào **`Dockerfile`** để tối ưu tài nguyên file handle limit khi chạy trên Linux Container.
- Cấu hình Listening Port động trong Dockerfile: `ENV ASPNETCORE_URLS=http://+:8080`.
