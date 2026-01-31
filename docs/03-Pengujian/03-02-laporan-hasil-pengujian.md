# Laporan Hasil Pengujian  
## Sistem Informasi Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly

### Dokumen
- Nama dokumen: Laporan Hasil Pengujian
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan
Dokumen ini saya susun untuk menyajikan rekap hasil uji fungsional (*black-box*), uji integrasi alur event, dan validasi UI dasbor. Dokumen ini juga saya susun untuk merangkum temuan (*bug*), modul yang paling sering bermasalah, dan status perbaikannya.

Dokumen ini diisi setelah menjalankan rencana uji pada dokumen 01.

---

## 2. Identitas Pengujian
- Lingkungan: Windows 11 Home, VS Code, .NET 10, PostgreSQL
- Tanggal pengujian: (isi)
- Versi aplikasi (git commit/tag): (isi)
- Penguji: (isi)
- DB: `cashflowpoly`
- URL API: (isi)
- URL Web: (isi)

---

## 3. Ringkasan Hasil Uji
### 3.1 Rekap keseluruhan
| Jenis Uji | Total Kasus | PASS | FAIL | Catatan |
|---|---:|---:|---:|---|
| Uji REST API (black-box) | (isi) | (isi) | (isi) | |
| Uji Integrasi | (isi) | (isi) | (isi) | |
| Validasi UI MVC | (isi) | (isi) | (isi) | |
| **Total** | (isi) | (isi) | (isi) | |

### 3.2 Kriteria kelulusan
Sistem dinyatakan valid jika:
1. semua skenario wajib berstatus PASS,
2. tidak ada temuan OPEN pada modul inti (M1–M6),
3. nilai metrik pada UI cocok dengan data pada basis data.

---

## 4. Rekap Hasil Uji Per Modul
Sistem merekap hasil berdasarkan modul pada dokumen 07.

| Kode Modul | Modul | PASS | FAIL | Temuan (BUG) |
|---|---|---:|---:|---|
| M1 | Manajemen Sesi | (isi) | (isi) | (isi) |
| M2 | Manajemen Ruleset | (isi) | (isi) | (isi) |
| M3 | Ingest Event | (isi) | (isi) | (isi) |
| M4 | Proyeksi Arus Kas | (isi) | (isi) | (isi) |
| M5 | Agregasi Metrik | (isi) | (isi) | (isi) |
| M6 | Analitika (Endpoint) | (isi) | (isi) | (isi) |
| M7 | UI MVC Dasbor | (isi) | (isi) | (isi) |
| M8 | Logging & Error Handling | (isi) | (isi) | (isi) |

---

## 5. Detail Hasil Uji REST API (Black-box)
Bagian ini memuat tabel hasil uji per kasus.

### 5.1 Template tabel hasil uji API
| Tanggal | ID Uji | Endpoint | Modul | Input Ringkas | Status | Bukti | Catatan | Trace ID |
|---|---|---|---|---|---|---|---|---|

### 5.2 Contoh pengisian (contoh format)
| 2026-01-27 | TC-API-01 | POST /api/sessions | M1 | session_name=Sesi Uji 01 | PASS | SS-API-01.png | 201 sesuai | - |
| 2026-01-27 | TC-API-13 | POST /api/sessions/{id}/events | M3 | sequence loncat | PASS | SS-API-13.png | 422 sesuai | 00-abc... |

Catatan:
- “Bukti” berupa nama file screenshot atau link Postman collection export.

---

## 6. Detail Hasil Uji Integrasi
### 6.1 Template tabel hasil uji integrasi
| Tanggal | ID Uji | Skenario | Modul Dominan | Status | Bukti | Catatan |
|---|---|---|---|---|---|---|

### 6.2 Kolom “Modul Dominan”
Kolom “Modul Dominan” diisi dengan modul yang paling berpengaruh pada hasil uji itu. Contoh:
- IT-01 gagal karena snapshot kosong → M5
- IT-02 gagal karena ruleset tidak memblokir event → M2/M3

---

## 7. Validasi UI MVC
### 7.1 Template validasi UI vs DB
| Tanggal | ID Uji | Halaman | Data UI | Query DB | Status | Bukti | Catatan |
|---|---|---|---|---|---|---|---|

Contoh query DB yang dipakai:
```sql
select metric_name, metric_value_numeric
from metric_snapshots
where session_id = '<SESSION_ID>' and player_id is null
order by computed_at desc;
```

Contoh validasi integritas referensi event (FK berbasis `event_pk`):
```sql
select count(*) as orphan_projections
from event_cashflow_projections ecp
left join events e on e.event_pk = ecp.event_pk
where e.event_pk is null;
```
Ekspektasi: `orphan_projections = 0`.

---

## 8. Rekap Temuan (*Bug*) dan Perbaikan
### 8.1 Template daftar temuan
| ID Bug | Modul | Ringkasan | Severity | Status | Tanggal Temuan | Tanggal Fix | Bukti | Trace ID |
|---|---|---|---|---|---|---|---|---|

**Severity (pakai salah satu):**
- S1 Kritikal: sistem tidak bisa jalan, data korup, endpoint utama gagal
- S2 Mayor: fitur inti salah namun sistem tetap jalan
- S3 Minor: tampilan/teks, validasi kecil, non-inti

### 8.2 Template detail temuan
Untuk setiap bug, sistem menulis deskripsi berikut:
- ID temuan:
- Modul:
- Ringkasan:
- Langkah reproduksi:
  1. ...
  2. ...
- Ekspektasi:
- Aktual:
- Dampak:
- Akar masalah (ringkas):
- Perbaikan:
- Status re-test:

---

## 9. Rekap “Temuan Uji Dominan”
Bagian ini menjawab pertanyaan: **temuan paling banyak terjadi di modul mana**.

### 9.1 Rekap jumlah temuan per modul
| Modul | Jumlah Temuan | Persentase |
|---|---:|---:|
| M1 | (isi) | (isi) |
| M2 | (isi) | (isi) |
| M3 | (isi) | (isi) |
| M4 | (isi) | (isi) |
| M5 | (isi) | (isi) |
| M6 | (isi) | (isi) |
| M7 | (isi) | (isi) |
| M8 | (isi) | (isi) |

### 9.2 Modul dengan temuan dominan
- Modul dominan: (isi Mx)
- Alasan dominan:
  1. (contoh: validasi payload kompleks)
  2. (contoh: urutan event dan idempotensi)
  3. (contoh: perhitungan metrik butuh aturan ruleset)

### 9.3 Tindakan pencegahan
Sistem mencatat tindakan pencegahan agar bug tidak berulang:
1. tambah uji otomatis (unit/integration),
2. tambah validasi kontrak request,
3. tambah logging dengan `trace_id`,
4. tambah seed data uji yang stabil.

---

## 10. Kesimpulan Hasil Pengujian
Sistem mencatat status akhir:
- Status akhir: VALID / BELUM VALID
- Ringkasan:
  - jumlah kasus uji PASS:
  - jumlah temuan tersisa:
  - modul yang masih perlu perbaikan:
- Rencana tindak lanjut (jika belum valid):
  1. ...

Catatan:
- Bagian ini diisi setelah semua uji pada dokumen 07 selesai.

---

## 11. Lampiran Bukti
Sistem menaruh bukti pada struktur folder berikut:
```
docs/evidence/
  api/
  ui/
  db/
  postman/
```

Daftar lampiran:
- SS-API-01.png: create session
- SS-API-08.png: activate ruleset
- SS-UI-01.png: detail sesi
- DB-01.sql: query metric_snapshots
- (isi sesuai bukti nyata)



