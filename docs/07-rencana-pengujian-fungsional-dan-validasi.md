# Rencana Pengujian Fungsional dan Validasi Sistem  
## Sistem Informasi Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly

### Dokumen
- Nama dokumen: Rencana Pengujian Fungsional dan Validasi
- Versi: 1.0
- Tanggal: (isi tanggal)
- Penyusun: (isi nama)

---

## 1. Tujuan
Dokumen ini menetapkan skenario uji fungsional (*black-box*), uji integrasi alur event, serta validasi konsistensi tampilan dasbor terhadap data basis data. Dokumen ini juga menetapkan format pencatatan hasil uji dan kriteria kelulusan.

---

## 2. Ruang Lingkup Pengujian
Sistem menguji tiga area.

1. **Uji RESTful API (black-box)**  
   Sistem menguji endpoint sesuai kontrak, validasi input, kode status respons, dan format error.

2. **Uji integrasi alur event dan konsistensi *state***  
   Sistem menguji urutan event, idempotensi, keterikatan event ke sesi dan *ruleset*, serta konsistensi proyeksi dan metrik.

3. **Validasi dasbor analitika (UI MVC)**  
   Sistem menguji kesesuaian nilai yang UI tampilkan dengan data pada tabel `metric_snapshots` dan `event_cashflow_projections`.

---

## 3. Lingkungan dan Prasyarat
### 3.1 Lingkungan
- OS: Windows 11 Home
- Runtime: .NET 10
- DBMS: PostgreSQL
- Alat uji: Swagger UI dan Postman
- Peramban: Google Chrome

### 3.2 Konfigurasi dasar
Sistem menyiapkan:
1. database kosong,
2. migrasi EF Core berjalan,
3. seed data minimal:
   - 1 *ruleset* default + 1 `ruleset_version` default,
   - 1 sesi contoh (opsional).

### 3.3 Data uji minimum
Sistem membutuhkan:
- `session_id`
- `player_id` (1–3 pemain)
- `ruleset_version_id` yang aktif pada sesi
- daftar event contoh dengan `sequence_number` berurutan

---

## 4. Standar Format Error API
Sistem menganggap respons error valid jika sistem mengembalikan struktur berikut:
```json
{
  "error_code": "STRING",
  "message": "STRING",
  "details": {},
  "trace_id": "STRING"
}
```

Sistem memeriksa:
1. `error_code` jelas dan konsisten,
2. `message` membantu diagnosa,
3. `trace_id` ada untuk pelacakan log.

---

## 5. Kriteria Kelulusan Umum
Sistem dinyatakan valid jika:
1. semua skenario uji wajib berstatus **PASS**,
2. sistem tidak menulis data korup pada basis data,
3. sistem menolak input tidak valid dengan kode status yang tepat,
4. UI menampilkan nilai metrik yang sama dengan query basis data yang relevan.

---

## 6. Daftar Modul yang Diuji
Sistem mengelompokkan hasil uji per modul agar laporan mudah dibaca.

| Kode Modul | Modul |
|---|---|
| M1 | Manajemen Sesi |
| M2 | Manajemen *Ruleset* |
| M3 | Ingest Event (REST API) |
| M4 | Proyeksi Arus Kas |
| M5 | Agregasi Metrik |
| M6 | Analitika (Endpoint ringkasan/transaksi) |
| M7 | UI MVC Dasbor |
| M8 | Logging & Error Handling |

Catatan: kalimat “Temuan uji dominan di modul mana?” berarti **modul mana yang paling sering menghasilkan temuan/bug** pada periode uji tertentu. Laporan hasil uji perlu mencatat frekuensi temuan per modul.

---

## 7. Uji RESTful API (Black-box)
Bagian ini merinci skenario uji wajib per endpoint.

### 7.1 Konvensi penulisan skenario
Setiap skenario memuat:
- ID uji
- Modul
- Endpoint
- Input
- Langkah uji
- Ekspektasi respons
- Ekspektasi perubahan data (bila ada)

