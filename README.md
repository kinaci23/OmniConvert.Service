# OmniConvert.Service

Windows Service tabanlı, production-oriented TIFF dönüşüm sistemi.

## Proje Amacı

Farklı formatlardaki dosyaları (DOCX, XLSX, PDF, JPEG, PNG, TIFF) 
pipeline mimarisiyle TIFF formatına dönüştürür.
Dönüşümler asenkron olarak işlenir; kullanıcı JobId alır ve sonucu polling ile sorgular.

---

## Mimari

Temiz katmanlı mimari uygulanmıştır. Her katmanın tek bir sorumluluğu vardır.

| Katman | Sorumluluk | Bağımlılık |
|---|---|---|
| **Core** | Domain modelleri, enum'lar, interface'ler | Hiçbir şeye bağımlı değil |
| **Contracts** | API request/response DTO'ları | Hiçbir şeye bağımlı değil |
| **Application** | İş mantığı: orchestration, profile resolver, handler'lar | Core, Contracts |
| **Conversion** | Dönüşüm pipeline'ları | Core |
| **Infrastructure** | In-memory repo, kuyruk, dosya sistemi, process runner | Core |
| **Worker** | BackgroundService: kuyruktan iş alır, orchestrator'a iletir | Application, Infrastructure, Conversion |
| **Api** | ASP.NET Core Web API: iş oluşturma ve durum sorgulama | Hepsi |

---

## Çalışma Modeli (Geliştirme Aşaması)

**API ve Worker aynı host içinde çalışır.**
```
OmniConvert.Service.Api  ← Ana entry point (F5 ile başlatılan)
    ├── JobsController    ← HTTP istekleri
    └── ConversionWorker  ← BackgroundService olarak kayıtlı
```

In-memory queue ve repository singleton olarak DI'a kayıtlıdır.
API ve Worker aynı instance'ları paylaşır — ayrı process gerektirmez.

> **Not:** Bu model yalnızca geliştirme aşaması içindir.
> Production'da Worker ayrı bir host/Windows Service olarak,
> gerçek bir kuyruk (RabbitMQ, Azure Service Bus) ve veritabanıyla çalışacaktır.

---

## Profil Sistemi

### Preset'ler

| Preset | DPI | Renk Modu | Sıkıştırma | Kullanım |
|---|---|---|---|---|
| `OcrGray300Lzw` | 300 | Gray | LZW | OCR işlemleri |
| `OcrBinary300G4` | 300 | Binary | G4 | Siyah-beyaz OCR, en küçük boyut |
| `ArchiveColor300Lzw` | 300 | Color | LZW | Renkli arşivleme |

### Kullanıcı Override Desteği

Kullanıcı sadece preset gönderebilir, ya da preset + override kombinasyonu kullanabilir.
```json
// Sadece preset
{ "fileName": "belge.pdf", "profileKind": "ArchiveColor300Lzw" }

// Preset + DPI override
{ "fileName": "belge.pdf", "profileKind": "ArchiveColor300Lzw", "dpi": 600 }

// Preset + çoklu override
{ "fileName": "belge.pdf", "profileKind": "OcrGray300Lzw", "dpi": 400, "colorMode": "Binary", "compression": "LZW" }
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

Geçersiz kombinasyon gönderilirse API `400 Bad Request` döner.
İzin verilen DPI değerleri: `150, 200, 300, 400, 600`

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

- ✅ Temiz katmanlı mimari
- ✅ Preset + override profil sistemi
- ✅ Pipeline seçimi ve fallback akışı
- ✅ Orchestrator: try/catch/finally, workspace cleanup
- ✅ Stub pipeline'lar (gerçek dönüşüm simüle edilir)
- ✅ In-memory repository ve kuyruk
- ⬜ Ghostscript entegrasyonu
- ⬜ LibreOffice entegrasyonu
- ⬜ Syncfusion entegrasyonu
- ⬜ ImageMagick entegrasyonu
- ⬜ Multipart file upload
- ⬜ SQL veritabanı
- ⬜ Gerçek kuyruk altyapısı

---

## Sonraki Adım

Gerçek Ghostscript entegrasyonu: `GhostscriptScaledPipeline` içindeki
`TODO` bloğu, `IExternalProcessRunner` kullanılarak implement edilecek.