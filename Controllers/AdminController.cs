using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
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

            var hoy = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).Date;
            var inicioHoy = new DateTimeOffset(hoy, TimeSpan.FromHours(-5));
            var finHoy = inicioHoy.AddDays(1);

            ViewBag.CitasHoy =
                _context.Citas.Count(c =>
                    c.FechaHora >= inicioHoy &&
                    c.FechaHora < finHoy);

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
            ValidarImagen(ImagenArchivo);
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

            ViewBag.TieneAcceso = _context.Users.AsNoTracking()
                .Any(u => u.EsBarbero && (u.Celular == barbero.Telefono || u.Nombre == barbero.Nombre));

            return View(barbero);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarBarbero(
    Barbero barbero,
    IFormFile? ImagenArchivo)
        {
            ValidarImagen(ImagenArchivo);
            if (!ModelState.IsValid)
            {
                ViewBag.TieneAcceso = await _context.Users.AsNoTracking()
                    .AnyAsync(u => u.EsBarbero && (u.Celular == barbero.Telefono || u.Nombre == barbero.Nombre));
                return View(barbero);
            }

            var existente = await _context.Barberos
                .FirstOrDefaultAsync(b => b.Id == barbero.Id);

            if (existente == null)
            {
                return NotFound();
            }

            var usuarioBarbero = await _context.Users.FirstOrDefaultAsync(u => u.EsBarbero &&
                (u.Celular == existente.Telefono || u.Nombre == existente.Nombre));

            // Datos básicos
            existente.Nombre = barbero.Nombre;
            existente.Telefono = barbero.Telefono;
            existente.Disponible = barbero.Disponible;
            existente.Activo = barbero.Activo;

            if (usuarioBarbero != null)
            {
                usuarioBarbero.Nombre = barbero.Nombre;
                usuarioBarbero.Celular = barbero.Telefono;
                usuarioBarbero.Disponible = barbero.Disponible && barbero.Activo;
            }

            // Solo cambiar la imagen si realmente se seleccionó una nueva
            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                existente.ImagenUrl = await GuardarImagen(
                    ImagenArchivo,
                    "barberos");
            }

            // Si NO se seleccionó imagen,
            // se conserva la que ya tenía el barbero.

            _context.Update(existente);

            await _context.SaveChangesAsync();

            TempData["ok"] = "Barbero actualizado correctamente.";

            return RedirectToAction(nameof(Barberos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearAccesoBarbero(int id, string email, string password)
        {
            var barbero = await _context.Barberos.FindAsync(id);
            if (barbero == null) return NotFound();

            email = email?.Trim() ?? string.Empty;
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                TempData["error"] = "Ingresa un correo válido para el acceso del barbero.";
                return RedirectToAction(nameof(EditarBarbero), new { id });
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                TempData["error"] = "Ese correo ya pertenece a una cuenta existente.";
                return RedirectToAction(nameof(EditarBarbero), new { id });
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nombre = barbero.Nombre,
                Celular = barbero.Telefono,
                EsBarbero = true,
                Disponible = barbero.Disponible
            };

            var creado = await _userManager.CreateAsync(user, password);
            if (!creado.Succeeded)
            {
                TempData["error"] = string.Join(" ", creado.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(EditarBarbero), new { id });
            }

            var rol = await _userManager.AddToRoleAsync(user, "Barbero");
            if (!rol.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                TempData["error"] = "No se pudo asignar el rol Barbero. No se creó el acceso.";
                return RedirectToAction(nameof(EditarBarbero), new { id });
            }

            TempData["ok"] = "Acceso de barbero creado correctamente.";
            return RedirectToAction(nameof(EditarBarbero), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevocarAccesoBarbero(int id)
        {
            var barbero = await _context.Barberos.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (barbero == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EsBarbero &&
                (u.Celular == barbero.Telefono || u.Nombre == barbero.Nombre));

            if (user != null)
            {
                await _userManager.RemoveFromRoleAsync(user, "Barbero");
                user.EsBarbero = false;
                user.Disponible = false;
                await _userManager.UpdateAsync(user);
            }

            TempData["ok"] = "Permiso de barbero revocado.";
            return RedirectToAction(nameof(EditarBarbero), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarBarbero(int id)
        {
            var barbero = await _context.Barberos.FindAsync(id);
            if (barbero == null) return NotFound();
            barbero.Activo = false;
            barbero.Disponible = false;
            await _context.SaveChangesAsync();
            TempData["ok"] = "Barbero desactivado correctamente.";
            return RedirectToAction(nameof(Barberos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivarBarbero(int id)
        {
            var barbero = await _context.Barberos.FindAsync(id);
            if (barbero == null) return NotFound();
            barbero.Activo = true;
            await _context.SaveChangesAsync();
            TempData["ok"] = "Barbero activado correctamente.";
            return RedirectToAction(nameof(Barberos));
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
        public async Task<IActionResult> CrearServicio(
            Servicio servicio,
            IFormFile? ImagenArchivo)
        {
            ValidarImagen(ImagenArchivo);
            if (!ModelState.IsValid)
            {
                return View(servicio);
            }

            servicio.Nombre = servicio.Nombre.Trim();
            servicio.Tipo = servicio.Tipo?.Trim() ?? string.Empty;

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                servicio.ImagenUrl = await GuardarImagen(ImagenArchivo, "servicios");
            }

            _context.Servicios.Add(servicio);
            await _context.SaveChangesAsync();

            TempData["ok"] = "Servicio creado correctamente.";
            return RedirectToAction(nameof(Servicios));
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
        public async Task<IActionResult> EditarServicio(
            Servicio servicio,
            IFormFile? ImagenArchivo)
        {
            ValidarImagen(ImagenArchivo);
            if (!ModelState.IsValid)
            {
                return View(servicio);
            }

            var existente = await _context.Servicios.FindAsync(servicio.Id);
            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre = servicio.Nombre.Trim();
            existente.Duracion = servicio.Duracion;
            existente.Precio = servicio.Precio;
            existente.Tipo = servicio.Tipo?.Trim() ?? string.Empty;
            existente.Activo = servicio.Activo;

            if (ImagenArchivo != null && ImagenArchivo.Length > 0)
            {
                existente.ImagenUrl = await GuardarImagen(ImagenArchivo, "servicios");
            }

            await _context.SaveChangesAsync();

            TempData["ok"] = "Servicio actualizado correctamente.";
            return RedirectToAction(nameof(Servicios));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            ValidarImagen(ImagenArchivo);
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
            ValidarImagen(ImagenArchivo);
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
            existente.Marca = producto.Marca;
            existente.Precio = producto.Precio;
            existente.DescuentoPorcentaje = producto.DescuentoPorcentaje;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                        .Where(p => p.Activo && p.Stock > 0 && p.Precio > 0)
                        .OrderBy(p => p.Nombre)
                        .ToList()
                };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVenta(VentaViewModel model)
        {
            model.ClienteNombre = model.ClienteNombre?.Trim() ?? string.Empty;

            if (model.ProductosIds.Count == 0 || model.ProductosIds.Count != model.Cantidades.Count)
                ModelState.AddModelError(string.Empty, "Agrega al menos un producto con una cantidad válida.");

            var lineas = model.ProductosIds
                .Zip(model.Cantidades, (id, cantidad) => new { id, cantidad })
                .Where(x => x.cantidad > 0)
                .GroupBy(x => x.id)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.cantidad) })
                .ToList();

            if (!lineas.Any())
                ModelState.AddModelError(string.Empty, "Las cantidades deben ser mayores que cero.");

            if (!ModelState.IsValid)
            {
                await PrepararVentaModel(model);
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var ids = lineas.Select(x => x.ProductoId).ToList();
                var productos = await _context.Productos
                    .Where(p => ids.Contains(p.Id) && p.Activo)
                    .ToDictionaryAsync(p => p.Id);

                if (productos.Count != ids.Count)
                {
                    ModelState.AddModelError(string.Empty, "Uno de los productos ya no está disponible.");
                    await transaction.RollbackAsync();
                    await PrepararVentaModel(model);
                    return View(model);
                }

                var venta = new Venta
                {
                    ClienteNombre = model.ClienteNombre,
                    Fecha = DateTime.UtcNow
                };

                foreach (var linea in lineas)
                {
                    var producto = productos[linea.ProductoId];
                    if (producto.Stock < linea.Cantidad)
                    {
                        ModelState.AddModelError(string.Empty, $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Stock}.");
                        await transaction.RollbackAsync();
                        await PrepararVentaModel(model);
                        return View(model);
                    }

                    producto.Stock -= linea.Cantidad;
                    var subtotal = producto.Precio * linea.Cantidad;
                    venta.Total += subtotal;
                    venta.Detalles.Add(new VentaDetalle
                    {
                        ProductoId = producto.Id,
                        Cantidad = linea.Cantidad,
                        PrecioUnitario = producto.Precio,
                        Subtotal = subtotal
                    });
                }

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ok"] = "Venta registrada correctamente.";
                return RedirectToAction(nameof(Ventas));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "No se pudo registrar la venta porque el inventario cambió. Revisa las cantidades.");
                await PrepararVentaModel(model);
                return View(model);
            }
        }

        // =========================
        // PEDIDOS DE TIENDA
        // =========================

        public async Task<IActionResult> Pedidos(EstadoPedido? estado)
        {
            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .AsQueryable();

            if (estado.HasValue)
                query = query.Where(p => p.Estado == estado.Value);

            ViewBag.Estado = estado;
            return View(await query.OrderByDescending(p => p.Fecha).ToListAsync());
        }

        public async Task<IActionResult> Pedido(int id)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            return pedido == null ? NotFound() : View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarEstadoPedido(int id, EstadoPedido estado)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (!EsTransicionValida(pedido.Estado, estado))
            {
                TempData["error"] = $"No se puede cambiar un pedido de {pedido.Estado} a {estado}.";
                return RedirectToAction(nameof(Pedido), new { id });
            }

            if (estado == EstadoPedido.Cancelado && pedido.Estado != EstadoPedido.Cancelado)
            {
                var ids = pedido.Detalles.Select(d => d.ProductoId).ToList();
                var productos = await _context.Productos
                    .Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                foreach (var detalle in pedido.Detalles)
                {
                    if (productos.TryGetValue(detalle.ProductoId, out var producto))
                        producto.Stock += detalle.Cantidad;
                }
            }

            pedido.Estado = estado;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ok"] = $"Pedido actualizado a {estado}.";
            return RedirectToAction(nameof(Pedido), new { id });
        }

        public async Task<IActionResult> Clientes()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var barberos = await _userManager.GetUsersInRoleAsync("Barbero");
            var idsExcluidos = admins.Select(a => a.Id)
                .Concat(barberos.Select(b => b.Id))
                .ToHashSet();

            var usuarios = await _context.Users.AsNoTracking()
                .Where(u => !u.EsBarbero)
                .ToListAsync();

            var pedidos = await _context.Pedidos.AsNoTracking()
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            var clientes = new List<ClienteAdminViewModel>();

            foreach (var usuario in usuarios.Where(u => !idsExcluidos.Contains(u.Id)))
            {
                clientes.Add(new ClienteAdminViewModel
                {
                    Nombre = usuario.Nombre,
                    Email = usuario.Email ?? string.Empty,
                    Telefono = usuario.Celular,
                    TieneCuenta = true,
                    CuentaBloqueada = usuario.LockoutEnd.HasValue && usuario.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            foreach (var pedido in pedidos)
            {
                var cliente = clientes.FirstOrDefault(c =>
                    (!string.IsNullOrWhiteSpace(pedido.Email) &&
                     string.Equals(c.Email, pedido.Email, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(pedido.Telefono) && c.Telefono == pedido.Telefono));

                if (cliente == null)
                {
                    cliente = new ClienteAdminViewModel
                    {
                        Nombre = pedido.ClienteNombre,
                        Email = pedido.Email,
                        Telefono = pedido.Telefono,
                        TieneCuenta = false
                    };
                    clientes.Add(cliente);
                }

                cliente.CantidadPedidos++;
                if (!cliente.UltimoPedido.HasValue || pedido.Fecha > cliente.UltimoPedido.Value)
                    cliente.UltimoPedido = pedido.Fecha;
                if (pedido.Estado == EstadoPedido.Entregado)
                    cliente.TotalEntregado += pedido.Total;
            }

            return View(clientes
                .OrderByDescending(c => c.UltimoPedido.HasValue)
                .ThenByDescending(c => c.UltimoPedido)
                .ThenBy(c => c.Nombre)
                .ToList());
        }

        public IActionResult ExportarCsv()
        {
            var model = ConstruirDashboardFinanciero();
            var sb = new StringBuilder();
            sb.AppendLine("Indicador,Valor");
            sb.AppendLine($"Ingresos diarios,{model.IngresosDiarios.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Ingresos semanales,{model.IngresosSemanales.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Ingresos mensuales,{model.IngresosMensuales.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Ingresos anuales,{model.IngresosAnuales.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            sb.AppendLine($"Citas,{model.CantidadCitas}");
            sb.AppendLine($"Clientes,{model.CantidadClientes}");
            var fechaColombia = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", $"ollin-dashboard-{fechaColombia:yyyyMMdd}.csv");
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

        private async Task PrepararVentaModel(VentaViewModel model)
        {
            model.ProductosDisponibles = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Stock > 0 && p.Precio > 0)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        private static bool EsTransicionValida(EstadoPedido actual, EstadoPedido siguiente)
        {
            if (actual == siguiente) return true;
            if (actual is EstadoPedido.Entregado or EstadoPedido.Cancelado) return false;

            return actual switch
            {
                EstadoPedido.Pendiente => siguiente is EstadoPedido.Confirmado or EstadoPedido.Cancelado,
                EstadoPedido.Confirmado => siguiente is EstadoPedido.Procesando or EstadoPedido.Cancelado,
                EstadoPedido.Procesando => siguiente is EstadoPedido.Enviado or EstadoPedido.Cancelado,
                EstadoPedido.Enviado => siguiente == EstadoPedido.Entregado,
                _ => false
            };
        }

        private void ValidarImagen(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0) return;

            if (archivo.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImagenArchivo", "La imagen no puede superar 5 MB.");
                return;
            }

            var tipo = archivo.ContentType?.ToLowerInvariant();
            if (tipo is not ("image/jpeg" or "image/png" or "image/webp"))
            {
                ModelState.AddModelError("ImagenArchivo", "Solo se permiten imágenes JPG, PNG o WEBP.");
                return;
            }

            Span<byte> cabecera = stackalloc byte[12];
            using var stream = archivo.OpenReadStream();
            var leidos = stream.Read(cabecera);
            var jpeg = leidos >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF;
            var png = leidos >= 8 && cabecera[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var webp = leidos >= 12 &&
                       cabecera[..4].SequenceEqual("RIFF"u8) &&
                       cabecera.Slice(8, 4).SequenceEqual("WEBP"u8);

            var firmaValida = tipo switch
            {
                "image/jpeg" => jpeg,
                "image/png" => png,
                "image/webp" => webp,
                _ => false
            };

            if (!firmaValida)
                ModelState.AddModelError("ImagenArchivo", "El contenido del archivo no corresponde a una imagen válida.");
        }

        private async Task<string> GuardarImagen(
            IFormFile archivo,
            string carpetaDestino)
        {
            var extension = archivo.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => throw new InvalidOperationException("Tipo de imagen no permitido.")
            };

            var nombreArchivo = Guid.NewGuid().ToString("N") + extension;

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

        private DashboardFinancieroViewModel ConstruirDashboardFinanciero()
        {
            var hoy = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).Date;
            var inicioSemana = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAnio = new DateTime(hoy.Year, 1, 1);
            var configuracion = ObtenerConfiguracion();

            var citas = _context.Citas
                .Include(c => c.Servicio)
                .Include(c => c.BarberoEntidad)
                .AsNoTracking()
                .ToList();

            var citasAtendidas = citas
                .Where(c => c.Estado == EstadoCita.Atendido && c.Servicio != null)
                .ToList();

            var ventas = _context.Ventas.AsNoTracking().ToList();
            var pedidosEntregados = _context.Pedidos.AsNoTracking()
                .Where(p => p.Estado == EstadoPedido.Entregado)
                .ToList();

            decimal IngresosDesde(DateTime inicio, DateTime? fin = null)
            {
                bool EnRango(DateTime fecha) => fecha >= inicio && (!fin.HasValue || fecha < fin.Value);

                var servicios = citasAtendidas
                    .Where(c => EnRango(c.FechaHora.ToOffset(TimeSpan.FromHours(-5)).Date))
                    .Sum(c => c.Servicio!.Precio);
                var puntoVenta = ventas.Where(v => EnRango(v.Fecha.Date)).Sum(v => v.Total);
                var tienda = pedidosEntregados
                    .Where(p => EnRango(p.Fecha.ToOffset(TimeSpan.FromHours(-5)).Date))
                    .Sum(p => p.Total);
                return servicios + puntoVenta + tienda;
            }

            var serviciosMasVendidos = citasAtendidas
                .GroupBy(c => c.Servicio!.Nombre)
                .Select(g => new ServicioVendidoResumen
                {
                    Nombre = g.Key,
                    Cantidad = g.Count(),
                    Total = g.Sum(x => x.Servicio!.Precio)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToList();

            var barberoTop = citasAtendidas
                .Where(c => c.BarberoEntidad != null)
                .GroupBy(c => c.BarberoEntidad!.Nombre)
                .Select(g => new BarberoResumen { Nombre = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .FirstOrDefault();

            var clientesUnicos = citas
                .Where(c => !string.IsNullOrWhiteSpace(c.Telefono))
                .Select(c => c.Telefono.Trim())
                .Concat(_context.Pedidos.AsNoTracking().Select(p => p.Telefono))
                .Distinct()
                .Count();

            var ingresosAnuales = IngresosDesde(inicioAnio, inicioAnio.AddYears(1));
            var transaccionesAnio = citasAtendidas.Count(c => c.FechaHora.ToOffset(TimeSpan.FromHours(-5)).Year == hoy.Year)
                + ventas.Count(v => v.Fecha.Year == hoy.Year)
                + pedidosEntregados.Count(p => p.Fecha.ToOffset(TimeSpan.FromHours(-5)).Year == hoy.Year);

            return new DashboardFinancieroViewModel
            {
                DashboardActivo = configuracion.DashboardActivo,
                IngresosDiarios = IngresosDesde(hoy, hoy.AddDays(1)),
                IngresosSemanales = IngresosDesde(inicioSemana, inicioSemana.AddDays(7)),
                IngresosMensuales = IngresosDesde(inicioMes, inicioMes.AddMonths(1)),
                IngresosAnuales = ingresosAnuales,
                TicketPromedio = transaccionesAnio == 0 ? 0 : ingresosAnuales / transaccionesAnio,
                CantidadCitas = citas.Count,
                CantidadClientes = clientesUnicos,
                CitasPendientes = citas.Count(c => c.Estado == EstadoCita.Pendiente),
                CitasCanceladas = citas.Count(c => c.Estado == EstadoCita.Cancelada),
                ServiciosMasVendidos = serviciosMasVendidos,
                BarberoConMasCitas = barberoTop
            };
        }
    }
}