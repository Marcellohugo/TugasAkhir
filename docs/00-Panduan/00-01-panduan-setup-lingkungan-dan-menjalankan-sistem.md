# Panduan Setup Lingkungan dan Menjalankan Sistem (Windows 11 + VS Code)  
## RESTful API + ASP.NET Core MVC (Razor Views) untuk Cashflowpoly

### Dokumen
- Nama dokumen: Panduan Setup Lingkungan dan Menjalankan Sistem
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan
Dokumen ini memandu setup lingkungan pengembangan dan pengujian pada Windows 11 Home, termasuk instalasi perangkat lunak, konfigurasi PostgreSQL, pengaturan *appsettings*, menjalankan skrip skema database dengan DBeaver, menjalankan REST API dan UI MVC, serta langkah troubleshooting yang paling sering terjadi.

---

## 2. Perangkat Lunak yang Dibutuhkan
Sistem membutuhkan komponen berikut pada mesin pengembang.

| No | Perangkat Lunak | Fungsi Penggunaan |
|---:|---|---|
| 1 | Windows 11 Home | Menjalankan lingkungan kerja pengembangan dan pengujian sistem |
| 2 | Visual Studio Code | Menulis kode, menjalankan perintah terminal, dan mengelola proyek .NET |
| 3 | Google Chrome | Menguji antarmuka web, mengakses Swagger UI, dan memverifikasi tampilan dasbor |
| 4 | ASP.NET Core 10 | Mengembangkan layanan RESTful API dan aplikasi web MVC |
| 5 | ASP.NET Core MVC (Razor Views) | Membangun antarmuka web untuk dasbor analitika dan manajemen ruleset |
| 6 | PostgreSQL | Menyimpan data sesi, pemain, ruleset, event, proyeksi cashflow, metrik, dan log validasi |
| 7 | Docker Desktop | Menjalankan PostgreSQL, API, dan UI dalam container untuk deployment dan uji integrasi |
| 8 | DBeaver | Mengelola basis data PostgreSQL: menjalankan DDL, melihat tabel, query data, dan memeriksa hasil pengujian |
| 9 | Swagger (Swashbuckle) | Mendokumentasikan dan menguji endpoint REST API secara interaktif melalui OpenAPI/Swagger UI |
| 10 | Postman | Melakukan pengujian fungsional endpoint REST API dengan skenario *black-box* |
| 11 | Tailwind CSS | Mengatur styling antarmuka dasbor agar konsisten dan responsif |

Catatan:
- Sistem memasang Swagger melalui paket NuGet pada proyek API, bukan instalasi terpisah.

---

## 3. Instalasi .NET 10 dan Verifikasi
### 3.1 Cek versi .NET
Jalankan:
```bash
dotnet --info
dotnet --list-sdks
```

Ekspektasi:
- Output menampilkan .NET SDK versi 10.x sebagai versi terpasang.

Jika output belum menampilkan .NET 10, sistem membutuhkan instalasi .NET 10 SDK.

---

## 4. Instalasi PostgreSQL dan Verifikasi
### 4.1 Buat database dan user
Sistem membuat database dan user terpisah untuk proyek.

Contoh (psql):
```sql
create user cashflowpoly_user with password 'cashflowpoly_pass';
create database cashflowpoly_db owner cashflowpoly_user;
```

### 4.2 Aktifkan extension UUID
Sistem memakai UUID sebagai PK. Sistem mengaktifkan extension:
```sql
create extension if not exists "uuid-ossp";
```

Catatan:
- Extension membantu jika kamu memakai `uuid_generate_v4()` pada DB.

---

## 5. Inisiasi Repo dan Struktur Proyek
Sistem menjalankan langkah berikut pada folder kerja.

### 5.1 Buat folder repo
```bash
mkdir cashflowpoly-dashboard
cd cashflowpoly-dashboard
```

### 5.2 Inisiasi git (opsional)
```bash
git init
```

