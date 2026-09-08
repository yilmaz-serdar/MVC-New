# MVC-New — arka plan analizi ve aşamalı topoloji

`MVC` projesinin bağımsız .NET 7.0 kopyasıdır. Orijinal MVC dosyaları değiştirilmez.
React/npm veya frontend build adımı gerekmez; Cytoscape.js yerelden yüklenir.

```powershell
cd MVC-New
dotnet run
```

Adres: http://localhost:5090

## Akış

1. Servis adı, son 1/3/7 gün ve sonuç e-posta adresi girilir.
2. `POST /Analysis/Analyze` sunucuda doğrular, anti-forgery token kontrol eder,
   rastgele GUID ile isteği diske kaydeder ve başlatıldı ekranına yönlendirir.
3. `AnalysisWorker` isteği arka planda işler. Sonuç ve durum diske kaydedilir;
   e-postada sonuç sayfası bağlantısı ve servis yanıtı JSON eki bulunur.
4. `GET /Analysis/Analyze/{id}` veya `/Analysis/Analyze?id={id}` kaydedilmiş sonucu
   mevcut Razor analiz tasarımında gösterir. Bu GET yeni analiz başlatmaz.
5. Henüz tamamlanmayan işlerde durum ekranı; bulunamayan ID için 404 gösterilir.

Kayıtlar `App_Data/Jobs`, test e-postaları `App_Data/Mail` dizinindedir. Bu dizinler
statik servis, Git ve publish çıktısının dışındadır. Uygulama yeniden başladığında
bekleyen işler devam eder ve eski ID'ler geçerliliğini korur. Dağıtımda bu dizini
kalıcı diskte tutun. Mevcut dosya deposu tek uygulama örneği içindir; çoklu örnekte
veritabanı ve iş kuyruğu sağlayıcısı kullanılmalıdır.

Analiz hatası ve e-posta hatası ayrı tutulur. E-posta başarısız olsa da tamamlanmış
sonuç ID üzerinden açılır. E-posta hatası otomatik tekrar denenmez. İşlem tam e-posta
gönderiminden sonra kesilirse yeniden başlamada tekrar gönderim olabilir.

## E-posta

Varsayılan **Pickup** test modudur: gerçek e-posta gönderilmez; `.eml` dosyası hazırlanır.
Durum ekranı bunu açıkça belirtir. Canlı SMTP için `appsettings.json` veya ortam
değişkenlerinden aşağıdaki ayarları verin:

| Anahtar | Değer |
| --- | --- |
| `Email:Mode` | `Smtp` |
| `Email:Host` | SMTP sunucunuz |
| `Email:Port` | Genellikle 587 |
| `Email:EnableSsl` | `true` |
| `Email:From` | Yetkili gönderici adresi |
| `Email:Username` | SMTP kullanıcı adı |
| `Email:Password` | Ortam değişkeni/secret store üzerinden parola |
| `Email:PublicBaseUrl` | Dışarıdan erişilen uygulama adresi; ör. `https://analiz.firma.com` |

Ortam değişkeni isimlerinde `:` yerine `__` kullanılır (`Email__Password` gibi).
Parolaları Git'e eklemeyin. Mail bağlantısı istek Host başlığından üretilmez;
`PublicBaseUrl` yapılandırmasından gelir. Sonuç ID bağlantısı erişim anahtarı
niteliğindedir; mevcut kurumsal kimlik doğrulama/iş sahipliği kontrolünüzü controller'a
entegre edin. Destek bağlantısı: `mailto:support@abc.com`.

## Topoloji

Başlangıçta istenen servis ve bağlantısız bileşenlerin başlangıç düğümleri görünür;
bağlantılar kapalıdır. Bir düğüme tıklandığında yalnızca **doğrudan** çağırdığı
servisler ve bu çağrıların `callType` etiketli kenarları eklenir. Yeni düğümlere
tıklayarak zincir ilerletilir. Tekrar tıklama öğeleri çoğaltmaz. Ortak hedefler,
paralel çağrılar, döngüler ve bağlantısız düğümler desteklenir. Konumları Cytoscape
otomatik hesaplar. Bağlantıların metin görünümü erişilebilir alternatif olarak korunur.

Aktif sağlayıcı hâlâ `JsonAnalysisService` ve eklenen örnek yanıttır; canlı analiz
API'si bağlı değildir. Servis adı ve süre örnek dosyayı filtrelemez. Gerçek sağlayıcıyı
`IAnalysisService` üzerinden değiştirin. Eski demo sınıfları kopyada korunmuştur.

## Kontroller

```powershell
dotnet build
dotnet run --project Tests/TopologyTests.csproj
node Tests/cytoscape.test.cjs
node Tests/explorer.test.cjs
# localhost:5090 üzerinde yalnızca Pickup modunda çalışan uygulama için:
node Tests/http.test.cjs
```

Node testleri için Node.js 22+ gerekir. HTTP testi yerel analiz ve `.eml` kayıtları
oluşturur; SMTP modunda çalıştırılmamalıdır. Tarayıcıda görsel test yapılmamıştır.

Yayın: `dotnet publish -c Release -o publish`. .NET 7 hedefi önceki isteğe uygun
korunmuştur; SDK destek süresi uyarısı verebilir.
