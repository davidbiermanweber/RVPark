using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Customer-facing account flows (G1, G2). Separate from AccountController, which
// handles employee/admin login. Customers are issued a Role=Customer cookie claim.
public class CustomerAccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IEmailSender _email;

    public CustomerAccountController(AppDbContext db, IPasswordService passwords, IEmailSender email)
    {
        _db = db;
        _passwords = passwords;
        _email = email;
    }

    // ---------- Registration + email verification (G1) ----------

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");
            return View(vm);
        }

        var token = Guid.NewGuid().ToString("N");
        var user = new User
        {
            Name = vm.Name.Trim(),
            Email = email,
            Phone = vm.Phone?.Trim() ?? string.Empty,
            PasswordHash = _passwords.Hash(vm.Password),
            Affiliation = vm.Affiliation,
            MilitaryStatus = vm.MilitaryStatus?.Trim(),
            IsEmailVerified = false,
            EmailVerificationToken = token,
            TokenExpiresUtc = DateTime.UtcNow.AddHours(24)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var link = Url.Action(nameof(VerifyEmail), "CustomerAccount", new { token }, Request.Scheme);
        await _email.SendAsync(user.Email, "Verify your FamCamp account",
            $"Welcome to Hill AFB FamCamp! Please confirm your account:<br/><a href=\"{link}\">{link}</a>");

        return RedirectToAction(nameof(VerifyEmailSent));
    }

    [HttpGet]
    public IActionResult VerifyEmailSent() => View();

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        var user = string.IsNullOrEmpty(token)
            ? null
            : await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null || user.TokenExpiresUtc == null || user.TokenExpiresUtc < DateTime.UtcNow)
        {
            ViewBag.Success = false;
            return View("VerifyEmailResult");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.TokenExpiresUtc = null;
        await _db.SaveChangesAsync();

        ViewBag.Success = true;
        return View("VerifyEmailResult");
    }

    // ---------- Login / logout (G2) ----------

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);

        // Generic error for both unknown email and bad password (G2 acceptance).
        if (user == null || !_passwords.Verify(user.PasswordHash, password ?? string.Empty, out bool needsUpgrade))
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        if (!user.IsEmailVerified)
        {
            ViewBag.Error = "Please verify your email before signing in. Check your inbox for the link.";
            return View();
        }

        if (needsUpgrade)
        {
            user.PasswordHash = _passwords.Hash(password!);
            await _db.SaveChangesAsync();
        }

        await SignInCustomerAsync(user);
        return RedirectToAction("Search", "Availability");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ---------- Profile (G2) ----------

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await CurrentUserAsync();
        if (user == null) return Challenge();
        return View(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string name, string phone, MilitaryAffiliation? affiliation, string militaryStatus)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Challenge();

        if (!string.IsNullOrWhiteSpace(name)) user.Name = name.Trim();
        user.Phone = (phone ?? string.Empty).Trim();
        user.Affiliation = affiliation;
        user.MilitaryStatus = militaryStatus?.Trim();
        await _db.SaveChangesAsync();

        ViewBag.Saved = true;
        return View(user);
    }

    // ---------- Password reset (G2) ----------

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);

        // Always show the same confirmation so we don't leak which emails exist.
        if (user != null)
        {
            user.PasswordResetToken = Guid.NewGuid().ToString("N");
            user.ResetExpiresUtc = DateTime.UtcNow.AddHours(2);
            await _db.SaveChangesAsync();

            var link = Url.Action(nameof(ResetPassword), "CustomerAccount",
                new { token = user.PasswordResetToken }, Request.Scheme);
            await _email.SendAsync(user.Email, "Reset your FamCamp password",
                $"Reset your password:<br/><a href=\"{link}\">{link}</a>");
        }

        ViewBag.Sent = true;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        var user = string.IsNullOrEmpty(token)
            ? null
            : await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

        if (user == null || user.ResetExpiresUtc == null || user.ResetExpiresUtc < DateTime.UtcNow)
        {
            ViewBag.Invalid = true;
            return View();
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
    {
        var user = string.IsNullOrEmpty(token)
            ? null
            : await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

        if (user == null || user.ResetExpiresUtc == null || user.ResetExpiresUtc < DateTime.UtcNow)
        {
            ViewBag.Invalid = true;
            return View();
        }

        if (string.IsNullOrEmpty(password) || password.Length < 8)
            ModelState.AddModelError("", "Password must be at least 8 characters.");
        if (password != confirmPassword)
            ModelState.AddModelError("", "The passwords do not match.");

        if (!ModelState.IsValid)
        {
            ViewBag.Token = token;
            return View();
        }

        user.PasswordHash = _passwords.Hash(password);
        user.PasswordResetToken = null;
        user.ResetExpiresUtc = null;
        await _db.SaveChangesAsync();

        ViewBag.Done = true;
        return View();
    }

    // ---------- helpers ----------

    private async Task SignInCustomerAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("Name", user.Name),
            new Claim("Role", "Customer")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private async Task<User?> CurrentUserAsync()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out int id) ? await _db.Users.FindAsync(id) : null;
    }
}