### 7.2 Daftar skenario uji inti
#### A. Manajemen Sesi (M1)
**TC-API-01 — Buat sesi**
- Endpoint: `POST /api/sessions`
- Input contoh:
```json
{ "session_name": "Sesi Uji 01", "mode": "PEMULA" }
```
- Langkah:
1. Kirim permintaan.
2. Catat `session_id`.
- Ekspektasi:
  - Status: `201`
  - Body memuat `session_id`
  - DB: tabel `sessions` menambah 1 baris.

**TC-API-02 — Mulai sesi**
- Endpoint: `POST /api/sessions/{sessionId}/start`
- Langkah:
1. Kirim permintaan pada sesi berstatus `CREATED`.
- Ekspektasi:
  - Status: `200`
  - DB: `status=STARTED`, `started_at` terisi.

**TC-API-03 — Akhiri sesi**
- Endpoint: `POST /api/sessions/{sessionId}/end`
- Langkah:
1. Kirim permintaan pada sesi berstatus `STARTED`.
- Ekspektasi:
  - Status: `200`
  - DB: `status=ENDED`, `ended_at` terisi.

**TC-API-04 — Tolak start sesi yang sudah END**
- Endpoint: `POST /api/sessions/{sessionId}/start`
- Kondisi: sesi `ENDED`
- Ekspektasi:
  - Status: `409` atau `422` (pilih satu dan konsisten)
  - Body error sesuai standar.

---

#### B. Manajemen *Ruleset* (M2)
**TC-API-05 — Buat ruleset + versi 1**
- Endpoint: `POST /api/rulesets`
- Input contoh:
```json
{
  "name": "Ruleset Default",
  "description": "Default pemula",
  "config_json": { "mode": "PEMULA", "actions_per_turn": 2, "starting_cash": 20 }
}
```
- Ekspektasi:
  - Status: `201`
  - Body memuat `ruleset_id` dan `ruleset_version_id`
  - DB: `rulesets` dan `ruleset_versions` bertambah.

**TC-API-06 — Update ruleset membuat versi baru**
- Endpoint: `PUT /api/rulesets/{rulesetId}`
- Input: perubahan kecil pada `config_json`
- Ekspektasi:
  - Status: `200`
  - DB: `ruleset_versions` menambah versi `version+1`.

**TC-API-07 — Tolak ruleset invalid (rentang salah)**
- Endpoint: `POST /api/rulesets`
- Input: `donation.min_amount > donation.max_amount`
- Ekspektasi:
  - Status: `422`
  - `error_code` menyebut validasi ruleset
  - DB: tidak menambah versi.

**TC-API-08 — Aktivasi ruleset pada sesi**
- Endpoint: `POST /api/sessions/{sessionId}/ruleset/activate`
- Input contoh:
```json
{ "ruleset_version_id": "UUID" }
```
- Ekspektasi:
  - Status: `200`
  - DB: `session_ruleset_activations` menambah 1 baris.

**TC-API-09 — Tolak aktivasi ruleset pada sesi END**
- Endpoint: `POST /api/sessions/{sessionId}/ruleset/activate`
- Kondisi: sesi `ENDED`
- Ekspektasi:
  - Status: `409` atau `422`
  - DB: tidak menambah aktivasi.

---

#### C. Ingest Event (M3)
**TC-API-10 — Terima event valid**
- Endpoint: `POST /api/sessions/{sessionId}/events`
- Input contoh:
```json
{
  "event_id": "UUID",
  "ruleset_version_id": "UUID",
  "sequence_number": 0,
  "timestamp": "2026-01-27T10:00:00Z",
  "day_index": 0,
  "weekday": "MON",
  "turn_number": 0,
  "action_type": "turn.action.used",
  "player_id": "UUID",
  "payload": { "used": 1 }
}
```
- Ekspektasi:
  - Status: `202` atau `201` (pilih satu dan konsisten)
  - DB: `events` bertambah 1 baris.

