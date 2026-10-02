using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    [Authorize(Roles = "Barbero")]
    public class NotificacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var barbero = await ObtenerBarberoActualAsync();
            if (barbero == null) return View(new List<Notificacion>());

            var notificaciones = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.BarberoId == barbero.Id)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(100)
                .ToListAsync();

            return View(notificaciones);
        }

        [HttpGet]
        public async Task<IActionResult> Resumen()
        {
            var barbero = await ObtenerBarberoActualAsync();
            if (barbero == null) return Json(new { noLeidas = 0, items = Array.Empty<object>() });

            var noLeidas = await _context.Notificaciones
                .Where(n => n.BarberoId == barbero.Id && !n.Leida)
                .CountAsync();

            var items = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.BarberoId == barbero.Id)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(8)
                .Select(n => new { n.Id, n.Mensaje, n.Leida, fecha = n.FechaCreacion.ToString("dd/MM/yyyy hh:mm tt") })
                .ToListAsync();

            return Json(new { noLeidas, items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var barbero = await ObtenerBarberoActualAsync();
            if (barbero == null) return Forbid();

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.BarberoId == barbero.Id);

            if (notificacion != null)
            {
                notificacion.Leida = true;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var barbero = await ObtenerBarberoActualAsync();
            if (barbero == null) return Forbid();

            var pendientes = await _context.Notificaciones
                .Where(n => n.BarberoId == barbero.Id && !n.Leida)
                .ToListAsync();

            foreach (var n in pendientes) n.Leida = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        private async Task<Barbero?> ObtenerBarberoActualAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            return await _context.Barberos.FirstOrDefaultAsync(b => b.Activo &&
                ((!string.IsNullOrWhiteSpace(user.Celular) && b.Telefono == user.Celular) ||
                 (!string.IsNullOrWhiteSpace(user.Nombre) && b.Nombre == user.Nombre)));
        }
    }
}
