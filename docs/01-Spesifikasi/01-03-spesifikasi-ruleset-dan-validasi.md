# Spesifikasi *Ruleset* dan Validasi Konfigurasi  
## Sistem Informasi Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly

### Dokumen
- Nama dokumen: Spesifikasi *Ruleset* dan Validasi Konfigurasi
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan Dokumen
Dokumen ini saya susun untuk menetapkan struktur data *ruleset*, daftar parameter konfigurasi, aturan validasi, dan kebijakan versi. Dokumen ini saya susun untuk memandu implementasi modul manajemen *ruleset* di REST API dan UI MVC.

---

## 2. Definisi dan Ruang Lingkup *Ruleset*
Sistem menggunakan *ruleset* sebagai kumpulan parameter konfigurasi permainan yang dapat berubah tanpa mengubah kode program. Sistem menempatkan aturan inti permainan sebagai batas yang tidak berubah, lalu sistem mengatur parameter variabel melalui *ruleset*.

### 2.1 Aturan inti
Sistem menjaga aturan inti berikut tetap stabil:
1. Sistem mencatat aksi sebagai event terstruktur.
2. Sistem mengurutkan event per sesi dan mencegah dampak ganda dari event duplikat.
3. Sistem mengikat event pada versi *ruleset* yang aktif saat event terjadi.

### 2.2 Parameter variabel
Sistem mengelola parameter variabel seperti:
- jumlah token aksi per giliran,
- saldo awal,
- batas kepemilikan,
- batas nominal transaksi,
- batas jumlah kartu bahan,
- batas pembelian kebutuhan primer per hari,
- parameter mode mahir (pinjaman, asuransi, tabungan).

---

## 3. Model Data *Ruleset*
### 3.1 Entitas minimum
Sistem menyimpan data *ruleset* melalui dua entitas:

1) **Ruleset**
- `ruleset_id` (UUID)
- `name` (string)
- `description` (string, opsional)
- `is_archived` (bool)
- `created_at` (timestamp)
- `created_by` (string/user_id)

2) **RulesetVersion**
- `ruleset_version_id` (UUID)
- `ruleset_id` (UUID)
- `version` (int, mulai dari 1)
- `status` (enum: `DRAFT`, `ACTIVE`, `RETIRED`)
- `config_json` (JSON)
- `config_hash` (string)  
  Sistem menghitung hash untuk mendeteksi perubahan dan memudahkan audit.
- `created_at` (timestamp)
- `created_by` (string/user_id)

### 3.2 Kebijakan versi
Sistem menerapkan kebijakan berikut:
1. Sistem membuat versi baru setiap kali instruktur mengubah konfigurasi.
2. Sistem menyimpan `ruleset_version_id` pada setiap event.
3. Sistem mengizinkan satu versi aktif per sesi permainan.
4. Sistem melarang penghapusan versi yang sudah dipakai oleh sesi.

---

## 4. Struktur `config_json`
Sistem menyimpan konfigurasi pada JSON dengan struktur top-level berikut.

### 4.1 Struktur top-level
```json
{
  "mode": "PEMULA",
  "actions_per_turn": 2,
  "starting_cash": 20,
  "weekday_rules": {
    "friday": { "feature": "DONATION", "enabled": true },
    "saturday": { "feature": "GOLD_TRADE", "enabled": true },
    "sunday": { "feature": "REST", "enabled": true }
  },
  "constraints": {
    "cash_min": 0,
    "max_ingredient_total": 6,
    "max_same_ingredient": 3,
    "primary_need_max_per_day": 1,
    "require_primary_before_others": true
  },
  "donation": {
    "min_amount": 1,
    "max_amount": 999999
  },
  "gold_trade": {
    "allow_buy": true,
    "allow_sell": true
  },
  "advanced": {
    "loan": { "enabled": false },
    "insurance": { "enabled": false },
    "saving_goal": { "enabled": false }
  }
}
```

Catatan:
- Sistem mengizinkan penambahan parameter baru, namun sistem menjaga kompatibilitas versi dengan validasi skema.
- Sistem menolak field tidak dikenal jika instruktur mengaktifkan mode validasi ketat.

---

## 5. Daftar Parameter *Ruleset*
Tabel berikut merangkum parameter utama. Penambahan parameter lain diperbolehkan selama aturan validasi dan dampaknya ke event dituliskan.

