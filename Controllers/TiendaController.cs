using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using OllinBarberApp.Models;

namespace OllinBarberApp.Controllers
{
    public class TiendaController : Controller
    {
        private const string CartSessionKey = "OllinCart";
        private const string LastOrderSessionKey = "OllinLastOrder";
        private static readonly string[] MetodosPagoPermitidos = { "Contra entrega", "Pago en barbería" };
        private static readonly string[] MetodosEntregaPermitidos = { "Recoger en barbería", "Domicilio coordinado" };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TiendaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? q,
            string? categoria,
            string? marca,
            string? orden)
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            var query = _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Precio > 0)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var termino = q.Trim();
                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre, $"%{termino}%") ||
                    EF.Functions.ILike(p.Descripcion, $"%{termino}%") ||
                    EF.Functions.ILike(p.Categoria, $"%{termino}%") ||
                    EF.Functions.ILike(p.Marca, $"%{termino}%"));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
                query = query.Where(p => p.Categoria == categoria);

            if (!string.IsNullOrWhiteSpace(marca))
                query = query.Where(p => p.Marca == marca);

            query = orden switch
            {
                "precio-asc" => query.OrderBy(p => p.Precio * (1 - p.DescuentoPorcentaje / 100m)),
                "precio-desc" => query.OrderByDescending(p => p.Precio * (1 - p.DescuentoPorcentaje / 100m)),
                "nombre-desc" => query.OrderByDescending(p => p.Nombre),
                _ => query.OrderBy(p => p.Categoria).ThenBy(p => p.Nombre)
            };

            ViewBag.Categorias = await _context.Productos.AsNoTracking()
                .Where(p => p.Activo && p.Precio > 0 && p.Categoria != "")
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            ViewBag.Marcas = await _context.Productos.AsNoTracking()
                .Where(p => p.Activo && p.Precio > 0 && p.Marca != "")
                .Select(p => p.Marca)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Categoria = categoria;
            ViewBag.Marca = marca;
            ViewBag.Orden = orden;
            ViewBag.CarritoCantidad = ObtenerCarrito().Values.Sum();

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Detalle(int id)
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            var producto = await _context.Productos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo && p.Precio > 0);

            return producto == null ? NotFound() : View(producto);
        }

        public async Task<IActionResult> Carrito()
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            return View(await ConstruirCarrito());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int productoId, int cantidad = 1, string? returnUrl = null)
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            var producto = await _context.Productos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productoId && p.Activo && p.Precio > 0);

            if (producto == null)
            {
                TempData["error"] = "El producto ya no está disponible.";
                return RedirectToAction(nameof(Index));
            }

            if (producto.Stock <= 0)
            {
                TempData["error"] = "Producto agotado.";
                return VolverSeguro(returnUrl);
            }

            cantidad = Math.Max(1, cantidad);
            var carrito = ObtenerCarrito();
            carrito.TryGetValue(productoId, out var actual);
            var nuevaCantidad = actual + cantidad;

            if (nuevaCantidad > producto.Stock)
            {
                TempData["error"] = $"Solo hay {producto.Stock} unidad(es) disponibles de {producto.Nombre}.";
                return VolverSeguro(returnUrl);
            }

            carrito[productoId] = nuevaCantidad;
            GuardarCarrito(carrito);
            TempData["ok"] = "Producto agregado al carrito.";
            return VolverSeguro(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(Dictionary<int, int> cantidades)
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            var carrito = ObtenerCarrito();
            var ids = carrito.Keys.ToList();
            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.Activo && p.Precio > 0)
                .ToDictionaryAsync(p => p.Id);

            foreach (var id in ids)
            {
                if (!productos.TryGetValue(id, out var producto))
                {
                    carrito.Remove(id);
                    continue;
                }

                var solicitada = cantidades.TryGetValue(id, out var valor) ? valor : carrito[id];
                if (solicitada <= 0)
                {
                    carrito.Remove(id);
                    continue;
                }

                carrito[id] = Math.Min(solicitada, producto.Stock);
                if (solicitada > producto.Stock)
                    TempData["error"] = $"Se ajustó {producto.Nombre} al stock disponible ({producto.Stock}).";
            }

            GuardarCarrito(carrito);
            TempData["ok"] ??= "Carrito actualizado.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int productoId)
        {
            var carrito = ObtenerCarrito();
            carrito.Remove(productoId);
            GuardarCarrito(carrito);
            TempData["ok"] = "Producto eliminado del carrito.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove(CartSessionKey);
            TempData["ok"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            var carrito = await ConstruirCarrito();
            if (!carrito.Items.Any())
            {
                TempData["error"] = "Tu carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            var model = new CheckoutViewModel { Carrito = carrito };
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.ClienteNombre = user.Nombre;
                    model.Email = user.Email ?? string.Empty;
                    model.Telefono = user.Celular;
                }
            }

            CargarOpcionesCheckout();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!await TiendaEstaActiva())
                return NotFound();

            model.ClienteNombre = model.ClienteNombre?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.Telefono = model.Telefono?.Trim() ?? string.Empty;
            model.Direccion = model.Direccion?.Trim() ?? string.Empty;
            model.Ciudad = model.Ciudad?.Trim() ?? string.Empty;

            if (!MetodosPagoPermitidos.Contains(model.MetodoPago))
                ModelState.AddModelError(nameof(model.MetodoPago), "Método de pago no válido.");

            if (!MetodosEntregaPermitidos.Contains(model.MetodoEntrega))
                ModelState.AddModelError(nameof(model.MetodoEntrega), "Método de entrega no válido.");

            var carritoSesion = ObtenerCarrito();
            if (!carritoSesion.Any())
                ModelState.AddModelError(string.Empty, "Tu carrito está vacío.");

            model.Carrito = await ConstruirCarrito();
            if (!ModelState.IsValid)
            {
                CargarOpcionesCheckout();
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var ids = carritoSesion.Keys.ToList();
                var productos = await _context.Productos
                    .Where(p => ids.Contains(p.Id) && p.Activo && p.Precio > 0)
                    .ToDictionaryAsync(p => p.Id);

                if (productos.Count != ids.Count)
                {
                    ModelState.AddModelError(string.Empty, "Uno de los productos ya no está disponible. Revisa el carrito.");
                    await transaction.RollbackAsync();
                    model.Carrito = await ConstruirCarrito();
                    CargarOpcionesCheckout();
                    return View(model);
                }

                var pedido = new Pedido
                {
                    Codigo = GenerarCodigoPedido(),
                    UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ClienteNombre = model.ClienteNombre,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    Direccion = model.Direccion,
                    Ciudad = model.Ciudad,
                    MetodoPago = model.MetodoPago,
                    MetodoEntrega = model.MetodoEntrega,
                    Notas = string.IsNullOrWhiteSpace(model.Notas) ? null : model.Notas.Trim(),
                    Fecha = DateTimeOffset.UtcNow,
                    Estado = EstadoPedido.Pendiente
                };

                decimal subtotal = 0;
                decimal descuentos = 0;

                foreach (var (productoId, cantidad) in carritoSesion)
                {
                    var producto = productos[productoId];
                    if (cantidad <= 0 || cantidad > producto.Stock)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Stock}.");
                        await transaction.RollbackAsync();
                        model.Carrito = await ConstruirCarrito();
                        CargarOpcionesCheckout();
                        return View(model);
                    }

                    var precioFinal = producto.PrecioFinal;
                    var descuentoUnitario = producto.Precio - precioFinal;
                    subtotal += producto.Precio * cantidad;
                    descuentos += descuentoUnitario * cantidad;

                    pedido.Detalles.Add(new PedidoDetalle
                    {
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.Precio,
                        DescuentoUnitario = descuentoUnitario,
                        Subtotal = precioFinal * cantidad
                    });

                    producto.Stock -= cantidad;
                }

                pedido.Subtotal = subtotal;
                pedido.DescuentoTotal = descuentos;
                pedido.Total = subtotal - descuentos;

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(CartSessionKey);
                HttpContext.Session.SetString(LastOrderSessionKey, pedido.Codigo);
                TempData["ok"] = "Pedido creado correctamente.";
                return RedirectToAction(nameof(Confirmacion), new { codigo = pedido.Codigo });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty,
                    "No pudimos confirmar el pedido porque el inventario cambió. Revisa el carrito e inténtalo de nuevo.");
                model.Carrito = await ConstruirCarrito();
                CargarOpcionesCheckout();
                return View(model);
            }
        }

        public async Task<IActionResult> Confirmacion(string codigo)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Codigo == codigo);

            if (pedido == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var esPropietario = !string.IsNullOrWhiteSpace(userId) && pedido.UsuarioId == userId;
            var vieneDeCheckout = HttpContext.Session.GetString(LastOrderSessionKey) == codigo;

            if (!esPropietario && !vieneDeCheckout && !User.IsInRole("Admin"))
                return Forbid();

            return View(pedido);
        }

        [Authorize]
        public async Task<IActionResult> MisPedidos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.UsuarioId == userId)
                .Include(p => p.Detalles)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return View(pedidos);
        }

        [HttpGet]
        public IActionResult Seguimiento() => View(new SeguimientoPedidoViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Seguimiento(SeguimientoPedidoViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var codigo = model.Codigo.Trim().ToUpperInvariant();
            var telefono = model.Telefono.Trim();

            model.Pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Codigo == codigo && p.Telefono == telefono);

            if (model.Pedido == null)
                ModelState.AddModelError(string.Empty, "No encontramos un pedido con ese código y celular.");

            return View(model);
        }

        private async Task<bool> TiendaEstaActiva() =>
            await _context.ConfiguracionSistema.AsNoTracking()
                .OrderByDescending(c => c.Id)
                .Select(c => c.TiendaActiva)
                .FirstOrDefaultAsync();

        private Dictionary<int, int> ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<int, int>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
            }
            catch (JsonException)
            {
                HttpContext.Session.Remove(CartSessionKey);
                return new Dictionary<int, int>();
            }
        }

        private void GuardarCarrito(Dictionary<int, int> carrito) =>
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(carrito));

        private async Task<CarritoViewModel> ConstruirCarrito()
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Any())
                return new CarritoViewModel();

            var ids = carrito.Keys.ToList();
            var productos = await _context.Productos.AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.Activo && p.Precio > 0)
                .ToDictionaryAsync(p => p.Id);

            var limpio = new Dictionary<int, int>();
            var viewModel = new CarritoViewModel();

            foreach (var (id, cantidadOriginal) in carrito)
            {
                if (!productos.TryGetValue(id, out var producto) || producto.Stock <= 0)
                    continue;

                var cantidad = Math.Min(Math.Max(cantidadOriginal, 1), producto.Stock);
                limpio[id] = cantidad;
                viewModel.Items.Add(new CarritoItemViewModel { Producto = producto, Cantidad = cantidad });
            }

            if (limpio.Count != carrito.Count || limpio.Any(x => !carrito.TryGetValue(x.Key, out var c) || c != x.Value))
                GuardarCarrito(limpio);

            return viewModel;
        }

        private IActionResult VolverSeguro(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        private void CargarOpcionesCheckout()
        {
            ViewBag.MetodosPago = MetodosPagoPermitidos;
            ViewBag.MetodosEntrega = MetodosEntregaPermitidos;
        }

        private string GenerarCodigoPedido() =>
            "OB-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }
}
