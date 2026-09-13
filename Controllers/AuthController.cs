using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            email = email?.Trim() ?? string.Empty;
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Ingresa tu correo y contraseña.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = result.IsLockedOut
                ? "Tu cuenta está temporalmente bloqueada por varios intentos fallidos. Intenta más tarde."
                : "Correo o contraseña incorrectos.";

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string nombre, string celular)
        {
            email = email?.Trim() ?? string.Empty;
            nombre = nombre?.Trim() ?? string.Empty;
            celular = celular?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 100)
            {
                ViewBag.Error = "Ingresa un nombre válido de máximo 100 caracteres.";
                return View();
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                ViewBag.Error = "Debe ingresar un correo electrónico válido.";
                return View();
            }


            if (!System.Text.RegularExpressions.Regex.IsMatch(celular, @"^\d{10}$"))
            {
                ViewBag.Error = "El celular debe contener exactamente 10 números.";
                return View();
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ViewBag.Error = "Este correo electrónico ya está registrado.";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nombre = nombre,
                Celular = celular,
                EsBarbero = false,
                Disponible = false
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Bienvenido {nombre}. Registro exitoso.";
                return RedirectToAction(nameof(Login));
            }

            ViewBag.Error = string.Join(" ", result.Errors.Select(e => e.Description));
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccesoDenegado() => View();
    }
}
