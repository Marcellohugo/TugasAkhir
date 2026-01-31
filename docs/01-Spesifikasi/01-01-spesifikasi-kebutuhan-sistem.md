# Analisis Kebutuhan Sistem  
## Sistem Informasi Dasbor Analitika & Manajemen Ruleset Cashflowpoly

### Dokumen
- Nama dokumen: Analisis Kebutuhan Sistem
- Versi: 1.0
- Tanggal: 28 Januari 2026
- Penyusun: Marco Marcello Hugo

---

## 1. Tujuan Sistem
Sistem menyediakan layanan pencatatan event permainan Cashflowpoly dan menyajikan analitika berbasis data untuk mendukung pemantauan progres belajar. Sistem juga menyediakan modul manajemen *ruleset* agar instruktur dapat mengubah konfigurasi permainan tanpa mengubah kode program. Sistem memproses log event yang tersimpan untuk menghasilkan metrik pembelajaran dan menampilkannya pada dasbor analitika.

---

## 2. Ruang Lingkup
### 2.1 Ruang lingkup pengembangan
Sistem mencakup:
1. Back-end berbasis RESTful API untuk menerima, memvalidasi, dan menyimpan event permainan.
2. Modul manajemen *ruleset* untuk membuat, memperbarui, menghapus, dan mengaktifkan konfigurasi permainan.
3. Modul analitika untuk menghitung metrik dari log event.
4. Antarmuka web berbasis ASP.NET Core MVC (*Razor Views*) untuk menampilkan dasbor analitika dan fitur administrasi *ruleset*.

### 2.2 Batasan ruang lingkup
Sistem:
1. Tidak membangun klien permainan (IDN) dan hanya menerima event dari klien IDN atau simulator.
2. Tidak melakukan pengenalan citra atau input otomatis dari media fisik.
3. Tidak menggantikan proses permainan manual, namun mendukung pencatatan dan analisis berbasis event.

---

## 3. Definisi Konsep Sistem
### 3.1 Event
Event merupakan representasi terstruktur dari aksi diskret yang terjadi selama permainan. Sistem menerima event dari klien, memvalidasi event, lalu menyimpannya sebagai log.

### 3.2 Log event
Log event merupakan urutan event di dalam satu sesi permainan. Sistem menggunakan urutan event untuk menghitung metrik dan merekonstruksi state permainan.

### 3.3 State permainan dan konsistensi
State permainan mencakup kondisi yang berubah akibat aksi pemain, seperti saldo, aset, status misi, dan variabel lain yang relevan. Sistem menjaga konsistensi state dengan memproses event secara berurutan, menolak event tidak valid, dan mencegah dampak ganda dari event duplikat.

### 3.4 Near real-time
Sistem memperbarui metrik dan tampilan dasbor setelah sistem menerima event pada setiap aksi atau setelah akhir giliran, sehingga instruktur dapat memantau progres secara cepat tanpa menunggu akhir permainan.

---

## 4. Aturan Domain yang Mempengaruhi Sistem
Sistem mengikuti aturan permainan yang berdampak pada pencatatan dan validasi, meliputi:
1. Pemain menjalankan dua aksi per giliran menggunakan token aksi.
2. Hari Jumat menjalankan mekanik donasi, dan hari Sabtu menjalankan mekanik investasi emas.
3. Setiap transaksi yang menambah atau mengurangi koin wajib tercatat sebagai arus kas.
4. Aturan pembelian kebutuhan primer mensyaratkan pembelian kebutuhan primer sebelum kebutuhan lain serta membatasi pembelian kebutuhan primer maksimal satu kali per hari.
5. Aturan pembelian bahan membatasi kepemilikan maksimal enam kartu bahan dan maksimal tiga kartu untuk jenis bahan yang sama.
6. Mode mahir menambah mekanik tambahan seperti pinjaman syariah dan asuransi multi risiko serta membatasi transaksi tertentu tanpa token aksi.

---

## 5. Aktor Sistem
Sistem melibatkan dua aktor:
1. Instruktur
2. Pemain

---

