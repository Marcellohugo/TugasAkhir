# Spesifikasi Event dan Kontrak REST API  
## Sistem Informasi Dasbor Analitika & Manajemen Ruleset Cashflowpoly

### Dokumen
- Nama dokumen: Spesifikasi Event dan Kontrak REST API
- Versi: 1.0
- Tanggal: (isi tanggal)
- Penyusun: (isi nama)

---

## 1. Tujuan Dokumen
Dokumen ini menetapkan spesifikasi event sebagai format data utama pencatatan permainan serta menetapkan kontrak REST API untuk menerima, memvalidasi, menyimpan, dan menyediakan data analitika serta manajemen ruleset. Dokumen ini menjadi acuan implementasi back-end dan acuan integrasi UI MVC serta pengujian fungsional.

---

## 2. Prinsip Desain Event
### 2.1 Event sebagai sumber kebenaran
Sistem memperlakukan event sebagai sumber utama untuk membentuk histori dan menghitung state serta metrik. Sistem tidak mengandalkan input agregat dari klien.

### 2.2 Idempotensi
Sistem menolak event duplikat berdasarkan kombinasi `session_id` dan `event_id`. Klien dapat mengirim ulang request yang sama tanpa menimbulkan dampak ganda.

### 2.3 Keterurutan
Sistem memproses event sesuai urutan `sequence_number` per sesi. Sistem menolak event dengan `sequence_number` lebih kecil dari event terakhir pada sesi.

### 2.4 Jejak ruleset
Setiap event wajib menyertakan `ruleset_version_id` agar analisis tetap konsisten walau ruleset berubah.

---

## 3. Model Event Umum
### 3.1 Struktur event
Setiap event dikirim sebagai JSON dengan skema umum berikut:

| Field | Tipe | Wajib | Deskripsi |
|---|---|---:|---|
| event_id | string (UUID) | Ya | ID unik event. |
| session_id | string (UUID) | Ya | ID sesi permainan. |
| player_id | string (UUID) | Ya* | ID pemain. Wajib untuk event aksi pemain. Kosong untuk event sistem. |
| actor_type | string | Ya | Nilai: `PLAYER` atau `SYSTEM`. |
| timestamp | string (ISO 8601) | Ya | Waktu event terjadi. |
| day_index | int | Ya | Indeks hari dalam sesi. |
| weekday | string | Ya | Nilai: `MON,TUE,WED,THU,FRI,SAT,SUN`. |
| turn_number | int | Ya | Nomor giliran pada sesi. |
| action_type | string | Ya | Jenis event. |
| sequence_number | long | Ya | Nomor urut event per sesi. |
| ruleset_version_id | string (UUID) | Ya | Versi ruleset yang aktif saat event terjadi. |
| payload | object | Ya | Detail event sesuai `action_type`. |
| client_request_id | string | Tidak | ID request dari klien untuk tracing. |

Catatan:
- `player_id` wajib saat `actor_type=PLAYER`.
- `timestamp` harus format UTC atau menyertakan offset zona waktu.

### 3.2 Struktur respons error (standar)
Semua error validasi mengikuti format ini:

```json
{
  "error_code": "VALIDATION_ERROR",
  "message": "Field wajib tidak lengkap",
  "details": [
    { "field": "session_id", "issue": "REQUIRED" },
    { "field": "payload.amount", "issue": "OUT_OF_RANGE" }
  ],
  "trace_id": "00-...-..."
}
```

---

## 4. Katalog Event dan Spesifikasi Payload
Bagian ini mendefinisikan event yang digunakan sistem. Sistem dapat menambah event baru, namun event baru wajib mengikuti struktur event umum dan aturan validasi.

### 4.1 Event sesi
#### 4.1.1 `session.created`
Tujuan: Membuat sesi permainan.

Payload:
```json
{
  "mode": "PEMULA",
  "session_name": "Kelas A - Pertemuan 1"
}
```

Validasi:
- `payload.mode` bernilai `PEMULA` atau `MAHIR`.
- `payload.session_name` panjang 1–100.

Efek data:
- Membuat record `Session`.

---

#### 4.1.2 `session.started`
Payload:
```json
{ "start_note": "Mulai sesi" }
```

Validasi:
- Status sesi harus `CREATED`.

Efek data:
- Mengubah status sesi menjadi `STARTED`.

---

#### 4.1.3 `session.ended`
Payload:
```json
{ "end_note": "Selesai sesi" }
```

Validasi:
- Status sesi harus `STARTED`.

Efek data:
- Mengubah status sesi menjadi `ENDED`.

---

### 4.2 Event giliran
#### 4.2.1 `turn.started`
Payload:
```json
{
  "action_tokens": 2
}
```

Validasi:
- `payload.action_tokens` sama dengan parameter ruleset `actions_per_turn`.

Efek data:
- Menandai awal giliran.

---

#### 4.2.2 `turn.action.used`
Payload:
```json
{
  "used": 1,
  "remaining": 1
}
```

Validasi:
- `remaining >= 0`.
- Total penggunaan pada giliran tidak melebihi `actions_per_turn`.