### 5.3 Buat solution dan proyek
Contoh struktur (sesuaikan dengan dokumen 08):
```bash
mkdir src tests docs
cd src

dotnet new sln -n Cashflowpoly

dotnet new classlib -n Cashflowpoly.Domain
dotnet new classlib -n Cashflowpoly.Application
dotnet new classlib -n Cashflowpoly.Infrastructure
dotnet new classlib -n Cashflowpoly.Contracts

dotnet new webapi -n Cashflowpoly.Api
dotnet new mvc -n Cashflowpoly.Web

dotnet sln Cashflowpoly.sln add Cashflowpoly.Domain/Cashflowpoly.Domain.csproj
dotnet sln Cashflowpoly.sln add Cashflowpoly.Application/Cashflowpoly.Application.csproj
dotnet sln Cashflowpoly.sln add Cashflowpoly.Infrastructure/Cashflowpoly.Infrastructure.csproj
dotnet sln Cashflowpoly.sln add Cashflowpoly.Contracts/Cashflowpoly.Contracts.csproj
dotnet sln Cashflowpoly.sln add Cashflowpoly.Api/Cashflowpoly.Api.csproj
dotnet sln Cashflowpoly.sln add Cashflowpoly.Web/Cashflowpoly.Web.csproj
```

### 5.4 Tambah project reference
Sistem menjalankan:
```bash
dotnet add Cashflowpoly.Application/Cashflowpoly.Application.csproj reference Cashflowpoly.Domain/Cashflowpoly.Domain.csproj
dotnet add Cashflowpoly.Infrastructure/Cashflowpoly.Infrastructure.csproj reference Cashflowpoly.Application/Cashflowpoly.Application.csproj
dotnet add Cashflowpoly.Infrastructure/Cashflowpoly.Infrastructure.csproj reference Cashflowpoly.Domain/Cashflowpoly.Domain.csproj

dotnet add Cashflowpoly.Api/Cashflowpoly.Api.csproj reference Cashflowpoly.Application/Cashflowpoly.Application.csproj
dotnet add Cashflowpoly.Api/Cashflowpoly.Api.csproj reference Cashflowpoly.Contracts/Cashflowpoly.Contracts.csproj
dotnet add Cashflowpoly.Web/Cashflowpoly.Web.csproj reference Cashflowpoly.Contracts/Cashflowpoly.Contracts.csproj
```

Catatan:
- UI MVC memanggil API via `HttpClient`. UI tidak mereferensikan Infrastructure.

---

## 6. Tambah Paket NuGet Inti
### 6.1 Proyek API (Swagger)
Jika template webapi belum menyertakan Swagger, pasang:
```bash
dotnet add Cashflowpoly.Api/Cashflowpoly.Api.csproj package Swashbuckle.AspNetCore
```

---

## 7. Konfigurasi Connection String
Sistem menyimpan koneksi PostgreSQL pada `appsettings.Development.json` di proyek API.

Contoh `src/Cashflowpoly.Api/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=cashflowpoly_db;Username=cashflowpoly_user;Password=cashflowpoly_pass"
  }
}
```

Jika kamu menjalankan API pada port tertentu, kamu dapat mengunci URL pada `Properties/launchSettings.json`.

---

## 8. Setup Skema Database (DBeaver)
Sistem tidak memakai EF Core. Skema dibuat dengan skrip SQL yang disediakan.

### 8.1 Buka koneksi PostgreSQL
1. Jalankan DBeaver.
2. Buat koneksi baru ke PostgreSQL (host, port, database, user, password).

### 8.2 Jalankan skrip skema
1. Buka file `database/00_create_schema.sql`.
2. Jalankan seluruh script di DBeaver pada database `cashflowpoly_db`.
3. Pastikan tabel dan indeks terbentuk tanpa error.

Catatan:
- Jika extension `uuid-ossp` belum aktif, jalankan perintah pada bagian 4.2 sebelum menjalankan script.

---

