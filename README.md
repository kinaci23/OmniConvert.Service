# OmniConvert.Service

Windows Service tabanlı, production-oriented TIFF dönüşüm sistemi.

## Proje Amacı

Farklı formatlardaki dosyaları (DOCX, XLSX, PDF, JPEG, PNG, TIFF)
pipeline mimarisiyle TIFF formatına dönüştürür.
Dönüşümler asenkron olarak işlenir; kullanıcı JobId alır ve sonucu polling ile sorgular.

---

## Mimari

| Katman | Sorumluluk | Bağımlılık |
|---|---|---|
| **Core** | Domain modelleri, enum'lar, interface'ler | Hiçbir şeye bağımlı değil |
| **Contracts** | API request/response DTO'ları | Core |
| **Application** | İş mantığı: orchestration, profile resolver, handler'lar | Core, Contracts |
| **Conversion** | Dönüşüm pipeline'ları | Core |
| **Infrastructure** | In-memory repo, kuyruk, dosya sistemi, process runner, concurrency | Core |
| **Worker** | BackgroundService: kuyruktan iş alır, orchestrator'a iletir | Application, Infrastructure, Conversion |
| **Api** | ASP.NET Core Web API: iş oluşturma ve durum sorgulama | Hepsi |

---

## Çalışma Modeli — Geliştirme Aşaması

**API ve Worker aynı host process içinde çalışır.**
```
OmniConvert.Service.Api   ← F5 ile başlatılan tek entry point
    ├── JobsController     ← HTTP isteklerini karşılar
    └── ConversionWorker   ← BackgroundService olarak kayıtlı
```

In-memory queue, repository ve concurrency limiter singleton olarak DI'a kayıtlıdır.

> **Bu model yalnızca geliştirme aşamasına özgüdür.**
> Production'da Worker ayrı bir Windows Service, gerçek kuyruk ve SQL veritabanıyla çalışacaktır.

---

## Motor Entegrasyonları

Tüm primary ve fallback conversion motor entegrasyonları tamamlanmıştır.

| Format | Birincil | Yedek |
|---|---|---|
| DOCX | LibreOffice Word → PDF Bridge | — |
| XLSX | Syncfusion Excel Render Merge | LibreOffice Excel → PDF Bridge |
| PDF | Ghostscript Scaled | — |
| JPEG, PNG, TIFF | RasterMagick | — |

---

## Production Hardening

Sistemin operasyonel dayanıklılığı aşağıdaki mekanizmalarla güçlendirilmiştir:

**Concurrency Control:**
Pipeline bazlı SemaphoreSlim ile eş zamanlı iş limiti uygulanır.
Toplam aktif iş limiti de ayrıca kontrol edilir.

| Pipeline | Limit |
|---|---|
| Ghostscript | 2 |
| RasterMagick | 2 |
| LibreOffice Word | 1 |
| Syncfusion Excel | 1 |
| LibreOffice Excel | 1 |
| **Toplam** | **4** |

**Timeout Policy:**
Her pipeline için config tabanlı timeout tanımlanmıştır.
Timeout aşılırsa `FailureCategory.Timeout` ile job Failed olur.

**Fallback Akışı:**
XLSX dönüşümünde Syncfusion başarısız olursa LibreOffice devreye girer.
`FailureCategory.Validation` ve `UnsupportedFormat` durumlarında fallback denenmez.

**Cleanup Garantisi:**
Temp workspace, job başarılı/başarısız/iptal edilmiş olsa da `finally` bloğunda temizlenir.

**Structured Logging:**
Her iş için JobId, Format, Pipeline, Fallback, ElapsedMs, FailureCategory loglanır.

---

## Profil Sistemi — Preset + Override

### Type-safe model

`ColorMode` ve `CompressionType` Core katmanında enum olarak tanımlıdır.

### Preset'ler

| Preset | DPI | Renk Modu | Sıkıştırma |
|---|---|---|---|
| `OcrGray300Lzw` | 300 | Gray | LZW |
| `OcrBinary300G4` | 300 | Binary | G4 |
| `ArchiveColor300Lzw` | 300 | Color | LZW |

---

## API
```
POST /api/jobs          → Job oluştur, kuyruğa ekle (202 Accepted)
GET  /api/jobs/{jobId}  → Job durumunu sorgula
```

Swagger: `https://localhost:{port}/swagger`

---

## Mevcut Durum

| Alan | Durum |
|---|---|
| Temiz katmanlı mimari | ✅ |
| Type-safe profil sistemi (preset + override) | ✅ |
| Pipeline seçimi ve fallback akışı | ✅ |
| Concurrency control (pipeline bazlı + toplam) | ✅ |
| Timeout policy (config tabanlı) | ✅ |
| Cleanup garantisi (finally bloğu) | ✅ |
| Structured logging (JobId, ElapsedMs, Category) | ✅ |
| TIFF çıktı doğrulaması (magic bytes + IFD0) | ✅ |
| Ghostscript entegrasyonu — PDF → TIFF | ✅ |
| RasterMagick entegrasyonu — JPEG/PNG/TIFF → TIFF | ✅ |
| LibreOffice Word — DOCX → TIFF | ✅ |
| Syncfusion Excel — XLSX → TIFF | ✅ |
| LibreOffice Excel fallback — XLSX → TIFF | ✅ |
| Multipart file upload | ⬜ |
| SQL veritabanı | ⬜ |
| Gerçek kuyruk altyapısı | ⬜ |

---

## Sonraki Adım

Multipart file upload: `sourceFilePath` geçici alanı kaldırılacak,
gerçek HTTP binary dosya yükleme implement edilecek.