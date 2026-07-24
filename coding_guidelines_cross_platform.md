# 📋 Quy Tắc Viết Code & Bảng So Sánh Đa Nền Tảng (Cross-Platform & Cloud-Ready .NET Core)

Tài liệu này cung cấp bảng so sánh kiến trúc triển khai và **bộ quy chuẩn viết code (Coding Guidelines/Requirements)** giúp ứng dụng ASP.NET Core có thể chạy tương thích 100% trên mọi nền tảng: **Windows Server (IIS)**, **Linux Server (Nginx)**, **Docker Container** và **Cloud PaaS (Render, Railway, Azure, Vercel)**.

---

## 📊 1. Bảng So Sánh Các Môi Trường Triển Khai

| Yếu tố so sánh | Windows Server (IIS) | Linux Server (Nginx/Systemd) | Docker Container | Cloud PaaS / Serverless (Render, Vercel) |
| :--- | :--- | :--- | :--- | :--- |
| **Hệ điều hành** | Windows Server 2016 - 2025 | Ubuntu / Debian / RHEL | Linux Container (Debian/Alpine) | Linux Managed Environment |
| **Cơ chế chạy App** | IIS Web-Host Module (`aspNetCore`) | Kestrel + Systemd Daemon | Kestrel trong Container Isolated | Docker / Serverless Functions |
| **Phân biệt chữ hoa/thường (Case Sensitivity)** | KHÔNG (Windows không phân biệt `App.cs` và `app.cs`) | CÓ (`Index.html` khác `index.html`) | CÓ (Theo Linux OS của container) | CÓ (Hệ điều hành Linux) |
| **Đường dẫn tệp (File Path)** | Dùng `\` (VD: `C:\inetpub\app`) | Dùng `/` (VD: `/var/www/app`) | Dùng `/` (VD: `/app/wwwroot`) | Dùng `/` (Theo chuẩn Linux) |
| **Cấu hình CSDL** | SQL Server / SQLite local | PostgreSQL / MySQL / SQLite | PostgreSQL / MySQL Container | Supabase / Managed Database Cloud |
| **Cổng kết nối (Port)** | IIS tự quản lý Port (80/443) | Reverse Proxy Nginx (Port 5000 -> 80) | `EXPOSE 8080` hoặc biến `PORT` | Đọc biến môi trường `$PORT` |
| **Bảo mật & SSL** | SSL Import trên IIS / Certbot | Certbot / Let's Encrypt trên Nginx | Cloudflare / Ingress Controller | HTTPS tự động do Cloud cấp |
| **Chi phí & Bảo trì** | Tốn bản quyền Windows Server | Rẻ, cần tự quản lý server Linux | Linh hoạt, đóng gói nhất quán | Miễn phí/Rẻ, tự động Auto-scale |

---

## 🏗️ 2. Bộ Quy Chuẩn Viết Code (Cross-Platform Coding Requirements)

> [!IMPORTANT]
> **QUY TẮC CỐT LÕI (RULE #0): CHUẨN HÓA TÊN BẢNG & CỘT VỀ CHỮ THƯỜNG (LOWERCASE) NGAY TỪ NGÀY ĐẦU THIẾT KẾ!**
> 
> - **Bài học quan trọng**: SQLite trên Windows KHÔNG phân biệt chữ hoa/thường (nên PascalCase C# như `Users`, `DisplayName` chạy local mượt mà). Tuy nhiên, khi đưa lên PostgreSQL/Supabase trên Linux Cloud, PostgreSQL phân biệt chữ hoa/thường RẤT NGHIÊM NGẶT nếu có ngoặc kép `"Users"`, gây ra các lỗi `42P01: relation "Users" does not exist` hoặc `42703: column u.avatar_url does not exist`.
> - **Yêu cầu Bắt Buộc Ngay Từ Đầu**:
>   1. **Khi thiết kế CSDL**: Đặt tên tất cả Bảng và Cột dưới dạng **chữ thường 100%** (ví dụ: `users`, `id`, `username`, `passwordhash`, `displayname`, `email`, `role`, `avatarurl`, `isactive`, `createdat`).
>   2. **Trong EF Core `AppDbContext.cs`**: Luôn áp dụng tự động `.ToLowerInvariant()` cho tất cả Bảng và Cột ngay trong `OnModelCreating` để ứng dụng tương thích 100% trên mọi loại CSDL mà không phải sửa lại code hay DB sau này:
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

Để đảm bảo ứng dụng .NET Core chạy không bị lỗi khi chuyển đổi giữa Windows local ➔ Linux ➔ Cloud/Docker, toàn bộ codebase cần tuân thủ các quy tắc vàng sau:

### 1️⃣ Trừu tượng hóa CSDL (Database Abstraction & Multi-Provider Support)
* **Quy tắc**: Không hardcode driver hay câu lệnh SQL riêng của 1 loại CSDL (như SQLite `PRAGMA`).
* **Thực thi**:
  - Sử dụng Entity Framework Core với khả năng đọc Provider theo chuỗi kết nối.

### 2️⃣ Quản lý Cấu hình & Bí mật theo Nguyên tắc 12-Factor App
* Đọc Passwords/Connection Strings từ Environment Variables (`ConnectionStrings__DefaultConnection`).

### 3️⃣ Xử lý Đường dẫn File chuẩn Đa hệ điều hành (Path & Case Sensitivity)
* Viết thường toàn bộ tên file static và dùng `Path.Combine()`.

### 4️⃣ Cấu hình CORS & SSL Redirection thích ứng Cloud Reverse Proxy
* Đặt `app.UseCors()` ở **ngay sau** `builder.Build()`.

### 5️⃣ Lắng nghe Cổng Động (Dynamic Port Binding)
* Khai báo lắng nghe tất cả giao diện mạng trong Dockerfile: `ENV ASPNETCORE_URLS=http://+:8080`.

### 6️⃣ Đóng gói Container hóa (Dockerization)
* Sử dụng `Dockerfile` đa tầng (Multi-stage build) để đóng gói nhẹ và chạy nhất quán trên mọi môi trường.