Efek data:
- Memperbarui penghitung token aksi.

---

#### 4.2.3 `turn.ended`
Payload:
```json
{ "note": "Akhir giliran" }
```

Validasi:
- Giliran harus sudah dimulai.

Efek data:
- Menandai akhir giliran.

---

### 4.3 Event transaksi dan arus kas
#### 4.3.1 `transaction.recorded`
Tujuan: Mencatat transaksi pemasukan/pengeluaran koin.

Payload:
```json
{
  "direction": "OUT",
  "amount": 5,
  "category": "NEED_PRIMARY",
  "counterparty": "BANK",
  "reference": "CARD-NEED-001",
  "note": "Beli kebutuhan primer"
}
```

Validasi:
- `direction` bernilai `IN` atau `OUT`.
- `amount > 0`.
- `category` termasuk enumerasi kategori transaksi.
- `counterparty` termasuk `BANK` atau `PLAYER`.
- Sistem menolak transaksi `OUT` jika saldo tidak cukup (bergantung ruleset).

Efek data:
- Menambah record transaksi atau memproyeksikan dari event.
- Mengubah saldo pada state.

---

### 4.4 Event aturan harian
#### 4.4.1 `day.friday.donation`
Payload:
```json
{
  "amount": 2
}
```

Validasi:
- `weekday` harus `FRI`.
- `amount >= donation_min`.
- `amount > 0`.

Efek data:
- Mengurangi saldo.
- Menambah total donasi pemain.

---

#### 4.4.2 `day.saturday.gold_trade`
Payload:
```json
{
  "trade_type": "BUY",
  "unit_price": 6,
  "qty": 2,
  "amount": 12
}
```

Validasi:
- `weekday` harus `SAT`.
- `trade_type` bernilai `BUY` atau `SELL`.
- `qty > 0`.
- `amount = unit_price * qty`.
- Sistem menolak BUY jika saldo tidak cukup.
- Sistem menolak SELL jika kepemilikan emas kurang.

Efek data:
- Mengubah saldo dan kepemilikan emas.

---

### 4.5 Event kebutuhan dan bahan (contoh minimal)
#### 4.5.1 `need.primary.purchased`
Payload:
```json
{
  "card_id": "NEED-001",
  "amount": 5
}
```

Validasi:
- `weekday` tidak membatasi, namun sistem membatasi maksimal 1 pembelian kebutuhan primer per hari.
- Sistem menolak pembelian kebutuhan non-primer jika kebutuhan primer belum terpenuhi pada hari itu.
- `amount > 0`.

Efek data:
- Mengurangi saldo.
- Menambah kepemilikan kartu kebutuhan.

---

#### 4.5.2 `ingredient.purchased`
Payload:
```json
{
  "card_id": "ING-012",
  "ingredient_name": "Beras",
  "amount": 1
}
```

Validasi:
- Total kartu bahan tidak melebihi 6.
- Kartu bahan yang sama tidak melebihi 3.
- `amount > 0`.

Efek data:
- Mengurangi saldo.
- Menambah inventori bahan.

---

#### 4.5.3 `order.claimed`
Payload:
```json
{
  "order_card_id": "ORD-005",
  "required_ingredient_card_ids": ["ING-001", "ING-012"],
  "income": 15
}
```

Validasi:
- Pemain memiliki semua bahan pada daftar.
- `income > 0`.

Efek data:
- Mengurangi inventori bahan.
- Menambah saldo.

---

### 4.6 Event mode mahir (minimum)
#### 4.6.1 `loan.syariah.taken`
Payload:
```json
{
  "loan_id": "LOAN-001",
  "principal": 20,
  "installment": 5,
  "duration_turn": 4
}
```

Validasi:
- `principal > 0`.
- `installment > 0`.
- `duration_turn > 0`.
- Sistem menolak jika aturan ruleset melarang pinjaman pada kondisi tertentu.

Efek data:
- Menambah saldo.
- Mencatat kewajiban cicilan.

---

#### 4.6.2 `loan.syariah.repaid`
Payload:
```json
{
  "loan_id": "LOAN-001",
  "amount": 5
}
```

Validasi:
- `amount > 0`.
- Pemain memiliki saldo cukup.
- Loan masih aktif.

Efek data:
- Mengurangi saldo.
- Mengurangi sisa kewajiban.

---

#### 4.6.3 `insurance.multirisk.purchased`
Payload:
```json
{
  "policy_id": "INS-001",
  "premium": 3,
  "coverage_type": "MULTIRISK"
}
```

Validasi:
- `premium > 0`.

Efek data:
- Mengurangi saldo.
- Menambah status proteksi.

---

## 5. Kontrak REST API
Kontrak berikut menjadi acuan Swagger dan pengujian.

### 5.1 Prinsip umum endpoint
- Semua endpoint mengirim dan menerima JSON.
- Sistem mengembalikan error sesuai format standar bagian 3.2.
- Sistem mengembalikan `trace_id` untuk pelacakan log.

---

