using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CVMatch.Web.Areas.Identity.Pages.Account;

public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(SignInManager<IdentityUser> signInManager,
        ILogger<LoginWithRecoveryCodeModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Kurtarma kodunu girin.")]
        [DataType(DataType.Text)]
        [Display(Name = "Kurtarma Kodu")]
        public string RecoveryCode { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var kullanici = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (kullanici is null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Admin");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid) return Page();

        var kullanici = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (kullanici is null) return RedirectToPage("./Login");

        var kod = Input.RecoveryCode.Replace(" ", string.Empty);

        var sonuc = await _signInManager.TwoFactorRecoveryCodeSignInAsync(kod);

        if (sonuc.Succeeded)
        {
            _logger.LogInformation("Kurtarma koduyla giriş yapıldı.");
            return LocalRedirect(returnUrl);
        }

        if (sonuc.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Çok fazla hatalı deneme yapıldı. Hesabınız geçici olarak kilitlendi, " +
                "bir süre sonra tekrar deneyin.");

            return Page();
        }

        ModelState.AddModelError(string.Empty,
            "Kurtarma kodu geçersiz. Her kod yalnızca bir kez kullanılabilir.");

        return Page();
    }
}