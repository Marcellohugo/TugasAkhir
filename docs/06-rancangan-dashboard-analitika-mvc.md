# Rancangan *Dashboard* Analitika Berbasis ASP.NET Core MVC (Razor Views)  
## Sistem Informasi Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly

### Dokumen
- Nama dokumen: Rancangan *Dashboard* Analitika (MVC)
- Versi: 1.0
- Tanggal: (isi tanggal)
- Penyusun: (isi nama)

---

## 1. Tujuan Dokumen
Dokumen ini mendefinisikan rancangan antarmuka *dashboard* analitika berbasis ASP.NET Core MVC (Razor Views) yang menampilkan metrik, histori transaksi, dan ringkasan sesi. Dokumen ini juga menetapkan pemetaan data UI ke kontrak REST API agar implementasi UI konsisten dengan data pada basis data.

---

## 2. Peran Pengguna dan Hak Akses
Sistem melayani dua peran.

### 2.1 Instruktur
Instruktur melakukan:
1. membuat dan memilih sesi,
2. memilih *ruleset* aktif pada sesi,
3. memantau ringkasan sesi dan per pemain,
4. meninjau histori transaksi dan event,
5. menjalankan hitung ulang metrik bila sistem menyediakan fitur ini.

### 2.2 Pemain
Pemain melakukan:
1. melihat ringkasan performa diri,
2. melihat histori transaksi diri,
3. melihat capaian metrik yang relevan.

Sistem dapat menjalankan UI satu peran terlebih dahulu (instruktur) pada tahap awal pengembangan, lalu menambah tampilan pemain setelah *endpoint* stabil.

---

## 3. Prinsip Desain UI
### 3.1 Struktur halaman jelas
Sistem menampilkan hierarki:
Daftar sesi → Detail sesi → Detail pemain.

### 3.2 Data tampil konsisten
UI membaca nilai dari *endpoint* analitika (`/api/analytics/...`) dan menampilkan nilai yang sama dengan data pada tabel `metric_snapshots` dan `event_cashflow_projections`.

### 3.3 *Filter* eksplisit
UI menyediakan *filter* utama: sesi, pemain, rentang waktu (hari/giliran), dan versi *ruleset* aktif.

### 3.4 Respons cepat
UI menampilkan ringkasan melalui snapshot metrik terbaru. UI hanya memuat histori detail saat pengguna membuka tab detail.

---

## 4. Struktur Navigasi dan Rute MVC
Sistem membangun rute halaman berikut.

| Menu | Controller | Action | Route | Tujuan |
|---|---|---|---|---|
| Beranda | `HomeController` | `Index` | `/` | Ringkas status sistem, tautan ke sesi terakhir. |
| Sesi | `SessionsController` | `Index` | `/sessions` | Daftar sesi dan status. |
| Detail Sesi | `SessionsController` | `Details` | `/sessions/{sessionId}` | Ringkasan metrik sesi, daftar pemain, tren. |
| Detail Pemain | `PlayersController` | `Details` | `/sessions/{sessionId}/players/{playerId}` | Metrik pemain dan histori transaksi. |
| *Ruleset* | `RulesetsController` | `Index` | `/rulesets` | Daftar *ruleset* dan versi. |
| Detail *Ruleset* | `RulesetsController` | `Details` | `/rulesets/{rulesetId}` | Lihat konfigurasi JSON, versi, status. |
| Aktivasi *Ruleset* | `SessionsController` | `Ruleset` | `/sessions/{sessionId}/ruleset` | Pilih versi dan aktifkan ke sesi. |

Catatan:
- UI tetap bisa menambah menu “Audit Event” setelah tahap analitika stabil.

---

## 5. *Layout* Global dan Komponen UI
### 5.1 Struktur *layout*
Sistem menyiapkan `Views/Shared/_Layout.cshtml` dengan komponen:
1. *topbar* berisi judul aplikasi dan menu,
2. konten utama,
3. *footer* sederhana.

Sistem menautkan Tailwind CSS dari proses *build* (disarankan) atau dari CDN (sementara).

### 5.2 Komponen yang dipakai ulang
Sistem membuat *partial view* agar UI konsisten.

| Komponen | File | Fungsi |
|---|---|---|
| Kartu metrik | `Views/Shared/_MetricCard.cshtml` | Menampilkan label, nilai, dan perubahan (opsional). |
| Tabel | `Views/Shared/_Table.cshtml` | Menampilkan daftar data generik. |
| *Filter bar* | `Views/Shared/_FilterBar.cshtml` | Menyediakan pilihan sesi/pemain/waktu. |
| Pesan error | `Views/Shared/_Alert.cshtml` | Menampilkan error API dan *fallback* UI. |

