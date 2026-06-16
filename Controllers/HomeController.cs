using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TropiNailsPro.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // ======================================================
        // Página principal genérica (Index)
        // ======================================================
        public IActionResult Index()
        {
            return View();
        }

        // ======================================================
        // Página de privacidad
        // ======================================================
        public IActionResult Privacy()
        {
            return View();
        }

        // ======================================================
        // Página de error
        // ======================================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        // ======================================================
        // 🔥 DASHBOARD INTELIGENTE (SaaS READY)
        // ======================================================
        public IActionResult Dashboard()
        {
            var usuarioNombre = HttpContext.Session.GetString("UsuarioNombre");

            // 🔐 Si no hay sesión → login
            if (string.IsNullOrEmpty(usuarioNombre))
                return RedirectToAction("Login", "Auth");

            // ======================================================
            // 🔥 NUEVO → detectar si es MANICURISTA
            // ======================================================
            var manicuristaId = HttpContext.Session.GetInt32("UsuarioId");

            if (manicuristaId != null)
            {
                // Redirige al dashboard privado SaaS
                return RedirectToAction("Dashboard", "Manicuristas");
            }

            // ======================================================
            // comportamiento actual (usuarios normales)
            // ======================================================
            ViewBag.UsuarioNombre = usuarioNombre;

            return View();
        }
    }
}