# Spesifikasi UI MVC dan Rancangan *ViewModel*  
## Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly (ASP.NET Core MVC + Razor Views)

### Dokumen
- Nama dokumen: Spesifikasi UI MVC dan Rancangan *ViewModel*
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan
Dokumen ini menetapkan:
1. struktur halaman UI MVC yang sistem bangun,
2. *ViewModel* per halaman,
3. struktur folder `Views/` dan *partial view*,
4. pemetaan DTO dari REST API ke *ViewModel*,
5. standar tampilan komponen utama dengan Tailwind CSS.

Dokumen ini membuat implementasi UI konsisten dan mudah diuji terhadap kontrak API.

---

## 2. Prinsip Implementasi UI
### 2.1 UI tidak mengakses basis data
UI memanggil REST API melalui `HttpClient`. UI tidak melakukan query PostgreSQL.

### 2.2 UI tidak menghitung metrik
UI menampilkan metrik dari endpoint analitika. UI tidak membuat rumus ulang di sisi tampilan.

### 2.3 UI memisahkan DTO dan ViewModel
UI menerima DTO dari API, lalu memetakan ke *ViewModel* yang khusus untuk tampilan.

### 2.4 UI menjaga format angka dan tanggal
UI melakukan format:
- integer koin dengan pemisah ribuan,
- rasio sebagai persentase 0–100,
- waktu sebagai `dd/MM/yyyy HH:mm` atau format yang kamu pakai pada laporan.

---

## 3. Struktur Navigasi dan Halaman
Sistem membangun halaman berikut (minimum untuk tugas akhir).

| Halaman | Route | Fokus |
|---|---|---|
| Daftar sesi | `/sessions` | pilih sesi dan lihat status |
| Detail sesi | `/sessions/{sessionId}` | ringkasan metrik sesi dan daftar pemain |
| Detail pemain | `/sessions/{sessionId}/players/{playerId}` | metrik pemain dan histori transaksi |
| Aktivasi ruleset | `/sessions/{sessionId}/ruleset` | pilih ruleset versi dan aktifkan |
| Daftar ruleset | `/rulesets` | lihat ruleset dan versi |
| Detail ruleset | `/rulesets/{rulesetId}` | lihat detail versi dan config JSON |

Catatan:
- UI dapat menambah halaman audit event setelah sistem stabil.

---

## 4. Struktur Folder UI
Sistem mengikuti struktur berikut pada proyek MVC.

```
Cashflowpoly.Web/
  Controllers/
    SessionsController.cs
    PlayersController.cs
    RulesetsController.cs
  Clients/
    ApiClient.cs
    ApiClientOptions.cs
  ViewModels/
    Common/
      MetricCardVm.cs
      BreadcrumbVm.cs
      PagingVm.cs
    Sessions/
      SessionListItemVm.cs
      SessionsIndexVm.cs
      SessionSummaryVm.cs
      SessionPlayerRowVm.cs
      SessionDetailsVm.cs
      SessionRulesetVm.cs
    Players/
      PlayerMetricVm.cs
      TransactionRowVm.cs
      PlayerDetailsVm.cs
    Rulesets/
      RulesetListItemVm.cs
      RulesetVersionVm.cs
      RulesetDetailsVm.cs
  Views/
    Shared/
      _Layout.cshtml
      _Alert.cshtml
      _MetricCard.cshtml
      _Breadcrumb.cshtml
      _Table.cshtml
      _Pagination.cshtml
    Sessions/
      Index.cshtml
      Details.cshtml
      Ruleset.cshtml
    Players/
      Details.cshtml
    Rulesets/
      Index.cshtml
      Details.cshtml
  wwwroot/
    css/
      site.css
    js/
      site.js
```

Catatan:
- Folder `Clients/` memuat *wrapper* `HttpClient` agar controller UI tidak penuh kode HTTP.

---

## 5. Kontrak API yang UI Pakai
UI memakai endpoint berikut sebagai sumber data.

| Kebutuhan UI | Endpoint |
|---|---|
| Daftar sesi | `GET /api/sessions` |
| Detail sesi + ringkasan metrik + per pemain | `GET /api/analytics/sessions/{sessionId}` |
| Histori transaksi pemain | `GET /api/analytics/sessions/{sessionId}/transactions?playerId=...` |
| Daftar ruleset + versi | `GET /api/rulesets` |
| Aktivasi ruleset | `POST /api/sessions/{sessionId}/ruleset/activate` |

