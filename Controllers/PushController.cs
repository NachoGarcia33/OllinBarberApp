using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    [Authorize(Roles = "Barbero")]
    [Route("Push")]
    public class PushController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public PushController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("PublicKey")]
        [AllowAnonymous]
        public IActionResult PublicKey() => Content(_config["WebPush:PublicKey"] ?? string.Empty);

        public class KeysInput
        {
            public string P256dh { get; set; } = string.Empty;
            public string Auth { get; set; } = string.Empty;
        }

        public class SuscripcionInput
        {
            public string Endpoint { get; set; } = string.Empty;
            public KeysInput Keys { get; set; } = new();
        }

        [HttpPost("Suscribir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suscribir([FromBody] SuscripcionInput input)
        {
            var barbero = await ObtenerBarberoActualAsync();
            if (barbero == null) return Forbid();
            if (string.IsNullOrWhiteSpace(input.Endpoint)) return BadRequest();

            var existente = await _context.SuscripcionesPush.FirstOrDefaultAsync(s => s.Endpoint == input.Endpoint);
            if (existente != null)
            {
                existente.BarberoId = barbero.Id;
                existente.P256dh = input.Keys.P256dh;
                existente.Auth = input.Keys.Auth;
            }
            else
            {
                _context.SuscripcionesPush.Add(new SuscripcionPush
                {
                    BarberoId = barbero.Id,
                    Endpoint = input.Endpoint,
                    P256dh = input.Keys.P256dh,
                    Auth = input.Keys.Auth
                });
            }

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
