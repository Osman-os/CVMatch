# CVMatch

CV yükleme, yapay zekâ destekli bilgi çıkarımı ve ilan-aday eşleştirme platformu.
Staj projesi olarak geliştirilmiştir.

## Ne yapar?

Adaylar açık ilanlar arasından birini seçip, üyelik açmadan CV'lerini PDF olarak yükler. Sistem CV metnini Claude API ile işleyip kişisel bilgileri, eğitim geçmişini, iş deneyimini, projeleri ve yetenekleri çıkarır. Projeler iş deneyiminden ayrı tutulur. Aday bu bilgileri kontrol edip düzelttikten sonra başvurusunu onaylar.

Yöneticiler admin panelinden başvuruları filtreleyip inceler, iş ilanları oluşturur ve ilanlara uygun adayları yetenek uyumuna göre skorlanmış biçimde görüntüler.

### Öne çıkan özellikler

* PDF'den metin ve vesikalık fotoğraf çıkarımı
* Claude API ile yapılandırılmış veri çıkarımı (eğitim, iş deneyimi, projeler, yetenek ve iletişim)
* Adayın çıkarılan bilgileri onaylamadan önce düzenleyebildiği kontrol ekranı
* KVKK onay kutuları ve başvuru sonrası düzenleme/silme hakkı
* Yetenek, şehir, deneyim, durum ve başvuru türüne göre filtrelenebilen aday listesi
* İlan bazlı eşleştirme ve yetenek uyum skoru
* Mükerrer başvuru kontrolü
* İstek sınırlama (rate limiting) ile kötüye kullanım koruması
* Panel üzerinden yetenek sözlüğü yönetimi
* Panel üzerinden yönetici ekleme ve kaldırma
* Yapay zekânın emin olamadığı alanların kontrol ekranında işaretlenmesi (ölçülmüş bir doğruluk yüzdesi değil, modelin kendi belirsizlik bildirimi)

## Teknolojiler

* .NET 10 / ASP.NET Core MVC
* Entity Framework Core
* SQL Server LocalDB
* ASP.NET Core Identity (yalnızca yönetici girişi)
* Anthropic Claude API — CV metninden yapılandırılmış veri çıkarımı
* PdfPig — PDF metin ve görsel çıkarımı
* PDFtoImage + SkiaSharp — CV ilk sayfasının JPEG önizlemesi
* Bootstrap 5

## Kurulum

### Gereksinimler

