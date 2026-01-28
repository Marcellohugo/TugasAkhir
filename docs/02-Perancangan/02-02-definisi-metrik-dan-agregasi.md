# Definisi Metrik dan Aturan Agregasi  
## Sistem Informasi Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly

### Dokumen
- Nama dokumen: Definisi Metrik dan Aturan Agregasi
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan Dokumen
Dokumen ini mendefinisikan metrik yang sistem hitung dari log event, beserta aturan agregasi, frekuensi perhitungan, dan format penyimpanan pada tabel `metric_snapshots`. Dokumen ini menjadi acuan implementasi modul analitika dan validasi tampilan dasbor.

---

## 2. Prinsip Perhitungan Metrik
### 2.1 Event sebagai input tunggal
Sistem menghitung metrik dari event yang tersimpan pada tabel `events` dan proyeksi arus kas pada tabel `event_cashflow_projections`. Sistem tidak menerima nilai metrik dari klien.

### 2.2 Metrik harus dapat ditelusuri
Setiap metrik harus memiliki:
1. sumber event yang jelas,
2. rumus perhitungan,
3. aturan agregasi waktu,
4. cara validasi dengan data basis data.

### 2.3 Metrik mengikuti konteks *ruleset*
Sistem menyertakan `ruleset_version_id` pada setiap snapshot metrik agar instruktur dapat membandingkan hasil antar konfigurasi.

### 2.4 Satuan dan tipe data
- Semua nilai koin memakai `integer`.
- Semua rasio memakai `double`.
- Semua metrik kompleks memakai `jsonb` pada kolom `metric_value_json`.

---

## 3. Waktu dan Level Agregasi
### 3.1 Level agregasi
Sistem menghasilkan metrik pada dua level:
1. level sesi (agregat semua pemain) dengan `player_id = null`,
2. level pemain dengan `player_id = <uuid pemain>`.

### 3.2 Jendela waktu
Sistem menghitung metrik pada tiga jendela:
1. sepanjang sesi (*lifetime session*),
2. per hari (`day_index`),
3. per giliran (`turn_number`).

Sistem menyimpan hasil hari dan giliran pada `metric_value_json` agar dasbor menampilkan tren.

### 3.3 Frekuensi perhitungan
Sistem memakai strategi berikut:
1. Sistem hitung metrik ringan setiap kali sistem menerima event arus kas atau event kepemilikan.
2. Sistem hitung metrik kepatuhan aturan saat sistem menerima event yang terkait aturan.
3. Sistem hitung ringkasan sesi saat instruktur mengakhiri sesi atau saat instruktur menekan tombol “Recompute”.

---

## 4. Konvensi Nama Metrik
Sistem menamai metrik dengan format:
`<domain>.<metric>.<scope>`

Contoh:
- `cashflow.in.total`
- `cashflow.out.total`
- `compliance.primary_need.rate`
- `inventory.ingredient.total`
- `rules.violations.count`

Sistem memakai nama yang sama untuk level sesi dan level pemain. Sistem membedakan level melalui `player_id`.

---

## 5. Daftar Metrik Minimum (untuk Dasbor)
Tabel berikut mendefinisikan metrik minimum yang dasbor tampilkan.

| Kode Metrik | Level | Tipe Nilai | Sumber Data | Ringkas Fungsi |
|---|---|---|---|---|
| cashflow.in.total | sesi & pemain | numeric | `event_cashflow_projections` | Total pemasukan koin. |
| cashflow.out.total | sesi & pemain | numeric | `event_cashflow_projections` | Total pengeluaran koin. |
| cashflow.net.total | sesi & pemain | numeric | hasil rumus | Selisih pemasukan dan pengeluaran. |
| donation.total | sesi & pemain | numeric | `events` (`day.friday.donation`) | Total donasi. |
| gold.qty.current | pemain | numeric | `events` (`day.saturday.gold_trade`) | Jumlah emas saat ini. |
| orders.completed.count | pemain | numeric | `events` (`order.claimed`) | Jumlah order yang berhasil diklaim. |
| inventory.ingredient.total | pemain | numeric | `events` (`ingredient.purchased`, `order.claimed`) | Total kartu bahan saat ini. |
| compliance.primary_need.rate | pemain | numeric | `events` + aturan ruleset | Rasio hari yang memenuhi kewajiban kebutuhan primer. |
| actions.used.total | pemain | numeric | `events` (`turn.action.used`) | Total token aksi yang pemain pakai. |
| rules.violations.count | sesi & pemain | numeric | `validation_logs` atau hasil validasi | Jumlah pelanggaran aturan domain. |

