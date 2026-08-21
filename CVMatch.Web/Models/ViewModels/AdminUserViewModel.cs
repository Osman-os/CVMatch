using System.ComponentModel.DataAnnotations;

namespace CVMatch.Web.Models.ViewModels;

public class AdminUserListViewModel
{
    public List<YoneticiSatiri> Yoneticiler { get; set; } = new();
    public YeniYoneticiInputModel Yeni { get; set; } = new();
}

public class YoneticiSatiri
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool KendisiMi { get; set; }
}

public class YeniYoneticiInputModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(256)]
    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Parola zorunludur.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Parola en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor.")]
    [Display(Name = "Parola (Tekrar)")]
    public string ConfirmPassword { get; set; } = null!;
}