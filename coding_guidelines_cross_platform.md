# 📋 Quy Tắc Viết Code & Bảng So Sánh Đa Nền Tảng (.NET Core Cross-Platform & Cloud-Ready)

Tài liệu này tổng hợp **Bộ Quy Chuẩn Lập Trình (Coding Guidelines)** và **Kiến Trúc Đa Nền Tảng Universal** được đúc kết từ thực tế giúp ứng dụng ASP.NET Core chạy tương thích 100% trên mọi môi trường: **Windows Server (IIS)**, **Linux Docker Container**, **Render Backend** và **Vercel + Supabase Cloud**.

---

## 📊 1. Bảng So Sánh Môi Trường Triển Khai

| Yếu tố so sánh | Windows Server (IIS Local) | Linux Server (Docker Container) | Cloud PaaS (Vercel + Render + Supabase) |
| :--- | :--- | :--- | :--- | :--- |
| **Hệ điều hành** | Windows Server 2016-2025 | Ubuntu / Debian / Alpine | Managed Linux Environment |
| **Phân biệt hoa/thường** | KHÔNG (Windows không phân biệt) | CÓ (Linux phân biệt nghiêm ngặt) | CÓ (PostgreSQL phân biệt hoa/thường) |
| **Cơ chế CSDL** | SQLite Local (`worldcup.db`) | PostgreSQL Container | Managed PostgreSQL (Supabase Cloud) |
| **Đường dẫn File** | Dùng Dấu `\` (`C:\inetpub\wwwroot`) | Dùng Dấu `/` (`/app/wwwroot`) | Dùng Dấu `/` (Chuẩn Linux Container) |
| **Độ trễ Mạng (Latency)** | 0ms (Local RAM / Disk) | 1ms (Internal Container) | 2ms - 5ms (Khu vực Singapore) |

---

## 🏗️ 2. Bộ Quy Chuẩn Viết Code Đa Nền Tảng (Universal Architecture)

> [!IMPORTANT]
> **QUY TẮC CỐT LÕI (RULE #0): CHUẨN HÓA TÊN BẢNG & CỘT VỀ CHỮ THƯỜNG (LOWERCASE) 100%!**
> 
> - **Bài học thực tế**: SQLite trên Windows KHÔNG phân biệt chữ hoa/thường. Nhưng khi đưa lên PostgreSQL/Supabase trên Linux, PostgreSQL phân biệt chữ hoa/thường rất nghiêm ngặt, tự động ép chữ hoa thành chữ thường ngoại trừ khi bọc ngoặc kép, gây lỗi `42703: column does not exist` hoặc `42501: permission denied`.
> - **Giải pháp bắt buộc**:
>   1. **Thiết kế CSDL & Script SQL**: Tên tất cả Bảng và Cột **100% viết chữ thường (lowercase)** (ví dụ: `users`, `id`, `username`, `passwordhash`, `displayname`, `email`, `role`, `avatarurl`, `isactive`, `createdat`, `matchid`, `userid`).
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

### 1️⃣ Tự Động Nhận Diện Multi-Provider CSDL (PostgreSQL Cloud vs SQLite Local)
Code C# trong `Program.cs` tự động phân nhánh thông minh mà không cần thay đổi source code khi chuyển đổi môi trường:
- Nếu `ConnectionString` chứa `Host=` hoặc `Server=`: Tự động kích hoạt **Npgsql PostgreSQL Driver** kèm cấu hình SSL `SSL Mode=Require;Trust Server Certificate=true;` và cơ chế Thử lại 5 lần (Retry Policy).
- Nếu `ConnectionString` là SQLite (`Data Source=`): Tự động quy đổi đường dẫn tương đối thành tuyệt đối bằng `AppContext.BaseDirectory`, đồng thời ưu tiên tự tìm file DB trong thư mục con `data/` (`C:\inetpub\WorldCupPredictor\data\worldcup.db`).

### 2️⃣ Tự Động Chuyển Đổi URL API Ở Frontend (`app.js`)
Trong `app.js`, tự động nhận diện tên miền đang truy cập:
```javascript
const API_BASE = (window.location.hostname.includes('vercel.app'))
    ? 'https://dudoanbongda-api.onrender.com'
    : '';
```
- **Khi mở trên IIS / Localhost**: `API_BASE` tự bằng `""` (gọi trực tiếp Backend IIS máy nhà, phản hồi 0ms).
- **Khi mở trên Vercel Cloud**: `API_BASE` tự chọn Render Cloud & Supabase Singapore.

### 3️⃣ Phòng Chống Lỗi Trùng Key (`ToDictionary` Defensive LINQ)
Để tránh hiện tượng văng lỗi HTTP 500 (`An item with the same key has already been added`) khi CSDL phát sinh dòng dữ liệu trùng lặp, luôn dùng `GroupBy` trước khi ép sang Dictionary:
```csharp
var myPredictions = await _db.Predictions
    .Where(p => p.UserId == userId && matchIds.Contains(p.MatchId))
    .ToListAsync();

var myPredictionsDict = myPredictions
    .GroupBy(p => p.MatchId)
    .ToDictionary(g => g.Key, g => g.First());
```

### 4️⃣ Khóa Ràng Buộc Duy Nhất (UNIQUE Constraint) Ở Tầng CSDL
Luôn tạo ràng buộc `UNIQUE` ở tầng CSDL để ngăn ngừa 100% việc chèn dữ liệu lặp trùng:
```sql
ALTER TABLE predictions ADD CONSTRAINT uq_predictions_user_match UNIQUE (userid, matchid);
ALTER TABLE tournamentmembers ADD CONSTRAINT uq_tournament_member UNIQUE (tournamentid, userid);
```

### 5️⃣ Chuẩn Hóa Kiểu Dữ Liệu Đa CSDL (Data Types)
- **Boolean**: Trong PostgreSQL dùng kiểu `BOOLEAN` (`true`/`false`), không dùng kiểu integer `1`/`0`.
- **DateTime**: Trong PostgreSQL dùng kiểu `TIMESTAMPTZ` (`timestamp with time zone`).

### 6️⃣ Vô Hiệu Hóa FileWatcher Trên Linux Container (Khắc phục Crash Status 139)
Thêm `ENV DOTNET_USE_POLLING_FILE_WATCHER=1` vào **`Dockerfile`** để tránh tràn inotify handle limit trên Linux Docker Container.
