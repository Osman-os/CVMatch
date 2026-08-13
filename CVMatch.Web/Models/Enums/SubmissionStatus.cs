namespace CVMatch.Web.Models.Enums;

public enum SubmissionStatus
{
    Uploaded = 1,        // dosya alındı, işlenmeyi bekliyor
    Processing = 2,      // metin çıkarma + AI çağrısı sürüyor
    AwaitingReview = 3,  // taslak hazır, aday onayı bekleniyor
    Approved = 4,        // aday onayladı, kalıcı kayıt oluştu
    Failed = 5           // işleme hatası
}