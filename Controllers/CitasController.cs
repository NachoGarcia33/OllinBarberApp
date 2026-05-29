using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    [Authorize]
    public class CitasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CitasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Crear(DateTime? fechaHora, string? barbero, int? barberoId)
        {
            var cita = new Cita();

            if (fechaHora.HasValue)
            {
                cita.FechaHora = fechaHora.Value;
            }

            var barberos = ObtenerBarberosPermitidos(disponibles: true);
            var seleccionado = barberoId.HasValue
                ? barberos.FirstOrDefault(b => b.Id == barberoId.Value)
                : barberos.FirstOrDefault(b => b.Nombre == barbero);

            if (seleccionado != null)
            {
                cita.BarberoId = seleccionado.Id;
                cita.Barbero = seleccionado.Nombre;
            }

            CargarCombos(barberos);
            return View(cita);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(Cita cita)
        {
            ModelState.Remove(nameof(Cita.Estado));
            ModelState.Remove(nameof(Cita.Servicio));
            ModelState.Remove(nameof(Cita.BarberoEntidad));

            var barberos = ObtenerBarberosPermitidos(disponibles: true);
            var servicio = _context.Servicios
                .FirstOrDefault(s => s.Id == cita.ServicioId && s.Activo);

            if (servicio == null)
            {
                ModelState.AddModelError("", "Selecciona un servicio activo.");
            }

            var barbero = cita.BarberoId.HasValue
                ? barberos.FirstOrDefault(b => b.Id == cita.BarberoId.Value)
                : barberos.FirstOrDefault(b => b.Nombre == cita.Barbero);

            if (barbero == null)
            {
                ModelState.AddModelError("", "Selecciona un barbero activo y disponible.");
            }

            if (servicio != null && barbero != null && ExisteConflictoHorario(cita, servicio, barbero))
            {
                ModelState.AddModelError("", "El barbero ya tiene una cita en ese horario.");
            }

            if (!ModelState.IsValid)
            {
                CargarCombos(barberos);
                return View(cita);
            }

            cita.Estado = EstadoCita.Pendiente;
            cita.BarberoId = barbero!.Id;
            cita.Barbero = barbero.Nombre;

            _context.Citas.Add(cita);
            _context.SaveChanges();

            PrepararConfirmacionWhatsApp(cita, servicio!, barbero);

            TempData["Success"] = "Cita registrada correctamente.";
            return RedirectToAction("Agenda");
        }

        public IActionResult Index(int? barberoId, string? barbero, DateTime? fecha)
        {
            var barberos = ObtenerBarberosPermitidos(disponibles: false);
            var citas = _context.Citas
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .AsQueryable();

            if (User.IsInRole("Barbero"))
            {
                var barberoActual = ObtenerBarberoActual();
                citas = barberoActual == null
                    ? citas.Where(c => false)
                    : citas.Where(c => c.BarberoId == barberoActual.Id || c.Barbero == barberoActual.Nombre);
            }
            else if (barberoId.HasValue)
            {
                citas = citas.Where(c => c.BarberoId == barberoId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(barbero))
            {
                citas = citas.Where(c => c.Barbero == barbero);
            }

            if (fecha.HasValue)
            {
                var fechaSeleccionada = fecha.Value.Date;
                citas = citas.Where(c => c.FechaHora.Date == fechaSeleccionada);
            }

            ViewBag.Barberos = barberos;
            ViewBag.BarberoId = barberoId;
            ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");

            return View(citas.OrderBy(c => c.FechaHora).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarcarAtendido(int id)
        {
            var cita = _context.Citas.FirstOrDefault(c => c.Id == id);

            if (cita != null && PuedeGestionarCita(cita))
            {
                cita.Estado = EstadoCita.Atendido;
                _context.SaveChanges();
            }

            return RedirectToAction("Agenda");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            var cita = _context.Citas.FirstOrDefault(c => c.Id == id);

            if (cita != null && PuedeGestionarCita(cita))
            {
                cita.Estado = EstadoCita.Cancelada;
                _context.SaveChanges();
            }

            return RedirectToAction("Agenda");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reactivar(int id)
        {
            var cita = _context.Citas.FirstOrDefault(c => c.Id == id);

            if (cita != null && PuedeGestionarCita(cita))
            {
                _context.Citas.Remove(cita);
                _context.SaveChanges();
            }

            return RedirectToAction("Agenda");
        }

        public IActionResult Agenda(DateTime? fecha)
        {
            var fechaBase = fecha ?? DateTime.Today;
            var horarios = CrearHorarios(fechaBase);
            var barberos = ObtenerBarberosPermitidos(disponibles: false);

            var citas = _context.Citas
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .Where(c => c.FechaHora.Date == fechaBase.Date)
                .ToList();

            if (User.IsInRole("Barbero"))
            {
                var barberoActual = ObtenerBarberoActual();
                citas = barberoActual == null
                    ? new List<Cita>()
                    : citas
                        .Where(c => c.BarberoId == barberoActual.Id || c.Barbero == barberoActual.Nombre)
                        .ToList();
            }

            var servicios = _context.Servicios.ToList();
            MarcarNoAsistencias(citas, servicios);

            ViewBag.Servicios = servicios;
            ViewBag.Barberos = barberos;
            ViewBag.Fecha = fechaBase.ToString("yyyy-MM-dd");

            return View((horarios, citas));
        }

        private void CargarCombos(List<Barbero> barberos)
        {
            ViewBag.Servicios = _context.Servicios
                .Where(s => s.Activo)
                .OrderBy(s => s.Nombre)
                .ToList();

            ViewBag.Barberos = barberos
                .OrderBy(b => b.Nombre)
                .ToList();
        }

        private List<Barbero> ObtenerBarberosPermitidos(bool disponibles)
        {
            var query = _context.Barberos
                .Where(b => b.Activo);

            if (disponibles)
            {
                query = query.Where(b => b.Disponible);
            }

            if (User.IsInRole("Barbero"))
            {
                var barberoActual = ObtenerBarberoActual();
                query = barberoActual == null
                    ? query.Where(b => false)
                    : query.Where(b => b.Id == barberoActual.Id);
            }

            return query.OrderBy(b => b.Nombre).ToList();
        }

        private Barbero? ObtenerBarberoActual()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            return _context.Barberos.FirstOrDefault(b =>
                b.Activo &&
                ((user.Celular != string.Empty && b.Telefono == user.Celular) ||
                 (user.Nombre != string.Empty && b.Nombre == user.Nombre)));
        }

        private bool PuedeGestionarCita(Cita cita)
        {
            if (!User.IsInRole("Barbero"))
            {
                return true;
            }

            var barberoActual = ObtenerBarberoActual();

            return barberoActual != null &&
                (cita.BarberoId == barberoActual.Id || cita.Barbero == barberoActual.Nombre);
        }

        private bool ExisteConflictoHorario(Cita nuevaCita, Servicio servicioNuevo, Barbero barbero)
        {
            var citasBarbero = _context.Citas
                .Where(c =>
                    c.Estado == EstadoCita.Pendiente &&
                    (c.BarberoId == barbero.Id || c.Barbero == barbero.Nombre))
                .ToList();

            foreach (var citaExistente in citasBarbero)
            {
                var servicioExistente = _context.Servicios
                    .FirstOrDefault(s => s.Id == citaExistente.ServicioId);

                if (servicioExistente == null)
                {
                    continue;
                }

                var inicioExistente = citaExistente.FechaHora;
                var finExistente = citaExistente.FechaHora.AddMinutes(servicioExistente.Duracion);
                var inicioNuevo = nuevaCita.FechaHora;
                var finNuevo = nuevaCita.FechaHora.AddMinutes(servicioNuevo.Duracion);

                if (inicioNuevo < finExistente && finNuevo > inicioExistente)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<DateTime> CrearHorarios(DateTime fechaBase)
        {
            var horarios = new List<DateTime>();
            var inicio = fechaBase.Date.AddHours(9);
            var fin = fechaBase.Date.AddHours(19);

            while (inicio < fin)
            {
                horarios.Add(inicio);
                inicio = inicio.AddMinutes(30);
            }

            return horarios;
        }

        private void MarcarNoAsistencias(List<Cita> citas, List<Servicio> servicios)
        {
            var huboCambios = false;

            foreach (var cita in citas.Where(c => c.Estado == EstadoCita.Pendiente))
            {
                var servicio = servicios.FirstOrDefault(s => s.Id == cita.ServicioId);

                if (servicio == null)
                {
                    continue;
                }

                var finCita = cita.FechaHora.AddMinutes(servicio.Duracion);

                var limiteNoAsistencia = finCita.AddMinutes(60);

                if (DateTime.Now > limiteNoAsistencia)
                {
                    cita.Estado = EstadoCita.NoAsistio;
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                _context.SaveChanges();
            }
        }

        private void PrepararConfirmacionWhatsApp(Cita cita, Servicio servicio, Barbero barbero)
        {
            var config = _context.ConfiguracionSistema
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            if (config?.WhatsappActivo != true || string.IsNullOrWhiteSpace(cita.Telefono))
            {
                return;
            }

            var telefono = new string(cita.Telefono.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(telefono))
            {
                return;
            }

            // Si tiene 10 dígitos asumimos Colombia
            if (telefono.Length == 10)
            {
                telefono = "57" + telefono;
            }

            var mensaje = Uri.EscapeDataString(
                $"Hola {cita.ClienteNombre}, tu cita en Ollin Barber quedó agendada para {cita.FechaHora:dd/MM/yyyy HH:mm} con {barbero.Nombre}. Servicio: {servicio.Nombre}.");

            TempData["WhatsAppUrl"] = $"https://wa.me/{telefono}?text={mensaje}";
        }
    }
}
