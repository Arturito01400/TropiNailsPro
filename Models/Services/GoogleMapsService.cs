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
                        "❌ Google Maps API Key no está configurada en GoogleMaps:ApiKey."
                    );

                    return null;
                }

                // No mostramos la API Key completa en los logs.
                _logger.LogInformation(
                    "🔑 Google Maps API Key detectada correctamente."
                );

                // ==================================================
                // 2. VALIDAR TEXTO
                // ==================================================

                if (string.IsNullOrWhiteSpace(texto))
                {
                    _logger.LogWarning(
                        "⚠️ Google Places recibió una búsqueda vacía."
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
                        "⚠️ Coordenadas inválidas para Google Places. Latitud: {Latitud}, Longitud: {Longitud}",
                        latitud,
                        longitud
                    );

                    return null;
                }

                _logger.LogInformation(
                    "📍 Búsqueda Google Places: {Texto} | Latitud: {Latitud} | Longitud: {Longitud}",
                    texto,
                    latitud,
                    longitud
                );

                // ==================================================
                // 4. GOOGLE PLACES API - TEXT SEARCH
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

                            // Radio de búsqueda: 10 kilómetros
                            radius = 10000.0
                        }
                    }
                };

                var json = JsonSerializer.Serialize(
                    datos,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    }
                );

                _logger.LogInformation(
                    "📤 Enviando petición a Google Places para: {Texto}",
                    texto
                );

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
                // 6. HEADERS DE GOOGLE
                // ==================================================

                request.Headers.Add(
                    "X-Goog-Api-Key",
                    apiKey
                );

                // ==================================================
                // 7. FIELD MASK
                // ==================================================
                //
                // Solicitamos todos los campos utilizados por
                // HomeController.
                //
                // ==================================================

                request.Headers.Add(
                    "X-Goog-FieldMask",
                    string.Join(
                        ",",
                        new[]
                        {
                            "places.id",
                            "places.displayName",
                            "places.formattedAddress",
                            "places.location",
                            "places.googleMapsUri",
                            "places.primaryType",
                            "places.rating",
                            "places.userRatingCount"
                        }
                    )
                );

                // ==================================================
                // 8. LLAMAR A GOOGLE
                // ==================================================

                using var response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead
                    );

                var respuesta =
                    await response.Content.ReadAsStringAsync();

                // ==================================================
                // 9. REGISTRAR ESTADO HTTP
                // ==================================================

                _logger.LogInformation(
                    "🌐 Google Places respondió HTTP {StatusCode} para '{Texto}'.",
                    (int)response.StatusCode,
                    texto
                );

                // ==================================================
                // 10. MANEJAR ERROR DE GOOGLE
                // ==================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "❌ ERROR GOOGLE PLACES | HTTP {StatusCode} | Búsqueda: {Texto} | Respuesta: {Respuesta}",
                        (int)response.StatusCode,
                        texto,
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
                        "⚠️ Google Places devolvió una respuesta vacía para: {Texto}",
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
                        JsonDocument.Parse(respuesta);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Google Places devolvió JSON inválido. Respuesta: {Respuesta}",
                        respuesta
                    );

                    return null;
                }

                // ==================================================
                // 13. COMPROBAR RESULTADOS
                // ==================================================

                if (
                    documento.RootElement.TryGetProperty(
                        "places",
                        out var places
                    )
                    &&
                    places.ValueKind ==
                    JsonValueKind.Array
                )
                {
                    int cantidad =
                        places.GetArrayLength();

                    _logger.LogInformation(
                        "✅ Google Places encontró {Cantidad} resultados para '{Texto}'.",
                        cantidad,
                        texto
                    );

                    // ==================================================
                    // MOSTRAR ALGUNOS RESULTADOS EN LOG
                    // SOLO PARA DIAGNÓSTICO
                    // ==================================================

                    int contador = 0;

                    foreach (
                        var place
                        in places.EnumerateArray()
                    )
                    {
                        contador++;

                        string nombre =
                            "Sin nombre";

                        if (
                            place.TryGetProperty(
                                "displayName",
                                out var displayName
                            )
                            &&
                            displayName.TryGetProperty(
                                "text",
                                out var displayNameText
                            )
                        )
                        {
                            nombre =
                                displayNameText.GetString()
                                ?? "Sin nombre";
                        }

                        _logger.LogInformation(
                            "📍 Google resultado #{Numero}: {Nombre}",
                            contador,
                            nombre
                        );

                        if (contador >= 5)
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ Google Places respondió correctamente pero NO devolvió la propiedad 'places'. Respuesta: {Respuesta}",
                        respuesta
                    );
                }

                // ==================================================
                // 14. DEVOLVER DOCUMENTO
                // ==================================================

                return documento;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error HTTP al consultar Google Places."
                );

                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "⏱️ La petición a Google Places fue cancelada o agotó el tiempo de espera."
                );

                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error procesando JSON de Google Places."
                );

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error inesperado al consultar Google Places."
                );

                return null;
            }
        }
    }
}