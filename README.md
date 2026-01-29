# Cashflowpoly Dashboard & Manajemen *Ruleset*

Repositori ini memuat sistem informasi yang merekam aktivitas gim papan Cashflowpoly sebagai rangkaian *event*, memvalidasi data masuk, menyimpan data secara konsisten di PostgreSQL, lalu mengolahnya menjadi metrik literasi finansial dan capaian misi yang tampil pada dasbor web. Sistem juga menyediakan modul manajemen *ruleset* berbasis konfigurasi dinamis agar instruktur mengubah parameter permainan tanpa mengubah kode.

## Tujuan
- Menerima *event* permainan dari IDN atau simulator melalui REST API.
- Menjaga kualitas data melalui validasi skema, validasi aturan domain, idempotensi, dan keterurutan *event*.
- Mengelola *ruleset* sebagai konfigurasi variabel permainan (CRUD, *versioning*, aktivasi).
- Menghitung metrik pada level sesi dan pemain dari log *event*.
- Menyajikan analitika melalui UI web berbasis ASP.NET Core MVC (Razor Views).

## Ruang lingkup fitur (sesuai kebutuhan TA)
### Platform web
- UI berjalan di browser dan mengonsumsi data dari REST API.

### Antarmuka analitik
- Menampilkan performa pembelajaran pemain individu.
- Menampilkan performa pembelajaran agregat.
- Menampilkan performa misi pemain individu.
- Menampilkan performa misi agregat.
- Mengelompokkan data berdasarkan *ruleset* yang aktif pada sesi.

### Antarmuka manajemen aturan
- Membuat *ruleset* baru untuk mengubah variabel permainan (contoh: batas giliran, nilai uang, batas transaksi).
- Menampilkan daftar *ruleset* (termasuk *ruleset* default Cashflowpoly).
- Menghapus *ruleset* dengan pembatasan saat sistem sudah memakai *ruleset* pada sesi.
- Mengatur *ruleset* aktif per sesi (aktivasi berbasis versi).

### API back-end
- Menulis data permainan ke basis data dari *event*.
- Membaca data dari basis data untuk kebutuhan analitika dan manajemen *ruleset*.

### Basis data
- Menyimpan pemain, sesi, *ruleset* dan versi, *event*, proyeksi arus kas, *metric snapshot*, serta log validasi.

## Arsitektur tingkat tinggi
- **Cashflowpoly.Api**: REST API (ASP.NET Core 10) + Swagger UI.
- **Cashflowpoly.Ui**: MVC (Controller + Razor Views) yang memanggil REST API via `HttpClient`.
- **PostgreSQL**: penyimpanan data dan sumber kebenaran untuk analitika.

UI tidak mengakses database secara langsung. UI membaca data dari REST API agar konsisten dengan kontrak API dan skema data.

## Teknologi dan alat
| No | Perangkat lunak | Fungsi penggunaan |
|---:|---|---|
| 1 | Windows 11 Home | Sistem operasi untuk pengembangan dan pengujian |
| 2 | Visual Studio Code | IDE untuk menulis kode dan menjalankan *debug* |
| 3 | .NET 10 SDK | Toolchain untuk membangun REST API dan MVC |
| 4 | PostgreSQL | DBMS untuk menyimpan sesi, *event*, *ruleset*, proyeksi, dan metrik |
| 5 | Docker Desktop | Menjalankan seluruh komponen melalui *container* |
| 6 | DBeaver | Mengelola PostgreSQL (koneksi, skema, query, inspeksi data) |
| 7 | Google Chrome | Menguji UI MVC dan mengakses Swagger UI |
| 8 | Swagger UI (Swashbuckle) | Dokumentasi dan uji *endpoint* API dari browser |
| 9 | Postman | Uji fungsional *endpoint* API (black-box) |
| 10 | Tailwind CSS | Styling UI dasbor MVC |

## Struktur repositori