Catatan:
- Sistem tetap dapat menghitung `cashflow.*` dari event langsung, namun sistem sebaiknya memakai proyeksi agar query cepat.

---

## 6. Definisi Detail dan Rumus Metrik

## 6.1 `cashflow.in.total`
**Definisi:** total pemasukan koin untuk sesi atau pemain.

**Sumber event:** baris `event_cashflow_projections` dengan `direction = IN`.

**Rumus:**
- level sesi: `sum(amount) untuk session_id`
- level pemain: `sum(amount) untuk session_id dan player_id`

**Validasi:** sistem cocokkan total `amount` pada proyeksi.

---

## 6.2 `cashflow.out.total`
**Definisi:** total pengeluaran koin untuk sesi atau pemain.

**Sumber event:** `event_cashflow_projections` dengan `direction = OUT`.

**Rumus:** sama seperti pemasukan, namun memakai `OUT`.

---

## 6.3 `cashflow.net.total`
**Definisi:** selisih pemasukan dan pengeluaran koin.

**Rumus:**  
`cashflow.in.total - cashflow.out.total`

---

## 6.4 `donation.total`
**Definisi:** total koin yang pemain donasikan pada hari Jumat.

**Sumber event:** `events.action_type = day.friday.donation`

**Rumus:**
- level pemain: `sum(payload.amount) per player_id`
- level sesi: `sum(payload.amount) semua pemain`

---

## 6.5 `gold.qty.current`
**Definisi:** jumlah emas yang pemain pegang saat ini.

**Sumber event:** `events.action_type = day.saturday.gold_trade`

**Aturan agregasi:**
- BUY menambah `qty`
- SELL mengurangi `qty`

**Rumus (pseudocode):**
- `qty_current = sum(BUY.qty) - sum(SELL.qty)`

**Validasi:**
- Nilai tidak boleh negatif.
- Sistem menolak SELL bila stok kurang saat validasi event.

---

## 6.6 `orders.completed.count`
**Definisi:** jumlah order yang pemain klaim.

**Sumber event:** `events.action_type = order.claimed`

**Rumus:** `count(*) per player_id`

---

## 6.7 `inventory.ingredient.total`
**Definisi:** total kartu bahan yang pemain miliki saat ini.

**Sumber event:**
- menambah: `ingredient.purchased`
- mengurangi: `order.claimed` (karena klaim order mengonsumsi bahan)

**Aturan agregasi minimum:**
- Sistem menambah 1 untuk setiap `ingredient.purchased`.
- Sistem mengurangi `len(required_ingredient_card_ids)` untuk setiap `order.claimed`.

**Rumus (pseudocode):**
- `ingredient_total = count(ingredient.purchased) - sum(len(order.claimed.required_ingredient_card_ids))`

**Validasi:**
- Nilai tidak boleh negatif.
- Sistem menolak `order.claimed` bila pemain tidak memiliki bahan yang diminta.

---

## 6.8 `compliance.primary_need.rate`
**Definisi:** rasio kepatuhan pemain terhadap pembelian kebutuhan primer per hari sesuai ruleset.

**Input aturan:**
- `constraints.primary_need_max_per_day`
- `constraints.require_primary_before_others`

**Sumber event:**
- `need.primary.purchased`
- `need.secondary.purchased`
- `need.tertiary.purchased`

**Definisi hari patuh:**
Sistem menilai setiap `day_index` pada sesi:
1. Sistem anggap hari patuh jika pemain membeli kebutuhan primer minimal 1 kali saat ruleset mengharuskan.
2. Sistem anggap hari patuh jika pemain tidak membeli kebutuhan lain sebelum kebutuhan primer saat ruleset mengharuskan.
3. Sistem anggap hari patuh jika pemain tidak melebihi batas `primary_need_max_per_day`.

**Rumus:**
- `rate = jumlah_hari_patuh / jumlah_hari_yang_dinilai`

