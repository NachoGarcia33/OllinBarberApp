using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // 🔥 LOGIN GET
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Agenda", "Citas");

            return View();
        }

        // 🔥 LOGIN POST
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                false,
                false);

            if (result.Succeeded)
                return RedirectToAction("Agenda", "Citas");

            ViewBag.Error = "Correo o contraseña incorrectos";

            return View();
        }

        // 🔥 REGISTER GET
        public IActionResult Register()
        {
            return View();
        }

        // 🔥 REGISTER POST
        [HttpPost]
        public async Task<IActionResult> Register(
    string email,
    string password,
    string nombre,
    string celular)
        {
            // Verificar si el correo ya existe
            var usuarioExistente = await _userManager.FindByEmailAsync(email);

            if (usuarioExistente != null)
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
                TempData["Success"] =
                    $"Bienvenido {nombre} 🎉 Registro exitoso";

                return RedirectToAction("Login");
            }

            ViewBag.Error = string.Join(", ",
                result.Errors.Select(e => e.Description));

            return View();
        }

        // 🔥 LOGOUT
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login");
        }
    }
}