**TC-API-11 — Idempotensi event (event_id sama)**
- Kirim input TC-API-10 dua kali.
- Ekspektasi:
  - Respons kedua: `200` atau `409` (pilih satu dan konsisten)
  - DB: tidak menambah baris kedua pada `events`.

**TC-API-12 — Tolak ruleset_version_id tidak cocok dengan sesi**
- Kondisi: sesi aktif memakai ruleset A, event mengirim ruleset B.
- Ekspektasi:
  - Status: `422`
  - DB: tidak menambah event
  - DB: `validation_logs` (jika dipakai) mencatat kegagalan.

**TC-API-13 — Tolak sequence_number loncat**
- Kirim event `sequence_number=2` saat sistem baru menerima `0`.
- Ekspektasi:
  - Status: `422`
  - `error_code` menyebut urutan event.

**TC-API-14 — Tolak payload tidak sesuai tipe**
- Kirim `payload.used = "satu"` pada event `turn.action.used`.
- Ekspektasi:
  - Status: `400` atau `422`
  - Body error sesuai standar.

---

#### D. Analitika (M6)
**TC-API-15 — Ringkasan analitika sesi**
- Endpoint: `GET /api/analytics/sessions/{sessionId}`
- Prasyarat: sistem sudah menerima minimal 5 event valid.
- Ekspektasi:
  - Status: `200`
  - Body memuat:
    - `summary` level sesi
    - `by_player` list per pemain
    - `ruleset_version_id` konteks

**TC-API-16 — Histori transaksi pemain**
- Endpoint: `GET /api/analytics/sessions/{sessionId}/transactions?playerId={playerId}`
- Ekspektasi:
  - Status: `200`
  - Body list transaksi terurut waktu desc/asc (pilih satu dan konsisten).

**TC-API-17 — Tolak query tanpa playerId saat endpoint mewajibkan**
- Endpoint: transaksi tanpa `playerId`
- Ekspektasi:
  - Status: `400`
  - `error_code` menyebut parameter wajib.

---

## 8. Uji Integrasi Alur Event dan Konsistensi *State*
Sistem menjalankan uji integrasi memakai rangkaian event yang menyerupai permainan nyata.

### 8.1 Skenario integrasi minimum (IT-01)
**Tujuan:** sistem membangun proyeksi arus kas dan metrik tanpa inkonsistensi.

**Langkah:**
1. Buat sesi (TC-API-01).
2. Aktivasi ruleset (TC-API-08).
3. Daftarkan 2 pemain (jika endpoint tersedia).
4. Kirim rangkaian event berurutan:
   - `turn.action.used`
   - `need.primary.purchased`
   - `ingredient.purchased`
   - `order.claimed`
   - `day.friday.donation`
5. Panggil `GET /api/analytics/sessions/{sessionId}`.

**Ekspektasi:**
- `events` berisi event sesuai urutan `sequence_number`.
- `event_cashflow_projections` berisi transaksi hasil proyeksi.
- `metric_snapshots` berisi metrik minimum.
- Sistem tidak menghasilkan nilai negatif untuk kepemilikan (bahan/emas).

### 8.2 Skenario integrasi pelanggaran aturan (IT-02)
**Tujuan:** sistem menolak event yang melanggar aturan ruleset.

**Langkah:**
1. Aktivasi ruleset dengan `constraints.require_primary_before_others=true`.
2. Kirim event `need.secondary.purchased` sebelum `need.primary.purchased` pada hari yang sama.
3. Ambil respons error.

**Ekspektasi:**
- Sistem mengembalikan `422`.
- Sistem tidak menyimpan event pada `events`.
- Sistem mencatat kegagalan pada `validation_logs` jika modul ini aktif.

### 8.3 Skenario integrasi perubahan ruleset (IT-03)
**Tujuan:** sistem menempelkan event ke versi ruleset yang benar.

**Langkah:**
1. Aktifkan ruleset versi 1 pada sesi.
2. Kirim 3 event valid.
3. Aktifkan ruleset versi 2 pada sesi.
4. Kirim 3 event valid berikutnya.
5. Ambil ringkasan analitika dan audit event.

