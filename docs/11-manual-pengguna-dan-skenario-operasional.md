# Manual Pengguna dan Skenario Operasional  
## Sistem Informasi Dasbor Analitika & Manajemen *Ruleset* Cashflowpoly

### Dokumen
- Nama dokumen: Manual Pengguna dan Skenario Operasional
- Versi: 1.0
- Tanggal: (isi tanggal)
- Penyusun: (isi nama)

---

## 1. Tujuan
Dokumen ini menjelaskan cara penggunaan sistem dari sudut pandang pengguna, terutama instruktur sebagai operator. Dokumen ini juga mendefinisikan skenario operasional standar dari pembuatan sesi sampai interpretasi metrik pada dasbor.

---

## 2. Peran Pengguna
### 2.1 Instruktur
Instruktur mengelola sesi, memilih *ruleset*, memantau ringkasan analitika, dan membaca tren hasil pembelajaran.

### 2.2 Pemain
Pemain melihat performa diri dan histori transaksi diri.

Jika implementasi tahap awal hanya menampilkan UI instruktur, sistem tetap mencatat kebutuhan UI pemain sebagai pengembangan berikutnya.

---

## 3. Akses Sistem
Sistem menyediakan dua komponen yang instruktur jalankan:
1. REST API (server) untuk menerima event dan menghasilkan analitika,
2. Web MVC (dasbor) untuk tampilan analitika.

Instruktur mengakses dasbor melalui browser:
- URL Web: `http://localhost:<PORT_WEB>/sessions`

Instruktur menguji API melalui Swagger:
- URL Swagger: `http://localhost:<PORT_API>/swagger`

Catatan:
- URL dapat berbeda tergantung konfigurasi `launchSettings.json`.

---

## 4. Alur Operasional Standar (Instruktur)
Bagian ini mendefinisikan langkah kerja yang instruktur lakukan pada setiap sesi.

### 4.1 Menyiapkan sistem
Instruktur memastikan:
1. PostgreSQL berjalan,
2. REST API berjalan,
3. Web MVC berjalan,
4. Swagger dapat diakses.

---

## 5. Skenario 1 — Membuat Sesi Baru
### Tujuan
Instruktur membuat sesi permainan untuk menerima event dan menyimpan histori.

### Langkah
1. Buka Swagger UI.
2. Buka endpoint `POST /api/sessions`.
3. Isi `session_name` dan `mode`.
4. Kirim permintaan.

### Hasil yang instruktur lihat
Sistem mengembalikan `session_id`.

### Verifikasi cepat (opsional)
Instruktur membuka halaman `/sessions` pada Web dan memastikan sesi muncul pada daftar.

---

## 6. Skenario 2 — Mengaktifkan *Ruleset* pada Sesi
### Tujuan
Instruktur menetapkan konfigurasi aturan yang berlaku pada sesi.

### Langkah
1. Buka Web MVC.
2. Buka daftar sesi `/sessions`.
3. Buka aksi “Ruleset” pada sesi.
4. Pilih ruleset dan versi.
5. Tekan tombol “Aktifkan”.

### Hasil yang instruktur lihat
Sistem menampilkan ruleset aktif dan versi yang terpilih.

### Aturan penting
Sistem menolak aktivasi jika sesi berstatus `ENDED`.

---

## 7. Skenario 3 — Memulai Sesi (opsional jika sistem menerapkan status)
### Tujuan
Instruktur menandai sesi siap menerima event permainan.

### Langkah
1. Buka Swagger.
2. Panggil `POST /api/sessions/{sessionId}/start`.

### Hasil yang instruktur lihat
Sistem mengubah status menjadi `STARTED`.

---

## 8. Skenario 4 — Mengirim Event Permainan (Klien/Simulator)
### Tujuan
Sistem menerima event permainan dan menyimpannya sebagai histori.

### Cara mengirim event
Sistem menerima event dari klien IDN/simulator atau pengujian manual via Postman/Swagger.

### Langkah uji manual (Postman/Swagger)
1. Buka endpoint `POST /api/sessions/{sessionId}/events`.
2. Isi payload event sesuai kontrak.
3. Kirim event berurutan berdasarkan `sequence_number`.

### Hal yang instruktur perhatikan
1. Sistem menerima event valid dengan status berhasil.
2. Sistem menolak event yang:
   - urutan `sequence_number` loncat,
   - `ruleset_version_id` tidak sesuai sesi,
   - tipe data payload salah.

### Bukti yang bisa instruktur simpan
Instruktur menyimpan:
- respons API,
- `trace_id` jika sistem menolak event.

---

## 9. Skenario 5 — Memantau Analitika pada Detail Sesi
### Tujuan
Instruktur menilai performa agregat sesi dan per pemain.

