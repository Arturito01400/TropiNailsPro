using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using System;
using System.Linq;

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

            // 🔒 Solo el propietario puede entrar (NO TOCADO)
            if (usuario != "Arturo Quezada Montero")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // 🔥 Traer usuarios con relaciones (sin romper estructura existente)
            var query = _context.Usuarios
                .Include(u => u.Clientas)
                .OrderByDescending(u => u.FechaRegistro)
                .AsQueryable();

            // 🔥 FILTROS (nuevo pero seguro)
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

            // 🔥 ESTADÍSTICAS PARA DASHBOARD (nuevas pero seguras)
            ViewBag.TotalUsuarios = _context.Usuarios.Count();

            ViewBag.TotalManicuristas = _context.Usuarios.Count(u => u.Rol == "Manicurista");

            ViewBag.TotalClientas = _context.Usuarios.Count(u => u.Rol == "Clienta");

            ViewBag.Pruebas = _context.Usuarios.Count(u => u.Plan == "Prueba");

            ViewBag.Suscritas = _context.Usuarios.Count(u => u.PlanActivo == true);

            ViewBag.Vencidas = _context.Usuarios.Count(u =>
                u.FechaVencimientoPlan != null &&
                u.FechaVencimientoPlan < DateTime.Now);


                ViewBag.ClientasPorManicurista =
    _context.Usuarios
        .Where(u => u.Rol == "Clienta")
        .ToList();


var manicuristas = _context.Usuarios
    .Where(x => x.Rol == "Manicurista")
    .ToList();

foreach (var m in manicuristas)
{
    var cantidad = _context.Usuarios.Count(x =>
        x.Rol == "Clienta" &&
        x.ManicuristaId == m.Id);

    Console.WriteLine(
        $"MANICURISTA: {m.Nombre} | UsuarioId: {m.Id} | Clientas: {cantidad}"
    );
}

ViewBag.Manicuristas = _context.Manicuristas.ToList();

            return View(usuarios);
        }
    }
}