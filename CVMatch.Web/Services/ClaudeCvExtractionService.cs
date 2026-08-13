using System.Text;
using System.Text.Json;
using CVMatch.Web.Models.Extraction;

namespace CVMatch.Web.Services;

public class ClaudeCvExtractionService : ICvExtractionService
{
    private const int MaxTextLength = 40_000;

    private readonly HttpClient _http;
    private readonly ILogger<ClaudeCvExtractionService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public ClaudeCvExtractionService(
        HttpClient http,
        IConfiguration config,
        ILogger<ClaudeCvExtractionService> logger)
    {
        _http = http;
        _logger = logger;

        _apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey yapılandırılmamış.");
        _model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
        _baseUrl = config["Anthropic:BaseUrl"] ?? "https://api.anthropic.com/v1/messages";
    }

    public async Task<CvExtractionResult> ExtractAsync(string cvText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cvText))
            return new CvExtractionResult(false, null, null, "CV metni boş.");

        if (cvText.Length > MaxTextLength)
            cvText = cvText[..MaxTextLength];

        try
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = 4000,
                system = SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = $"<cv_metni>\n{cvText}\n</cv_metni>" }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API hatası: {Status} {Body}",
                    response.StatusCode, responseBody);
                return new CvExtractionResult(false, null, null,
                    $"AI servisi yanıt vermedi ({(int)response.StatusCode}).");
            }

            var rawJson = ExtractTextContent(responseBody);
            if (string.IsNullOrWhiteSpace(rawJson))
                return new CvExtractionResult(false, null, null, "AI servisinden boş yanıt geldi.");

            rawJson = StripCodeFences(rawJson);

            var data = JsonSerializer.Deserialize<ExtractedCvData>(rawJson);
            if (data is null)
                return new CvExtractionResult(false, null, rawJson, "Yanıt çözümlenemedi.");

            return new CvExtractionResult(true, data, rawJson, null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Claude yanıtı JSON olarak çözümlenemedi.");
            return new CvExtractionResult(false, null, null, "Yanıt beklenen biçimde değil.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CV çıkarımı sırasında hata.");
            return new CvExtractionResult(false, null, null, "Beklenmeyen bir hata oluştu.");
        }
    }

    private static string ExtractTextContent(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.TryGetProperty("content", out var content))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) &&
                type.GetString() == "text" &&
                block.TryGetProperty("text", out var text))
            {
                sb.Append(text.GetString());
            }
        }

        return sb.ToString().Trim();
    }

    private static string StripCodeFences(string raw)
    {
        raw = raw.Trim();
        if (!raw.StartsWith("```")) return raw;

        var firstNewline = raw.IndexOf('\n');
        if (firstNewline < 0) return raw;

        raw = raw[(firstNewline + 1)..];

        var lastFence = raw.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence >= 0) raw = raw[..lastFence];

        return raw.Trim();
    }

    private const string SystemPrompt = """
        Sen bir CV ayrıştırma sistemisin. Sana <cv_metni> etiketleri arasında bir CV'den
        çıkarılmış ham metin verilecek. Bu metinden yapılandırılmış veri üreteceksin.

        ÇOK ÖNEMLİ KURALLAR

        1. Yalnızca geçerli JSON döndür. Açıklama, giriş cümlesi, markdown kod bloğu
           veya başka hiçbir metin ekleme. Yanıtın ilk karakteri { ve son karakteri }
           olmalı.

        2. Metin bir PDF'ten çıkarıldığı için satır sırası bozuk olabilir. İki sütunlu
           CV'lerde sol ve sağ sütun içerikleri birbirine karışmış olabilir. Kelimeler
           birbirine yapışmış olabilir (örnek: "Sertifika2023-2024"). Anlamlı yapıyı
           kurmaya çalış.

        3. CV Türkçe, İngilizce veya başka bir dilde olabilir. Alan değerlerini CV'de
           yazdığı gibi bırak, çeviri yapma.

        4. Bulamadığın alanları null bırak. Asla tahmin etme, uydurma.

        ALAN KURALLARI

        city: Adayın ikamet ettiği şehir. YALNIZCA açık bir ikamet/adres bilgisi
        varsa doldur. Şu durumlarda kesinlikle null bırak:
        - Şehir yalnızca iş deneyimlerinin yanında geçiyorsa
        - Şehir yalnızca eğitim kurumunun yanında geçiyorsa
        - Adres bölümü yoksa
        Şehir adını Türkçe yaz (Istanbul değil İstanbul)..

        totalExperienceMonths: Tüm iş deneyimlerinin toplam süresi, ay cinsinden
        tam sayı. Her deneyim için (bitiş - başlangıç) ay farkını hesapla ve
        topla. Örnek: 2012-01 ile 2017-01 arası 60 ay, 2017-01 ile 2022-01 arası
        60 ay, toplam 120. Çakışan dönemleri bir kez say. Devam eden işler için
        bugüne kadar hesapla. Hesaplayamıyorsan null bırak.

        educations[].level: CV'nin dili ne olursa olsun YALNIZCA şu değerlerden biri:
        HighSchool, AssociateDegree, BachelorDegree, MasterDegree, Doctorate
        "Lisans" ve "Bachelor's Degree" ve "BSc" → BachelorDegree
        "Yüksek Lisans" ve "Master's" ve "MSc" → MasterDegree
        "Ön Lisans" ve "Associate Degree" → AssociateDegree
        "Lise" ve "High School" → HighSchool
        "Doktora" ve "PhD" → Doctorate
        Belirleyemiyorsan null bırak.

        fullName ve school: Tümü büyük harfle yazılmışsa normal yazım düzenine
        çevir (ERKAN ALAGÖZ → Erkan Alagöz).

        startDate ve endDate: "yyyy-MM" biçiminde. Yalnızca yıl biliniyorsa ay yerine
        01 yaz (2019 → "2019-01"). Bilinmiyorsa null.

        isCurrent: Devam eden eğitim veya iş için true. "Devam ediyor", "Present",
        "Halen" gibi ifadeler bunu gösterir. true ise endDate null olmalı.

        skills: Teknik ve mesleki yetenekler. CV'de yazdığı gibi al. Dil bilgisi
        (İngilizce C2 gibi) ve sertifikaları BURAYA EKLEME.

        workExperiences[].description: Varsa kısa görev açıklaması. Yoksa null.
        Anlamsız yer tutucu metinleri (Lorem ipsum gibi) null bırak.

        ÇIKTI ŞEMASI

        {
          "fullName": string | null,
          "email": string | null,
          "phoneNumber": string | null,
          "city": string | null,
          "address": string | null,
          "linkedInUrl": string | null,
          "gitHubUrl": string | null,
          "totalExperienceMonths": number | null,
          "educations": [
            {
              "school": string | null,
              "fieldOfStudy": string | null,
              "level": string | null,
              "startDate": string | null,
              "endDate": string | null,
              "isCurrent": boolean
            }
          ],
          "workExperiences": [
            {
              "companyName": string | null,
              "position": string | null,
              "description": string | null,
              "startDate": string | null,
              "endDate": string | null,
              "isCurrent": boolean
            }
          ],
          "skills": [string]
        }
        """;
}