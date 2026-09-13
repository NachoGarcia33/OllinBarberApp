using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;

namespace OllinBarberApp.Controllers
{
    [AllowAnonymous]
    public class BarberosController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BarberosController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var barberos = await _context.Barberos
                .AsNoTracking()
                .Where(b => b.Activo)
                .OrderBy(b => b.Nombre)
                .ToListAsync();
            return View(barberos);
        }
    }
}