Jika API belum menyediakan salah satu endpoint, UI menampilkan pesan error terstruktur pada halaman tersebut.

---

## 6. Spesifikasi *ViewModel* (Detail)

## 6.1 *Common ViewModel*
### 6.1.1 `MetricCardVm`
UI menampilkan kartu metrik dengan struktur:
- `Label` (string)
- `Value` (string)
- `Hint` (string, opsional)
- `Badge` (string, opsional)
- `Trend` (string, opsional)

Contoh:
- Label: “Total Pemasukan”
- Value: “15”
- Hint: “koin”

### 6.1.2 `BreadcrumbVm`
- `Items`: list `{ Text, Url }`

### 6.1.3 `PagingVm`
- `Page`
- `PageSize`
- `Total`
- `HasNext`
- `HasPrev`

---

## 6.2 Halaman `/sessions` (Daftar sesi)
### 6.2.1 `SessionListItemVm`
- `SessionId` (Guid)
- `SessionName` (string)
- `Mode` (string)
- `Status` (string)
- `CreatedAt` (DateTimeOffset)
- `StartedAt` (DateTimeOffset?)
- `EndedAt` (DateTimeOffset?)

### 6.2.2 `SessionsIndexVm`
- `Breadcrumb` (BreadcrumbVm)
- `Rows` (List<SessionListItemVm>)
- `Alert` (string, opsional)

---

## 6.3 Halaman `/sessions/{sessionId}` (Detail sesi)
### 6.3.1 `SessionSummaryVm`
- `SessionId`
- `SessionName`
- `Mode`
- `Status`
- `ActiveRulesetName` (string)
- `ActiveRulesetVersion` (int?)
- `EventCount` (long)
- `TotalIn` (int)
- `TotalOut` (int)
- `Net` (int)
- `ViolationsCount` (int)

UI juga membuat versi kartu:
- `MetricCards` (List<MetricCardVm>)

### 6.3.2 `SessionPlayerRowVm`
- `PlayerId`
- `DisplayName`
- `TotalIn` (int)
- `TotalOut` (int)
- `DonationTotal` (int)
- `OrdersCompleted` (int)
- `PrimaryComplianceRate` (double?)
- `ViolationsCount` (int?)

### 6.3.3 `SessionDetailsVm`
- `Breadcrumb` (BreadcrumbVm)
- `Summary` (SessionSummaryVm)
- `Players` (List<SessionPlayerRowVm>)
- `Alert` (string, opsional)

---

## 6.4 Halaman `/sessions/{sessionId}/players/{playerId}` (Detail pemain)
### 6.4.1 `PlayerMetricVm`
- `MetricName` (string)
- `Value` (string)
- `Hint` (string, opsional)

Contoh:
- MetricName: “Kepatuhan Kebutuhan Primer”
- Value: “75%”

### 6.4.2 `TransactionRowVm`
- `Timestamp` (DateTimeOffset)
- `Direction` (string: IN/OUT)
- `Amount` (int)
- `Category` (string)
- `Note` (string?)

### 6.4.3 `PlayerDetailsVm`
- `Breadcrumb` (BreadcrumbVm)
- `SessionId`
- `PlayerId`
- `DisplayName`
- `Metrics` (List<PlayerMetricVm>)
- `Transactions` (List<TransactionRowVm>)
- `Paging` (PagingVm, opsional)
- `Alert` (string, opsional)

---

## 6.5 Halaman Aktivasi ruleset `/sessions/{sessionId}/ruleset`
### 6.5.1 `SessionRulesetVm`
- `Breadcrumb` (BreadcrumbVm)
- `SessionId`
- `SessionName`
- `Status`
- `ActiveRulesetVersionId` (Guid?)
- `ActiveRulesetLabel` (string)
- `Rulesets` (List<RulesetListItemVm>)
- `SelectedRulesetId` (Guid?)
- `SelectedRulesetVersionId` (Guid?)
- `Alert` (string, opsional)

---

## 6.6 Halaman `/rulesets`
### 6.6.1 `RulesetListItemVm`
- `RulesetId`
- `Name`
- `LatestVersion` (int?)
- `ActiveVersion` (int?) (jika ada konsep aktif global)
- `IsArchived` (bool)

### 6.6.2 `RulesetVersionVm`
- `RulesetVersionId`
- `Version`
- `Status`
- `CreatedAt`