* Windows (PDF önizleme üretimi Windows'a bağımlıdır)
* .NET 10 SDK
* SQL Server LocalDB
* Anthropic API anahtarı

### 1. Depoyu klonlayın

```bash
git clone https://github.com/Osman-os/CVMatch.git
cd CVMatch
```

### 2. Yapılandırma dosyasını oluşturun

`CVMatch.Web/appsettings.Development.json` dosyasını oluşturun:

```json
{
  "SeedAdmin": {
    "Email": "admin@cvmatch.local",
    "Password": "ORNEK-PAROLA-DEGISTIRIN"
  },
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-sonnet-5",
    "BaseUrl": "https://api.anthropic.com/v1/messages"
  }
}
```

Yukarıdaki parola yalnızca örnektir. İlk çalıştırmadan önce kendi parolanızla değiştirin.

`appsettings.Development.json` dosyası `.gitignore` içindedir ve depoya gönderilmez.

`Model` değeri kullanılan Claude model kimliğidir. Farklı bir model kullanmak isterseniz bu alanı kullandığınız model kimliğiyle değiştirebilirsiniz.

### 3. Dosya depolama yolunu ayarlayın

`CVMatch.Web/appsettings.json` içinde `FileStorage` bölümünü ayarlayın:

```json
{
  "FileStorage": {
    "RootPath": "C:\\CVMatchStorage"
  }
}
```

Yüklenen CV'ler `wwwroot` dışında saklanır ve statik dosya olarak sunulmaz. Erişim yalnızca kontrollü action'lar üzerinden olur: aday tarafında geçerli taslak bağlantısı, yönetici tarafında Identity yetkilendirmesi gerekir.

Belirtilen klasörün var olduğundan emin olun.

### 4. Bağımlılıkları yükleyin ve projeyi derleyin

Proje kök dizininde:

```bash
dotnet restore
dotnet build
```

### 5. Veritabanını oluşturun

Proje kök dizininde:

```bash
dotnet ef database update --project CVMatch.Web --startup-project CVMatch.Web
```

`dotnet ef` komutu sisteminizde bulunmuyorsa önce EF Core CLI aracını yükleyin:

```bash
dotnet tool install --global dotnet-ef
```

Ardından veritabanı komutunu tekrar çalıştırın.

### 6. Uygulamayı çalıştırın

Proje kök dizininde:

```bash
dotnet run --project CVMatch.Web
```

Uygulama ilk açılışta:

* 81 şehri
* 40 yeteneği
* Yönetici kullanıcısını

veritabanına ekler.

## Ekran akışı

### Aday tarafı

CV yükle → işleniyor → bilgileri kontrol et → özet ve KVKK onayları → başvuru tamamlandı → düzenleme bağlantısıyla düzenle veya sil

### Yönetici tarafı

Giriş → genel bakış → aday listesi (filtreli) → aday detayı → ilan yönetimi → eşleştirme sonuçları → yetenek yönetimi → yönetici yönetimi

## Mimari kararlar

### Aday üyeliği yoktur

ASP.NET Core Identity yalnızca yönetici girişi için kullanılır. Adayların sisteme kayıt olması veya giriş yapması gerekmez.

Adaylar başvurularına, başvuru tamamlandıktan sonra kendilerine verilen düzenleme bağlantısıyla erişir.

Yönetici hesapları yalnızca panel üzerinden, mevcut bir yönetici tarafından oluşturulur. 

Identity'nin kayıt sayfası kapalıdır; panelden eklenen hesaplar e-posta doğrulaması beklemeden giriş yapabilir. 

Sistemde her zaman en az bir yönetici bulunmalıdır.

### Taslak / onay ayrımı

Yapay zekâ tarafından çıkarılan bilgiler doğrudan kalıcı aday kayıtlarına yazılmaz.

Aday bilgileri onaylayana kadar çıkarım sonucu `CvSubmission.ExtractedJson` alanında taslak olarak tutulur.

Aday başvuruyu onayladığında `CandidateProfile` ve ilişkili kayıtlar oluşturulur.

Bu sayede hatalı veya eksik bir yapay zekâ çıkarımı doğrudan kalıcı veri kirliliğine yol açmaz.

### Düzenleme anahtarı hash'lenerek saklanır

Ham düzenleme anahtarı yalnızca başvuru tamamlandı ekranında bir kez gösterilir.

Veritabanında anahtarın SHA-256 özeti saklanır. Düzenleme bağlantısı 30 gün boyunca geçerlidir.

Kaybedilen düzenleme anahtarı yeniden oluşturulamaz veya kurtarılamaz.

### Eşleştirme skoru veritabanında tutulmaz

Eşleştirme skorları her istekte yeniden hesaplanır.

Skor yalnızca yetenek uyumundan oluşur:

* Zorunlu yetenekler: 2 puan
* Tercih edilen yetenekler: 1 puan

Şehir, deneyim ve çalışma türü uyumu skoru etkilemez. Bu bilgiler yöneticinin karar vermesini kolaylaştırmak amacıyla ayrıca gösterilir.

Bu yapı sayesinde ilan gereksinimleri değiştirildiğinde adayların eşleştirme skorları da otomatik olarak güncellenir.

### Mükerrer başvuru kontrolü

Aynı ilana, aynı e-posta adresi veya normalleştirilmiş telefon numarasıyla, düzenleme süresi henüz dolmamış ikinci bir başvuru oluşturulmaz.

Düzenleme süresi dolmuş başvurular yeni başvuru yapılmasını engellemez. Böylece adayın kalıcı olarak yeni başvuru yapmasının önüne geçilmez.

### Kötüye kullanım koruması

Aday tarafındaki uçlar kimlik doğrulaması gerektirmediğinden IP bazlı istek sınırlaması
uygulanır: CV yükleme saatte 20, yapay zekâ çıkarımı saatte 30, yönetici girişi 15
dakikada 10 istekle sınırlıdır.

Yüklenen PDF'lerde dosya imzası doğrulanır, boyut 10 MB ile sınırlıdır ve metin
çıkarımı en fazla 30 sayfa okur. Bir başvuruya en fazla 50 yetenek kaydedilir.

Eşzamanlı istekler durum kontrolü ve `CvSubmission.RowVersion` sürüm damgası
birlikte denetlenir: ikinci istek güncel durumu görüp işlemi atlar ya da aynı eski
sürümle kaydetmeye çalışırsa concurrency çakışmasına düşer. Böylece aynı taslak
üzerinde mükerrer kayıt oluşturma ve tekrarlanan yapay zekâ çağrıları önlenir.
### Veri bütünlüğü

Tüm foreign key ilişkilerinde `DeleteBehavior.Restrict` kullanılır.

Enum değerleri veritabanı kararlılığı için 1'den başlar.

## Proje yapısı

```text
CVMatch.Web/
├── Controllers/       Cv, Admin, JobPostings, Home
├── Data/              DbContext, migration'lar, seed
├── Models/
│   ├── Entities/      Veritabanı varlıkları
│   ├── Enums/         Durum ve tür sabitleri
│   ├── Extraction/    Yapay zekâ çıktı şeması
│   ├── Validation/    Özel doğrulama öznitelikleri
│   └── ViewModels/    Görünüm modelleri
├── Services/          Dosya depolama, PDF işleme, çıkarım, eşleştirme
├── Views/             Razor görünümleri
└── wwwroot/
    ├── css/           admin.css (yönetim), public.css (aday)
    └── js/            cv-review.js (form etkileşimleri)

CVMatch.Tests/         PDF çıkarımı ve taslak erişim kuralı testleri
```

## Testler

Proje kök dizininde:

```bash
dotnet test
```

Test projesi 18 test içerir:

* PDF metin çıkarımı ve 30 sayfa okuma sınırı
* CV önizleme üretimi
* PDF içerisinden görsel ve fotoğraf çıkarımı
* Taslak bağlantısı erişim kuralları (süre dolması, onay sonrası erişim)
* `Cv/File` action'ının erişim denetimi
* Proje kayıtlarının çıkarımdan forma aktarımı ve iş deneyiminden ayrı tutulması
* İş deneyimi tarih doğrulama kuralları

## Proje durumu

Aday başvuru akışı, yönetici paneli ve ilan-aday eşleştirmesi tamamlanmıştır.

Uygulama uçtan uca çalışır durumdadır.

## Bilinen sınırlar

* Taranmış veya yalnızca görüntü içeren CV'lerden metin çıkarılamaz. OCR proje kapsamı dışında tutulmuştur.
* Mükerrer başvuru kontrolü aynı ilan için yapılır; aday farklı ilanlara ayrı ayrı başvurabilir. Kontrol hem e-posta hem telefon değiştirilerek aşılabilir. Aday tarafında kimlik doğrulaması bulunmadığından kesin engelleme mümkün değildir; kontrolün amacı kazara oluşan tekrarları azaltmaktır.
* Aday kaydı ile başvuru aynı varlıkta (`CandidateProfile`) tutulur. Bu yapı, adayın tek bir havuza başvurduğu ilk tasarımdan gelir. İlan bazlı başvuruya geçildiğinde, aynı kişi birden fazla ilana başvurduğunda kişisel bilgileri, yetenekleri, eğitim ve deneyim kayıtları her başvuru için ayrı ayrı saklanır hâle gelmiştir. Yönetici panelindeki aday listesi bu kayıtları e-postaya göre gruplayarak tek satırda gösterir, ancak veritabanındaki tekrar sürer: aday bir başvurusunu düzenlediğinde diğer başvurusundaki bilgiler güncellenmez.
* Bunun doğru çözümü kişi ile başvurunun ayrı varlıklara bölünmesidir (`CandidateProfile` kişiyi, ayrı bir `Application` varlığı kişi–ilan bağını tutar). Bu değişiklik yeni bir varlık, veri taşıma ve eşleştirme ile yönetici panelinin önemli bölümünün yeniden yazılmasını gerektirdiğinden proje kapsamı dışında bırakılmıştır.
* Yetenek adları serbest metin olarak girilebildiğinden, yetenek sözlüğünde bulunmayan farklı yazımlar ayrı kayıtlar oluşturabilir. Yönetici panelindeki yetenek ekranından kullanımda olmayan kayıtlar temizlenebilir.
* Eşleştirme hesabı tüm aday havuzu üzerinde bellekte yapılır. Staj projesi ölçeğinde
  sorun oluşturmaz; çok büyük veri kümelerinde hesaplamanın veritabanı tarafına
  taşınması gerekir.
* CV önizlemesi üretimi Windows'a bağımlıdır; Linux veya macOS üzerinde farklı bir
  render kütüphanesi gerekir.
* İstek sınırlaması uygulama belleğinde tutulur. Birden fazla sunucu örneğiyle
  çalıştırılacaksa dağıtık bir sayaç (örneğin Redis) gerekir.
* Başvuru silindiğinde önce veritabanı kayıtları, sonra diskteki dosyalar kaldırılır.
  Dosya silme başarısız olursa kayıt zaten silinmiş olduğundan sahipsiz dosya diskte
  kalabilir; bu durumda manuel temizlik gerekir.
