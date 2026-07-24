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

### 1️⃣ Trừu tượng hóa CSDL (Database Abstraction & Multi-Provider Support)
* **Quy tắc**: Không hardcode driver hay câu lệnh SQL riêng của 1 loại CSDL (như SQLite `PRAGMA`).
* **Thực thi**:
  - Sử dụng Entity Framework Core với khả năng đọc Provider theo chuỗi kết nối:
  ```csharp
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  builder.Services.AddDbContext<AppDbContext>(options =>
  {
      if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("Server=")))
      {
          if (!connectionString.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase))
              connectionString = connectionString.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true;";
          
          options.UseNpgsql(connectionString);
      }
      else
      {
          options.UseSqlite(connectionString ?? "Data Source=worldcup.db");
      }
  });
  ```
  - **Tránh SQL thô**: Các câu lệnh PRAGMA hoặc SQL dialect riêng phải được kiểm tra theo Provider.

---

### 2️⃣ Quản lý Cấu hình & Bí mật theo Nguyên tắc 12-Factor App
* **Quy tắc**: **KHÔNG BAO GIỜ** hardcode Passwords, API Keys, hay JWT Keys trong `appsettings.json` hoặc code C#.
* **Thực thi**:
  - Đặt giá trị mặc định cho Môi trường Local trong `appsettings.json`.
  - Trên Production/Cloud, ưu tiên đọc từ **Environment Variables** (trong Linux/Docker dùng 2 dấu gạch dưới `__` thay cho dấu `:` như `ConnectionStrings__DefaultConnection`).

---

### 3️⃣ Xử lý Đường dẫn File chuẩn Đa hệ điều hành (Path & Case Sensitivity)
* **Quy tắc**:
  - Không sử dụng dấu gạch ngược `\` hằng số.
  - Tên file static (HTML, JS, CSS, Images) phải viết thường toàn bộ (lowercase) để tránh lỗi 404 trên Linux Server.
* **Thực thi**:
  - Luôn sử dụng `Path.Combine()` thay vì cộng chuỗi đường dẫn.

---

### 4️⃣ Cấu hình CORS & SSL Redirection thích ứng Cloud Reverse Proxy
* **Quy tắc**:
  - Đặt `app.UseCors()` ở **ngay sau** `builder.Build()` để mọi phản hồi (kể cả 500 error hay Exception) luôn được đính kèm CORS headers.

---

### 5️⃣ Lắng nghe Cổng Động (Dynamic Port Binding)
* **Quy tắc**:
  - Không hardcode `http://localhost:5000` trong `Program.cs`.
  - Khai báo lắng nghe tất cả giao diện mạng trong Dockerfile: `ENV ASPNETCORE_URLS=http://+:8080`.

---

### 6️⃣ Đóng gói Container hóa (Dockerization)
* Sử dụng `Dockerfile` đa tầng (Multi-stage build) để đóng gói nhẹ và chạy nhất quán trên mọi môi trường.