## 9. Menjalankan REST API dan Swagger
### 9.1 Jalankan API
Dari folder `src`:
```bash
dotnet run --project Cashflowpoly.Api
```

### 9.2 Akses Swagger
Buka Chrome:
- `https://localhost:<PORT>/swagger`

Jika port memakai HTTP:
- `http://localhost:<PORT>/swagger`

Jika `weatherforecast` bisa dibuka tetapi swagger gagal, penyebab yang paling sering:
1. middleware swagger belum diaktifkan untuk environment `Development`,
2. URL salah (http vs https),
3. port berbeda dari yang kamu buka,
4. browser blokir sertifikat dev (untuk https).

Sistem menyelesaikan dengan:
- cek output console API untuk URL listen,
- pakai URL itu pada Chrome,
- jika pakai https dan sertifikat ditolak, tekan “Advanced” lalu “Proceed”.

---

## 10. Menjalankan UI MVC
### 10.1 Jalankan Web
Dari folder `src`:
```bash
dotnet run --project Cashflowpoly.Web
```

### 10.2 Atur base URL API
Sistem menaruh base URL API di `Cashflowpoly.Web/appsettings.Development.json`:
```json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

### 10.3 Uji halaman
Buka:
- `https://localhost:<PORT_WEB>/sessions`

Jika halaman masih kosong, itu normal jika endpoint `/api/sessions` belum kamu implementasikan.

---

## 11. Menjalankan Dua Proyek Sekaligus (VS Code)
### 11.1 Cara cepat (dua terminal)
1. Terminal 1:
```bash
dotnet run --project src/Cashflowpoly.Api
```
2. Terminal 2:
```bash
dotnet run --project src/Cashflowpoly.Web
```

### 11.2 Cara *debug* dengan `launch.json`
Sistem bisa membuat `.vscode/launch.json` untuk dua konfigurasi. Kamu dapat menjalankan keduanya melalui “Run and Debug”.

Catatan:
- Dokumen ini tidak memaksa kamu memakai `launch.json` karena setup tiap mesin berbeda.

---

## 12. Setup Tailwind CSS (MVC)
Sistem punya dua opsi.

### 12.1 Opsi A (sementara, cepat): CDN
Tambahkan pada `_Layout.cshtml`:
```html
<script src="https://cdn.tailwindcss.com"></script>
```

### 12.2 Opsi B (disarankan): build Tailwind
Sistem memasang Node.js dan menjalankan Tailwind CLI. Jika kampus tidak menuntut build pipeline, opsi A sudah cukup untuk tahap awal.

---

## 13. Troubleshooting Umum
### 13.1 PostgreSQL connection error
Penyebab umum:
- username/password salah,
- service PostgreSQL tidak berjalan,
- port berbeda,
- firewall.

Langkah cek:
1. uji login melalui pgAdmin/psql,
2. uji connection string pada API,
3. cek log output API.

### 13.2 Swagger tidak bisa dibuka
Penyebab umum:
- `app.UseSwagger()` atau `app.UseSwaggerUI()` belum dipanggil,
- environment bukan Development,
- port/URL salah.

Solusi:
- pastikan `Swagger` aktif pada `Development`,
- cek URL listen pada console,
- akses `/swagger`.

### 13.3 CORS error saat Web memanggil API
Jika Web berjalan pada origin berbeda dan memanggil API via browser, kamu bisa:
- aktifkan CORS pada API untuk origin Web, atau
- jalankan Web sebagai server-side MVC yang memanggil API dari server (bukan dari JS browser).

Dokumen rancangan UI memakai pendekatan server-side call via `HttpClient`, sehingga CORS biasanya tidak muncul.

---

## 14. Checklist Setup Berhasil
Setup selesai jika:
1. `dotnet build` sukses untuk solution,
2. skrip `database/00_create_schema.sql` berhasil dan tabel terbentuk,
3. Swagger UI bisa kamu akses,
4. endpoint sample bisa kamu panggil,
5. MVC bisa jalan dan menampilkan halaman.