### Langkah
1. Buka Web MVC.
2. Buka `/sessions`.
3. Pilih sesi dan buka “Detail”.
4. Lihat kartu metrik ringkasan:
   - total pemasukan,
   - total pengeluaran,
   - net cashflow,
   - jumlah event,
   - jumlah pelanggaran aturan (jika ada).
5. Lihat tabel pemain untuk ringkasan per pemain.

### Interpretasi cepat
- Pemasukan tinggi dan pengeluaran tinggi perlu dilihat bersama net cashflow.
- Donasi menunjukkan keputusan sosial pemain pada hari Jumat.
- Kepatuhan kebutuhan primer menunjukkan kedisiplinan pemain terhadap aturan pembelian.

---

## 10. Skenario 6 — Membaca Detail Pemain dan Histori Transaksi
### Tujuan
Instruktur atau pemain menilai perilaku pemain berdasarkan transaksi dan metrik.

### Langkah
1. Dari detail sesi, pilih pemain.
2. Buka halaman detail pemain.
3. Lihat:
   - metrik pemain (kartu/daftar),
   - tabel histori transaksi.

### Cara membaca tabel transaksi
Instruktur menilai:
- kategori transaksi dominan,
- frekuensi transaksi per hari,
- transaksi besar yang memengaruhi saldo,
- hubungan transaksi dengan event tertentu (bila sistem menyediakan audit event).

---

## 11. Skenario 7 — Mengakhiri Sesi dan Membekukan Data
### Tujuan
Instruktur menandai sesi selesai agar sistem tidak menerima perubahan konfigurasi.

### Langkah
1. Buka Swagger.
2. Panggil `POST /api/sessions/{sessionId}/end`.

### Hasil yang instruktur lihat
Sistem mengubah status menjadi `ENDED`.

### Dampak
Sistem menolak:
- aktivasi ruleset baru pada sesi ini,
- perubahan status lain yang tidak relevan.

---

## 12. Skenario 8 — Menghitung Ulang Metrik (opsional)
Jika sistem menyediakan endpoint “recompute”, instruktur menjalankan fitur ini saat:
- instruktur memperbaiki data seed,
- instruktur menemukan inkonsistensi snapshot,
- instruktur ingin menghasilkan snapshot akhir sesi.

### Langkah
1. Buka Swagger.
2. Panggil `POST /api/analytics/sessions/{sessionId}/recompute` (jika ada).
3. Buka ulang detail sesi pada Web.

---

## 13. Skenario 9 — Pengambilan Bukti untuk Laporan Tugas Akhir
Instruktur menyiapkan bukti uji dan bukti tampilan untuk laporan.

### Bukti yang disarankan
1. Screenshot Swagger untuk uji endpoint utama:
   - create session,
   - activate ruleset,
   - ingest event,
   - analytics summary.
2. Screenshot Postman untuk skenario validasi input.
3. Screenshot halaman Web:
   - daftar sesi,
   - detail sesi,
   - detail pemain,
   - halaman aktivasi ruleset.
4. Hasil query PostgreSQL untuk validasi:
   - `metric_snapshots`
   - `event_cashflow_projections`
   - `events`

### Format penyimpanan bukti
Instruktur menyimpan bukti pada folder:
```
docs/evidence/
  api/
  ui/
  db/
```

---

## 14. Masalah Umum dan Solusi Cepat
### 14.1 Web menampilkan error “API tidak dapat diakses”
Instruktur melakukan:
1. cek API berjalan pada URL yang benar,
2. cek `ApiBaseUrl` pada `Cashflowpoly.Web/appsettings.Development.json`,
3. cek sertifikat https jika memakai https.

### 14.2 Analitika kosong padahal event sudah terkirim
Instruktur melakukan:
1. cek event tersimpan pada tabel `events`,
2. cek proyeksi transaksi pada `event_cashflow_projections`,
3. cek snapshot pada `metric_snapshots`,
4. jalankan “recompute” jika endpoint tersedia.

### 14.3 Event ditolak terus
Instruktur melakukan:
1. cek urutan `sequence_number`,
2. cek `ruleset_version_id` sesuai sesi aktif,
3. cek struktur payload sesuai kontrak,
4. lihat `error_code` dan `trace_id`.

---

## 15. Ringkasan Alur Operasional
Instruktur menjalankan urutan ini pada setiap sesi:
1. buat sesi,
2. aktifkan ruleset,
3. mulai sesi (jika dipakai),
4. kirim event berurutan,
5. buka dasbor untuk memantau metrik,
6. evaluasi detail pemain,
7. akhiri sesi,
8. simpan bukti uji dan tampilan.

