# Rencana Implementasi dan Struktur *Solution* .NET  
## RESTful API + ASP.NET Core MVC (Razor Views) untuk Cashflowpoly

### Dokumen
- Nama dokumen: Rencana Implementasi dan Struktur *Solution* .NET
- Versi: 1.0
- Tanggal: (isi tanggal)
- Penyusun: (isi nama)

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
Sistem memakai pola berlapis (*layered architecture*) yang memisahkan:
1. API (HTTP boundary),
2. aplikasi (*use case*),
3. domain (aturan bisnis),
4. infrastruktur (EF Core, PostgreSQL, logging).

Pola ini memenuhi karakter “*clean architecture*” pada level praktis jika:
- domain tidak bergantung pada EF Core,
- aplikasi tidak bergantung pada ASP.NET MVC,
- infrastruktur bergantung ke domain dan aplikasi, bukan sebaliknya.

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
cashflowpoly-dashboard/
  .editorconfig
  .gitignore
  README.md
  docs/
    02-spesifikasi-event-dan-kontrak-api.md
    03-spesifikasi-ruleset-dan-validasi.md
    04-rancangan-model-data-dan-basis-data.md
    05-definisi-metrik-dan-agregasi.md
    06-rancangan-dashboard-analitika-mvc.md
    07-rencana-pengujian-fungsional-dan-validasi.md
    08-rencana-implementasi-dan-struktur-solution-dotnet.md
  src/
    Cashflowpoly.sln
    Cashflowpoly.Api/
    Cashflowpoly.Web/
    Cashflowpoly.Application/
    Cashflowpoly.Domain/
    Cashflowpoly.Infrastructure/
    Cashflowpoly.Contracts/
  tests/
    Cashflowpoly.Tests.Unit/
    Cashflowpoly.Tests.Integration/
```

Catatan:
- Folder `docs/` berisi semua dokumen MD untuk laporan tugas akhir.
- Folder `src/` berisi semua proyek utama.

---

## 4. Struktur *Solution* dan Tanggung Jawab Proyek
### 4.1 `Cashflowpoly.Domain`
**Tanggung jawab:**
- entitas domain inti: Session, Ruleset, Event, Projection, Metric
- *value object* dan enum
- aturan validasi domain tingkat tinggi (batasan urutan, batasan kepemilikan) sebagai fungsi murni bila memungkinkan

**Larangan:**
- tidak boleh referensi EF Core, ASP.NET, `HttpContext`, atau JSON serializer khusus.

### 4.2 `Cashflowpoly.Application`
**Tanggung jawab:**
- *use case* / layanan aplikasi:
  - CreateSession, StartSession, EndSession
  - CreateRuleset, UpdateRuleset, ActivateRuleset
  - IngestEvent (validasi + persist + trigger proyeksi)
  - ComputeMetrics / Recompute
  - QueryAnalyticsSummary, QueryTransactions
- interface port:
  - `ISessionRepository`
  - `IRulesetRepository`
  - `IEventRepository`
  - `IAnalyticsQuery`
  - `IUnitOfWork`
  - `IClock`, `IIdGenerator`, `IHashingService`

**Output:**
- DTO internal untuk kebutuhan API (bukan ViewModel).

### 4.3 `Cashflowpoly.Infrastructure`
**Tanggung jawab:**
- EF Core DbContext dan konfigurasi entitas
- implementasi repository
- migrasi database
- implementasi hashing, time, id generator (bila perlu)
- implementasi query analitika berbasis SQL/EF
- implementasi proyeksi arus kas

**Dependensi:**
- bergantung pada Application dan Domain.

### 4.4 `Cashflowpoly.Contracts`
**Tanggung jawab:**
- DTO publik untuk kontrak API
- request/response model
- enum untuk kontrak API

Tujuan proyek ini:
- API dan Web dapat berbagi kontrak tanpa saling bergantung.
- Pengujian *contract* lebih mudah.

### 4.5 `Cashflowpoly.Api`
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

**Dependensi:**
- bergantung pada Application dan Contracts.

### 4.6 `Cashflowpoly.Web`
**Tanggung jawab:**
- ASP.NET Core MVC + Razor Views
- Controller UI:
  - SessionsController (UI)
  - PlayersController (UI)
  - RulesetsController (UI)
- ViewModel + mapping dari DTO API
- Tailwind setup dan komponen view

**Dependensi:**
- bergantung pada Contracts (untuk DTO) atau *client DTO* sendiri.
- bergantung pada `HttpClient` untuk memanggil API.

---

## 5. Konvensi Folder per Proyek

### 5.1 Domain
```
Cashflowpoly.Domain/
  Sessions/
  Rulesets/
  Events/
  Metrics/
  Common/
