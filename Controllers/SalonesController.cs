using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        // Acepta:
        //
        // /Salones/Index/14
        //
        // /Salones/Index?id=14
        //
        // /Salones/Index?slug=nombre-del-negocio
        //
        // El ID se mantiene por compatibilidad con enlaces
        // existentes.
        //
        // El slug permite utilizar URLs públicas más amigables.
        //
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? slug,
            int? id)
        {
            // =================================================
            // BUSCAR NEGOCIO
            // =================================================

            Manicurista? manicurista = null;

            // =================================================
            // OPCIÓN 1: BUSCAR POR ID
            // Ejemplo:
            // /Salones/Index/14
            // =================================================

            if (id.HasValue)
            {
                manicurista = await _context.Manicuristas
                    .Include(m => m.Usuario)
                    .Include(m => m.Servicios)
                    .FirstOrDefaultAsync(m =>
                        m.Id == id.Value);
            }

            // =================================================
            // OPCIÓN 2: BUSCAR POR SLUG
            // Ejemplo:
            // /Salones/Index?slug=nombre-del-negocio
            // =================================================

            if (manicurista == null &&
                !string.IsNullOrWhiteSpace(slug))
            {
                slug = slug.Trim();

                manicurista = await _context.Manicuristas
                    .Include(m => m.Usuario)
                    .Include(m => m.Servicios)
                    .FirstOrDefaultAsync(m =>
                        m.Slug != null &&
                        m.Slug.ToLower() == slug.ToLower());
            }

            // =================================================
            // SI NO EXISTE EL NEGOCIO
            // =================================================

            if (manicurista == null)
            {
                return NotFound();
            }

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

            ViewBag.WhatsApp =
                whatsapp;

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
                if (
                    fotoNegocio.StartsWith(
                        "http://",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    fotoNegocio.StartsWith(
                        "https://",
                        StringComparison.OrdinalIgnoreCase)
                   )
                {
                    // Ya es una URL absoluta válida.
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
                ?? manicurista.Usuario?.FotoPerfil
                ?? "/img/user-default.png";

            ViewBag.FotoPerfil =
                fotoPerfil;

            // =================================================
            // MODELOS / TRABAJOS / PORTAFOLIO
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

            // =================================================
            // RETORNAR VISTA
            // =================================================

            return View();
        }
    }
}