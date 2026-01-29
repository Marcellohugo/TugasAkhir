# Rencana Implementasi dan Struktur *Solution* .NET  
## RESTful API + ASP.NET Core MVC (Razor Views) untuk Cashflowpoly

### Dokumen
- Nama dokumen: Rencana Implementasi dan Struktur *Solution* .NET
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan
Dokumen ini menetapkan struktur repositori, struktur *solution* .NET, pembagian proyek REST API dan UI MVC, konvensi folder, dependensi, serta urutan implementasi agar pengerjaan berjalan terukur.

Dokumen ini menargetkan lingkungan:
- Windows 11 Home
- VS Code
- .NET 10
- PostgreSQL
- Swagger UI, Postman

---

## 2. Prinsip Arsitektur yang Dipakai
### 2.1 Target arsitektur
Sistem memakai dua aplikasi dalam satu *solution*:
1. REST API untuk penerimaan event dan analitika,
2. UI MVC (Razor Views) untuk dasbor analitika dan manajemen *ruleset*.

### 2.2 Integrasi UI
UI MVC bertindak sebagai *client* internal yang memanggil REST API melalui `HttpClient`. UI tidak mengakses DB langsung. UI memetakan DTO API ke ViewModel.

### 2.3 Kenapa tetap “MVC” walau ada API
Kampus meminta MVC untuk UI. Sistem tetap memenuhi itu karena:
- UI memakai *Controller* + Razor Views (MVC),
- REST API memakai controller API terpisah,
- keduanya tetap berada dalam satu *solution*.

---

## 3. Struktur Repositori
Sistem memakai monorepo satu repositori dengan satu *solution*.

```
cashflowpoly-analytics-platform/
  .env
  .github/
  .gitignore
  Cashflowpoly.slnx
  Img/
  docker-compose.yml
  README.md
  database/
  docs/
    00-Panduan/
      00-01-panduan-setup-lingkungan.md
      00-02-manual-pengguna-dan-skenario-operasional.md
      00-03-panduan-menjalankan-sistem.md
    01-Spesifikasi/
      01-01-spesifikasi-kebutuhan-sistem.md
      01-02-spesifikasi-event-dan-kontrak-api.md
      01-03-spesifikasi-ruleset-dan-validasi.md
    02-Perancangan/
      02-01-rancangan-model-data-dan-basis-data.md
      02-02-definisi-metrik-dan-agregasi.md
      02-03-rancangan-dashboard-analitika-mvc.md
      02-04-rencana-implementasi-dan-struktur-solution-dotnet.md
      02-05-spesifikasi-ui-mvc-dan-rancangan-viewmodel.md
    03-Pengujian/
      03-01-rencana-pengujian-fungsional-dan-validasi.md
      03-02-laporan-hasil-pengujian.md
  src/
    Cashflowpoly.Api/
    Cashflowpoly.Ui/
```

Catatan:
- Folder `docs/` berisi semua dokumen MD untuk laporan tugas akhir.
- Folder `src/` berisi semua proyek utama.

---

## 4. Struktur *Solution* dan Tanggung Jawab Proyek
### 4.1 `Cashflowpoly.Api`
**Tanggung jawab:**
- ASP.NET Core Web API
- Controller:
  - SessionsController
  - RulesetsController
  - EventsController
  - AnalyticsController
- Validasi model request (atribut + *problem details* custom)
- Swagger dan versi API
- Middleware error handling dan tracing

### 4.2 `Cashflowpoly.Ui`
**Tanggung jawab:**
- ASP.NET Core MVC + Razor Views
- Controller UI:
  - SessionsController (UI)
  - PlayersController (UI)
  - RulesetsController (UI)
- ViewModel + mapping dari DTO API
- Tailwind setup dan komponen view

**Dependensi:**
- bergantung pada `HttpClient` untuk memanggil API.

---

## 5. Konvensi Folder per Proyek
### 5.1 API
```
Cashflowpoly.Api/
  Controllers/
  Middlewares/
  Mappers/
  Swagger/
```

### 5.2 UI (MVC)
```
Cashflowpoly.Ui/
  Controllers/
  ViewModels/
  Views/
    Shared/
  wwwroot/
```

---

## 6. Dependensi Paket
### 6.1 API
- `Swashbuckle.AspNetCore` (Swagger UI)
- `Microsoft.AspNetCore.OpenApi` (opsional)
- `FluentValidation.AspNetCore` (opsional, kalau kamu mau validasi yang rapi)
- paket akses PostgreSQL sesuai implementasi (mis. `Npgsql`)

### 6.2 UI (MVC)
- `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` (opsional, membantu saat dev)

Catatan: kamu bisa mulai tanpa FluentValidation agar implementasi cepat, lalu tambah kalau butuh.

---

## 7. Konfigurasi *Solution* di VS Code
### 7.1 Struktur run
Sistem menjalankan API dan UI secara paralel:
- API: `https://localhost:7041` atau `http://localhost:5041`
- UI: `https://localhost:7203` atau `http://localhost:5203`

