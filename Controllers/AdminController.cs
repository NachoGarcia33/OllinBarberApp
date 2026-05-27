using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;
using Microsoft.AspNetCore.Hosting;

namespace OllinBarberApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // =========================
        // DASHBOARD
        // =========================

        public IActionResult Index()
        {
            ViewBag.Configuracion = ObtenerConfiguracion();

            ViewBag.BarberosActivos =
                _context.Barberos.Count(b => b.Activo);

            ViewBag.ServiciosActivos =
                _context.Servicios.Count(s => s.Activo);

            ViewBag.ProductosActivos =
                _context.Productos.Count(p => p.Activo);

            var hoy = DateTime.Today;

            ViewBag.CitasHoy =
                _context.Citas.Count(c =>
                    c.FechaHora >= hoy &&
                    c.FechaHora < hoy.AddDays(1));

            return View(ConstruirDashboardFinanciero());
        }

        public IActionResult Dashboard()
        {
            return View(ConstruirDashboardFinanciero());
        }

        // =========================
        // BARBEROS
        // =========================

        public IActionResult Barberos()
        {
            var lista = _context.Barberos
                .OrderByDescending(b => b.Activo)
                .ThenBy(b => b.Nombre)
                .ToList();

            return View(lista);
        }

        public IActionResult CrearBarbero()
        {
            return View(new Barbero
            {
                Activo = true,
                Disponible = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearBarbero(
            Barbero barbero,
            IFormFile? ImagenArchivo)
        {
            if (!ModelState.IsValid)
            {
                return View(barbero);
            }

            if (ImagenArchivo != null &&
                ImagenArchivo.Length > 0)
            {
                barbero.ImagenUrl =
                    await GuardarImagen(
                        ImagenArchivo,
                        "barberos");
            }

            _context.Barberos.Add(barbero);

            await _context.SaveChangesAsync();

            TempData["ok"] =
                "Barbero agregado correctamente";

            return RedirectToAction("Barberos");
        }

        public IActionResult EditarBarbero(int id)
        {
            var barbero =
                _context.Barberos.Find(id);

            if (barbero == null)
            {
                return NotFound();
            }

            return View(barbero);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarBarbero(
            Barbero barbero,
            IFormFile? ImagenArchivo)
        {
            if (!ModelState.IsValid)
            {
                return View(barbero);
            }

            var existente =
                _context.Barberos.Find(barbero.Id);

            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre =
                barbero.Nombre;

            existente.Telefono =
                barbero.Telefono;

            existente.Disponible =
                barbero.Disponible;

            existente.Activo =
                barbero.Activo;

            if (ImagenArchivo != null &&
                ImagenArchivo.Length > 0)
            {
                existente.ImagenUrl =
                    await GuardarImagen(
                        ImagenArchivo,
                        "barberos");
            }

            await _context.SaveChangesAsync();

            TempData["ok"] =
                "Barbero actualizado";

            return RedirectToAction("Barberos");
        }

        public IActionResult EliminarBarbero(int id)
        {
            var barbero =
                _context.Barberos.Find(id);

            if (barbero != null)
            {
                barbero.Activo = false;
                barbero.Disponible = false;

                _context.SaveChanges();
            }

            TempData["ok"] =
                "Barbero desactivado";

            return RedirectToAction("Barberos");
        }

        public IActionResult ActivarBarbero(int id)
        {
            var barbero =
                _context.Barberos.Find(id);

            if (barbero != null)
            {
                barbero.Activo = true;
                barbero.Disponible = true;

                _context.SaveChanges();
            }

            TempData["ok"] =
                "Barbero activado";

            return RedirectToAction("Barberos");
        }

        // =========================
        // SERVICIOS
        // =========================

        public IActionResult Servicios()
        {
            var lista = _context.Servicios
                .OrderByDescending(s => s.Activo)
                .ThenBy(s => s.Nombre)
                .ToList();

            return View(lista);
        }

        public IActionResult CrearServicio()
        {
            return View(new Servicio
            {
                Activo = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearServicio(
            Servicio servicio)
        {
            if (!ModelState.IsValid)
            {
                return View(servicio);
            }

            _context.Servicios.Add(servicio);

            _context.SaveChanges();

            TempData["ok"] =
                "Servicio creado";

            return RedirectToAction("Servicios");
        }

        public IActionResult EditarServicio(int id)
        {
            var servicio =
                _context.Servicios.Find(id);

            if (servicio == null)
            {
                return NotFound();
            }

            return View(servicio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarServicio(
            Servicio servicio)
        {
            if (!ModelState.IsValid)
            {
                return View(servicio);
            }

            var existente =
                _context.Servicios.Find(servicio.Id);

            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre = servicio.Nombre;
            existente.Duracion = servicio.Duracion;
            existente.Precio = servicio.Precio;
            existente.Tipo = servicio.Tipo;
            existente.Activo = servicio.Activo;

            _context.SaveChanges();

            TempData["ok"] =
                "Servicio actualizado";

            return RedirectToAction("Servicios");
        }

        public IActionResult EliminarServicio(int id)
        {
            var servicio =
                _context.Servicios.Find(id);

            if (servicio != null)
            {
                servicio.Activo = false;

                _context.SaveChanges();
            }

            TempData["ok"] =
                "Servicio desactivado";

            return RedirectToAction("Servicios");
        }

        public IActionResult ActivarServicio(int id)
        {
            var servicio =
                _context.Servicios.Find(id);

            if (servicio != null)
            {
                servicio.Activo = true;

                _context.SaveChanges();
            }

            TempData["ok"] =
                "Servicio activado";

            return RedirectToAction("Servicios");
        }

        // =========================
        // PRODUCTOS
        // =========================

        public IActionResult Productos()
        {
            var lista = _context.Productos
                .OrderByDescending(p => p.Activo)
                .ThenBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToList();

            return View(lista);
        }

        public IActionResult CrearProducto()
        {
            return View(new Producto
            {
                Activo = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearProducto(
            Producto producto,
            IFormFile? ImagenArchivo)
        {
            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            if (ImagenArchivo != null &&
                ImagenArchivo.Length > 0)
            {
                producto.ImagenUrl =
                    await GuardarImagen(
                        ImagenArchivo,
                        "productos");
            }

            _context.Productos.Add(producto);

            await _context.SaveChangesAsync();

            TempData["ok"] =
                "Producto creado";

            return RedirectToAction("Productos");
        }

        public IActionResult EditarProducto(int id)
        {
            var producto =
                _context.Productos.Find(id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProducto(
            Producto producto,
            IFormFile? ImagenArchivo)
        {
            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            var existente =
                _context.Productos.Find(producto.Id);

            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre = producto.Nombre;
            existente.Descripcion = producto.Descripcion;
            existente.Categoria = producto.Categoria;
            existente.Precio = producto.Precio;
            existente.Stock = producto.Stock;
            existente.Activo = producto.Activo;

            if (ImagenArchivo != null &&
                ImagenArchivo.Length > 0)
            {
                existente.ImagenUrl =
                    await GuardarImagen(
                        ImagenArchivo,
                        "productos");
            }

            await _context.SaveChangesAsync();

            TempData["ok"] =
                "Producto actualizado";

            return RedirectToAction("Productos");
        }

        public IActionResult EliminarProducto(int id)
        {
            var producto =
                _context.Productos.Find(id);

            if (producto != null)
            {
                producto.Activo = false;

                _context.SaveChanges();
            }

            TempData["ok"] =
                "Producto desactivado";

            return RedirectToAction("Productos");
        }

        public IActionResult ActivarProducto(int id)
        {
            var producto =
                _context.Productos.Find(id);

            if (producto != null)
            {
                producto.Activo = true;

                _context.SaveChanges();
            }

            TempData["ok"] =
                "Producto activado";

            return RedirectToAction("Productos");
        }

        // =========================
        // VENTAS
        // =========================

        public IActionResult Ventas()
        {
            var ventas = _context.Ventas
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .OrderByDescending(v => v.Fecha)
                .ToList();

            return View(ventas);
        }

        public IActionResult CrearVenta()
        {
            var viewModel =
                new VentaViewModel
                {
                    ProductosDisponibles =
                        _context.Productos
                        .Where(p => p.Activo && p.Stock > 0)
                        .OrderBy(p => p.Nombre)
                        .ToList()
                };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVenta(
            VentaViewModel model)
        {
            if (model.ProductosIds.Count == 0)
            {
                TempData["error"] =
                    "Debes agregar productos";

                return RedirectToAction("CrearVenta");
            }

            var venta = new Venta
            {
                ClienteNombre =
                    model.ClienteNombre,

                Fecha = DateTime.Now
            };

            decimal total = 0;

            for (int i = 0;
                 i < model.ProductosIds.Count;
                 i++)
            {
                var producto =
                    _context.Productos
                    .FirstOrDefault(p =>
                        p.Id ==
                        model.ProductosIds[i]);

                if (producto == null)
                {
                    continue;
                }

                var cantidad =
                    model.Cantidades[i];

                if (cantidad <= 0)
                {
                    continue;
                }

                if (producto.Stock < cantidad)
                {
                    TempData["error"] =
                        $"Stock insuficiente para {producto.Nombre}";

                    return RedirectToAction("CrearVenta");
                }

                producto.Stock -= cantidad;

                var subtotal =
                    producto.Precio * cantidad;

                total += subtotal;

                venta.Detalles.Add(
                    new VentaDetalle
                    {
                        ProductoId =
                            producto.Id,

                        Cantidad =
                            cantidad,

                        PrecioUnitario =
                            producto.Precio,

                        Subtotal =
                            subtotal
                    });
            }

            venta.Total = total;

            _context.Ventas.Add(venta);

            await _context.SaveChangesAsync();

            TempData["ok"] =
                "Venta registrada correctamente";

            return RedirectToAction("Ventas");
        }
        // =========================
        // CONFIGURACION
        // =========================

        public IActionResult Configuracion()
        {
            return View(ObtenerConfiguracion());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Configuracion(
            ConfiguracionSistema model)
        {
            var config =
                ObtenerConfiguracion();

            config.WhatsappActivo =
                model.WhatsappActivo;

            config.DashboardActivo =
                model.DashboardActivo;

            config.TiendaActiva =
                model.TiendaActiva;

            _context.SaveChanges();

            TempData["ok"] =
                "Configuracion actualizada";

            return RedirectToAction("Configuracion");
        }

        // =========================
        // METODOS PRIVADOS
        // =========================

        private async Task<string> GuardarImagen(
            IFormFile archivo,
            string carpetaDestino)
        {
            var extension =
                Path.GetExtension(archivo.FileName);

            var nombreArchivo =
                Guid.NewGuid().ToString() +
                extension;

            var carpeta =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    carpetaDestino);

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            var rutaCompleta =
                Path.Combine(
                    carpeta,
                    nombreArchivo);

            using (var stream =
                new FileStream(
                    rutaCompleta,
                    FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return
                "/uploads/" +
                carpetaDestino +
                "/" +
                nombreArchivo;
        }

        private ConfiguracionSistema ObtenerConfiguracion()
        {
            var config =
                _context.ConfiguracionSistema
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            if (config != null)
            {
                return config;
            }

            config = new ConfiguracionSistema();

            _context.ConfiguracionSistema
                .Add(config);

            _context.SaveChanges();

            return config;
        }

        private DashboardFinancieroViewModel
    ConstruirDashboardFinanciero()
        {
            // =========================
            // FECHAS BASE
            // =========================

            var hoy = DateTime.Today;

            var inicioSemana =
                hoy.AddDays(-(int)hoy.DayOfWeek);

            var inicioMes =
                new DateTime(
                    hoy.Year,
                    hoy.Month,
                    1);

            var inicioAnio =
                new DateTime(
                    hoy.Year,
                    1,
                    1);



            // =========================
            // CONFIGURACION
            // =========================

            var configuracion =
                ObtenerConfiguracion();

            // =========================
            // CONSULTA BASE
            // =========================

            var citas =
                _context.Citas
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .AsNoTracking()
                .ToList();

            // =========================
            // INGRESOS
            // =========================

            decimal ingresosDiarios =
                citas
                .Where(c =>
                    c.FechaHora.Date == hoy &&
                    c.Servicio != null)
                .Sum(c => c.Servicio!.Precio);

            decimal ingresosSemanales =
                citas
                .Where(c =>
                    c.FechaHora >= inicioSemana &&
                    c.Servicio != null)
                .Sum(c => c.Servicio!.Precio);

            decimal ingresosMensuales =
                citas
                .Where(c =>
                    c.FechaHora >= inicioMes &&
                    c.Servicio != null)
                .Sum(c => c.Servicio!.Precio);

            decimal ingresosAnuales =
                citas
                .Where(c =>
                    c.FechaHora >= inicioAnio &&
                    c.Servicio != null)
                .Sum(c => c.Servicio!.Precio);

            // =========================
            // SERVICIOS MAS VENDIDOS
            // =========================

            var serviciosMasVendidos =
                citas
                .Where(c => c.Servicio != null)
                .GroupBy(c => c.Servicio!.Nombre)
                .Select(g => new ServicioVendidoResumen
                {
                    Nombre = g.Key,

                    Cantidad =
                        g.Count(),

                    Total =
                        g.Sum(x =>
                            x.Servicio!.Precio)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToList();

            // =========================
            // BARBERO TOP
            // =========================

            var barberoTop =
                citas
                .Where(c =>
                    c.BarberoEntidad != null)
                .GroupBy(c =>
                    c.BarberoEntidad!.Nombre)
                .Select(g => new BarberoResumen
                {
                    Nombre = g.Key,

                    Cantidad =
                        g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .FirstOrDefault();

            // =========================
            // CLIENTES UNICOS
            // =========================

            var clientesUnicos =
                citas
                .Where(c =>
                    !string.IsNullOrWhiteSpace(
                        c.ClienteNombre))
                .Select(c =>
                    c.ClienteNombre.Trim()
                    .ToLower())
                .Distinct()
                .Count();

            // =========================
            // RESULTADO FINAL
            // =========================

            return new DashboardFinancieroViewModel
            {
                DashboardActivo =
                    configuracion.DashboardActivo,

                IngresosDiarios =
                    ingresosDiarios,

                IngresosSemanales =
                    ingresosSemanales,

                IngresosMensuales =
                    ingresosMensuales,

                IngresosAnuales =
                    ingresosAnuales,

                CantidadCitas =
                    citas.Count,

                CantidadClientes =
                    clientesUnicos,

                ServiciosMasVendidos =
                    serviciosMasVendidos,

                BarberoConMasCitas =
                    barberoTop
            };
        }
    }
}