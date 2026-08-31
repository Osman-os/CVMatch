using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CVMatch.Web.Areas.Identity.Pages.Account;

public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginWith2faModel> _logger;

    public LoginWith2faModel(SignInManager<IdentityUser> signInManager,
        ILogger<LoginWith2faModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Doğrulama kodunu girin.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Kod 6 haneli olmalıdır.")]
        [DataType(DataType.Text)]
        [Display(Name = "Doğrulama Kodu")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Bu cihazı hatırla")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var kullanici = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (kullanici is null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Admin");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid) return Page();

        var kullanici = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (kullanici is null) return RedirectToPage("./Login");

        var kod = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var sonuc = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            kod, rememberMe, Input.RememberMachine);

        if (sonuc.Succeeded)
        {
            _logger.LogInformation("İki aşamalı doğrulama tamamlandı.");
            return LocalRedirect(returnUrl);
        }

        if (sonuc.IsLockedOut)
        {
            _logger.LogWarning("Hesap geçici olarak kilitlendi.");

            ModelState.AddModelError(string.Empty,
                "Çok fazla hatalı deneme yapıldı. Hesabınız geçici olarak kilitlendi, " +
                "bir süre sonra tekrar deneyin.");

            return Page();
        }

        ModelState.AddModelError(string.Empty,
            "Doğrulama kodu hatalı. Uygulamadaki güncel kodu girin.");

        return Page();
    }
}