## 6. Endpoint Session
### 6.1 Buat sesi
- Method: `POST`
- Path: `/api/sessions`
- Request:
```json
{
  "session_name": "Kelas A - Pertemuan 1",
  "mode": "PEMULA",
  "ruleset_id": "uuid"
}
```
- Response 201:
```json
{ "session_id": "uuid" }
```

Status code:
- 201 Created
- 400 Validation Error
- 404 Ruleset tidak ditemukan

---

### 6.2 Mulai sesi
- Method: `POST`
- Path: `/api/sessions/{sessionId}/start`
- Response 200:
```json
{ "status": "STARTED" }
```

---

### 6.3 Akhiri sesi
- Method: `POST`
- Path: `/api/sessions/{sessionId}/end`
- Response 200:
```json
{ "status": "ENDED" }
```

---

## 7. Endpoint Event
### 7.1 Kirim event tunggal
- Method: `POST`
- Path: `/api/events`
- Request: sesuai struktur event umum bagian 3.1
- Response 201:
```json
{ "stored": true, "event_id": "uuid" }
```

Status code:
- 201 Created
- 400 Validation Error
- 404 Session tidak ditemukan
- 409 Duplicate event_id
- 422 Domain rule violated

---

### 7.2 Kirim event batch
- Method: `POST`
- Path: `/api/events/batch`
- Request:
```json
{ "events": [ { ... }, { ... } ] }
```
- Response 200:
```json
{
  "stored_count": 10,
  "failed": [
    { "event_id": "uuid", "error_code": "VALIDATION_ERROR" }
  ]
}
```

---

### 7.3 Ambil event per sesi
- Method: `GET`
- Path: `/api/sessions/{sessionId}/events?fromSeq=0&limit=200`
- Response 200:
```json
{
  "session_id": "uuid",
  "events": [ { ... }, { ... } ]
}
```

---

## 8. Endpoint Ruleset
### 8.1 Buat ruleset
- Method: `POST`
- Path: `/api/rulesets`
- Request:
```json
{
  "name": "Ruleset Default",
  "description": "Konfigurasi awal",
  "config": {
    "actions_per_turn": 2,
    "starting_cash": 20,
    "donation_min": 1,
    "max_ingredient_total": 6,
    "max_same_ingredient": 3
  }
}
```
- Response 201:
```json
{ "ruleset_id": "uuid", "version": 1 }
```

---

### 8.2 Update ruleset (menciptakan versi baru)
- Method: `PUT`
- Path: `/api/rulesets/{rulesetId}`
- Response 200:
```json
{ "ruleset_id": "uuid", "version": 2 }
```

---

### 8.3 Aktivasi ruleset untuk sesi
- Method: `POST`
- Path: `/api/sessions/{sessionId}/ruleset/activate`
- Request:
```json
{ "ruleset_id": "uuid", "version": 2 }
```
- Response 200:
```json
{ "session_id": "uuid", "ruleset_version_id": "uuid" }
```

---

### 8.4 Ambil daftar ruleset
- Method: `GET`
- Path: `/api/rulesets`
- Response 200:
```json
{ "items": [ { "ruleset_id":"uuid", "name":"Ruleset Default", "latest_version": 2 } ] }
```

---

## 9. Endpoint Metrics dan Dashboard
### 9.1 Ambil metrik sesi
- Method: `GET`
- Path: `/api/analytics/sessions/{sessionId}`
- Response 200:
```json
{
  "session_id": "uuid",
  "summary": {
    "event_count": 120,
    "cash_in_total": 200,
    "cash_out_total": 150
  },
  "by_player": [
    {
      "player_id": "uuid",
      "cash_in_total": 120,
      "cash_out_total": 90,
      "donation_total": 10,
      "gold_qty": 2
    }
  ]
}
```

---

### 9.2 Ambil histori transaksi
- Method: `GET`
- Path: `/api/analytics/sessions/{sessionId}/transactions?playerId=uuid`
- Response 200:
```json
{
  "items": [
    { "timestamp":"...", "direction":"OUT", "amount":5, "category":"NEED_PRIMARY" }
  ]
}
```

---

## 10. Status Code dan Makna
| Status | Makna |
|---:|---|
| 200 | Request berhasil. |
| 201 | Data berhasil dibuat dan disimpan. |
| 400 | Struktur request tidak valid atau field wajib kosong. |
| 401 | Tidak terautentikasi. |
| 403 | Tidak berhak akses. |
| 404 | Resource tidak ditemukan. |
| 409 | Duplikasi data, terutama event idempotensi. |
| 422 | Aturan domain permainan dilanggar. |
| 500 | Kesalahan internal server. |

---

## 11. Checklist Konsistensi (Event–API–Data)
Dokumen ini konsisten jika:
1. Setiap event memiliki definisi payload dan validasi.
2. Setiap endpoint memiliki request/response dan status code.
3. Setiap validasi domain dapat ditelusuri ke aturan ruleset atau aturan permainan.
4. Setiap endpoint yang dipakai UI memiliki kebutuhan data yang tersedia.
