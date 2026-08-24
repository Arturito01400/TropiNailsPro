using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly GoogleMapsService _googleMapsService;

        public HomeController(
            ILogger<HomeController> logger,
            IConfiguration configuration,
            AppDbContext context,
            GoogleMapsService googleMapsService)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _googleMapsService = googleMapsService;
        }

        // ==========================================================
        // 🏠 PÁGINA PRINCIPAL PÚBLICA
        // ==========================================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ==========================================================
        // 🔎 BUSCADOR PÚBLICO TROPINAILS PRO
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Buscar(
            string? texto,
            double? latitud,
            double? longitud)
        {
            try
            {
                // ==================================================
                // 1. LIMPIAR TEXTO
                // ==================================================

                texto = LimpiarTextoBusqueda(texto);

                // ==================================================
                // 2. VALIDAR BÚSQUEDA
                // ==================================================

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return Json(new
                    {
                        exito = false,
                        mensaje =
                            "Escribe un servicio, profesional, negocio o lugar que quieras encontrar.",
                        profesionales = Array.Empty<object>(),
                        negocios = Array.Empty<object>(),
                        totalProfesionales = 0,
                        totalNegociosGoogle = 0,
                        totalResultados = 0
                    });
                }

                // ==================================================
                // 3. VALIDAR UBICACIÓN
                // ==================================================

                bool tieneUbicacion =
                    latitud.HasValue &&
                    longitud.HasValue;

                if (tieneUbicacion)
                {
                    if (
                        latitud < -90 ||
                        latitud > 90 ||
                        longitud < -180 ||
                        longitud > 180
                    )
                    {
                        latitud = null;
                        longitud = null;
                        tieneUbicacion = false;
                    }
                }

                // ==================================================
                // 4. PREPARAR VARIACIONES DE BÚSQUEDA
                // ==================================================

                var terminosBusqueda =
                    ObtenerTerminosRelacionados(texto);

                // ==================================================
                // 5. BUSCAR EN TROPINAILS
                // ==================================================
                //
                // IMPORTANTE:
                // La búsqueda se hace en memoria después de traer
                // solamente los profesionales con ubicación activa.
                //
                // Esto evita problemas de traducción de EF Core/MySQL
                // con terminosBusqueda.Any(...) dentro del Where.
                //
                // ==================================================

                var candidatosBase =
                    await _context.Manicuristas
                        .AsNoTracking()
                        .Where(m => m.UbicacionActiva == true)
                        .Select(m => new
                        {
                            id = m.Id,

                            nombre = m.NombreNegocio,

                            direccion = m.DireccionNegocio,

                            ciudad = m.Ciudad,

                            provincia = m.Provincia,

                            latitud = m.Latitud,

                            longitud = m.Longitud
                        })
                        .Take(500)
                        .ToListAsync();

                // ==================================================
                // 6. FILTRAR CANDIDATOS
                // ==================================================
                //
                // Se normaliza el texto para permitir búsquedas como:
                //
                // peluqueria -> Peluquería
                // estetica   -> Estética
                // barberia   -> Barbería
                // pestanas   -> Pestañas
                //
                // ==================================================

                var candidatos =
                    candidatosBase
                        .Where(m =>
                        {
                            string nombre =
                                NormalizarTexto(m.nombre);

                            string direccion =
                                NormalizarTexto(m.direccion);

                            string ciudad =
                                NormalizarTexto(m.ciudad);

                            string provincia =
                                NormalizarTexto(m.provincia);

                            return terminosBusqueda.Any(t =>
                            {
                                string termino =
                                    NormalizarTexto(t);

                                if (string.IsNullOrWhiteSpace(termino))
                                    return false;

                                return
                                    (!string.IsNullOrWhiteSpace(nombre) &&
                                     nombre.Contains(
                                         termino,
                                         StringComparison.OrdinalIgnoreCase))
                                    ||
                                    (!string.IsNullOrWhiteSpace(direccion) &&
                                     direccion.Contains(
                                         termino,
                                         StringComparison.OrdinalIgnoreCase))
                                    ||
                                    (!string.IsNullOrWhiteSpace(ciudad) &&
                                     ciudad.Contains(
                                         termino,
                                         StringComparison.OrdinalIgnoreCase))
                                    ||
                                    (!string.IsNullOrWhiteSpace(provincia) &&
                                     provincia.Contains(
                                         termino,
                                         StringComparison.OrdinalIgnoreCase));
                            });
                        })
                        .ToList();

                // ==================================================
                // 7. RANKING INTELIGENTE
                // ==================================================

                var profesionales =
                    candidatos
                        .Select(m =>
                        {
                            double? distanciaKm = null;

                            if (
                                tieneUbicacion &&
                                m.latitud.HasValue &&
                                m.longitud.HasValue
                            )
                            {
                                distanciaKm =
                                    CalcularDistanciaKm(
                                        latitud!.Value,
                                        longitud!.Value,
                                        (double)m.latitud.Value,
                                        (double)m.longitud.Value
                                    );
                            }

                            int relevancia =
                                CalcularRelevancia(
                                    texto,
                                    m.nombre,
                                    m.direccion,
                                    m.ciudad,
                                    m.provincia
                                );

                            return new
                            {
                                m.id,
                                m.nombre,
                                m.direccion,
                                m.ciudad,
                                m.provincia,

                                m.latitud,
                                m.longitud,

                                distanciaKm,

                                relevancia
                            };
                        })
                        .OrderByDescending(m => m.relevancia)
                        .ThenBy(m =>
                            m.distanciaKm.HasValue
                                ? m.distanciaKm.Value
                                : double.MaxValue
                        )
                        .Take(20)
                        .ToList();

                // ==================================================
                // 8. ¿CUÁNTOS RESULTADOS TENEMOS?
                // ==================================================

                int cantidadTropiNails =
                    profesionales.Count;

                // ==================================================
                // 9. GOOGLE PLACES
                // ==================================================

                bool necesitaGoogle =
                    tieneUbicacion &&
                    cantidadTropiNails < 10;

                object[] negociosGoogle =
                    Array.Empty<object>();

                if (necesitaGoogle)
                {
                    try
                    {
                        // ==========================================
                        // Consulta ampliada para belleza
                        // ==========================================

                        string consultaGoogle =
                            ConstruirConsultaGoogle(texto);

                        var resultadoGoogle =
                            await _googleMapsService.BuscarLugaresAsync(
                                consultaGoogle,
                                latitud!.Value,
                                longitud!.Value
                            );

                        if (resultadoGoogle != null)
                        {
                            negociosGoogle =
                                ProcesarResultadosGoogle(
                                    resultadoGoogle,
                                    latitud.Value,
                                    longitud.Value
                                );
                        }
                    }
                    catch (Exception ex)
                    {
                        // ==========================================
                        // Google falla → TropiNails sigue funcionando
                        // ==========================================

                        _logger.LogWarning(
                            ex,
                            "Google Places no pudo completar la búsqueda pública de TropiNails."
                        );

                        negociosGoogle =
                            Array.Empty<object>();
                    }
                }

                // ==================================================
                // 10. RESULTADO FINAL
                // ==================================================

                return Json(new
                {
                    exito = true,

                    profesionales,

                    negocios = negociosGoogle,

                    totalProfesionales =
                        profesionales.Count,

                    totalNegociosGoogle =
                        negociosGoogle.Length,

                    totalResultados =
                        profesionales.Count +
                        negociosGoogle.Length,

                    busquedaGoogleRealizada =
                        necesitaGoogle,

                    tieneUbicacion,

                    terminoBuscado = texto,

                    categoriaDetectada =
                        DetectarCategoria(texto)
                });
            }
            catch (Exception ex)
            {
                // ==================================================
                // ERROR GENERAL
                // ==================================================

                _logger.LogError(
                    ex,
                    "Error en el buscador público de TropiNails Pro."
                );

                return Json(new
                {
                    exito = false,

                    mensaje =
                        "No pudimos completar la búsqueda. Inténtalo nuevamente.",

                    profesionales =
                        Array.Empty<object>(),

                    negocios =
                        Array.Empty<object>(),

                    totalProfesionales = 0,

                    totalNegociosGoogle = 0,

                    totalResultados = 0
                });
            }
        }

        // ==========================================================
        // 🧠 LIMPIAR TEXTO
        // ==========================================================

        private static string LimpiarTextoBusqueda(
            string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            texto = texto.Trim();

            while (texto.Contains("  "))
            {
                texto = texto.Replace(
                    "  ",
                    " "
                );
            }

            return texto;
        }

        // ==========================================================
        // 🧠 TÉRMINOS RELACIONADOS
        // ==========================================================

        private static List<string> ObtenerTerminosRelacionados(
            string texto)
        {
            var resultado =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            string normalizado =
                NormalizarTexto(texto);

            if (!string.IsNullOrWhiteSpace(texto))
                resultado.Add(texto);

            // ======================================================
            // UÑAS
            // ======================================================

            if (
                normalizado.Contains("una") ||
                normalizado.Contains("manicure") ||
                normalizado.Contains("manicura") ||
                normalizado.Contains("pedicure") ||
                normalizado.Contains("pedicura") ||
                normalizado.Contains("nail")
            )
            {
                resultado.Add("uñas");
                resultado.Add("manicure");
                resultado.Add("manicura");
                resultado.Add("pedicure");
                resultado.Add("pedicura");
                resultado.Add("nail");
            }

            // ======================================================
            // CABELLO
            // ======================================================

            if (
                normalizado.Contains("pelo") ||
                normalizado.Contains("cabello") ||
                normalizado.Contains("peluquer") ||
                normalizado.Contains("hair") ||
                normalizado.Contains("corte") ||
                normalizado.Contains("tinte")
            )
            {
                resultado.Add("peluquería");
                resultado.Add("peluqueria");
                resultado.Add("cabello");
                resultado.Add("pelo");
                resultado.Add("hair");
                resultado.Add("salón");
                resultado.Add("salon");
            }

            // ======================================================
            // BARBERÍA
            // ======================================================

            if (
                normalizado.Contains("barber") ||
                normalizado.Contains("barba") ||
                normalizado.Contains("afeitado") ||
                normalizado.Contains("fade")
            )
            {
                resultado.Add("barbería");
                resultado.Add("barberia");
                resultado.Add("barber");
                resultado.Add("barba");
            }

            // ======================================================
            // MAQUILLAJE
            // ======================================================

            if (
                normalizado.Contains("maquill") ||
                normalizado.Contains("makeup") ||
                normalizado.Contains("make up")
            )
            {
                resultado.Add("maquillaje");
                resultado.Add("makeup");
                resultado.Add("make up");
            }

            // ======================================================
            // PESTAÑAS
            // ======================================================

            if (
                normalizado.Contains("pestana") ||
                normalizado.Contains("lash")
            )
            {
                resultado.Add("pestañas");
                resultado.Add("pestanas");
                resultado.Add("lash");
                resultado.Add("lashes");
            }

            // ======================================================
            // CEJAS
            // ======================================================

            if (
                normalizado.Contains("ceja") ||
                normalizado.Contains("brow")
            )
            {
                resultado.Add("cejas");
                resultado.Add("brow");
                resultado.Add("brows");
            }

            // ======================================================
            // ESTÉTICA
            // ======================================================

            if (
                normalizado.Contains("estetica") ||
                normalizado.Contains("facial") ||
                normalizado.Contains("piel") ||
                normalizado.Contains("skin")
            )
            {
                resultado.Add("estética");
                resultado.Add("estetica");
                resultado.Add("facial");
                resultado.Add("skin");
            }

            // ======================================================
            // SPA
            // ======================================================

            if (
                normalizado.Contains("spa") ||
                normalizado.Contains("relaj") ||
                normalizado.Contains("masaje")
            )
            {
                resultado.Add("spa");
                resultado.Add("masaje");
                resultado.Add("masajes");
                resultado.Add("relajación");
            }

            return resultado.ToList();
        }

        // ==========================================================
        // 🧠 DETECTAR CATEGORÍA
        // ==========================================================

        private static string DetectarCategoria(
            string texto)
        {
            string normalizado =
                NormalizarTexto(texto);

            if (
                normalizado.Contains("una") ||
                normalizado.Contains("manicure") ||
                normalizado.Contains("manicura") ||
                normalizado.Contains("pedicure") ||
                normalizado.Contains("pedicura") ||
                normalizado.Contains("nail")
            )
                return "Uñas";

            if (
                normalizado.Contains("pelo") ||
                normalizado.Contains("cabello") ||
                normalizado.Contains("peluquer") ||
                normalizado.Contains("hair") ||
                normalizado.Contains("tinte")
            )
                return "Cabello";

            if (
                normalizado.Contains("barber") ||
                normalizado.Contains("barba") ||
                normalizado.Contains("afeitado") ||
                normalizado.Contains("fade")
            )
                return "Barbería";

            if (
                normalizado.Contains("maquill") ||
                normalizado.Contains("makeup") ||
                normalizado.Contains("make up")
            )
                return "Maquillaje";

            if (
                normalizado.Contains("pestana") ||
                normalizado.Contains("lash")
            )
                return "Pestañas";

            if (
                normalizado.Contains("ceja") ||
                normalizado.Contains("brow")
            )
                return "Cejas";

            if (
                normalizado.Contains("spa") ||
                normalizado.Contains("masaje")
            )
                return "Spa";

            if (
                normalizado.Contains("estetica") ||
                normalizado.Contains("facial")
            )
                return "Estética";

            return "Belleza";
        }

        // ==========================================================
        // 🔎 RANKING DE RESULTADOS
        // ==========================================================

        private static int CalcularRelevancia(
            string texto,
            string? nombre,
            string? direccion,
            string? ciudad,
            string? provincia)
        {
            int puntos = 0;

            string busqueda =
                NormalizarTexto(texto);

            string nombreNormalizado =
                NormalizarTexto(nombre);

            string direccionNormalizada =
                NormalizarTexto(direccion);

            string ciudadNormalizada =
                NormalizarTexto(ciudad);

            string provinciaNormalizada =
                NormalizarTexto(provincia);

            // ======================================================
            // COINCIDENCIA EXACTA
            // ======================================================

            if (
                !string.IsNullOrWhiteSpace(nombreNormalizado) &&
                nombreNormalizado.Equals(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                puntos += 100;
            }

            // ======================================================
            // NOMBRE EMPIEZA POR LA BÚSQUEDA
            // ======================================================

            if (
                !string.IsNullOrWhiteSpace(nombreNormalizado) &&
                nombreNormalizado.StartsWith(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                puntos += 70;
            }

            // ======================================================
            // NOMBRE CONTIENE
            // ======================================================

            if (
                !string.IsNullOrWhiteSpace(nombreNormalizado) &&
                nombreNormalizado.Contains(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                puntos += 50;
            }

            // ======================================================
            // CIUDAD
            // ======================================================

            if (
                !string.IsNullOrWhiteSpace(ciudadNormalizada) &&
                ciudadNormalizada.Contains(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                puntos += 25;
            }

            // ======================================================
            // PROVINCIA
            // ======================================================

            if (
                !string.IsNullOrWhiteSpace(provinciaNormalizada) &&
                provinciaNormalizada.Contains(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                puntos += 20;
            }

            // ======================================================
            // DIRECCIÓN
            // ======================================================

            if (
                !string.IsNullOrWhiteSpace(direccionNormalizada) &&
                direccionNormalizada.Contains(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                puntos += 15;
            }

            return puntos;
        }

        // ==========================================================
        // 🌎 NORMALIZAR TEXTO
        // ==========================================================

        private static string NormalizarTexto(
            string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string normalizado =
                texto.Trim().ToLowerInvariant();

            var caracteres =
                normalizado.Normalize(
                    NormalizationForm.FormD
                );

            var resultado =
                new StringBuilder();

            foreach (
                char caracter
                in caracteres)
            {
                var categoria =
                    System.Globalization.CharUnicodeInfo
                        .GetUnicodeCategory(caracter);

                if (
                    categoria !=
                    System.Globalization.UnicodeCategory.NonSpacingMark
                )
                {
                    resultado.Append(caracter);
                }
            }

            return resultado
                .ToString()
                .Normalize(
                    NormalizationForm.FormC
                );
        }

        // ==========================================================
        // 🌐 CONSTRUIR CONSULTA PARA GOOGLE
        // ==========================================================

        private static string ConstruirConsultaGoogle(
            string texto)
        {
            string categoria =
                DetectarCategoria(texto);

            if (categoria == "Belleza")
            {
                return
                    $"{texto} belleza salón profesional";
            }

            return
                $"{texto} {categoria}";
        }

        // ==========================================================
        // 🗺️ PROCESAR GOOGLE PLACES
        // ==========================================================

        private static object[] ProcesarResultadosGoogle(
            JsonDocument resultadoGoogle,
            double latitudUsuario,
            double longitudUsuario)
        {
            var root =
                resultadoGoogle.RootElement;

            if (
                !root.TryGetProperty(
                    "places",
                    out var places
                )
            )
            {
                return Array.Empty<object>();
            }

            var lista =
                new List<ResultadoGoogle>();

            foreach (
                var place
                in places.EnumerateArray()
            )
            {
                string? nombre = null;
                string? direccion = null;
                string? mapsUrl = null;

                double? placeLat = null;
                double? placeLng = null;

                double? rating = null;

                int? cantidadReviews = null;

                string? idGoogle = null;

                // ==================================================
                // ID
                // ==================================================

                if (
                    place.TryGetProperty(
                        "id",
                        out var idProp
                    )
                )
                {
                    idGoogle =
                        idProp.GetString();
                }

                // ==================================================
                // NOMBRE
                // ==================================================

                if (
                    place.TryGetProperty(
                        "displayName",
                        out var displayName
                    )
                )
                {
                    if (
                        displayName.TryGetProperty(
                            "text",
                            out var nombreText
                        )
                    )
                    {
                        nombre =
                            nombreText.GetString();
                    }
                }

                // ==================================================
                // DIRECCIÓN
                // ==================================================

                if (
                    place.TryGetProperty(
                        "formattedAddress",
                        out var direccionProp
                    )
                )
                {
                    direccion =
                        direccionProp.GetString();
                }

                // ==================================================
                // GOOGLE MAPS URL
                // ==================================================

                if (
                    place.TryGetProperty(
                        "googleMapsUri",
                        out var mapsProp
                    )
                )
                {
                    mapsUrl =
                        mapsProp.GetString();
                }

                // ==================================================
                // UBICACIÓN
                // ==================================================

                if (
                    place.TryGetProperty(
                        "location",
                        out var location
                    )
                )
                {
                    if (
                        location.TryGetProperty(
                            "latitude",
                            out var latProp
                        )
                    )
                    {
                        if (
                            latProp.ValueKind ==
                            JsonValueKind.Number
                        )
                        {
                            placeLat =
                                latProp.GetDouble();
                        }
                    }

                    if (
                        location.TryGetProperty(
                            "longitude",
                            out var lngProp
                        )
                    )
                    {
                        if (
                            lngProp.ValueKind ==
                            JsonValueKind.Number
                        )
                        {
                            placeLng =
                                lngProp.GetDouble();
                        }
                    }
                }

                // ==================================================
                // RATING
                // ==================================================

                if (
                    place.TryGetProperty(
                        "rating",
                        out var ratingProp
                    )
                )
                {
                    if (
                        ratingProp.ValueKind ==
                        JsonValueKind.Number
                    )
                    {
                        rating =
                            ratingProp.GetDouble();
                    }
                }

                // ==================================================
                // REVIEWS
                // ==================================================

                if (
                    place.TryGetProperty(
                        "userRatingCount",
                        out var reviewsProp
                    )
                )
                {
                    if (
                        reviewsProp.ValueKind ==
                        JsonValueKind.Number
                    )
                    {
                        cantidadReviews =
                            reviewsProp.GetInt32();
                    }
                }

                // ==================================================
                // DISTANCIA
                // ==================================================

                double? distanciaKm = null;

                if (
                    placeLat.HasValue &&
                    placeLng.HasValue
                )
                {
                    distanciaKm =
                        CalcularDistanciaKm(
                            latitudUsuario,
                            longitudUsuario,
                            placeLat.Value,
                            placeLng.Value
                        );
                }

                lista.Add(
                    new ResultadoGoogle
                    {
                        IdGoogle = idGoogle,

                        Nombre = nombre,

                        Direccion = direccion,

                        MapsUrl = mapsUrl,

                        Latitud = placeLat,

                        Longitud = placeLng,

                        Rating = rating,

                        Reviews = cantidadReviews,

                        DistanciaKm = distanciaKm
                    }
                );
            }

            // ======================================================
            // ORDEN INTELIGENTE
            // ======================================================

            return lista
                .OrderByDescending(x =>
                    x.Rating ?? 0)
                .ThenByDescending(x =>
                    x.Reviews ?? 0)
                .ThenBy(x =>
                    x.DistanciaKm ??
                    double.MaxValue)
                .Take(20)
                .Select(x => new
                {
                    idGoogle = x.IdGoogle,

                    nombre = x.Nombre,

                    direccion = x.Direccion,

                    mapsUrl = x.MapsUrl,

                    latitud = x.Latitud,

                    longitud = x.Longitud,

                    rating = x.Rating,

                    reviews = x.Reviews,

                    distanciaKm = x.DistanciaKm
                })
                .Cast<object>()
                .ToArray();
        }

        // ==========================================================
        // 📍 CALCULAR DISTANCIA
        // ==========================================================

        private static double CalcularDistanciaKm(
            double latitud1,
            double longitud1,
            double latitud2,
            double longitud2)
        {
            const double radioTierraKm = 6371.0;

            double dLat =
                GradosARadianes(
                    latitud2 - latitud1
                );

            double dLon =
                GradosARadianes(
                    longitud2 - longitud1
                );

            double lat1Rad =
                GradosARadianes(
                    latitud1
                );

            double lat2Rad =
                GradosARadianes(
                    latitud2
                );

            double a =
                Math.Sin(dLat / 2) *
                Math.Sin(dLat / 2)
                +
                Math.Cos(lat1Rad) *
                Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            double c =
                2 *
                Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a)
                );

            return Math.Round(
                radioTierraKm * c,
                2
            );
        }

        // ==========================================================
        // 📐 GRADOS → RADIANES
        // ==========================================================

        private static double GradosARadianes(
            double grados)
        {
            return grados *
                   Math.PI /
                   180.0;
        }

        // ==========================================================
        // 🔐 PRIVACIDAD
        // ==========================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        // ==========================================================
        // ❌ ERROR
        // ==========================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                }
            );
        }

        // ==========================================================
        // 🔥 DASHBOARD INTELIGENTE
        // ==========================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            var usuarioNombre =
                HttpContext.Session.GetString(
                    "UsuarioNombre"
                );

            // ======================================================
            // SIN SESIÓN
            // ======================================================

            if (string.IsNullOrEmpty(usuarioNombre))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            // ======================================================
            // PROFESIONAL
            // ======================================================

            var manicuristaId =
                HttpContext.Session.GetInt32(
                    "UsuarioId"
                );

            if (manicuristaId != null)
            {
                return RedirectToAction(
                    "Dashboard",
                    "Manicuristas"
                );
            }

            // ======================================================
            // USUARIO NORMAL
            // ======================================================

            ViewBag.UsuarioNombre =
                usuarioNombre;

            return View();
        }

        // ==========================================================
        // 🧱 MODELO INTERNO PARA GOOGLE
        // ==========================================================

        private sealed class ResultadoGoogle
        {
            public string? IdGoogle { get; set; }

            public string? Nombre { get; set; }

            public string? Direccion { get; set; }

            public string? MapsUrl { get; set; }

            public double? Latitud { get; set; }

            public double? Longitud { get; set; }

            public double? Rating { get; set; }

            public int? Reviews { get; set; }

            public double? DistanciaKm { get; set; }
        }
    }
}