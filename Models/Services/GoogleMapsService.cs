using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TropiNailsPro.Services
{
    public class GoogleMapsService
    {
        private const string PlacesSearchUrl =
            "https://places.googleapis.com/v1/places:searchText";

        private const double RadioBusquedaMetros =
            10000.0;

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
            var apiKey =
                _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Permite también utilizar:
                //
                // GoogleMaps__ApiKey
                //
                // especialmente útil en Azure/App Service.

                apiKey =
                    _configuration["GoogleMaps__ApiKey"];
            }

            return apiKey?.Trim();
        }

        // ==========================================================
        // 🗺️ BUSCAR LUGARES EN GOOGLE PLACES
        // ==========================================================

        public async Task<JsonDocument?> BuscarLugaresAsync(
            string texto,
            double latitud,
            double longitud)
        {
            try
            {
                // ==================================================
                // 1. API KEY
                // ==================================================

                var apiKey =
                    ObtenerApiKey();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError(
                        "Google Maps API Key no está configurada. Se esperaba GoogleMaps:ApiKey."
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

                texto =
                    texto.Trim();

                // ==================================================
                // 3. VALIDAR COORDENADAS
                // ==================================================

                if (
                    !double.IsFinite(latitud) ||
                    !double.IsFinite(longitud) ||
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

                _logger.LogInformation(
                    "Buscando en Google Places: {Texto} | Latitud: {Latitud} | Longitud: {Longitud}",
                    texto,
                    latitud,
                    longitud
                );

                // ==================================================
                // 4. CUERPO DE LA PETICIÓN
                // ==================================================

                var datos = new
                {
                    textQuery = texto,

                    languageCode = "es",

                    regionCode = "DO",

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

                            radius =
                                RadioBusquedaMetros
                        }
                    }
                };

                string json =
                    JsonSerializer.Serialize(
                        datos,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy =
                                JsonNamingPolicy.CamelCase
                        });

                // ==================================================
                // 5. CREAR REQUEST
                // ==================================================

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        PlacesSearchUrl);

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                // ==================================================
                // 6. API KEY
                // ==================================================

                request.Headers.TryAddWithoutValidation(
                    "X-Goog-Api-Key",
                    apiKey);

                // ==================================================
                // 7. FIELD MASK
                // ==================================================
                //
                // Solo pedimos los campos que HomeController
                // realmente necesita.
                //
                // Google exige FieldMask para Text Search (New).
                //
                // ==================================================

                const string fieldMask =
                    "places.id," +
                    "places.displayName," +
                    "places.formattedAddress," +
                    "places.location," +
                    "places.googleMapsUri," +
                    "places.rating," +
                    "places.userRatingCount";

                request.Headers.TryAddWithoutValidation(
                    "X-Goog-FieldMask",
                    fieldMask);

                // ==================================================
                // 8. ENVIAR PETICIÓN
                // ==================================================

                using var response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead);

                string respuesta =
                    await response.Content
                        .ReadAsStringAsync();

                // ==================================================
                // 9. LOG HTTP
                // ==================================================

                _logger.LogInformation(
                    "Google Places respondió HTTP {StatusCode} para '{Texto}'.",
                    (int)response.StatusCode,
                    texto
                );

                // ==================================================
                // 10. ERROR HTTP
                // ==================================================

                if (!response.IsSuccessStatusCode)
                {
                    string cuerpoSeguro =
                        respuesta.Length > 4000
                            ? respuesta[..4000]
                            : respuesta;

                    _logger.LogError(
                        "Google Places rechazó la búsqueda. HTTP {StatusCode} ({ReasonPhrase}). Texto: {Texto}. Respuesta: {Respuesta}",
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        texto,
                        cuerpoSeguro
                    );

                    // Errores comunes que quedarán visibles
                    // claramente en Azure Log Stream:
                    //
                    // 400 INVALID_ARGUMENT
                    // 403 PERMISSION_DENIED
                    // 429 RESOURCE_EXHAUSTED
                    // 500 INTERNAL

                    return null;
                }

                // ==================================================
                // 11. RESPUESTA VACÍA
                // ==================================================

                if (string.IsNullOrWhiteSpace(respuesta))
                {
                    _logger.LogWarning(
                        "Google Places devolvió una respuesta vacía para '{Texto}'.",
                        texto
                    );

                    return null;
                }

                // ==================================================
                // 12. PARSEAR JSON
                // ==================================================

                JsonDocument documento;

                try
                {
                    documento =
                        JsonDocument.Parse(
                            respuesta);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Google Places devolvió JSON inválido."
                    );

                    return null;
                }

                // ==================================================
                // 13. COMPROBAR PLACES
                // ==================================================

                if (
                    documento.RootElement.TryGetProperty(
                        "places",
                        out var places)
                    &&
                    places.ValueKind ==
                    JsonValueKind.Array
                )
                {
                    int cantidad =
                        places.GetArrayLength();

                    _logger.LogInformation(
                        "Google Places encontró {Cantidad} resultados para '{Texto}'.",
                        cantidad,
                        texto
                    );

                    // ==================================================
                    // LOG DE DIAGNÓSTICO
                    // ==================================================

                    int contador = 0;

                    foreach (
                        var place
                        in places.EnumerateArray())
                    {
                        contador++;

                        string nombre =
                            "Sin nombre";

                        if (
                            place.TryGetProperty(
                                "displayName",
                                out var displayName)
                            &&
                            displayName.ValueKind ==
                            JsonValueKind.Object
                            &&
                            displayName.TryGetProperty(
                                "text",
                                out var displayNameText)
                        )
                        {
                            nombre =
                                displayNameText.GetString()
                                ??
                                "Sin nombre";
                        }

                        _logger.LogInformation(
                            "Google resultado #{Numero}: {Nombre}",
                            contador,
                            nombre
                        );

                        if (contador >= 5)
                            break;
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Google Places respondió correctamente pero no encontró lugares para '{Texto}'.",
                        texto
                    );
                }

                // ==================================================
                // 14. DEVOLVER JSON
                // ==================================================

                return documento;
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
                    "La petición a Google Places fue cancelada o agotó el tiempo."
                );

                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Error procesando JSON de Google Places."
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