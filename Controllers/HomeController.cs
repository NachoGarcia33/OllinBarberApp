using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OllinBarberApp.Data;
using System.Linq;

namespace OllinBarberApp.Controllers
{
    [Authorize] // 🔥 ESTO ES LO QUE FALTABA
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