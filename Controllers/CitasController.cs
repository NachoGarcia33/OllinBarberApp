using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    public class CitasController : Controller
    {
        private static readonly TimeSpan ColombiaOffset = TimeSpan.FromHours(-5);
        private readonly ApplicationDbContext _context;

        public CitasController(ApplicationDbContext context) => _context = context;

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Crear(DateTime? fechaHora, string? barbero, int? barberoId, int? servicioId)
        {
            var ahora = AhoraColombia();
            var cita = new Cita { FechaHora = ahora.AddMinutes(30) };

            if (fechaHora.HasValue)
                cita.FechaHora = new DateTimeOffset(DateTime.SpecifyKind(fechaHora.Value, DateTimeKind.Unspecified), ColombiaOffset);

            var barberos = ObtenerBarberosPermitidos(disponibles: true);
            var seleccionado = barberoId.HasValue
                ? barberos.FirstOrDefault(b => b.Id == barberoId.Value)
                : barberos.FirstOrDefault(b => b.Nombre == barbero);

            if (seleccionado != null)
            {
                cita.BarberoId = seleccionado.Id;
                cita.Barbero = seleccionado.Nombre;
            }

            if (servicioId.HasValue && _context.Servicios.Any(s => s.Id == servicioId.Value && s.Activo))
                cita.ServicioId = servicioId.Value;

            CargarCombos(barberos);
            return View(cita);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Cita cita)
        {
            ModelState.Remove(nameof(Cita.Estado));
            ModelState.Remove(nameof(Cita.Servicio));
            ModelState.Remove(nameof(Cita.BarberoEntidad));
            ModelState.Remove(nameof(Cita.TokenConfirmacion));

            // datetime-local no contiene zona horaria. La aplicación opera en Colombia (UTC-5).
            cita.FechaHora = new DateTimeOffset(DateTime.SpecifyKind(cita.FechaHora.DateTime, DateTimeKind.Unspecified), ColombiaOffset);

            var barberos = ObtenerBarberosPermitidos(disponibles: true);
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == cita.ServicioId && s.Activo);
            var barbero = cita.BarberoId.HasValue
                ? barberos.FirstOrDefault(b => b.Id == cita.BarberoId.Value)
                : barberos.FirstOrDefault(b => b.Nombre == cita.Barbero);

            if (servicio == null)
                ModelState.AddModelError(string.Empty, "Selecciona un servicio activo.");

            if (barbero == null)
                ModelState.AddModelError(string.Empty, "Selecciona un barbero activo y disponible.");

            if (cita.FechaHora < AhoraColombia().AddMinutes(5))
                ModelState.AddModelError(nameof(cita.FechaHora), "Selecciona una fecha y hora futura.");

            if (servicio != null && !EstaDentroDelHorario(cita.FechaHora, servicio.Duracion))
                ModelState.AddModelError(nameof(cita.FechaHora), "La cita debe iniciar y finalizar dentro del horario: 9:00–12:00 o 14:00–20:00.");

            if (!ModelState.IsValid)
            {
                CargarCombos(barberos);
                return View(cita);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                if (await ExisteConflictoHorario(cita.FechaHora, servicio!, barbero!))
                {
                    ModelState.AddModelError(string.Empty, "El barbero ya tiene una cita que se cruza con ese horario.");
                    await transaction.RollbackAsync();
                    CargarCombos(barberos);
                    return View(cita);
                }

                cita.Estado = EstadoCita.Pendiente;
                cita.BarberoId = barbero!.Id;
                cita.Barbero = barbero.Nombre;
                cita.TokenConfirmacion = Guid.NewGuid().ToString("N");

                _context.Citas.Add(cita);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _context.Entry(cita).Reference(c => c.Servicio).LoadAsync();
                await _context.Entry(cita).Reference(c => c.BarberoEntidad).LoadAsync();
                PrepararConfirmacionWhatsApp(cita, servicio!, barbero);
                ViewBag.WhatsAppUrl = TempData["WhatsAppUrl"]?.ToString();
                return View("ReservaExitosa", cita);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "No pudimos reservar el horario porque cambió mientras confirmabas. Selecciona otro horario.");
                CargarCombos(barberos);
                return View(cita);
            }
        }

        [Authorize(Roles = "Admin,Barbero")]
        public IActionResult Index(int? barberoId, string? barbero, DateTime? fecha)
        {
            var barberos = ObtenerBarberosPermitidos(disponibles: false);
            var citas = _context.Citas.AsNoTracking()
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .AsQueryable();

            if (User.IsInRole("Barbero"))
            {
                var actual = ObtenerBarberoActual();
                citas = actual == null ? citas.Where(c => false) : citas.Where(c => c.BarberoId == actual.Id || c.Barbero == actual.Nombre);
            }
            else if (barberoId.HasValue)
                citas = citas.Where(c => c.BarberoId == barberoId.Value);
            else if (!string.IsNullOrWhiteSpace(barbero))
                citas = citas.Where(c => c.Barbero == barbero);

            if (fecha.HasValue)
            {
                var inicio = new DateTimeOffset(fecha.Value.Date, ColombiaOffset);
                var fin = inicio.AddDays(1);
                citas = citas.Where(c => c.FechaHora >= inicio && c.FechaHora < fin);
            }

            ViewBag.Barberos = barberos;
            ViewBag.BarberoId = barberoId;
            ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
            return View(citas.OrderBy(c => c.FechaHora).ToList());
        }

        [Authorize(Roles = "Admin,Barbero")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarAtendido(int id)
        {
            var cita = await _context.Citas.FirstOrDefaultAsync(c => c.Id == id);
            if (cita == null) return NotFound();
            if (!PuedeGestionarCita(cita)) return Forbid();

            if (cita.Estado is EstadoCita.Pendiente or EstadoCita.Confirmada)
            {
                cita.Estado = EstadoCita.Atendido;
                await _context.SaveChangesAsync();
                TempData["ok"] = "Cita marcada como atendida.";
            }
            return VolverAgenda(cita);
        }

        [Authorize(Roles = "Admin,Barbero")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var cita = await _context.Citas.FirstOrDefaultAsync(c => c.Id == id);
            if (cita == null) return NotFound();
            if (!PuedeGestionarCita(cita)) return Forbid();

            if (cita.Estado is not EstadoCita.Atendido and not EstadoCita.Cancelada)
            {
                cita.Estado = EstadoCita.Cancelada;
                await _context.SaveChangesAsync();
                TempData["ok"] = "Cita cancelada.";
            }
            return VolverAgenda(cita);
        }

        [Authorize(Roles = "Admin,Barbero")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            var cita = await _context.Citas.Include(c => c.Servicio).FirstOrDefaultAsync(c => c.Id == id);
            if (cita == null) return NotFound();
            if (!PuedeGestionarCita(cita)) return Forbid();

            var barbero = cita.BarberoId.HasValue ? await _context.Barberos.FindAsync(cita.BarberoId.Value) : null;
            if (barbero == null || cita.Servicio == null || !barbero.Activo)
            {
                TempData["error"] = "No se puede reactivar: el barbero o servicio ya no está disponible.";
                return VolverAgenda(cita);
            }

            if (await ExisteConflictoHorario(cita.FechaHora, cita.Servicio, barbero, cita.Id))
            {
                TempData["error"] = "No se puede reactivar: el horario ya está ocupado.";
                return VolverAgenda(cita);
            }

            cita.Estado = EstadoCita.Pendiente;
            await _context.SaveChangesAsync();
            TempData["ok"] = "Cita reactivada como pendiente.";
            return VolverAgenda(cita);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Confirmar(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return NotFound();

            var cita = await _context.Citas.AsNoTracking()
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .FirstOrDefaultAsync(c => c.TokenConfirmacion == token);

            if (cita == null) return NotFound();
            return View("ConfirmarCita", cita);
        }

        [AllowAnonymous]
        [HttpPost]
        [ActionName("Confirmar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPost(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return NotFound();

            var cita = await _context.Citas
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .FirstOrDefaultAsync(c => c.TokenConfirmacion == token);

            if (cita == null) return NotFound();

            if (cita.Estado == EstadoCita.Pendiente)
            {
                cita.Estado = EstadoCita.Confirmada;
                await _context.SaveChangesAsync();
                TempData["ok"] = "Cita confirmada correctamente.";
            }

            return View("ConfirmacionBarbero", cita);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Agenda(DateTime? fecha)
        {
            var fechaBase = (fecha ?? AhoraColombia().Date).Date;
            var inicio = new DateTimeOffset(fechaBase, ColombiaOffset);
            var fin = inicio.AddDays(1);

            if (User.IsInRole("Admin") || User.IsInRole("Barbero"))
                await MarcarNoAsistencias(inicio);

            var horarios = CrearHorarios(fechaBase);
            var barberos = ObtenerBarberosPermitidos(disponibles: false);
            var citas = await _context.Citas.AsNoTracking()
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .Where(c => c.FechaHora >= inicio && c.FechaHora < fin)
                .ToListAsync();

            if (User.IsInRole("Barbero"))
            {
                var actual = ObtenerBarberoActual();
                citas = actual == null ? new List<Cita>() : citas.Where(c => c.BarberoId == actual.Id || c.Barbero == actual.Nombre).ToList();
            }

            ViewBag.Servicios = await _context.Servicios.AsNoTracking().ToListAsync();
            ViewBag.Barberos = barberos;
            ViewBag.Fecha = fechaBase.ToString("yyyy-MM-dd");
            return View((horarios, citas));
        }

        private void CargarCombos(List<Barbero> barberos)
        {
            var ahora = AhoraColombia();
            ViewBag.Servicios = _context.Servicios.Where(s => s.Activo).OrderBy(s => s.Nombre).ToList();
            ViewBag.Barberos = barberos.OrderBy(b => b.Nombre).ToList();
            ViewBag.FechaHoyColombia = ahora.ToString("yyyy-MM-dd");
            ViewBag.HoraAhoraColombia = ahora.ToString("HH:mm");
        }

        private List<Barbero> ObtenerBarberosPermitidos(bool disponibles)
        {
            var query = _context.Barberos.Where(b => b.Activo);
            if (disponibles) query = query.Where(b => b.Disponible);

            if (User.IsInRole("Barbero"))
            {
                var actual = ObtenerBarberoActual();
                query = actual == null ? query.Where(b => false) : query.Where(b => b.Id == actual.Id);
            }

            return query.OrderBy(b => b.Nombre).ToList();
        }

        private Barbero? ObtenerBarberoActual()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
            if (user == null) return null;

            return _context.Barberos.FirstOrDefault(b => b.Activo &&
                ((!string.IsNullOrWhiteSpace(user.Celular) && b.Telefono == user.Celular) ||
                 (!string.IsNullOrWhiteSpace(user.Nombre) && b.Nombre == user.Nombre)));
        }

        private bool PuedeGestionarCita(Cita cita)
        {
            if (User.IsInRole("Admin")) return true;
            if (!User.IsInRole("Barbero")) return false;
            var actual = ObtenerBarberoActual();
            return actual != null && (cita.BarberoId == actual.Id || cita.Barbero == actual.Nombre);
        }

        private async Task<bool> ExisteConflictoHorario(DateTimeOffset inicioNuevo, Servicio servicioNuevo, Barbero barbero, int? excluirCitaId = null)
        {
            var finNuevo = inicioNuevo.AddMinutes(servicioNuevo.Duracion);
            var fechaLocal = inicioNuevo.ToOffset(ColombiaOffset).Date;
            var inicioConsulta = new DateTimeOffset(fechaLocal.AddDays(-1), ColombiaOffset);
            var finConsulta = new DateTimeOffset(fechaLocal.AddDays(2), ColombiaOffset);

            var citas = await _context.Citas.AsNoTracking()
                .Include(c => c.Servicio)
                .Where(c => c.Estado != EstadoCita.Cancelada && c.Estado != EstadoCita.NoAsistio &&
                            (c.BarberoId == barbero.Id || c.Barbero == barbero.Nombre) &&
                            c.FechaHora >= inicioConsulta && c.FechaHora < finConsulta &&
                            (!excluirCitaId.HasValue || c.Id != excluirCitaId.Value))
                .ToListAsync();

            return citas.Any(c => c.Servicio != null &&
                inicioNuevo < c.FechaHora.AddMinutes(c.Servicio.Duracion) &&
                finNuevo > c.FechaHora);
        }

        private static bool EstaDentroDelHorario(DateTimeOffset inicio, int duracionMinutos)
        {
            var fin = inicio.AddMinutes(duracionMinutos).TimeOfDay;
            var hora = inicio.TimeOfDay;
            var manana = hora >= new TimeSpan(9, 0, 0) && fin <= new TimeSpan(12, 0, 0);
            var tarde = hora >= new TimeSpan(14, 0, 0) && fin <= new TimeSpan(20, 0, 0);
            return manana || tarde;
        }

        private static List<DateTime> CrearHorarios(DateTime fechaBase)
        {
            var horarios = new List<DateTime>();
            for (var hora = fechaBase.Date.AddHours(9); hora < fechaBase.Date.AddHours(12); hora = hora.AddMinutes(30)) horarios.Add(hora);
            for (var hora = fechaBase.Date.AddHours(14); hora < fechaBase.Date.AddHours(20); hora = hora.AddMinutes(30)) horarios.Add(hora);
            return horarios;
        }

        private async Task MarcarNoAsistencias(DateTimeOffset inicioDia)
        {
            var limite = AhoraColombia().AddMinutes(-60);
            var citas = await _context.Citas.Include(c => c.Servicio)
                .Where(c => c.FechaHora >= inicioDia.AddDays(-1) && c.FechaHora < inicioDia.AddDays(1) &&
                            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada))
                .ToListAsync();

            var cambios = false;
            foreach (var cita in citas)
            {
                if (cita.Servicio != null && cita.FechaHora.AddMinutes(cita.Servicio.Duracion) < limite)
                {
                    cita.Estado = EstadoCita.NoAsistio;
                    cambios = true;
                }
            }
            if (cambios) await _context.SaveChangesAsync();
        }

        private void PrepararConfirmacionWhatsApp(Cita cita, Servicio servicio, Barbero barbero)
        {
            var config = _context.ConfiguracionSistema.AsNoTracking().OrderByDescending(c => c.Id).FirstOrDefault();
            if (config?.WhatsappActivo != true || string.IsNullOrWhiteSpace(cita.TokenConfirmacion)) return;

            var telefono = new string(barbero.Telefono.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(telefono)) return;
            if (telefono.Length == 10) telefono = "57" + telefono;

            var urlConfirmacion = Url.Action(nameof(Confirmar), "Citas", new { token = cita.TokenConfirmacion }, Request.Scheme, Request.Host.Value);
            var mensaje = Uri.EscapeDataString($@"Nueva cita pendiente de confirmación

Cliente: {cita.ClienteNombre}
Teléfono: {cita.Telefono}
Servicio: {servicio.Nombre}
Valor: ${servicio.Precio:N0} COP
Fecha: {cita.FechaHora:dd/MM/yyyy}
Hora: {cita.FechaHora:hh:mm tt}

CONFIRMAR CITA:
{urlConfirmacion}");

            TempData["WhatsAppUrl"] = $"https://wa.me/{telefono}?text={mensaje}";
        }

        private IActionResult VolverAgenda(Cita cita) =>
            RedirectToAction(nameof(Agenda), new
            {
                fecha = cita.FechaHora.ToOffset(ColombiaOffset).ToString("yyyy-MM-dd")
            });

        private static DateTimeOffset AhoraColombia() => DateTimeOffset.UtcNow.ToOffset(ColombiaOffset);
    }
}