## 6. Kebutuhan Pengguna
### 6.1 Kebutuhan instruktur
Instruktur membutuhkan:
1. Fitur untuk membuat, memperbarui, menghapus, dan mengaktifkan *ruleset*.
2. Fitur untuk memulai dan mengakhiri sesi permainan.
3. Fitur untuk memantau performa pembelajaran dan performa misi pada level sesi (agregat) dan per pemain.
4. Fitur untuk melihat histori keputusan dan transaksi berbasis urutan event.
5. Fitur filter dan pengelompokan berdasarkan *ruleset*, sesi, dan pemain.
6. Fitur audit untuk melihat ringkasan aktivitas dan kesalahan validasi event.

### 6.2 Kebutuhan pemain
Pemain membutuhkan:
1. Tampilan performa pembelajaran dan performa misi personal berbasis metrik dan histori keputusan.
2. Tampilan ringkasan progres per sesi permainan.
3. Tampilan histori transaksi arus kas untuk melihat pola pemasukan dan pengeluaran.

---

## 7. Kebutuhan Fungsional
Bagian ini merinci kebutuhan yang dapat diuji.

### 7.1 Penerimaan event dan validasi (REST API)
- FR-API-01 Sistem menerima event dari klien IDN atau simulator melalui RESTful API.
- FR-API-02 Sistem memvalidasi struktur payload, tipe data, dan field wajib pada event.
- FR-API-03 Sistem memvalidasi aturan domain yang relevan pada event, seperti batas kepemilikan, batas transaksi, dan prasyarat aksi.
- FR-API-04 Sistem menerapkan idempotensi dengan menolak event duplikat pada kombinasi `session_id` dan `event_id`.
- FR-API-05 Sistem menjaga keterurutan event di dalam sesi dengan `sequence_number` atau `turn_number`.
- FR-API-06 Sistem menyimpan event yang valid ke basis data.
- FR-API-07 Sistem menyediakan endpoint untuk mengambil daftar event per sesi dalam urutan yang konsisten.

Aturan server (ringkas, tanpa mengubah kontrak payload):
- Sistem menyimpan event dengan `event_pk` sebagai primary key internal.
- Sistem menerapkan idempotensi pada kombinasi `session_id + event_id`.
- Sistem menolak `sequence_number` duplikat dalam satu sesi.
- Sistem mengizinkan `events.player_id = null` untuk event sistem.

Kriteria uji minimum:
1. Sistem mengembalikan kode status konsisten untuk sukses dan gagal.
2. Sistem mengembalikan pesan error yang menunjuk field atau aturan yang dilanggar.
3. Sistem menolak event duplikat dan tidak menggandakan dampak pada data.

### 7.2 Manajemen ruleset
- FR-RS-01 Sistem menyediakan pembuatan ruleset.
- FR-RS-02 Sistem menyediakan pembaruan ruleset.
- FR-RS-03 Sistem menyediakan penghapusan ruleset dengan pembatasan bila ruleset sudah dipakai pada sesi.
- FR-RS-04 Sistem menyediakan aktivasi ruleset untuk sesi tertentu.
- FR-RS-05 Sistem menyimpan versi ruleset dan riwayat perubahan agar analisis tetap akurat saat konfigurasi berubah.
- FR-RS-06 Sistem memvalidasi konfigurasi ruleset sebelum aktivasi untuk mencegah nilai di luar batas dan konflik konfigurasi.

### 7.3 Pengolahan dan agregasi metrik
- FR-MTR-01 Sistem menghitung metrik dari log event mentah menjadi indikator terukur.
- FR-MTR-02 Sistem menghasilkan metrik performa pembelajaran dan performa misi pada level sesi dan level pemain.
- FR-MTR-03 Sistem menghasilkan histori keputusan sebagai jejak event yang dapat ditelusuri.
- FR-MTR-04 Sistem menyediakan endpoint untuk mengambil hasil metrik berdasarkan filter sesi, pemain, dan ruleset.

### 7.4 Dasbor analitika (UI MVC)
- FR-DSH-01 Sistem menampilkan ringkasan performa pembelajaran dan performa misi pada level sesi (agregat).
- FR-DSH-02 Sistem menampilkan performa pembelajaran dan performa misi per pemain.
- FR-DSH-03 Sistem menampilkan histori transaksi dan histori keputusan berdasarkan event.
- FR-DSH-04 Sistem menyediakan filter dan pengelompokan berdasarkan ruleset, sesi, pemain, dan rentang waktu.
- FR-DSH-05 Sistem memperbarui tampilan setelah sistem menerima event pada setiap aksi atau akhir giliran.

