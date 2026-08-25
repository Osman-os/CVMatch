using System.ComponentModel.DataAnnotations;

namespace CVMatch.Web.Models.ViewModels;

public class AdminSkillListViewModel
{
    public List<YetenekSatiri> Yetenekler { get; set; } = new();
    public YeniYetenekInputModel Yeni { get; set; } = new();
}

public class YetenekSatiri
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int AdaySayisi { get; set; }
    public int IlanSayisi { get; set; }

    // Kullanımdaki yetenek silinemez; veri bütünlüğü korunur
    public bool SilinebilirMi => AdaySayisi == 0 && IlanSayisi == 0;
}

public class YeniYetenekInputModel
{
    [Required(ErrorMessage = "Yetenek adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Yetenek adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Yetenek adı")]
    public string? Name { get; set; }
}