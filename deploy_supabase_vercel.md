# 🚀 Hướng Dẫn Deploy DuDoanBongDa (Supabase + Vercel)

Tài liệu này hướng dẫn chi tiết cách kết nối Cơ sở dữ liệu **Supabase (PostgreSQL)** và phát hành ứng dụng **DuDoanBongDa** lên **Vercel** & Cloud Hosting.

---

## 📐 1. Kiến Trúc Hệ Thống (Deployment Architecture)

Do ứng dụng **DuDoanBongDa** được phát triển trên nền tảng **ASP.NET Core 8.0 (C#)** kết hợp với Giao diện Web (HTML/JS/CSS), mô hình triển khai tối ưu nhất bao gồm:

```mermaid
graph TD
    User["🌐 Người dùng (Trình duyệt)"] -->|Truy cập Website| Vercel["⚡ Vercel (Frontend UI)\nhttps://dudoanbongda.vercel.app"]
    Vercel -->|Gọi REST API (CORS)| Render["⚙️ Render / Railway (Backend .NET 8 API)\nhttps://dudoanbongda-api.onrender.com"]
    Render -->|Kết nối PostgreSQL (Port 6543/5432)| Supabase["🗄️ Supabase Database (PostgreSQL)\naws-0-xxxx.pooler.supabase.com"]
```

---

## 🛠️ 2. Cấu Hình Backend Kết Nối Supabase PostgreSQL

Code Backend trong dự án đã được cập nhật tự động để tương thích với **PostgreSQL (Supabase)** thông qua thư viện `Npgsql.EntityFrameworkCore.PostgreSQL`.

---

## ⚙️ 3. Deploy Backend API lên Render.com (Miễn phí)

### Bước 3.1: Đưa Code lên GitHub (Đã hoàn thành thành công)

1. Khởi tạo và push code ban đầu:
   ```bash
   git init
   git add .
   git commit -m "Deploy setup for Supabase and Vercel"
   git branch -M main
   git remote add origin https://github.com/toanpv1/DuDoanBongDa.git
   git push -u origin main
   ```

2. **Đẩy nấc cập nhật nhỏ (Bổ sung vercel.json & .gitignore):**
   ```bash
   git add .
   git commit -m "Add vercel.json and gitignore config"
   git push
   ```

### Bước 3.2: Tạo Web Service trên Render

1. Truy cập [https://render.com](https://render.com) và đăng nhập bằng GitHub.
2. Click **New +** ➔ Chọn **Web Service**.
3. Kết nối với Repository `toanpv1/DuDoanBongDa` vừa push.
4. Cấu hình thông số:
   * **Name**: `dudoanbongda-api`
   * **Region**: Singapore
   * **Language**: `Docker`
   * **Dockerfile Path**: `Dockerfile`
   * **Instance Type**: `Free`
5. Thêm các biến môi trường trong phần **Environment Variables**:
   * `ConnectionStrings__DefaultConnection`: `Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.vjtuszbyuealeawspsqt;Password=VuiCungWorldCup2026;`
   * `Jwt__Key`: `WorldCupPredictor2026SuperSecretKey!@#$%^&*()`
6. Click **Create Web Service**.
7. URL Backend thu được sẽ có dạng: `https://dudoanbongda-api.onrender.com`

---

## 🎨 4. Cấu Hình & Deploy Frontend Lên Vercel.com

### Bước 4.1: Kết nối Frontend tới Backend API URL

Mở file `wwwroot/js/app.js` và cập nhật hằng số `API_BASE` ở dòng 2 thành URL Render của bạn, sau đó push lên GitHub:
```bash
git add wwwroot/js/app.js
git commit -m "Update API_BASE to Render backend URL"
git push
```

### Bước 4.2: Triển khai lên Vercel.com

1. Truy cập [https://vercel.com](https://vercel.com) ➔ Import repository `toanpv1/DuDoanBongDa`.
2. Cấu hình:
   * **Root Directory**: Chọn `wwwroot`
   * **Framework Preset**: `Other` hoặc `Static HTML`
3. Click **Deploy**.