Sistem mengatur base URL API untuk UI melalui `Cashflowpoly.Ui/appsettings.json`:
```json
{
  "ApiBaseUrl": "http://localhost:5041"
}
```

### 7.2 Konvensi port
Sistem mengunci port via file `.env`:
- API HTTP: `5041`
- API HTTPS: `7041`
- UI HTTP: `5203`
- UI HTTPS: `7203`

---

## 8. Urutan Implementasi
Sistem menerapkan urutan berikut agar setiap langkah langsung bisa diuji.

### Tahap A — *Bootstrap* proyek
1. Buat repo dan *solution*.
2. Buat proyek Api dan Ui.

Output:
- solusi bisa *build* tanpa error,
- swagger API bisa terbuka,
- halaman UI bisa terbuka.

### Tahap B — Database dan skema SQL
1. Jalankan skrip `database/00_create_schema.sql` pada PostgreSQL (mis. lewat DBeaver).
2. Pastikan semua tabel dan indeks terbentuk sesuai dokumen 04.

Output:
- skema terbentuk sesuai dokumen 04.

### Tahap C — Endpoint inti modul sesi dan ruleset
1. Implementasi use case:
   - CreateSession, StartSession, EndSession
   - CreateRuleset, UpdateRuleset, ActivateRuleset
2. Implementasi controller API untuk endpoint tersebut.
3. Uji dengan Swagger/Postman.

Output:
- TC-API-01 s.d. TC-API-09 bisa kamu jalankan.

### Tahap D — Ingest event + idempotensi + urutan
1. Implementasi endpoint ingest event.
2. Implementasi validasi:
   - format dan tipe payload dasar,
   - idempotensi `(session_id, event_id)`,
   - urutan `sequence_number`,
   - kecocokan `ruleset_version_id` terhadap sesi aktif.
3. Simpan event ke tabel `events` dengan `event_pk` sebagai PK internal, sementara `event_id` tetap dipakai untuk idempotensi.
4. Simpan kegagalan ke `validation_logs` (jika kamu pakai).

Output:
- TC-API-10 s.d. TC-API-14 bisa kamu jalankan.

### Tahap E — Proyeksi arus kas
1. Buat translator event → `event_cashflow_projections`.
2. Tulis proyeksi hanya untuk event yang memengaruhi uang:
   - `transaction.recorded`, `day.friday.donation`, `ingredient.purchased`, `order.claimed`.
3. Saat menyimpan proyeksi, simpan referensi `event_pk` (FK ke `events.event_pk`) agar integritas terjaga.
4. Uji query tabel proyeksi.

Output:
- proyeksi transaksi terbentuk otomatis.

### Tahap F — Agregasi metrik + snapshot
1. Implementasi komputasi metrik minimum dari dokumen 05.
2. Simpan ke `metric_snapshots`.
3. Sediakan endpoint “recompute” (opsional) untuk memudahkan uji.

Output:
- snapshot metrik muncul dan konsisten.

### Tahap G — Endpoint analitika
1. Implementasi:
   - `GET /api/analytics/sessions/{sessionId}`
   - `GET /api/analytics/sessions/{sessionId}/transactions?playerId=...`
2. Pastikan query cepat memakai indeks dan snapshot.

Output:
- TC-API-15 s.d. TC-API-17 lulus.

### Tahap H — UI MVC
1. Buat halaman:
   - `/sessions`
   - `/sessions/{sessionId}`
   - `/sessions/{sessionId}/players/{playerId}`
   - `/sessions/{sessionId}/ruleset`
2. Panggil API via `HttpClient`.
3. Terapkan Tailwind untuk tampilan.

Output:
- UI-01 s.d. UI-03 siap diuji.

---

## 9. Definisi “Satu file dengan *solution* berbeda”
Kalimat “membuat semuanya dalam 1 file dengan *solution* yang berbeda” biasanya rancu.

Struktur yang benar pada .NET:
- satu repositori,
- satu file `.slnx`,
- beberapa proyek (`.csproj`) untuk API dan UI.

Sistem tidak menaruh semua kode pada satu file `.cs` karena itu menghambat pengujian dan pemeliharaan.

---

## 10. Checklist Kesiapan Mulai Coding
Sistem siap kamu mulai implementasikan jika:
1. kamu sudah membuat struktur `src/`,
2. API sudah jalan dengan Swagger,
3. UI MVC sudah jalan dan bisa memanggil API (meski endpoint masih dummy),
4. PostgreSQL siap dan koneksi sudah benar.

---

## 11. *Deliverable* per Minggu (opsional)
Jika kamu butuh target mingguan:
- Minggu 1: Tahap A–B
- Minggu 2: Tahap C–D
- Minggu 3: Tahap E–F
- Minggu 4: Tahap G–H + pengujian (dokumen 07)