---

## 6. Rancangan Halaman Utama

## 6.1 Halaman Daftar Sesi (`/sessions`)
### Tujuan
UI membantu instruktur memilih sesi aktif dan melihat status terbaru.

### Elemen UI
1. Tabel sesi: nama sesi, mode, status, waktu dibuat, waktu mulai, waktu selesai.
2. Tombol aksi:
   - “Detail” menuju `/sessions/{sessionId}`,
   - “Aktifkan *ruleset*” menuju `/sessions/{sessionId}/ruleset`.

### Sumber data
- `GET /api/sessions` (endpoint ini dapat kamu tambah bila belum ada).

### *Empty state*
UI menampilkan pesan “Belum ada sesi” dan tombol “Buat Sesi” bila daftar kosong.

---

## 6.2 Halaman Detail Sesi (`/sessions/{sessionId}`)
### Tujuan
UI menampilkan ringkasan metrik sesi dan daftar pemain agar instruktur dapat menilai performa.

### Elemen UI minimum
1. Ringkasan sesi (kartu metrik):
   - jumlah event,
   - total pemasukan,
   - total pengeluaran,
   - *net cashflow*,
   - jumlah pelanggaran aturan (bila tersedia).
2. Tren (opsional tahap awal):
   - grafik total pemasukan vs pengeluaran per hari.
3. Daftar pemain:
   - nama pemain,
   - total pemasukan,
   - total pengeluaran,
   - donasi,
   - jumlah order berhasil,
   - pelanggaran aturan.
4. Tautan:
   - “Detail pemain” menuju `/sessions/{sessionId}/players/{playerId}`.

### Sumber data
- `GET /api/analytics/sessions/{sessionId}` untuk ringkasan dan agregat per pemain.
- `GET /api/sessions/{sessionId}/events?fromSeq=0&limit=...` hanya untuk audit bila instruktur butuh.

### Validasi tampilan
UI membandingkan nilai ringkasan yang UI tampilkan dengan isi respons API. UI tidak menghitung ulang metrik di sisi klien.

---

## 6.3 Halaman Detail Pemain (`/sessions/{sessionId}/players/{playerId}`)
### Tujuan
UI menampilkan detail perilaku pemain berbasis event dan proyeksi transaksi.

### Elemen UI minimum
1. Kartu metrik pemain:
   - total pemasukan,
   - total pengeluaran,
   - donasi,
   - jumlah order berhasil,
   - jumlah token aksi yang dipakai,
   - kepatuhan kebutuhan primer (rasio),
   - jumlah emas saat ini (bila aturan aktif).
2. Histori transaksi:
   - waktu,
   - arah (IN/OUT),
   - nominal,
   - kategori,
   - catatan.
3. Tren (opsional):
   - grafik saldo perkiraan per waktu (butuh proyeksi saldo atau perhitungan akumulatif pada API).

### Sumber data
- `GET /api/analytics/sessions/{sessionId}` untuk ringkasan per pemain (opsi cepat).
- `GET /api/analytics/sessions/{sessionId}/transactions?playerId={playerId}` untuk histori transaksi.

### *Empty state*
Jika pemain belum punya transaksi, UI menampilkan tabel kosong dengan pesan “Belum ada transaksi”.

---

## 6.4 Halaman Aktivasi *Ruleset* pada Sesi (`/sessions/{sessionId}/ruleset`)
### Tujuan
UI membantu instruktur memilih versi *ruleset* dan mengaktifkannya.

### Elemen UI
1. Informasi sesi: nama sesi, mode, status.
2. Informasi *ruleset* aktif: nama, versi, waktu aktivasi terakhir.
3. Form aktivasi:
   - *dropdown* *ruleset*,
   - *dropdown* versi,
   - tombol “Aktifkan”.

### Sumber data
- `GET /api/rulesets` untuk daftar *ruleset* dan versi terbaru.
- `POST /api/sessions/{sessionId}/ruleset/activate` untuk aktivasi versi.

### Validasi UI
UI menonaktifkan tombol aktivasi jika sesi berstatus `ENDED`.

---

## 7. Pemetaan Endpoint API ke UI
Sistem memakai pemetaan berikut agar integrasi jelas.

