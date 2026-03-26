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
| **Api** | ASP.NET Core Web API + Windows Service host | Hepsi |

---

## Çalışma Modları

### Development Modu
Visual Studio'da F5 veya terminalde `dotnet run` ile başlatılır.
Swagger UI, HTTPS ve debug logging etkindir.
```
cd OmniConvert.Service.Api
dotnet run
```

### Windows Service Modu
Uygulama gerçek bir Windows Service olarak çalışır.
Swagger UI devre dışı, EventLog logging etkin, HTTP (5000 portu) üzerinden erişilir.
```
# Publish
dotnet publish OmniConvert.Service.Api -c Release -o C:\OmniConvert\Service

# Service oluştur (Yönetici olarak)
sc create OmniConvert.Service binPath="C:\OmniConvert\Service\OmniConvert.Service.Api.exe" start=auto

# Service'i başlat
sc start OmniConvert.Service

# Service'i durdur
sc stop OmniConvert.Service

# Service'i sil
sc delete OmniConvert.Service
```

---

## API
```
POST /api/jobs          → Multipart upload ile yeni iş oluştur (202 Accepted)
GET  /api/jobs/{jobId}  → İş durumunu ve sonucunu sorgula
```

### Multipart Upload

Dosya `multipart/form-data` formatında gönderilir — kullanıcı dosya path'i vermez,
dosyayı doğrudan binary olarak upload eder. API dosyayı storage'a güvenli biçimde kaydeder.

**Form alanları:**
- `file` (zorunlu) — dönüştürülecek dosya
- `profileKind` (zorunlu) — `OcrGray300Lzw` / `OcrBinary300G4` / `ArchiveColor300Lzw`
- `dpi` (opsiyonel) — DPI override
- `colorMode` (opsiyonel) — `Binary` / `Gray` / `Color`
- `compression` (opsiyonel) — `None` / `LZW` / `G4` / `Jpeg`

**İzin verilen uzantılar:** `.pdf`, `.png`, `.jpg`, `.jpeg`, `.tif`, `.tiff`, `.docx`, `.xlsx`

**Maksimum dosya boyutu:** 50 MB (appsettings üzerinden değiştirilebilir)

Swagger: `https://localhost:{port}/swagger` (yalnızca Development modunda)

---

## Motor Entegrasyonları

| Format | Birincil | Yedek | Durum |
|---|---|---|---|
| DOCX | LibreOffice Word → PDF Bridge | — | ✅ |
| XLSX | Syncfusion Excel Render Merge | LibreOffice Excel → PDF Bridge | ✅ |
| PDF | Ghostscript Scaled | — | ✅ |
| JPEG, PNG, TIFF | RasterMagick | — | ✅ |

---

## Production Hardening

**Concurrency Control:** Pipeline bazlı SemaphoreSlim ile eş zamanlı iş limiti.

| Pipeline | Limit |
|---|---|
| Ghostscript | 2 |
| RasterMagick | 2 |
| LibreOffice Word | 1 |
| Syncfusion Excel | 1 |
| LibreOffice Excel | 1 |
| **Toplam** | **4** |

**Timeout:** Her pipeline için config tabanlı timeout. Aşılırsa `FailureCategory.Timeout`.

**Fallback:** XLSX'te Syncfusion başarısızsa LibreOffice devreye girer.

**TIFF Doğrulama:** Magic bytes + IFD0 frame kontrolü.

**Cleanup:** Temp workspace her durumda `finally` bloğunda silinir.

**Logging:** Structured logging. Development'da Console, Windows Service modunda EventLog.

---

## Konfigürasyon

`appsettings.json` (production) ve `appsettings.Development.json` (development) üzerinden yönetilir.

| Bölüm | Alan | Açıklama |
|---|---|---|
| `Storage` | `BasePath` | Job dosyalarının saklandığı kök dizin |
| `Upload` | `MaxFileSizeBytes` | Maksimum upload boyutu (varsayılan 50 MB) |
| `Ghostscript` | `Path`, `TimeoutSeconds` | GS executable ve timeout |
| `LibreOffice` | `Path`, `TimeoutSeconds` | LO executable ve timeout |
| `Concurrency` | Pipeline limitleri | Her pipeline için eş zamanlı limit |
| `Urls` | — | Dinlenen adres (production: `http://localhost:5000`) |