---

## 8. Kebutuhan Data
Sistem menyimpan data minimum berikut:
1. Data pemain
2. Data sesi permainan
3. Data event permainan
4. Data ruleset dan versi ruleset
5. Data metrik hasil agregasi

### 8.1 Entitas minimum
- Player: `player_id`, nama/alias, atribut peran
- Session: `session_id`, waktu mulai, waktu selesai, status, mode, ruleset aktif
- Ruleset: `ruleset_id`, nama, deskripsi, status, konfigurasi
- RulesetVersion: `ruleset_version_id`, ruleset_id, nomor versi, konfigurasi, waktu dibuat, pembuat
- EventLog: `event_pk` (PK internal), `event_id` (idempotensi dari klien), session_id, player_id (boleh null untuk event sistem), timestamp, turn_number, action_type, payload, ruleset_version_id, sequence_number
- MetricSnapshot: `metric_id`, session_id, player_id (opsional), timestamp, nama metrik, nilai metrik

### 8.2 Integritas data
Sistem menjaga:
1. Keunikan event pada kombinasi `session_id` dan `event_id`.
2. Keterurutan event per sesi berdasarkan `sequence_number`.
3. Keterkaitan event dengan ruleset versi aktif untuk sesi.

---

## 9. Kebutuhan Non-Fungsional
### 9.1 Keamanan dan hak akses
- NFR-SEC-01 Sistem menerapkan akses berbasis peran untuk instruktur dan pemain.
- NFR-SEC-02 Sistem membatasi fitur pengelolaan ruleset hanya untuk instruktur.
- NFR-SEC-03 Sistem memvalidasi input untuk mencegah payload tidak valid dan manipulasi data.

### 9.2 Audit dan logging
- NFR-AUD-01 Sistem mencatat request dan hasil validasi event untuk audit.
- NFR-AUD-02 Sistem mencatat error dan exception dengan informasi yang cukup untuk debugging.
- NFR-AUD-03 Sistem menyediakan jejak event terurut untuk rekonstruksi state.

### 9.3 Kinerja
- NFR-PERF-01 Sistem merespons penerimaan event dalam waktu wajar pada skenario kelas.
- NFR-PERF-02 Sistem memperbarui metrik dan tampilan dasbor setelah menerima event pada setiap aksi atau akhir giliran.

### 9.4 Reliabilitas
- NFR-REL-01 Sistem tidak menggandakan dampak saat menerima event duplikat.
- NFR-REL-02 Sistem tetap menjaga konsistensi data saat terjadi kegagalan parsial, melalui transaksi dan mekanisme penanganan error.

---

## 10. Katalog Event Awal
Katalog berikut menjadi dasar desain payload, validasi, dan model data.

### 10.1 Event sesi
- session.created
- session.started
- session.ended

### 10.2 Event giliran
- turn.started
- turn.ended
- turn.action.used

### 10.3 Event transaksi dan arus kas
- cash.increased
- cash.decreased
- transaction.recorded

### 10.4 Event aturan harian
- day.friday.donation
- day.saturday.gold_trade

### 10.5 Event kebutuhan dan bahan
- need.primary.purchased
- need.secondary.purchased
- need.tertiary.purchased
- ingredient.purchased
- order.claimed

### 10.6 Event mode mahir
- loan.syariah.taken
- loan.syariah.repaid
- insurance.multirisk.purchased
- saving.deposit.created
- saving.deposit.withdrawn

Catatan: daftar event ini dapat bertambah saat spesifikasi desain dibuat, namun setiap event wajib mengikuti konteks minimum event dan aturan validasi.

---

## 11. Checklist Kelengkapan dan Konsistensi
Sistem dinyatakan siap masuk tahap perancangan apabila:
1. Setiap event pada katalog memiliki definisi field wajib dan aturan validasi.
2. Setiap kebutuhan fungsional memiliki kriteria uji minimum.
3. Setiap metrik pada dasbor memiliki sumber data dari event.
4. Setiap aktor memiliki daftar fitur dan hak akses yang jelas.
5. Setiap aturan domain yang berdampak pada validasi memiliki aturan validasi yang tertulis.

---