```

### 5.2 Application
```
Cashflowpoly.Application/
  Abstractions/
    Persistence/
    Services/
  UseCases/
    Sessions/
    Rulesets/
    Events/
    Analytics/
    Metrics/
  Dtos/
  Validation/
```

### 5.3 Infrastructure
```
Cashflowpoly.Infrastructure/
  Persistence/
    AppDbContext.cs
    Configurations/
    Migrations/
  Repositories/
  Analytics/
  Projections/
  Observability/
```

### 5.4 API
```
Cashflowpoly.Api/
  Controllers/
  Middlewares/
  Mappers/
  Swagger/
```

### 5.5 Web (MVC)
```
Cashflowpoly.Web/
  Controllers/
  ViewModels/
  Views/
    Shared/
  wwwroot/
```

---

## 6. Dependensi Paket yang Disarankan
### 6.1 API
- `Swashbuckle.AspNetCore` (Swagger UI)
- `Microsoft.AspNetCore.OpenApi` (opsional)
- `FluentValidation.AspNetCore` (opsional, kalau kamu mau validasi yang rapi)

### 6.2 Infrastructure
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.Design`

### 6.3 Web (MVC)
- `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` (opsional, membantu saat dev)

Catatan: kamu bisa mulai tanpa FluentValidation agar implementasi cepat, lalu tambah kalau butuh.

---

## 7. Konfigurasi *Solution* di VS Code
### 7.1 Struktur run
Sistem menjalankan API dan Web secara paralel:
- API: `https://localhost:7xxx` atau `http://localhost:5xxx`
- Web: `https://localhost:8xxx` atau `http://localhost:6xxx`

Sistem mengatur base URL API untuk Web melalui `Cashflowpoly.Web/appsettings.json`:
```json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

### 7.2 Konvensi port
Sistem mengunci port agar dokumentasi dan Postman stabil. Kamu bisa set port via `launchSettings.json` atau `--urls`.

---

## 8. Urutan Implementasi yang Disarankan
Sistem menerapkan urutan berikut agar setiap langkah langsung bisa diuji.

### Tahap A — *Bootstrap* proyek
1. Buat repo dan *solution*.
2. Buat proyek Domain, Application, Infrastructure, Contracts, Api, Web.
3. Tambah referensi proyek sesuai dependensi.

Output:
- solusi bisa *build* tanpa error,
- swagger API bisa terbuka,
- halaman Web bisa terbuka.

### Tahap B — Database dan migrasi
1. Buat `AppDbContext` + entitas EF Core untuk tabel inti:
   - players, sessions, session_players,
   - rulesets, ruleset_versions, session_ruleset_activations,
   - events,
   - event_cashflow_projections, metric_snapshots, validation_logs (opsional).
2. Buat migrasi dan jalankan ke PostgreSQL.

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
3. Simpan event ke tabel `events`.
4. Simpan kegagalan ke `validation_logs` (jika kamu pakai).

Output:
- TC-API-10 s.d. TC-API-14 bisa kamu jalankan.

### Tahap E — Proyeksi arus kas
1. Buat translator event → `event_cashflow_projections`.
2. Tulis proyeksi hanya untuk event yang memengaruhi uang:
   - `transaction.recorded`, `day.friday.donation`, `ingredient.purchased`, `order.claimed`.
3. Uji query tabel proyeksi.

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
- satu file `.sln`,
- beberapa proyek (`.csproj`) untuk API dan Web.

Sistem tidak menaruh semua kode pada satu file `.cs` karena itu menghambat pengujian dan pemeliharaan.

---

## 10. Checklist Kesiapan Mulai Coding
Sistem siap kamu mulai implementasikan jika:
1. kamu sudah membuat struktur `src/` dan `tests/`,
2. API sudah jalan dengan Swagger,
3. Web MVC sudah jalan dan bisa memanggil API (meski endpoint masih dummy),
4. PostgreSQL siap dan koneksi sudah benar.

---

## 11. *Deliverable* per Minggu (opsional)
Jika kamu butuh target mingguan:
- Minggu 1: Tahap A–B
- Minggu 2: Tahap C–D
- Minggu 3: Tahap E–F
- Minggu 4: Tahap G–H + pengujian (dokumen 07)

