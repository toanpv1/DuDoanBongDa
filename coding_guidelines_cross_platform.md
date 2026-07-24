# 📋 Quy Tắc Viết Code & Bảng So Sánh Đa Nền Tảng (.NET Core Cross-Platform & Cloud-Ready)

Tài liệu này tổng hợp bảng so sánh các môi trường triển khai và **bộ quy chuẩn lập trình (Coding Guidelines)** đúc kết từ thực tế giúp ứng dụng ASP.NET Core chạy tương thích 100% trên mọi hệ điều hành (**Windows Server IIS**, **Linux Server**, **Docker Container**) và **Cloud PaaS (Vercel, Render, Supabase)**.

---

## 📊 1. Bảng So Sánh Các Môi Trường Triển Khai

| Yếu tố so sánh | Windows Server (IIS) | Linux Server (Nginx) | Docker Container | Cloud PaaS (Vercel + Render + Supabase) |
| :--- | :--- | :--- | :--- | :--- |
| **Hệ điều hành** | Windows Server 2016-2025 | Ubuntu / Debian / RHEL | Linux Container (Debian/Alpine) | Managed Linux Environment |
| **Phân biệt hoa/thường** | KHÔNG (Windows không phân biệt) | CÓ (Linux phân biệt chữ hoa/thường) | CÓ (Theo hệ điều hành Linux) | CÓ (Rất nghiêm ngặt đối với CSDL & Path) |
| **Cơ chế CSDL** | SQL Server / SQLite Local | PostgreSQL / MySQL / SQLite | Containerized PostgreSQL | Managed PostgreSQL (Supabase Cloud) |
| **Đường dẫn File** | Dùng Dấu `\` (`C:\app\wwwroot`) | Dùng Dấu `/` (`/var/www/app`) | Dùng Dấu `/` (`/app/wwwroot`) | Dùng Dấu `/` (Chuẩn Linux) |
| **Bảo mật & SSL** | IIS Certificate / Certbot | Certbot / Nginx SSL | Cloudflare / Ingress | HTTPS tự động do Cloud cấp |

---

## 🏗️ 2. Bộ Quy Chuẩn Viết Code Lập Trình Đa Nền Tảng (Cross-Platform Requirements)

> [!IMPORTANT]
> **QUY TẮC CỐT LÕI (RULE #0): CHUẨN HÓA TÊN BẢNG & CỘT VỀ CHỮ THƯỜNG (LOWERCASE) NGAY TỪ NGÀY ĐẦU THIẾT KẾ!**
> 
> - **Bài học cốt lõi**: SQLite trên Windows KHÔNG phân biệt chữ hoa/thường (nên PascalCase C# như `Users`, `DisplayName` chạy local bình thường). Nhưng khi đưa lên PostgreSQL/Supabase trên Linux, PostgreSQL phân biệt chữ hoa/thường RẤT NGHIÊM NGẶT nếu tên cột chứa chữ hoa hoặc ngoặc kép, gây ra các lỗi `42P01: relation does not exist` hoặc `42703: column does not exist`.
> - **Giải pháp bắt buộc**:
>   1. **Thiết kế CSDL**: Tên tất cả Bảng và Cột **100% viết chữ thường (lowercase)** (ví dụ: `users`, `id`, `username`, `passwordhash`, `displayname`, `email`, `role`, `avatarurl`, `isactive`, `createdat`).
>   2. **Trong EF Core `AppDbContext.cs`**: Tự động ép `.ToLowerInvariant()` cho tất cả Bảng và Cột ngay trong `OnModelCreating`:
>      ```csharp
>      foreach (var entity in modelBuilder.Model.GetEntityTypes())
>      {
>          entity.SetTableName(entity.GetTableName()?.ToLowerInvariant());
>          foreach (var property in entity.GetProperties())
>          {
>              var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
>              property.SetColumnName(property.GetColumnName(storeObject)?.ToLowerInvariant());
>          }
>      }
>      ```

---

### 1️⃣ Chuẩn hóa Kiểu dữ liệu Đa CSDL (Data Type Compatibility)
- **Boolean**: Trong PostgreSQL dùng kiểu `BOOLEAN` (`true`/`false`), không dùng kiểu `INTEGER` (`1`/`0`) của SQLite.
- **DateTime**: Trong PostgreSQL dùng kiểu `TIMESTAMPTZ` (`timestamp with time zone`), không dùng chuỗi `TEXT`.

### 2️⃣ Tự động Nhận diện Multi-Provider CSDL & SSL Connection
- Đọc chuỗi kết nối để tự động chọn `UseNpgsql` (PostgreSQL) hoặc `UseSqlite` (SQLite local).
- Tự động bổ sung `SSL Mode=Require;Trust Server Certificate=true;` khi kết nối PostgreSQL Cloud.

### 3️⃣ Vô hiệu hóa FileWatcher trên Linux Docker (Khắc phục Crash Status 139)
- Thêm `ENV DOTNET_USE_POLLING_FILE_WATCHER=1` vào **`Dockerfile`**.
- Thêm `reloadOnChange: false` khi đọc file `appsettings.json` trong **`Program.cs`** để tránh xung đột inotify handle limits của Linux Docker container.

### 4️⃣ Tối ưu Hiệu năng Truy vấn & Lọc trên Bộ nhớ Trình duyệt (0ms Latency)
- **Backend C#**: Dùng `.AsNoTracking()` cho tất cả truy vấn đọc dữ liệu (Read-only queries) để tăng tốc độ CSDL 2-3 lần.
- **Frontend JS**: Tải danh sách 1 lần duy nhất khi mở trang, khi lọc Dropdown (Vòng đấu, Trạng thái) dùng JavaScript Array Filter (`matches.filter(...)`) trực tiếp trên RAM trình duyệt để cho kết quả **hiển thị tức thì 0ms**.

### 5️⃣ Cấu hình CORS Middleware đúng Thứ tự Pipeline
- Đặt `app.UseCors("AllowFrontend")` ngay sau `builder.Build()`, trước Exception Handling và Authentication Middleware để các phản hồi 500/401 vẫn đính kèm đầy đủ CORS headers.

### 6️⃣ Lắng nghe Cổng Động (Dynamic Port Binding)
- Khai báo lắng nghe tất cả giao diện mạng trong Dockerfile: `ENV ASPNETCORE_URLS=http://+:8080`.