### 5.1 Parameter umum
| Parameter | Tipe | Wajib | Default | Rentang | Deskripsi |
|---|---|---:|---:|---|---|
| mode | enum | Ya | PEMULA | PEMULA/MAHIR | Sistem memilih paket fitur sesuai mode. |
| actions_per_turn | int | Ya | 2 | 1–10 | Sistem membatasi jumlah aksi per giliran. |
| starting_cash | int | Ya | 0 | 0–1.000.000 | Sistem menetapkan saldo awal per pemain pada awal sesi. |
| constraints.cash_min | int | Ya | 0 | 0–1.000.000 | Sistem melarang saldo turun di bawah nilai ini. |

### 5.2 Parameter batas kepemilikan dan pembelian
| Parameter | Tipe | Wajib | Default | Rentang | Deskripsi |
|---|---|---:|---:|---|---|
| constraints.max_ingredient_total | int | Ya | 6 | 0–50 | Sistem membatasi total kartu bahan yang pemain simpan. |
| constraints.max_same_ingredient | int | Ya | 3 | 0–50 | Sistem membatasi jumlah kartu bahan dengan jenis sama. |
| constraints.primary_need_max_per_day | int | Ya | 1 | 0–10 | Sistem membatasi pembelian kebutuhan primer per hari. |
| constraints.require_primary_before_others | bool | Ya | true | - | Sistem memaksa pembelian kebutuhan primer sebelum kebutuhan lain. |

### 5.3 Parameter hari khusus
| Parameter | Tipe | Wajib | Default | Rentang | Deskripsi |
|---|---|---:|---:|---|---|
| weekday_rules.friday.enabled | bool | Ya | true | - | Sistem mengaktifkan fitur Jumat. |
| weekday_rules.friday.feature | enum | Ya | DONATION | DONATION | Sistem menetapkan fitur Jumat. |
| weekday_rules.saturday.enabled | bool | Ya | true | - | Sistem mengaktifkan fitur Sabtu. |
| weekday_rules.saturday.feature | enum | Ya | GOLD_TRADE | GOLD_TRADE | Sistem menetapkan fitur Sabtu. |
| weekday_rules.sunday.enabled | bool | Ya | true | - | Sistem menandai Minggu sebagai libur. |
| weekday_rules.sunday.feature | enum | Ya | REST | REST | Sistem menetapkan fitur Minggu. |

### 5.4 Parameter donasi
| Parameter | Tipe | Wajib | Default | Rentang | Deskripsi |
|---|---|---:|---:|---|---|
| donation.min_amount | int | Ya | 1 | 1–1.000.000 | Sistem menolak donasi di bawah nilai ini. |
| donation.max_amount | int | Ya | 999999 | 1–1.000.000 | Sistem menolak donasi di atas nilai ini. |

### 5.5 Parameter mode mahir
| Parameter | Tipe | Wajib | Default | Rentang | Deskripsi |
|---|---|---:|---:|---|---|
| advanced.loan.enabled | bool | Ya | false | - | Sistem mengaktifkan event pinjaman. |
| advanced.insurance.enabled | bool | Ya | false | - | Sistem mengaktifkan event asuransi. |
| advanced.saving_goal.enabled | bool | Ya | false | - | Sistem mengaktifkan event tabungan tujuan. |

---

## 6. Aturan Validasi Konfigurasi
Sistem menjalankan validasi berikut sebelum sistem menyimpan versi baru dan sebelum instruktur mengaktifkan versi untuk sesi.

### 6.1 Validasi struktur
Sistem memeriksa:
1. JSON harus valid dan dapat diparse.
2. Field wajib harus ada.
3. Tipe data setiap parameter harus sesuai.
4. Enum harus memakai nilai yang terdaftar.

### 6.2 Validasi rentang nilai
Sistem memeriksa:
1. `actions_per_turn` berada pada rentang.
2. `starting_cash` tidak negatif.
3. `donation.min_amount` tidak lebih besar dari `donation.max_amount`.
4. `max_same_ingredient` tidak lebih besar dari `max_ingredient_total`.

### 6.3 Validasi konflik dan dependensi
Sistem memeriksa:
1. Jika `mode=PEMULA`, sistem menolak `advanced.*.enabled=true`.
2. Jika `weekday_rules.friday.enabled=false`, sistem menolak event `day.friday.donation` pada sesi yang memakai versi ini.
3. Jika `constraints.require_primary_before_others=true`, sistem menolak event pembelian kebutuhan lain sebelum pemain memenuhi kebutuhan primer pada hari itu.
4. Jika `constraints.primary_need_max_per_day=0`, sistem menolak event `need.primary.purchased`.

### 6.4 Validasi kompatibilitas versi
Sistem menetapkan nomor versi secara otomatis. Sistem melarang instruktur mengubah nomor versi manual.

