using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TropiNailsPro.Controllers
{
    public class ProfesionalesController : Controller
    {
        private readonly AppDbContext _context;

        public ProfesionalesController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // CATÁLOGO PÚBLICO DE PROFESIONALES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var profesionales = await _context.Manicuristas
                .AsNoTracking()
                .Include(m => m.Servicios)
                .Include(m => m.Usuario)
                .OrderBy(m => m.NombreNegocio)
                .ToListAsync();

            var categorias = new List<CategoriaCatalogo>
            {
                new CategoriaCatalogo
                {
                    Clave = "unas",
                    Nombre = "El arte de las uñas",
                    Descripcion = "Profesionales que convierten cada detalle en una obra de arte.",
                    Icono = "💅"
                },

                new CategoriaCatalogo
                {
                    Clave = "cabello",
                    Nombre = "Cabello & Estilo",
                    Descripcion = "Expertas en transformar tu look y resaltar tu personalidad.",
                    Icono = "💇‍♀️"
                },

                new CategoriaCatalogo
                {
                    Clave = "pestanas",
                    Nombre = "Pestañas",
                    Descripcion = "Miradas que hablan por sí solas.",
                    Icono = "👁️"
                },

                new CategoriaCatalogo
                {
                    Clave = "cejas",
                    Nombre = "Cejas",
                    Descripcion = "Diseño, definición y armonía para tu mirada.",
                    Icono = "✨"
                },

                new CategoriaCatalogo
                {
                    Clave = "maquillaje",
                    Nombre = "Maquillaje",
                    Descripcion = "Belleza para cada ocasión.",
                    Icono = "💄"
                },

                new CategoriaCatalogo
                {
                    Clave = "spa",
                    Nombre = "Spa & Bienestar",
                    Descripcion = "Momentos para consentirte, relajarte y renovarte.",
                    Icono = "🧖‍♀️"
                },

                new CategoriaCatalogo
                {
                    Clave = "depilacion",
                    Nombre = "Depilación",
                    Descripcion = "Cuidado personal con profesionales especializados.",
                    Icono = "🌸"
                },

                new CategoriaCatalogo
                {
                    Clave = "belleza",
                    Nombre = "Belleza",
                    Descripcion = "Descubre profesionales que ofrecen experiencias únicas de belleza.",
                    Icono = "💎"
                }
            };

            var tarjetas = profesionales.Select(m =>
            {
                var serviciosActivos = m.Servicios?
                    .Where(s => s.Activo)
                    .OrderBy(s => s.Nombre)
                    .ToList()
                    ?? new List<Servicio>();

                var categoriasProfesional =
                    ClasificarServicios(serviciosActivos);

                if (!categoriasProfesional.Any())
                {
                    categoriasProfesional.Add("belleza");
                }

                return new ProfesionalCatalogo
                {
                    Id = m.Id,
                    Nombre = m.NombreNegocio,
                    Foto = m.FotoNegocio,
                    Slug = m.Slug,
                    CodigoPublico = m.CodigoPublico,

                    Ciudad = m.Ciudad,
                    Provincia = m.Provincia,
                    Direccion = m.DireccionNegocio,

                    Latitud = m.Latitud,
                    Longitud = m.Longitud,

                    UbicacionActiva = m.UbicacionActiva,

                    Categorias = categoriasProfesional,

                    Servicios = serviciosActivos
                        .Select(s => new ServicioCatalogo
                        {
                            Id = s.Id,
                            Nombre = s.Nombre,
                            Descripcion = s.Descripcion,
                            Precio = s.Precio,
                            DuracionMinutos = s.DuracionMinutos
                        })
                        .ToList()
                };
            }).ToList();

            foreach (var categoria in categorias)
            {
                categoria.Profesionales = tarjetas
                    .Where(p => p.Categorias.Contains(categoria.Clave))
                    .ToList();
            }

            ViewBag.TotalProfesionales = tarjetas.Count;

            ViewBag.TotalServicios = tarjetas
                .SelectMany(p => p.Servicios)
                .Count();

            ViewBag.CategoriasActivas = categorias
                .Count(c => c.Profesionales.Any());

            return View(categorias);
        }

        // ============================================================
        // CLASIFICACIÓN AUTOMÁTICA
        // ============================================================

        private static List<string> ClasificarServicios(
            IEnumerable<Servicio> servicios)
        {
            var categorias = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var servicio in servicios)
            {
                var nombre = Normalizar(
                    $"{servicio.Nombre} {servicio.Descripcion}");

                // UÑAS
                if (Contiene(nombre,
                    "uña",
                    "unas",
                    "manicure",
                    "manicura",
                    "pedicure",
                    "pedicura",
                    "gel",
                    "acrilico",
                    "acrilica",
                    "acrilicos",
                    "acrilicas",
                    "polygel",
                    "rubber",
                    "nail",
                    "nails",
                    "esmalte",
                    "semipermanente",
                    "press on",
                    "presson",
                    "tips",
                    "builder"))
                {
                    categorias.Add("unas");
                }

                // CABELLO
                if (Contiene(nombre,
                    "cabello",
                    "pelo",
                    "hair",
                    "corte",
                    "peinado",
                    "blow",
                    "blower",
                    "secado",
                    "lavado",
                    "tinte",
                    "color",
                    "balayage",
                    "mechas",
                    "highlights",
                    "keratina",
                    "queratina",
                    "alisado",
                    "desrizado",
                    "tratamiento capilar",
                    "extensiones de cabello"))
                {
                    categorias.Add("cabello");
                }

                // PESTAÑAS
                if (Contiene(nombre,
                    "pestaña",
                    "pestana",
                    "pestañas",
                    "pestanas",
                    "lash",
                    "lashes",
                    "extensiones de pestañas",
                    "lifting de pestañas",
                    "lash lift"))
                {
                    categorias.Add("pestanas");
                }

                // CEJAS
                if (Contiene(nombre,
                    "ceja",
                    "cejas",
                    "brow",
                    "brows",
                    "microblading",
                    "micropigmentacion de cejas",
                    "laminado de cejas",
                    "diseño de cejas"))
                {
                    categorias.Add("cejas");
                }

                // MAQUILLAJE
                if (Contiene(nombre,
                    "maquillaje",
                    "maquilladora",
                    "makeup",
                    "make up",
                    "maquillaje social",
                    "maquillaje profesional"))
                {
                    categorias.Add("maquillaje");
                }

                // SPA / BIENESTAR
                if (Contiene(nombre,
                    "spa",
                    "masaje",
                    "masajes",
                    "relajacion",
                    "relajación",
                    "facial",
                    "limpieza facial",
                    "exfoliacion",
                    "exfoliación",
                    "aromaterapia",
                    "bienestar",
                    "hidratacion facial",
                    "hidratación facial"))
                {
                    categorias.Add("spa");
                }

                // DEPILACIÓN
                if (Contiene(nombre,
                    "depilacion",
                    "depilación",
                    "wax",
                    "waxing",
                    "cera",
                    "depilado",
                    "depilada"))
                {
                    categorias.Add("depilacion");
                }
            }

            return categorias.ToList();
        }

        // ============================================================
        // UTILIDADES
        // ============================================================

        private static bool Contiene(
            string texto,
            params string[] valores)
        {
            return valores.Any(texto.Contains);
        }

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            return texto
                .ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c =>
                    System.Globalization.CharUnicodeInfo
                        .GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate("", (actual, c) => actual + c)
                .Normalize(System.Text.NormalizationForm.FormC);
        }
    }

    // ================================================================
    // MODELO PARA LA VISTA
    // ================================================================

    public class CategoriaCatalogo
    {
        public string Clave { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Icono { get; set; } = "";

        public List<ProfesionalCatalogo> Profesionales { get; set; }
            = new List<ProfesionalCatalogo>();
    }

    // ================================================================
    // PROFESIONAL PARA EL CATÁLOGO
    // ================================================================

    public class ProfesionalCatalogo
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string? Foto { get; set; }

        public string? Slug { get; set; }

        public string CodigoPublico { get; set; } = "";

        public string? Ciudad { get; set; }

        public string? Provincia { get; set; }

        public string? Direccion { get; set; }

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        public bool UbicacionActiva { get; set; }

        public List<string> Categorias { get; set; }
            = new List<string>();

        public List<ServicioCatalogo> Servicios { get; set; }
            = new List<ServicioCatalogo>();

        public string UrlPerfil
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Slug))
                {
                    return $"/Salones/Index?slug={Uri.EscapeDataString(Slug)}";
                }

                return $"/Salones/Index?id={Id}";
            }
        }
    }

    // ================================================================
    // SERVICIO PARA EL CATÁLOGO
    // ================================================================

    public class ServicioCatalogo
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string? Descripcion { get; set; }

        public decimal? Precio { get; set; }

        public int? DuracionMinutos { get; set; }
    }
}