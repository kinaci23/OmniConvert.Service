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
| **Infrastructure** | In-memory repo, kuyruk, dosya sistemi, process runner | Core |
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

In-memory queue ve repository **singleton** olarak DI container'a kayıtlıdır.
API ve Worker aynı instance'ları paylaşır — ayrı process veya IPC gerekmez.

> **Bu model yalnızca geliştirme aşamasına özgüdür.**
> Production'da Worker ayrı bir Windows Service veya container olarak,
> gerçek bir kuyruk (RabbitMQ / Azure Service Bus) ve SQL veritabanıyla çalışacaktır.

---

## Profil Sistemi — Preset + Override

### Type-safe model

Profil sistemi string tabanlı değildir. `ColorMode` ve `CompressionType` 
Core katmanında enum olarak tanımlıdır; tüm validasyon derleme zamanında ve 
çalışma zamanında kontrol edilir.

### Preset'ler

| Preset | DPI | Renk Modu | Sıkıştırma | Kullanım |
|---|---|---|---|---|
| `OcrGray300Lzw` | 300 | Gray | LZW | OCR işlemleri |
| `OcrBinary300G4` | 300 | Binary | G4 | Siyah-beyaz OCR, en küçük boyut |
| `ArchiveColor300Lzw` | 300 | Color | LZW | Renkli arşivleme |

### Kullanıcı Override Desteği

Sadece preset ya da preset + override kombinasyonu gönderilebilir.
Enum değerleri JSON'da string olarak yazılır.
```json
// Sadece preset
{ "fileName": "belge.pdf", "profileKind": "ArchiveColor300Lzw" }

// DPI override
{ "fileName": "belge.pdf", "profileKind": "ArchiveColor300Lzw", "dpi": 600 }

// Çoklu override
{ "fileName": "belge.pdf", "profileKind": "OcrGray300Lzw",
  "dpi": 400, "colorMode": "Binary", "compression": "LZW" }
```

### Geçerli Kombinasyonlar

| Renk Modu | Sıkıştırma | Geçerli? |
|---|---|---|
| Binary | G4 | ✅ |
| Binary | LZW | ✅ |
| Gray | LZW | ✅ |
| Color | LZW | ✅ |
| Color | G4 | ❌ |
| Gray | G4 | ❌ |

Geçersiz kombinasyon → `400 Bad Request`
İzin verilen DPI: `150, 200, 300, 400, 600`

---

## V1 Pipeline Kararları

| Format | Birincil | Yedek |
|---|---|---|
| DOCX | LibreOffice Word → PDF Bridge | — |
| XLSX | Syncfusion Excel Render Merge | LibreOffice Excel → PDF Bridge |
| PDF | Ghostscript Scaled | — |
| JPEG, PNG, TIFF | RasterMagick | — |

---

## API
```
POST /api/jobs          → Job oluştur, kuyruğa ekle (202 Accepted)
GET  /api/jobs/{jobId}  → Job durumunu sorgula
```

Swagger: `https://localhost:{port}/swagger`

---

## Mevcut Durum

**Pre-production skeleton — motor entegrasyonları henüz yapılmamıştır.**

| Alan | Durum |
|---|---|
| Temiz katmanlı mimari | ✅ |
| Type-safe profil sistemi (preset + override) | ✅ |
| Pipeline seçimi ve fallback akışı | ✅ |
| Orchestrator: try/catch/finally, workspace cleanup | ✅ |
| Stub pipeline'lar (gerçek dönüşüm simüle edilir) | ✅ |
| In-memory repository ve kuyruk | ✅ |
| Output validasyonu (path + uzantı + file exists) | ✅ |
| 21 test (unit + integration) | ✅ |
| Ghostscript entegrasyonu | ⬜ |
| LibreOffice entegrasyonu | ⬜ |
| Syncfusion entegrasyonu | ⬜ |
| ImageMagick entegrasyonu | ⬜ |
| Multipart file upload | ⬜ |
| SQL veritabanı | ⬜ |
| Gerçek kuyruk altyapısı | ⬜ |

---

## Sonraki Adım

**Ghostscript entegrasyonu:** `GhostscriptScaledPipeline` içindeki `TODO` bloğu,
`IExternalProcessRunner` kullanılarak implement edilecek.
Bu entegrasyon tamamlandığında PDF → TIFF dönüşümü gerçek çıktı üretecektir.