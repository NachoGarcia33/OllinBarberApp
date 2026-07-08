using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OllinBarberApp.Data;

namespace OllinBarberApp.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var servicios = _context.Servicios.ToList();
            return View(servicios);
        }
    }
}