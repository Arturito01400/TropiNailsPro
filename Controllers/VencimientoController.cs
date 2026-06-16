using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace TropiNailsPro.Controllers
{
    public class VencimientoController : Controller
    {
        public IActionResult Index()
        {
            // 🔐 Obtener usuario logueado
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // ⚠️ AQUÍ conectaremos tu BD real
            // Ejemplo: traer suscripción real del usuario
            var fechaInicio = HttpContext.Session.GetString("FechaInicioSuscripcion");
            var fechaFin = HttpContext.Session.GetString("FechaFinSuscripcion");

            if (string.IsNullOrEmpty(fechaInicio) || string.IsNullOrEmpty(fechaFin))
            {
                ViewBag.Error = "No tienes suscripción activa.";
                ViewBag.DiasRestantes = 0;
                ViewBag.Estado = "SIN PLAN";
                return View();
            }

            DateTime inicio = DateTime.Parse(fechaInicio);
            DateTime fin = DateTime.Parse(fechaFin);

            int diasRestantes = (fin - DateTime.Now).Days;
            if (diasRestantes < 0) diasRestantes = 0;

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            ViewBag.DiasRestantes = diasRestantes;

            ViewBag.Estado =
                diasRestantes > 0 ? "ACTIVA" : "VENCIDA";

            return View();
        }
    }
}