# Panduan Setup Lingkungan (Windows 11 + VS Code)
## RESTful API + ASP.NET Core MVC (Razor Views) untuk Cashflowpoly

### Dokumen
- Nama dokumen: Panduan Setup Lingkungan
- Versi: 1.0
- Tanggal: 29 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan
Dokumen ini saya susun untuk memandu setup lingkungan pengembangan dan pengujian pada Windows 11 Home, termasuk instalasi perangkat lunak, konfigurasi PostgreSQL, pengaturan *appsettings*, menjalankan skrip skema database dengan DBeaver, serta setup Tailwind untuk UI MVC.

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
Sistem memakai database dan user yang sama dengan `.env`.

Contoh (psql):
```sql
create user cashflowpoly with password 'cashflowpoly';
create database cashflowpoly owner cashflowpoly;
```

### 4.2 Aktifkan extension UUID
Sistem memakai UUID sebagai PK. Sistem mengaktifkan extension:
```sql
create extension if not exists "pgcrypto";
```

Catatan:
- Extension membantu jika menggunakan `gen_random_uuid()` pada DB.

---

## 5. Inisiasi Repo dan Struktur Proyek
Sistem menjalankan langkah berikut pada folder kerja.

### 5.1 Buat folder repo
```bash
mkdir cashflowpoly-analytics-platform
cd cashflowpoly-analytics-platform
```

### 5.2 Inisiasi git
```bash
git init
```

### 5.3 Struktur solution dan proyek
Struktur proyek yang dipakai:
```
cashflowpoly-analytics-platform/
  .env
  .github/
  Cashflowpoly.slnx
  Img/
  docker-compose.yml
  README.md
  docs/
  database/
  src/
    Cashflowpoly.Api/
    Cashflowpoly.Ui/
```

Catatan:
- UI MVC (`Cashflowpoly.Ui`) memanggil API via `HttpClient`. UI tidak mengakses DB langsung.

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
    "Default": "Host=localhost;Port=5432;Database=cashflowpoly;Username=cashflowpoly;Password=cashflowpoly"
  }
}
```

Jika API dijalankan pada port tertentu, URL dapat dikunci pada `Properties/launchSettings.json`.

---

## 8. Setup Skema Database (DBeaver)
Sistem tidak memakai EF Core. Skema dibuat dengan skrip SQL yang disediakan.

### 8.1 Buka koneksi PostgreSQL
1. Jalankan DBeaver.
2. Buat koneksi baru ke PostgreSQL (host, port, database, user, password).

### 8.2 Jalankan skrip skema
1. Buka file `database/00_create_schema.sql`.
2. Jalankan seluruh script di DBeaver pada database `cashflowpoly`.
3. Pastikan tabel dan indeks terbentuk tanpa error.

Catatan:
- Jika extension `pgcrypto` belum aktif, jalankan perintah pada bagian 4.2 sebelum menjalankan script.

---

## 9. Setup Tailwind CSS (MVC)
Sistem memakai Tailwind CLI (bukan CDN).

### 9.1 Instal dependensi
Masuk ke proyek UI:
```bash
cd src/Cashflowpoly.Ui
npm install
```

### 9.2 Build CSS Tailwind
```bash
npm run tailwind:build
```

### 9.3 Mode watch (opsional)
Gunakan saat development agar CSS otomatis ter-update:
```bash
npm run tailwind:watch
```

---

## 10. Checklist Setup Berhasil
Setup selesai jika:
1. `dotnet --list-sdks` menampilkan .NET 10.x,
2. skrip `database/00_create_schema.sql` berhasil dan tabel terbentuk,
3. connection string PostgreSQL sudah benar di `Cashflowpoly.Api/appsettings.Development.json`,
4. `ApiBaseUrl` sudah sesuai di `Cashflowpoly.Ui/appsettings.Development.json`,
5. dependensi Tailwind sudah terpasang (npm install) dan build CSS berhasil.

Untuk menjalankan sistem, lanjutkan ke: `docs/00-Panduan/00-03-panduan-menjalankan-sistem.md`.


