using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OllinBarberApp.Data;

namespace OllinBarberApp.Controllers
{
    [Authorize]
    public class TiendaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TiendaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var config = _context.ConfiguracionSistema
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            if (config?.TiendaActiva != true)
            {
                return NotFound();
            }

            var productos = _context.Productos
                .Where(p => p.Activo && p.Stock > 0)
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToList();

            return View(productos);
        }
    }
}