**Format penyimpanan:**
- `metric_value_numeric` menyimpan `rate`.
- `metric_value_json` menyimpan rincian per hari:
```json
{
  "days": [
    { "day_index": 0, "compliant": true, "reason": [] },
    { "day_index": 1, "compliant": false, "reason": ["BOUGHT_SECONDARY_BEFORE_PRIMARY"] }
  ],
  "evaluated_days": 2,
  "compliant_days": 1
}
```

---

## 6.9 `actions.used.total`
**Definisi:** total token aksi yang pemain pakai selama sesi.

**Sumber event:** `turn.action.used`

**Rumus:**
- `sum(payload.used) per player_id`

---

## 6.10 `rules.violations.count`
**Definisi:** jumlah pelanggaran aturan domain yang terjadi dalam sesi atau yang terkait pemain.

**Sumber data (pilih satu dan konsisten):**
1. `validation_logs.is_valid=false`, atau
2. hasil validasi domain saat sistem menolak event (log aplikasi).

**Aturan:**
- Jika sistem menolak event, sistem tetap mencatat kegagalan pada `validation_logs`.
- Sistem tidak menyimpan event gagal pada tabel `events`.

**Rumus:**
- level sesi: `count(validation_logs) untuk session_id dan is_valid=false`
- level pemain: sistem pakai `details_json.player_id` atau metadata request bila tersedia

---

## 7. Pemetaan Event ke Metrik (Ringkas)
Sistem memakai pemetaan berikut sebagai aturan implementasi.

| Event | Metrik yang terpengaruh |
|---|---|
| transaction.recorded | cashflow.in.total, cashflow.out.total, cashflow.net.total |
| day.friday.donation | donation.total, cashflow.out.total, cashflow.net.total |
| day.saturday.gold_trade | gold.qty.current, cashflow.* (bila ada biaya/hasil) |
| ingredient.purchased | inventory.ingredient.total, cashflow.out.total |
| order.claimed | orders.completed.count, inventory.ingredient.total, cashflow.in.total |
| need.primary.purchased | compliance.primary_need.rate, cashflow.out.total |
| need.secondary/tertiary.purchased | compliance.primary_need.rate, cashflow.out.total |
| turn.action.used | actions.used.total |
| event ditolak validasi | rules.violations.count |

---

## 8. Aturan Penyimpanan Snapshot
### 8.1 Struktur record
Sistem menyimpan hasil pada tabel `metric_snapshots` dengan:
- `session_id` dan opsional `player_id`,
- `metric_name`,
- `metric_value_numeric` atau `metric_value_json`,
- `ruleset_version_id`.

Catatan implementasi:
- Snapshot agregat level sesi menyimpan `player_id = null`.

### 8.2 Strategi “latest only”
Untuk dasbor real-time, sistem boleh menyimpan snapshot “terbaru saja” per metrik:
- sistem ambil snapshot terbaru per `metric_name` berdasarkan `computed_at`.

Jika sistem membutuhkan histori, sistem simpan snapshot berkala berdasarkan `turn_number` atau `day_index` pada JSON.

---

## 9. Contoh Perhitungan (Skenario Singkat)
### 9.1 Data contoh
Sesi S1, pemain P1:
1. `transaction.recorded` OUT 5 (beli kebutuhan primer)
2. `ingredient.purchased` OUT 1
3. `order.claimed` IN 15, konsumsi 2 bahan
4. `day.friday.donation` OUT 2

### 9.2 Hasil metrik
- `cashflow.in.total = 15`
- `cashflow.out.total = 5 + 1 + 2 = 8`
- `cashflow.net.total = 15 - 8 = 7`
- `donation.total = 2`
- `orders.completed.count = 1`
- `inventory.ingredient.total = 1 - 2 = -1` → sistem tidak boleh menghasilkan nilai ini  
  Sistem harus menolak `order.claimed` bila pemain belum punya 2 bahan.

Catatan: contoh ini menegaskan pentingnya validasi domain sebelum agregasi.

---

## 10. Kriteria Uji Metrik
Sistem lulus uji metrik jika:
1. Sistem menghasilkan nilai yang sama dengan hasil query manual pada tabel proyeksi dan event.
2. Sistem tidak menghasilkan nilai negatif untuk metrik kepemilikan (emas, bahan).
3. Sistem memperbarui snapshot setelah sistem menerima event yang relevan.
4. Sistem menampilkan nilai pada dasbor yang cocok dengan isi `metric_snapshots`.
