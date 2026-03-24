# OmniConvert.Service

Windows Service tabanlı, production-oriented TIFF dönüşüm sistemi.

## Proje Amacı

Farklı formatlardaki dosyaları (DOCX, XLSX, PDF, JPEG, PNG, TIFF) pipeline mimarisiyle TIFF formatına dönüştürür. Dönüşümler asenkron olarak işlenir; kullanıcı JobId alır ve sonucu polling ile sorgular.

## Mimari

Temiz katmanlı mimari uygulanmıştır. Her katmanın tek bir sorumluluğu vardır.
```
Core          → Domain modelleri, enum'lar, interface'ler. Hiçbir dış bağımlılık yok.
Contracts     → API request/response DTO'ları.
Application   → İş mantığı: orchestration, pipeline seçimi, handler'lar.
Conversion    → Dönüşüm pipeline'ları (LibreOffice, Ghostscript, Syncfusion, RasterMagick).
Infrastructure→ In-memory repo, kuyruk, dosya sistemi, dış süreç çalıştırıcı.
Worker        → BackgroundService: kuyruktan iş alır, orchestrator'a iletir.
Api           → ASP.NET Core Web API: job oluşturma ve durum sorgulama.
```

Worker ve Api aynı process içinde çalışır. Shared in-memory repo ve queue üzerinden iletişim kurar.

## Mevcut Durum

**Skeleton / Pre-production**

- Tüm pipeline'lar stub'dır — gerçek dönüşüm henüz uygulanmamıştır.
- Veritabanı yoktur — tüm veriler in-memory tutulur, restart'ta sıfırlanır.
- Gerçek dosya yükleme (multipart) uygulanmamıştır.
- Sistem başarıyla derlenir, ayağa kalkar ve uçtan uca akışı tamamlar.

## V1 Pipeline Kararları

| Format | Birincil | Yedek |
|--------|----------|-------|
| DOCX   | LibreOffice Word → PDF Bridge | — |
| XLSX   | Syncfusion Excel Render Merge | LibreOffice Excel → PDF Bridge |
| PDF    | Ghostscript Scaled | — |
| JPEG, PNG, TIFF | RasterMagick | — |

## API
```
POST /api/jobs          → Job oluştur, kuyruğa ekle (202 Accepted)
GET  /api/jobs/{jobId}  → Job durumunu sorgula
```

Swagger: `https://localhost:{port}/swagger`

## Sonraki Adımlar

- [ ] Ghostscript entegrasyonu — PDF → TIFF
- [ ] LibreOffice entegrasyonu — DOCX/XLSX → PDF → TIFF
- [ ] Syncfusion entegrasyonu — XLSX render
- [ ] ImageMagick entegrasyonu — raster dönüşüm
- [ ] SQL repository (EF Core)
- [ ] Multipart file upload
- [ ] Retry mekanizması
- [ ] Gerçek TIFF output doğrulaması (DPI, frame, compression)