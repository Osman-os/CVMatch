using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CVMatch.Web.Areas.Identity.Pages.Account.Manage;

public class EnableAuthenticatorModel : PageModel
{
    private const string AuthenticatorUriFormat =
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly UrlEncoder _urlEncoder;

    public EnableAuthenticatorModel(UserManager<IdentityUser> userManager, UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _urlEncoder = urlEncoder;
    }

    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;

    [TempData]
    public string[]? RecoveryCodes { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Doğrulama kodunu girin.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Kod 6 haneli olmalıdır.")]
        [DataType(DataType.Text)]
        [Display(Name = "Doğrulama Kodu")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        await AnahtarHazirlaAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await AnahtarHazirlaAsync(user);
            return Page();
        }

        var kod = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var gecerli = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, kod);

        if (!gecerli)
        {
            ModelState.AddModelError("Input.Code", "Kod doğrulanamadı. Tekrar deneyin.");
            await AnahtarHazirlaAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        // Her yeni kurulumda kurtarma kodları da baştan üretilir
        var kodlar = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = kodlar?.ToArray();

        return RedirectToPage("./ShowRecoveryCodes");
    }

    private async Task AnahtarHazirlaAsync(IdentityUser user)
    {
        var anahtar = await _userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(anahtar))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            anahtar = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = OkunakliYap(anahtar!);

        var eposta = await _userManager.GetEmailAsync(user);

        AuthenticatorUri = string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            _urlEncoder.Encode("Yesilmavi IK"),
            _urlEncoder.Encode(eposta ?? "hesap"),
            anahtar);
    }

    private static string OkunakliYap(string anahtar)
    {
        var sonuc = new StringBuilder();
        var i = 0;

        while (i + 4 < anahtar.Length)
        {
            sonuc.Append(anahtar.AsSpan(i, 4)).Append(' ');
            i += 4;
        }

        if (i < anahtar.Length)
            sonuc.Append(anahtar.AsSpan(i));

        return sonuc.ToString().ToLowerInvariant();
    }
}