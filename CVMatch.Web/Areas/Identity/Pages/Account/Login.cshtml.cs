using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CVMatch.Web.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "E-posta adresinizi girin.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta Adresi")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parolanızı girin.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Yarım kalmış dış oturum varsa temizlenir
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Giriş yönetici içindir; varsayılan varış paneldir
        returnUrl ??= Url.Content("~/Admin");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid) return Page();

        var sonuc = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (sonuc.Succeeded)
        {
            _logger.LogInformation("Yönetici girişi yapıldı.");
            return LocalRedirect(returnUrl);
        }

        if (sonuc.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa",
                new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
        }

        if (sonuc.IsLockedOut)
        {
            _logger.LogWarning("Hesap geçici olarak kilitlendi.");

            ModelState.AddModelError(string.Empty,
                "Çok fazla hatalı deneme yapıldı. Hesabınız geçici olarak kilitlendi, " +
                "bir süre sonra tekrar deneyin.");

            return Page();
        }

        // Hangi bilginin yanlış olduğu söylenmez; hesap varlığı sızmasın
        ModelState.AddModelError(string.Empty, "E-posta adresi veya parola hatalı.");
        return Page();
    }
}