```
.
+- .env
+- .github/
+- Cashflowpoly.slnx
+- Img/
+- README.md
+- docker-compose.yml
+- database/
¦  +- 00_create_schema.sql
+- docs/
¦  +- 00-Panduan/
¦  ¦  +- 00-01-panduan-setup-lingkungan.md
¦  ¦  +- 00-02-manual-pengguna-dan-skenario-operasional.md
¦  ¦  +- 00-03-panduan-menjalankan-sistem.md
¦  +- 01-Spesifikasi/
¦  ¦  +- 01-01-spesifikasi-kebutuhan-sistem.md
¦  ¦  +- 01-02-spesifikasi-event-dan-kontrak-api.md
¦  ¦  +- 01-03-spesifikasi-ruleset-dan-validasi.md
¦  +- 02-Perancangan/
¦  ¦  +- 02-01-rancangan-model-data-dan-basis-data.md
¦  ¦  +- 02-02-definisi-metrik-dan-agregasi.md
¦  ¦  +- 02-03-rancangan-dashboard-analitika-mvc.md
¦  ¦  +- 02-04-rencana-implementasi-dan-struktur-solution-dotnet.md
¦  ¦  +- 02-05-spesifikasi-ui-mvc-dan-rancangan-viewmodel.md
¦  +- 03-Pengujian/
¦  ¦  +- 03-01-rencana-pengujian-fungsional-dan-validasi.md
¦  ¦  +- 03-02-laporan-hasil-pengujian.md
+- src/
   +- Cashflowpoly.Api/
   +- Cashflowpoly.Ui/
```

## Menjalankan dengan Docker (Docker Compose)
### 1) Jalankan semua layanan
Jalankan perintah berikut dari root repositori:

```bash
docker compose up -d --build
```

Akses (sesuai `.env`):
- API + Swagger: `http://localhost:5041/swagger`
- UI MVC: `http://localhost:5203`

### 2) Sambungkan DBeaver ke PostgreSQL
Gunakan konfigurasi berikut:
- Host: `localhost`
- Port: `5432`
- Database: `cashflowpoly`
- User: `cashflowpoly`
- Password: `cashflowpoly`

DBeaver menampilkan tabel pada schema `public` setelah PostgreSQL menjalankan `database/00_create_schema.sql` saat volume database masih kosong.

### 3) Hentikan layanan
```bash
docker compose down
```

Jika kamu ingin menghapus data database:
```bash
docker compose down -v
```

## Menjalankan lokal tanpa Docker
### 1) Siapkan PostgreSQL
Buat database dan user, lalu jalankan skrip DDL dari `database/00_create_schema.sql`.

### 2) Atur konfigurasi API dan UI
API memakai koneksi database dari `ConnectionStrings:Default`.
UI memakai base URL API dari `ApiBaseUrl`.

Contoh lokal (sesuai `.env` dan launch settings):
- API: `http://localhost:5041` atau `https://localhost:7041`
- UI: `http://localhost:5203` atau `https://localhost:7203`

### 3) Jalankan API
```bash
dotnet run --project src/Cashflowpoly.Api
```

### 4) Jalankan UI
Buka terminal baru:
```bash
dotnet run --project src/Cashflowpoly.Ui
```

## Catatan pengembangan
- Hapus *template endpoint* `GET /weatherforecast` setelah kamu menyiapkan *controller* sesuai kontrak API pada dokumen spesifikasi.
- Gunakan Swagger untuk uji cepat *endpoint* dan gunakan Postman untuk skenario uji *black-box* yang terdokumentasi.

## Dokumen desain dan spesifikasi
Simpan seluruh dokumen TA pada folder `docs/` agar repositori memuat artefak desain dan artefak implementasi pada satu tempat.

Dokumen kunci:
- Spesifikasi kebutuhan sistem (SRS): `docs/01-Spesifikasi/01-01-spesifikasi-kebutuhan-sistem.md`
- Kontrak API: `docs/01-Spesifikasi/01-02-spesifikasi-event-dan-kontrak-api.md`
- Spesifikasi *ruleset*: `docs/01-Spesifikasi/01-03-spesifikasi-ruleset-dan-validasi.md`
- Model data dan basis data: `docs/02-Perancangan/02-01-rancangan-model-data-dan-basis-data.md`
- Definisi metrik dan agregasi: `docs/02-Perancangan/02-02-definisi-metrik-dan-agregasi.md`
- Rancangan dasbor MVC: `docs/02-Perancangan/02-03-rancangan-dashboard-analitika-mvc.md`
- Rencana implementasi dan struktur solusi: `docs/02-Perancangan/02-04-rencana-implementasi-dan-struktur-solution-dotnet.md`
- Spesifikasi UI dan ViewModel: `docs/02-Perancangan/02-05-spesifikasi-ui-mvc-dan-rancangan-viewmodel.md`

## Pengujian
- Uji *endpoint* melalui Swagger UI untuk verifikasi cepat.
- Jalankan skenario pengujian fungsional melalui Postman sesuai dokumen rencana pengujian.
- Validasi dasbor dengan membandingkan metrik UI vs data pada tabel `metric_snapshots` dan proyeksi transaksi.

## Lisensi
Tentukan lisensi yang kamu pakai untuk repositori ini.

