using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace TropiNailsPro.Controllers
{
    public class ManicuristasController : Controller
    {
        private readonly AppDbContext _context;

        public ManicuristasController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // 🌐 PERFIL PÚBLICO POR SLUG (NUEVO)
        // =====================================================
        [HttpGet("/salon/{slug}")]
        public async Task<IActionResult> Perfil(string slug)
        {
            var manicurista = await _context.Manicuristas
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (manicurista == null)
                return NotFound();

            // Guardar en sesión para todo el sistema
            HttpContext.Session.SetInt32("ManicuristaId", manicurista.Id);

            return RedirectToAction("Register", "Auth", new
            {
                manicuristaId = manicurista.Id
            });
        }

        // =====================================================
        // REGISTER
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> Register(Manicurista model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 🔐 Hash contraseña (FIX 🔥)
            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // 🎁 15 días gratis
            model.FechaInicioPrueba = DateTime.UtcNow;
            model.FechaVencimiento = DateTime.UtcNow.AddDays(15);

            // ✅ activa
            model.Activa = true;

            // 🔗 código único
            model.CodigoReferencia = Guid.NewGuid().ToString("N").Substring(0, 8);

            // 🌐 SLUG AUTOMÁTICO
            if (!string.IsNullOrEmpty(model.NombreNegocio))
            {
                model.Slug = model.NombreNegocio
                    .ToLower()
                    .Replace(" ", "-")
                    .Replace("á", "a")
                    .Replace("é", "e")
                    .Replace("í", "i")
                    .Replace("ó", "o")
                    .Replace("ú", "u");
            }

            // =====================================================
            // 🔥 FIX IMPORTANTE SIN ROMPER TU APP
            // =====================================================

            if (model.UsuarioId <= 0)
            {
                ModelState.AddModelError("", "Usuario inválido.");
                return View(model);
            }

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.Id == model.UsuarioId);

            if (!usuarioExiste)
            {
                ModelState.AddModelError("", "El usuario no existe en la base de datos.");
                return View(model);
            }

            // =====================================================
            // GUARDADO (NO SE TOCA TU FLUJO)
            // =====================================================
            _context.Manicuristas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // =====================================================
        // LOGIN
        // =====================================================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =====================================================
        // DASHBOARD
        // =====================================================
        [HttpGet]
        public IActionResult Dashboard()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");

            return RedirectToAction("Dashboard", "Dashboard");
        }

        // =====================================================
        // LOGOUT
        // =====================================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}