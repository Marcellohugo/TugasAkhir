# Panduan Menjalankan Sistem (Windows 11 + VS Code)
## RESTful API + ASP.NET Core MVC (Razor Views) untuk Cashflowpoly

### Dokumen
- Nama dokumen: Panduan Menjalankan Sistem
- Versi: 1.0
- Tanggal: 29 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan
Dokumen ini saya susun untuk memandu cara menjalankan REST API dan UI MVC, mengakses Swagger UI, menjalankan dua proyek sekaligus, serta langkah troubleshooting yang paling sering terjadi.

---

## 2. Prasyarat
Pastikan setup lingkungan sudah selesai:
- .NET 10 SDK terpasang,
- PostgreSQL siap dan skema `database/00_create_schema.sql` sudah dijalankan,
- connection string API dan `ApiBaseUrl` UI sudah benar.

Lihat detailnya pada: `docs/00-Panduan/00-01-panduan-setup-lingkungan.md`.

---

## 3. Menjalankan REST API dan Swagger
### 3.1 Jalankan API
Dari folder `src`:
```bash
dotnet run --project Cashflowpoly.Api
```

### 3.2 Akses Swagger
Buka Chrome:
- `https://localhost:7041/swagger` (HTTPS)
- `http://localhost:5041/swagger` (HTTP)

Jika `weatherforecast` bisa dibuka tetapi swagger gagal, penyebab yang paling sering:
1. middleware swagger belum diaktifkan untuk environment `Development`,
2. URL salah (http vs https),
3. port berbeda dari yang dibuka,
4. browser blokir sertifikat dev (untuk https).

Sistem menyelesaikan dengan:
- cek output console API untuk URL listen,
- pakai URL itu pada Chrome,
- jika pakai https dan sertifikat ditolak, tekan "Advanced" lalu "Proceed".

---

## 4. Menjalankan UI MVC
### 4.1 Jalankan Web
Dari folder `src`:
```bash
dotnet run --project Cashflowpoly.Ui
```

### 4.2 Atur base URL API
Sistem menaruh base URL API di `Cashflowpoly.Ui/appsettings.Development.json`:
```json
{
  "ApiBaseUrl": "http://localhost:5041"
}
```

### 4.3 Uji halaman
Buka:
- `https://localhost:7203/sessions` (HTTPS)
- `http://localhost:5203/sessions` (HTTP)

Jika halaman masih kosong, itu normal jika endpoint `/api/sessions` belum diimplementasikan.

Catatan:
- Jika styling belum muncul, pastikan Tailwind sudah dibuild sesuai panduan setup.

---

## 5. Menjalankan Dua Proyek Sekaligus (VS Code)
### 5.1 Cara cepat (dua terminal)
1. Terminal 1:
```bash
dotnet run --project src/Cashflowpoly.Api
```
2. Terminal 2:
```bash
dotnet run --project src/Cashflowpoly.Ui
```

### 5.2 Cara *debug* dengan `launch.json`
Sistem bisa membuat `.vscode/launch.json` untuk dua konfigurasi. Keduanya dapat dijalankan keduanya melalui "Run and Debug".

Catatan:
- Dokumen ini tidak mewajibkan penggunaan `launch.json` karena setup tiap mesin berbeda.

---

## 6. Troubleshooting Umum
### 6.1 PostgreSQL connection error
Penyebab umum:
- username/password salah,
- service PostgreSQL tidak berjalan,
- port berbeda,
- firewall.

Langkah cek:
1. uji login melalui pgAdmin/psql,
2. uji connection string pada API,
3. cek log output API.

### 6.2 Swagger tidak bisa dibuka
Penyebab umum:
- `app.UseSwagger()` atau `app.UseSwaggerUI()` belum dipanggil,
- environment bukan Development,
- port/URL salah.

Solusi:
- pastikan `Swagger` aktif pada `Development`,
- cek URL listen pada console,
- akses `/swagger`.

### 6.3 CORS error saat Web memanggil API
Jika Web berjalan pada origin berbeda dan memanggil API via browser, dapat dilakukan:
- aktifkan CORS pada API untuk origin Web, atau
- jalankan Web sebagai server-side MVC yang memanggil API dari server (bukan dari JS browser).

Dokumen rancangan UI memakai pendekatan server-side call via `HttpClient`, sehingga CORS biasanya tidak muncul.

---

## 7. Checklist Menjalankan Sistem
Sistem berjalan baik jika:
1. `dotnet build` sukses untuk solution,
2. Swagger UI dapat diakses,
3. endpoint sample dapat dipanggil,
4. MVC bisa jalan dan menampilkan halaman.


