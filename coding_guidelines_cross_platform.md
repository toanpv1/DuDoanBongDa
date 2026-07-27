# 📋 Quy Tắc Viết Code & Quy Chuẩn Phát Triển Đa Môi Trường (.NET Core & Database Standards)

Tài liệu này quy định các chuẩn mực lập trình (Coding Guidelines) và quy tắc thiết kế kiến trúc chuẩn dành cho ứng dụng **ASP.NET Core** kết hợp các hệ quản trị CSDL (**PostgreSQL**, **Oracle**, **SQL Server**), đảm bảo ứng dụng vận hành mượt mà trên môi trường Local và Cloud PaaS (**Vercel Frontend**, **Render Backend Docker**, **Supabase Cloud**).

---

## 1. Bộ Quy Chuẩn Thiết Kế & Lập Trình (Development Guidelines)

> [!IMPORTANT]
> **QUY TẮC CỐT LÕI (RULE #0): CHUẨN HÓA TÊN BẢNG & CỘT VỀ CHỮ THƯỜNG (LOWERCASE) 100%!**
> 
> Các hệ quản trị CSDL như PostgreSQL mặc định phân biệt chữ hoa/thường và tự động chuyển các định danh không bọc trong ngoặc kép về chữ thường. Để tránh lỗi sai tên cột (`column does not exist`) giữa các môi trường:
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

### 📍 Quy Chuẩn Ràng Buộc Dữ Liệu Tầng CSDL (Integrity & Constraints)
- Đảm bảo tất cả các bảng đều có **Khóa chính (PRIMARY KEY)**.
- Khai báo đầy đủ các **Ràng buộc Duy nhất (UNIQUE Constraint)** để bảo vệ tính toàn vẹn dữ liệu ở tầng CSDL:
  ```sql
  ALTER TABLE users ADD CONSTRAINT uq_users_username UNIQUE (username);
  ALTER TABLE tournamentmembers ADD CONSTRAINT uq_tournament_member UNIQUE (tournamentid, userid);
  ALTER TABLE predictions ADD CONSTRAINT uq_predictions_user_match UNIQUE (userid, matchid);
  ```

### 📍 Chuẩn Hóa Kiểu Dữ Liệu CSDL (Data Types Standard)
- **Kiểu Luận lý (Boolean)**: Trong PostgreSQL dùng kiểu `BOOLEAN` (`true`/`false`), không dùng kiểu integer `1`/`0`.
- **Kiểu Ngày giờ (DateTime)**: Trong PostgreSQL dùng kiểu `TIMESTAMPTZ` (`timestamp with time zone`) để đảm bảo chính xác múi giờ quốc tế.

---

## 2. Quy Chuẩn Quản Lý Chuỗi Kết Nối & PostgreSQL Driver Configuration và Oracle / SQL Server

### 📍 Đọc Chuỗi Kết Nối Động Qua Configuration
Đọc chuỗi kết nối từ `builder.Configuration.GetConnectionString("DefaultConnection")` để tự động ưu tiên nạp từ Biến môi trường khi triển khai trên Cloud.

### 📍 Quy Chuẩn Cấu Hình Driver PostgreSQL (Npgsql)
Luôn đính kèm cấu hình **SSL Mode** và **Retry Policy** khi khai báo Npgsql PostgreSQL trong `Program.cs`:
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

### 📍 So Sánh & Mẫu Chuỗi Kết Nối Theo Hệ Quản Trị CSDL (PostgreSQL vs Oracle vs SQL Server)

| Hệ Quản Trị CSDL | Mẫu Chuỗi Kết Nối Chuẩn | Chú Ý Khi Cấu Hình |
| :--- | :--- | :--- |
| **PostgreSQL (Supabase)** | `Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[PROJECT-REF];Password=***;SSL Mode=Require;Trust Server Certificate=true;` | Cần SSL Mode khi kết nối Cloud; Username chứa mã Project Ref. |
| **SQL Server** | `Server=myserver.database.windows.net,1433;Database=mydb;User Id=myuser;Password=***;Encrypt=True;TrustServerCertificate=False;` | Cần `Encrypt=True` khi nối Azure SQL Server. |
| **Oracle Database** | `Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=myhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=myora)));User Id=myuser;Password=***;` | Chú ý Service Name / SID và Oracle Managed Data Access Client. |

---

## 3. Quy Chuẩn Điều Hướng API Tự Động Ở Frontend (app.js)

Tự động nhận diện tên miền đang thực thi trên trình duyệt để điều hướng API chính xác giữa môi trường Local và Cloud PaaS:

```javascript
// ===== API Configuration =====
const API_BASE = (window.location.hostname.includes('vercel.app'))
    ? 'https://dudoanbongda-api.onrender.com'
    : '';
```

- **Khi chạy trên Localhost**: `API_BASE` tự quy về chuỗi rỗng `""` (gọi trực tiếp Backend Local cùng Host).
- **Khi chạy trên Vercel Cloud**: `API_BASE` tự động trỏ sang URL Render Cloud Backend API (`https://dudoanbongda-api.onrender.com`).

---

## 4. Cấu Hình Docker Container Chạy Trên Linux Server / Render

### 📍 Tối Ưu FileWatcher Trên Linux Container
Thêm biến môi trường trong **`Dockerfile`** để khắc phục lỗi tràn file handle limit (`inotify`) trên Linux Container:
```dockerfile
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
```

### 📍 Cấu Hình Port Lắng Nghe Động (Dynamic Port Binding)
Khai báo cổng lắng nghe mạng mặc định trong Dockerfile để ứng dụng tự nhận cổng do Cloud Server cấp phát:
```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
```