**Ekspektasi:**
- Event awal mereferensikan `ruleset_version_id` versi 1.
- Event berikutnya mereferensikan versi 2.
- Snapshot metrik menyertakan `ruleset_version_id` konteks saat hitung.

---

## 9. Validasi Dasbor Analitika (UI MVC)
Sistem memvalidasi UI dengan cara membandingkan nilai UI terhadap data DB.

### 9.1 Prinsip validasi
UI hanya menampilkan nilai dari API. UI tidak menghitung ulang metrik di klien.

### 9.2 Skenario validasi UI (UI-01)
**Tujuan:** halaman detail sesi menampilkan ringkasan yang sama dengan DB.

**Langkah:**
1. Buka `/sessions/{sessionId}`.
2. Catat nilai kartu metrik:
   - total pemasukan
   - total pengeluaran
   - *net cashflow*
3. Jalankan query DB:
```sql
select metric_name, metric_value_numeric
from metric_snapshots
where session_id = '<SESSION_ID>' and player_id is null
order by computed_at desc;
```
4. Bandingkan nilai UI dan nilai DB untuk metrik yang sama.

**Ekspektasi:**
- UI menampilkan nilai yang sama (toleransi 0 untuk integer).

### 9.3 Skenario validasi UI transaksi (UI-02)
**Tujuan:** tabel transaksi pemain cocok dengan tabel proyeksi.

**Langkah:**
1. Buka `/sessions/{sessionId}/players/{playerId}`.
2. Catat 5 baris transaksi teratas.
3. Jalankan query DB:
```sql
select timestamp, direction, amount, category
from event_cashflow_projections
where session_id = '<SESSION_ID>' and player_id = '<PLAYER_ID>'
order by timestamp desc
limit 5;
```
4. Bandingkan nilai.

**Ekspektasi:**
- waktu, arah, nominal, kategori cocok.

### 9.4 Validasi error UI (UI-03)
**Tujuan:** UI menampilkan error terstruktur.
- Paksa kondisi 404 (akses sesi tidak ada).
- Ekspektasi: UI menampilkan pesan “Sesi tidak ditemukan” dan menampilkan `trace_id` bila API menyertakannya.

---

## 10. Kriteria *Pass/Fail* per Skenario
Sistem menetapkan:
- **PASS** jika respons API sesuai kontrak dan perubahan data sesuai ekspektasi.
- **FAIL** jika salah satu kondisi berikut terjadi:
  1. status code salah,
  2. format body salah,
  3. sistem menyimpan event tidak valid,
  4. UI menampilkan nilai tidak sama dengan data basis data.

---

## 11. Pencatatan Hasil Uji
### 11.1 Template tabel hasil uji
Sistem mencatat hasil uji pada tabel berikut.

| Tanggal | ID Uji | Modul | Status | Bukti (URL/SS) | Catatan | *Trace ID* |
|---|---|---|---|---|---|---|

### 11.2 Template temuan (*bug report*)
Sistem mencatat temuan dengan format berikut:
- ID temuan: BUG-XXX
- Modul: M1–M8
- Ringkasan: (1 kalimat)
- Langkah reproduksi:
  1. ...
  2. ...
- Ekspektasi:
- Aktual:
- Dampak:
- Bukti:
- *Trace ID*:
- Status: OPEN / FIXED / RE-TEST

### 11.3 Rekap “temuan dominan”
Sistem membuat rekap mingguan:
- total temuan,
- jumlah temuan per modul,
- modul dengan temuan terbanyak (temuan dominan).

---

## 12. Checklist Kelulusan Akhir
Sistem lulus tahap pengujian dan validasi jika:
1. sistem menyelesaikan semua TC-API-01 s.d. TC-API-17 dengan PASS,
2. sistem menyelesaikan IT-01 s.d. IT-03 dengan PASS,
3. sistem menyelesaikan UI-01 s.d. UI-03 dengan PASS,
4. sistem tidak meninggalkan temuan berstatus OPEN pada modul inti (M1–M6).