---

## Kurulum — Windows Service

### Gereksinimler

- .NET 8 Runtime
- Ghostscript 10.x — [ghostscript.com](https://www.ghostscript.com)
- LibreOffice 7.x+ — [libreoffice.org](https://www.libreoffice.org)
- Syncfusion lisansı (XlsIO + XlsIORenderer)

### Adım Adım Kurulum

**1. Publish**
```bash
dotnet publish OmniConvert.Service.Api -c Release -r win-x64 --self-contained false -o C:\OmniConvert\Service
```

**2. appsettings.json Düzenle**

`C:\OmniConvert\Service\appsettings.json` içinde şunları kendi ortamına göre ayarla:
```json
{
  "Storage": { "BasePath": "C:\\OmniConvert\\jobs" },
  "Ghostscript": { "Path": "C:\\Program Files\\gs\\gs10.06.0\\bin\\gswin64c.exe" },
  "LibreOffice": { "Path": "C:\\Program Files\\LibreOffice\\program\\soffice.exe" }
}
```

**3. Storage Klasörü Oluştur ve İzin Ver**
```bash
mkdir C:\OmniConvert\jobs
# Service hesabına (LocalSystem veya özel hesap) bu klasöre yazma izni ver
icacls "C:\OmniConvert\jobs" /grant "NETWORK SERVICE:(OI)(CI)F"
```

**4. Windows Service Kur (Yönetici PowerShell)**
```powershell
sc.exe create OmniConvert.Service `
  binPath="C:\OmniConvert\Service\OmniConvert.Service.Api.exe" `
  DisplayName="OmniConvert Service" `
  start=auto

sc.exe description OmniConvert.Service "TIFF donusum servisi"
sc.exe start OmniConvert.Service
```

**5. Doğrula**
```bash
# Service durumunu kontrol et
sc query OmniConvert.Service

# API'ye bağlan
curl http://localhost:5000/api/jobs
```

**6. Log İzleme**

Windows Olay Görüntüleyicisi → Windows Günlükleri → Uygulama → Kaynak: `OmniConvert.Service`

### Service Yönetimi
```powershell
sc.exe stop OmniConvert.Service    # Durdur
sc.exe start OmniConvert.Service   # Başlat
sc.exe delete OmniConvert.Service  # Kaldır (önce durdur)
```

---

## Profil Sistemi

| Preset | DPI | Renk Modu | Sıkıştırma |
|---|---|---|---|
| `OcrGray300Lzw` | 300 | Gray | LZW |
| `OcrBinary300G4` | 300 | Binary | G4 |
| `ArchiveColor300Lzw` | 300 | Color | LZW |

---

## Mevcut Durum

| Alan | Durum |
|---|---|
| Temiz katmanlı mimari | ✅ |
| Type-safe profil sistemi (preset + override) | ✅ |
| Ghostscript — PDF → TIFF | ✅ |
| RasterMagick — JPEG/PNG/TIFF → TIFF | ✅ |
| LibreOffice Word — DOCX → TIFF | ✅ |
| Syncfusion Excel — XLSX → TIFF | ✅ |
| LibreOffice Excel fallback — XLSX → TIFF | ✅ |
| TIFF çıktı doğrulaması (magic bytes + IFD0) | ✅ |
| Concurrency control (pipeline bazlı + toplam) | ✅ |
| Timeout policy (config tabanlı) | ✅ |
| Cleanup garantisi (finally bloğu) | ✅ |
| Structured logging | ✅ |
| Multipart file upload | ✅ |
| Windows Service desteği | ✅ |
| SQL veritabanı (EF Core) | ⬜ |
| Gerçek kuyruk (RabbitMQ / Azure Service Bus) | ⬜ |
| Service vs Benchmark performans karşılaştırması | ⬜ |

---

## Sonraki Adım

**Service vs Benchmark performans karşılaştırması:**
Aynı dosyalar için OmniConvert.Service ve BenchmarkLab sonuçları
(ElapsedMs, dosya boyutu, kalite) karşılaştırılacak.