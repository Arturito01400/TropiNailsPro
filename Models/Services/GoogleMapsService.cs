using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TropiNailsPro.Services
{
    public class GoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleMapsService> _logger;

        public GoogleMapsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // ==========================================================
        // 🔐 OBTENER API KEY
        // ==========================================================

        private string? ObtenerApiKey()
        {
            return _configuration["GoogleMaps:ApiKey"];
        }

        // ==========================================================
        // 🗺️ BUSCAR NEGOCIOS EN GOOGLE PLACES
        // ==========================================================

        public async Task<JsonDocument?> BuscarLugaresAsync(
            string texto,
            double latitud,
            double longitud)
        {
            try
            {
                // ==================================================
                // 1. OBTENER API KEY
                // ==================================================

                var apiKey = ObtenerApiKey();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError(
                        "Google Maps API Key no está configurada."
                    );

                    return null;
                }

                // ==================================================
                // 2. VALIDAR TEXTO
                // ==================================================

                if (string.IsNullOrWhiteSpace(texto))
                {
                    _logger.LogWarning(
                        "Google Places recibió una búsqueda vacía."
                    );

                    return null;
                }

                texto = texto.Trim();

                // ==================================================
                // 3. VALIDAR COORDENADAS
                // ==================================================

                if (
                    latitud < -90 ||
                    latitud > 90 ||
                    longitud < -180 ||
                    longitud > 180
                )
                {
                    _logger.LogWarning(
                        "Coordenadas inválidas para Google Places. Latitud: {Latitud}, Longitud: {Longitud}",
                        latitud,
                        longitud
                    );

                    return null;
                }

                // ==================================================
                // 4. GOOGLE PLACES - TEXT SEARCH NEW
                // ==================================================

                const string url =
                    "https://places.googleapis.com/v1/places:searchText";

                // ==================================================
                // 5. PREPARAR PETICIÓN
                // ==================================================

                var datos = new
                {
                    textQuery = texto,

                    languageCode = "es",

                    pageSize = 20,

                    locationBias = new
                    {
                        circle = new
                        {
                            center = new
                            {
                                latitude = latitud,
                                longitude = longitud
                            },

                            // 10 KM
                            radius = 10000.0
                        }
                    }
                };

                var json = JsonSerializer.Serialize(datos);

                using var contenido =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        url
                    );

                request.Content = contenido;

                // ==================================================
                // 6. API KEY
                // ==================================================

                request.Headers.Add(
                    "X-Goog-Api-Key",
                    apiKey
                );

                // ==================================================
                // 7. FIELD MASK
                // ==================================================
                //
                // Pedimos exactamente los campos utilizados
                // posteriormente por HomeController.
                //
                // ==================================================

                request.Headers.Add(
                    "X-Goog-FieldMask",
                    "places.id," +
                    "places.displayName," +
                    "places.formattedAddress," +
                    "places.location," +
                    "places.googleMapsUri," +
                    "places.rating," +
                    "places.userRatingCount"
                );

                // ==================================================
                // 8. LLAMAR A GOOGLE
                // ==================================================

                using var response =
                    await _httpClient.SendAsync(request);

                var respuesta =
                    await response.Content.ReadAsStringAsync();

                // ==================================================
                // 9. REGISTRAR RESPUESTA
                // ==================================================

                _logger.LogInformation(
                    "Google Places respondió con código {StatusCode} para la búsqueda: {Texto}",
                    (int)response.StatusCode,
                    texto
                );

                // ==================================================
                // 10. MANEJAR ERROR DE GOOGLE
                // ==================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Google Places respondió con error. StatusCode: {StatusCode}. Respuesta: {Respuesta}",
                        response.StatusCode,
                        respuesta
                    );

                    return null;
                }

                // ==================================================
                // 11. VALIDAR RESPUESTA VACÍA
                // ==================================================

                if (string.IsNullOrWhiteSpace(respuesta))
                {
                    _logger.LogWarning(
                        "Google Places devolvió una respuesta vacía para: {Texto}",
                        texto
                    );

                    return null;
                }

                // ==================================================
                // 12. VALIDAR JSON
                // ==================================================

                try
                {
                    var documento =
                        JsonDocument.Parse(respuesta);

                    // ==================================================
                    // COMPROBAR SI EXISTEN PLACES
                    // ==================================================

                    if (
                        documento.RootElement.TryGetProperty(
                            "places",
                            out var places
                        )
                    )
                    {
                        _logger.LogInformation(
                            "Google Places encontró {Cantidad} resultados para: {Texto}",
                            places.GetArrayLength(),
                            texto
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Google Places respondió correctamente pero no devolvió la propiedad 'places' para: {Texto}. Respuesta: {Respuesta}",
                            texto,
                            respuesta
                        );
                    }

                    return documento;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Google Places devolvió una respuesta que no es JSON válido. Respuesta: {Respuesta}",
                        respuesta
                    );

                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Error HTTP al consultar Google Places."
                );

                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "La petición a Google Places fue cancelada o agotó el tiempo de espera."
                );

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al consultar Google Places."
                );

                return null;
            }
        }
    }
}