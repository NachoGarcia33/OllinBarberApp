using Microsoft.AspNetCore.Mvc;
using OllinBarberApp.Data;

namespace OllinBarberApp.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiciosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var servicios = _context.Servicios
                .Where(s => s.Activo)
                .OrderBy(s => s.Nombre)
                .ToList();

            return View(servicios);
        }
    }
}