### 6.6.3 `RulesetDetailsVm`
- `Breadcrumb` (BreadcrumbVm)
- `RulesetId`
- `Name`
- `Description`
- `Versions` (List<RulesetVersionVm>)
- `SelectedVersion` (RulesetVersionVm?)
- `ConfigJsonPretty` (string)
- `Alert` (string, opsional)

---

## 7. Pemetaan DTO API → ViewModel
Sistem memetakan data dari API ke ViewModel pada layer `Clients/` atau helper mapper.

### 7.1 Prinsip mapping
Sistem melakukan:
- parsing angka dan tanggal,
- fallback nilai null ke “-” pada UI,
- format angka sesuai kebutuhan.

### 7.2 Mapping ringkas
Contoh mapping untuk ringkasan sesi:
- API `summary.event_count` → VM `EventCount`
- API `summary.cashflow_in_total` → VM `TotalIn`
- API `summary.cashflow_out_total` → VM `TotalOut`
- API `summary.cashflow_net_total` → VM `Net`
- API `summary.ruleset.name` → VM `ActiveRulesetName`

Contoh mapping untuk transaksi:
- API `transactions[].timestamp` → VM `Timestamp`
- API `transactions[].direction` → VM `Direction`
- API `transactions[].amount` → VM `Amount`

---

## 8. Spesifikasi Razor Views (per halaman)
Bagian ini menetapkan elemen yang wajib ada pada tiap view.

### 8.1 Sessions/Index.cshtml
Wajib:
1. judul “Daftar Sesi”
2. tabel sesi dengan kolom:
   - Nama Sesi, Mode, Status, Dibuat, Mulai, Selesai, Aksi
3. tombol “Detail” dan “Ruleset”

### 8.2 Sessions/Details.cshtml
Wajib:
1. judul berisi nama sesi
2. baris informasi (mode, status, ruleset aktif)
3. grid kartu metrik (minimal 4 kartu)
4. tabel pemain
5. tautan ke detail pemain

### 8.3 Players/Details.cshtml
Wajib:
1. judul pemain
2. daftar metrik pemain
3. tabel transaksi dengan urutan waktu konsisten
4. *empty state* saat transaksi kosong

### 8.4 Sessions/Ruleset.cshtml
Wajib:
1. informasi sesi
2. ruleset aktif saat ini
3. form dropdown ruleset dan versi
4. tombol aktivasi (nonaktif jika sesi ENDED)

### 8.5 Rulesets/Index.cshtml
Wajib:
1. daftar ruleset dalam tabel
2. tautan ke detail ruleset

### 8.6 Rulesets/Details.cshtml
Wajib:
1. daftar versi ruleset
2. tampilan config JSON yang bisa dibaca (`<pre>`)

---

## 9. Spesifikasi *Partial View*
### 9.1 `_MetricCard.cshtml`
Wajib:
- label, nilai, hint
- struktur HTML mudah dipakai ulang

### 9.2 `_Alert.cshtml`
Wajib:
- menerima string `Alert`
- menampilkan blok pesan error

### 9.3 `_Table.cshtml`
Opsional:
- dipakai jika kamu ingin satu komponen tabel generik

---

## 10. Standar Styling (Tailwind)
Sistem memakai pedoman berikut:
- kontainer: `max-w-6xl mx-auto px-4`
- kartu: `rounded-xl border p-4`
- tabel: `w-full text-sm`, header `bg-gray-50`
- tombol utama: `px-3 py-2 rounded-md bg-black text-white`
- tombol sekunder: `px-3 py-2 rounded-md border`

Catatan:
- Kamu boleh mengubah kelas sesuai preferensi, tapi sistem menjaga konsistensi antar halaman.

---

## 11. Standar Error UI
Jika API mengembalikan error standar, UI menampilkan:
- `message` pada `_Alert`,
- `trace_id` pada teks kecil di bawahnya.

Jika API tidak bisa diakses, UI menampilkan:
- “API tidak dapat diakses. Pastikan API berjalan.”

---

## 12. Checklist Implementasi UI
UI siap uji jika:
1. semua view jalan tanpa error runtime,
2. semua controller UI memanggil `ApiClient`,
3. UI menampilkan *loading* atau minimal *empty state*,
4. UI menampilkan error terstruktur,
5. nilai UI sama dengan data API.
