
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TropiNailsPro.Controllers
{
    public class PropietarioController : Controller
    {
        private readonly AppDbContext _context;

        public PropietarioController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Crecimiento(string filtro = "todos")
        {
            var usuario = HttpContext.Session.GetString("UsuarioNombre");

            // Solo el propietario puede entrar.
            if (usuario != "Arturo Quezada Montero")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var query = _context.Usuarios
                .Include(u => u.Manicurista)
                .OrderByDescending(u => u.FechaRegistro)
                .AsQueryable();

            switch (filtro)
            {
                case "manicuristas":
                    query = query.Where(u => u.Rol == "Manicurista");
                    break;

                case "clientas":
                    query = query.Where(u => u.Rol == "Clienta");
                    break;

                case "prueba":
                    query = query.Where(u => u.Plan == "Prueba");
                    break;

                case "suscritas":
                    query = query.Where(u => u.PlanActivo == true);
                    break;

                case "vencidas":
                    query = query.Where(u =>
                        u.FechaVencimientoPlan != null &&
                        u.FechaVencimientoPlan < DateTime.Now);
                    break;

                default:
                    break;
            }

            var usuarios = query.ToList();

            // Estadísticas del panel.
            ViewBag.TotalUsuarios = _context.Usuarios.Count();

            ViewBag.TotalManicuristas =
                _context.Usuarios.Count(u => u.Rol == "Manicurista");

            ViewBag.TotalClientas =
                _context.Usuarios.Count(u => u.Rol == "Clienta");

            ViewBag.Pruebas =
                _context.Usuarios.Count(u => u.Plan == "Prueba");

            ViewBag.Suscritas =
                _context.Usuarios.Count(u => u.PlanActivo == true);

            ViewBag.Vencidas = _context.Usuarios.Count(u =>
                u.FechaVencimientoPlan != null &&
                u.FechaVencimientoPlan < DateTime.Now);

            ViewBag.ClientasPorManicurista = _context.Usuarios
                .Where(u => u.Rol == "Clienta")
                .ToList();

            var manicuristas = _context.Usuarios
                .Where(u => u.Rol == "Manicurista")
                .ToList();

            foreach (var m in manicuristas)
            {
                var cantidad = _context.Usuarios.Count(u =>
                    u.Rol == "Clienta" &&
                    u.ManicuristaId == m.Id);

                Console.WriteLine(
                    $"MANICURISTA: {m.Nombre} | UsuarioId: {m.Id} | Clientas: {cantidad}"
                );
            }

            ViewBag.Manicuristas = _context.Manicuristas.ToList();

            return View(usuarios);
        }

        public IActionResult AdministrarUsuario(int id)
        {
            // Solo el propietario puede entrar.
            var usuarioSesion =
                HttpContext.Session.GetString("UsuarioNombre");

            if (usuarioSesion != "Arturo Quezada Montero")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var usuario = _context.Usuarios
                .Include(u => u.Manicurista)
                .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        [HttpPost]
        public IActionResult RegalarMeses(int id, int meses)
        {
            // Solo el propietario puede realizar esta acción.
            var usuarioSesion =
                HttpContext.Session.GetString("UsuarioNombre");

            if (usuarioSesion != "Arturo Quezada Montero")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            if (meses <= 0)
            {
                TempData["Error"] = "La cantidad de meses debe ser mayor que cero.";

                return RedirectToAction(
                    nameof(AdministrarUsuario),
                    new { id });
            }

            // Si nunca ha tenido un plan o ya venció, comenzar desde hoy.
            var fechaBase =
                usuario.FechaVencimientoPlan.HasValue &&
                usuario.FechaVencimientoPlan.Value > DateTime.Now
                    ? usuario.FechaVencimientoPlan.Value
                    : DateTime.Now;

            // Actualizar datos principales del usuario.
            usuario.PlanActivo = true;
            usuario.Plan = "Premium";

            if (!usuario.FechaInicioPlan.HasValue)
            {
                usuario.FechaInicioPlan = DateTime.Now;
            }

            usuario.FechaVencimientoPlan = fechaBase.AddMonths(meses);

            // Actualizar la suscripción real del sistema.
            var manicurista = _context.Manicuristas
                .FirstOrDefault(m => m.UsuarioId == usuario.Id);

            if (manicurista != null)
            {
                var suscripcion = _context.Suscripciones
                    .Where(s => s.ManicuristaId == manicurista.Id)
                    .OrderByDescending(s => s.FechaInicio)
                    .FirstOrDefault();

                if (suscripcion != null)
                {
                    suscripcion.Activa = true;
                    suscripcion.Cancelada = false;
                    suscripcion.Plan = "Premium";
                    suscripcion.FechaVencimiento =
                        fechaBase.AddMonths(meses);

                    suscripcion.EstadoPago = "REGALO";
                    suscripcion.MetodoPago = "Administrador";
                }
                else
                {
                    var nuevaSuscripcion = new Suscripcion
                    {
                        ManicuristaId = manicurista.Id,
                        FechaInicio = DateTime.Now,
                        FechaVencimiento = fechaBase.AddMonths(meses),
                        Plan = "Premium",
                        Activa = true,
                        Cancelada = false,
                        MetodoPago = "Administrador",
                        EstadoPago = "REGALO",
                        Monto = 0,
                        Moneda = "USD"
                    };

                    _context.Suscripciones.Add(nuevaSuscripcion);
                }
            }

            _context.SaveChanges();

            TempData["Success"] =
                $"Se regalaron {meses} mes(es) a {usuario.Nombre}.";

            return RedirectToAction(
                nameof(AdministrarUsuario),
                new { id });
        }

        
        // ============================================================
        // RETIRAR CUENTA DE CLIENTA Y LIBERAR SUS DATOS DE REGISTRO
        //
        // Conserva el registro de Usuario y su Id para mantener
        // los vínculos con el historial financiero.
        // No elimina citas, pagos, publicaciones ni otras relaciones.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuarioSesion =
                HttpContext.Session.GetString("UsuarioNombre");

            if (usuarioSesion != "Arturo Quezada Montero")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var usuarioSesionId =
                HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioSesionId.HasValue &&
                usuarioSesionId.Value == id)
            {
                TempData["Error"] =
                    "No puedes retirar tu propia cuenta.";

                return RedirectToAction(
                    nameof(AdministrarUsuario),
                    new { id });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                TempData["Error"] =
                    "La cuenta seleccionada no existe.";

                return RedirectToAction(nameof(Crecimiento));
            }

            if (usuario.Nombre == "Arturo Quezada Montero")
            {
                TempData["Error"] =
                    "La cuenta del propietario está protegida.";

                return RedirectToAction(nameof(Crecimiento));
            }

            if (!string.Equals(
                    usuario.Rol,
                    "Clienta",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Esta operación solo está permitida para cuentas de clientas.";

                return RedirectToAction(
                    nameof(AdministrarUsuario),
                    new { id });
            }

            try
            {
                // Desactivar el acceso a la cuenta anterior.
                usuario.Activo = false;
                usuario.PlanActivo = false;

                // Liberar los datos únicos para permitir
                // un nuevo registro con las mismas credenciales.
                usuario.Email = null;
                usuario.Telefono = null;
                usuario.UsuarioLogin = null;

                // Invalidar cualquier recuperación pendiente.
                usuario.ResetToken = null;
                usuario.TokenExpira = null;
                usuario.CodigoSMS = null;
                usuario.CodigoSMSExpira = null;

                // Desvincularla de la manicurista, sin eliminar
                // las citas ni los registros operativos históricos.
                usuario.ManicuristaId = null;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "La cuenta anterior fue desactivada y sus datos de registro fueron liberados. El historial financiero se conservó.";
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "No se pudo actualizar la cuenta. Verifica el estado de la base de datos antes de volver a intentarlo.";

                return RedirectToAction(
                    nameof(AdministrarUsuario),
                    new { id });
            }

            
            return RedirectToAction(nameof(Crecimiento));
        }
    }
}