---

## 7. Hubungan *Ruleset* dengan Event
Sistem menerapkan aturan berikut:
1. Klien mengirim `ruleset_version_id` pada setiap event.
2. Sistem memeriksa kecocokan `ruleset_version_id` dengan ruleset aktif sesi.
3. Sistem menolak event jika `ruleset_version_id` tidak cocok.
4. Jika sistem mencatat log validasi ke `validation_logs`, sistem sebaiknya menyimpan referensi `event_pk` bila event sudah tersimpan, dan memakai `event_id` sebagai atribut audit/idempotensi.

Sistem memetakan aturan *ruleset* ke validasi event:
- `actions_per_turn` memvalidasi `turn.action.used`.
- `donation.min_amount` memvalidasi `day.friday.donation`.
- batas kartu bahan memvalidasi `ingredient.purchased`.
- aturan kebutuhan primer memvalidasi `need.*.purchased`.
- fitur mode mahir memvalidasi event `loan.*` dan `insurance.*`.

Catatan implementasi logging:
- Untuk event yang ditolak sebelum disimpan ke `events`, `validation_logs.event_pk` dapat bernilai `null`.
- Untuk event yang valid dan tersimpan, `validation_logs.event_pk` sebaiknya mengacu ke `events.event_pk` agar keterlacakan dan integritas referensi terjaga.

---

## 8. Kontrak API untuk *Ruleset*
Bagian ini merangkum endpoint yang menangani *ruleset*. Dokumen kontrak lengkap tetap berada pada dokumen “Spesifikasi Event dan Kontrak REST API”.

### 8.1 Endpoint minimum
1. `POST /api/rulesets`  
   Sistem membuat *ruleset* dan versi 1.

2. `PUT /api/rulesets/{rulesetId}`  
   Sistem membuat versi baru dari *ruleset* yang sama.

3. `GET /api/rulesets`  
   Sistem mengembalikan daftar *ruleset* beserta versi terbaru.

4. `POST /api/sessions/{sessionId}/ruleset/activate`  
   Instruktur mengaktifkan versi tertentu untuk sesi.

### 8.2 Respons aktivasi
Sistem mengembalikan `ruleset_version_id` yang aktif pada sesi.

---

## 9. Contoh Konfigurasi
### 9.1 Contoh *ruleset* mode pemula
```json
{
  "mode": "PEMULA",
  "actions_per_turn": 2,
  "starting_cash": 20,
  "weekday_rules": {
    "friday": { "feature": "DONATION", "enabled": true },
    "saturday": { "feature": "GOLD_TRADE", "enabled": true },
    "sunday": { "feature": "REST", "enabled": true }
  },
  "constraints": {
    "cash_min": 0,
    "max_ingredient_total": 6,
    "max_same_ingredient": 3,
    "primary_need_max_per_day": 1,
    "require_primary_before_others": true
  },
  "donation": { "min_amount": 1, "max_amount": 999999 },
  "gold_trade": { "allow_buy": true, "allow_sell": true },
  "advanced": {
    "loan": { "enabled": false },
    "insurance": { "enabled": false },
    "saving_goal": { "enabled": false }
  }
}
```

### 9.2 Contoh *ruleset* mode mahir
```json
{
  "mode": "MAHIR",
  "actions_per_turn": 2,
  "starting_cash": 20,
  "weekday_rules": {
    "friday": { "feature": "DONATION", "enabled": true },
    "saturday": { "feature": "GOLD_TRADE", "enabled": true },
    "sunday": { "feature": "REST", "enabled": true }
  },
  "constraints": {
    "cash_min": 0,
    "max_ingredient_total": 6,
    "max_same_ingredient": 3,
    "primary_need_max_per_day": 1,
    "require_primary_before_others": true
  },
  "donation": { "min_amount": 1, "max_amount": 999999 },
  "gold_trade": { "allow_buy": true, "allow_sell": true },
  "advanced": {
    "loan": { "enabled": true },
    "insurance": { "enabled": true },
    "saving_goal": { "enabled": true }
  }
}
```

---

## 10. Kriteria Kesiapan Implementasi
Sistem siap masuk tahap implementasi modul manajemen *ruleset* jika:
1. Sistem memvalidasi JSON sesuai aturan pada bagian 6.
2. Sistem mengeluarkan error terstruktur saat validasi gagal.
3. Sistem menyimpan versi baru saat instruktur mengubah konfigurasi.
4. Sistem mengunci versi aktif pada sesi.
5. Sistem menolak event dengan `ruleset_version_id` yang tidak cocok.