| Kebutuhan UI | Endpoint | Dipakai di | Output minimum |
|---|---|---|---|
| Daftar sesi | `GET /api/sessions` | `/sessions` | daftar sesi dan status |
| Buat sesi | `POST /api/sessions` | modal/buat sesi | `session_id` |
| Mulai sesi | `POST /api/sessions/{sessionId}/start` | detail sesi | status |
| Akhiri sesi | `POST /api/sessions/{sessionId}/end` | detail sesi | status |
| Aktivasi ruleset | `POST /api/sessions/{sessionId}/ruleset/activate` | halaman ruleset sesi | `ruleset_version_id` |
| Daftar ruleset | `GET /api/rulesets` | `/rulesets`, aktivasi ruleset | daftar ruleset |
| Ringkasan analitika sesi | `GET /api/analytics/sessions/{sessionId}` | detail sesi | summary + by_player |
| Histori transaksi | `GET /api/analytics/sessions/{sessionId}/transactions?playerId=...` | detail pemain | daftar transaksi |
| Audit event | `GET /api/sessions/{sessionId}/events?fromSeq=...` | opsional | daftar event |

Catatan:
- Jika API belum menyediakan `GET /api/sessions`, kamu bisa membuat endpoint ini lebih dulu karena UI membutuhkannya.

---

## 8. Kontrak Data untuk MVC (*ViewModel*)
Sistem memakai *view model* agar Razor Views tidak bergantung langsung pada model domain.

### 8.1 `SessionListItemVm`
Field:
- `SessionId`, `SessionName`, `Mode`, `Status`, `CreatedAt`, `StartedAt`, `EndedAt`

### 8.2 `SessionDetailsVm`
Field:
- `SessionId`, `SessionName`, `Mode`, `Status`
- `Summary` (kumpulan metrik ringkas)
- `Players` (list ringkas per pemain)
- `ActiveRuleset` (nama dan versi)

### 8.3 `PlayerDetailsVm`
Field:
- `SessionId`, `PlayerId`, `DisplayName`
- `Metrics` (kumpulan metrik pemain)
- `Transactions` (list histori)

### 8.4 *Ruleset* VM ringkas
- `RulesetListVm` menampilkan `ruleset_id`, `name`, `latest_version`
- `RulesetVersionVm` menampilkan `ruleset_version_id`, `version`, `status`, `created_at`

---

## 9. Alur Interaksi Halaman
### 9.1 Detail sesi
1. UI memanggil `GET /api/analytics/sessions/{sessionId}` saat halaman dimuat.
2. UI mengisi kartu metrik dan tabel pemain.
3. UI memuat tren hanya saat pengguna membuka tab tren.

### 9.2 Detail pemain
1. UI memanggil `GET /api/analytics/sessions/{sessionId}` atau endpoint detail pemain (jika kamu buat).
2. UI memanggil `GET /api/analytics/sessions/{sessionId}/transactions?playerId=...` untuk tabel transaksi.
3. UI menampilkan *loading state* saat memuat tabel transaksi.

### 9.3 Aktivasi ruleset
1. UI memanggil `GET /api/rulesets`.
2. UI memanggil aktivasi saat instruktur memilih versi dan menekan tombol.
3. UI menampilkan hasil aktivasi dan kembali ke detail sesi.

---

## 10. Penanganan Error dan Status UI
Sistem menampilkan status berikut:
1. *Loading*: UI menampilkan *skeleton* sederhana pada kartu metrik.
2. Error 404: UI menampilkan pesan “Sesi tidak ditemukan”.
3. Error 422: UI menampilkan alasan aturan domain dilanggar dari `error_code` dan `details`.
4. Error 500: UI menampilkan pesan generik dan menyarankan coba ulang.

UI menampilkan `trace_id` pada pesan error agar instruktur dapat menelusuri log server.

---

## 11. Kinerja dan Optimasi
Sistem menjaga respons UI dengan strategi berikut:
1. UI memanggil endpoint ringkasan yang memakai snapshot.
2. UI membatasi ukuran tabel transaksi melalui parameter `limit` dan *pagination* jika histori panjang.
3. UI memuat grafik secara *lazy*.
4. API menambahkan indeks pada tabel `metric_snapshots` dan `event_cashflow_projections`.

---

## 12. Rekomendasi Implementasi Front-end
Sistem dapat memakai:
- Tailwind CSS untuk *layout* dan komponen,
- Chart.js untuk grafik (opsional),
- *fetch* atau `HttpClient` pada MVC untuk panggilan API internal.

Sistem menyimpan konfigurasi base URL API pada `appsettings.json`.

---

## 13. Checklist Kesiapan Implementasi UI
UI siap kamu implementasikan jika:
1. endpoint daftar sesi tersedia,
2. endpoint analitika sesi menghasilkan ringkasan dan daftar pemain,
3. endpoint histori transaksi berjalan,
4. UI menampilkan data tanpa menghitung ulang metrik di sisi klien,
5. UI menampilkan error sesuai format standar API.
