using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TropiNailsPro.Data;
using System.Linq;

namespace TropiNailsPro.Controllers
{
    public class SalonesController : Controller
    {
        private readonly AppDbContext _context;

        public SalonesController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // PERFIL PÚBLICO DEL NEGOCIO
        // =====================================================
        //
        // Ejemplo:
        // /Salones/Index?slug=nombre-del-negocio
        //
        // Este controlador está preparado para trabajar con
        // cualquier negocio de belleza registrado en TropiNails Pro.
        //
        // NO depende exclusivamente de que sea una manicurista.
        //
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string slug)
        {
            // =================================================
            // VALIDAR SLUG
            // =================================================

            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            slug = slug.Trim();

            // =================================================
            // BUSCAR NEGOCIO
            // =================================================

            var manicurista = await _context.Manicuristas
                .Include(m => m.Usuario)
                .FirstOrDefaultAsync(m =>
                    m.Slug != null &&
                    m.Slug.ToLower() == slug.ToLower());

            if (manicurista == null)
                return NotFound();

            // =================================================
            // WHATSAPP DEL NEGOCIO
            // =================================================

            string? whatsapp =
                manicurista.TelefonoNegocio;

            // Si el negocio no tiene teléfono propio,
            // intentamos utilizar el WhatsApp del usuario.
            if (string.IsNullOrWhiteSpace(whatsapp))
            {
                whatsapp =
                    manicurista.Usuario?.WhatsApp;
            }

            // Si tampoco existe WhatsApp,
            // utilizamos el teléfono personal.
            if (string.IsNullOrWhiteSpace(whatsapp))
            {
                whatsapp =
                    manicurista.Usuario?.Telefono;
            }

            // =================================================
            // LIMPIAR WHATSAPP
            // =================================================

            if (!string.IsNullOrWhiteSpace(whatsapp))
            {
                whatsapp = new string(
                    whatsapp
                        .Where(char.IsDigit)
                        .ToArray()
                );

                // República Dominicana
                if (whatsapp.Length == 10)
                {
                    whatsapp = "1" + whatsapp;
                }
            }

            ViewBag.WhatsApp = whatsapp;

            // =================================================
            // FOTO DEL NEGOCIO
            // =================================================

            string fotoNegocio =
                "/img/user-default.png";

            if (!string.IsNullOrWhiteSpace(
                manicurista.FotoNegocio))
            {
                fotoNegocio =
                    manicurista.FotoNegocio.Trim();

                // Azure Blob Storage
                if (fotoNegocio.StartsWith(
                        "http://",
                        System.StringComparison.OrdinalIgnoreCase) ||
                    fotoNegocio.StartsWith(
                        "https://",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    // Ya es una URL válida.
                }
                else
                {
                    if (!fotoNegocio.StartsWith("/"))
                    {
                        fotoNegocio =
                            "/" + fotoNegocio;
                    }
                }
            }

            ViewBag.FotoNegocio =
                fotoNegocio;

            // =================================================
            // FOTO DE PERFIL DEL PROFESIONAL
            // =================================================

            string fotoPerfil =
                manicurista.Usuario?.FotoPerfilUrl
                ?? "/img/user-default.png";

            ViewBag.FotoPerfil =
                fotoPerfil;

            // =================================================
            // MODELOS / TRABAJOS
            // =================================================

            var modelos =
                await _context.ModelosUnas
                    .Where(m =>
                        m.ManicuristaId ==
                        manicurista.Id)
                    .OrderByDescending(m => m.Id)
                    .ToListAsync();

            ViewBag.Modelos =
                modelos;

            // =================================================
            // PUBLICACIONES
            // =================================================

            var feed =
                await _context.Publicaciones
                    .Include(p => p.Usuario)
                    .Where(p =>
                        p.ManicuristaId ==
                        manicurista.Id)
                    .OrderByDescending(p => p.Fecha)
                    .ToListAsync();

            ViewBag.Feed =
                feed;

            // =================================================
            // INFORMACIÓN DE UBICACIÓN
            // =================================================

            ViewBag.UbicacionActiva =
                manicurista.UbicacionActiva;

            ViewBag.Latitud =
                manicurista.Latitud;

            ViewBag.Longitud =
                manicurista.Longitud;

            ViewBag.DireccionNegocio =
                manicurista.DireccionNegocio;

            ViewBag.Ciudad =
                manicurista.Ciudad;

            ViewBag.Provincia =
                manicurista.Provincia;

            // =================================================
            // DATOS PRINCIPALES
            // =================================================

            ViewBag.Manicurista =
                manicurista;

            // =================================================
            // INDICAR QUE ES PERFIL PÚBLICO
            // =================================================

            ViewBag.EsPerfilPublico =
                true;

            return View();
        }
    }
}