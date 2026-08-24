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
                var apiKey = ObtenerApiKey();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError(
                        "Google Maps API Key no está configurada."
                    );

                    return null;
                }

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return null;
                }

                texto = texto.Trim();

                // ==================================================
                // GOOGLE PLACES - TEXT SEARCH NEW
                // ==================================================

                const string url =
                    "https://places.googleapis.com/v1/places:searchText";

                // ==================================================
                // PETICIÓN
                // ==================================================

                var datos = new
                {
                    textQuery = texto,

                    languageCode = "es",

                    pageSize = 10,

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
                            radius = 10000
                        }
                    }
                };

                var json = JsonSerializer.Serialize(datos);

                using var contenido = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    url
                );

                request.Content = contenido;

                // ==================================================
                // API KEY
                // ==================================================

                request.Headers.Add(
                    "X-Goog-Api-Key",
                    apiKey
                );

                // ==================================================
                // FIELD MASK
                //
                // ⚠️ SOLO PEDIMOS LO NECESARIO
                // ==================================================

                request.Headers.Add(
                    "X-Goog-FieldMask",
                    "places.id," +
                    "places.displayName," +
                    "places.formattedAddress," +
                    "places.location," +
                    "places.googleMapsUri," +
                    "places.primaryType"
                );

                // ==================================================
                // LLAMAR A GOOGLE
                // ==================================================

                using var response =
                    await _httpClient.SendAsync(request);

                var respuesta =
                    await response.Content.ReadAsStringAsync();

                // ==================================================
                // ERROR
                // ==================================================

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Google Places respondió {StatusCode}: {Respuesta}",
                        response.StatusCode,
                        respuesta
                    );

                    return null;
                }

                // ==================================================
                // RESPUESTA
                // ==================================================

                return JsonDocument.Parse(respuesta);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar Google Places."
                );

                return null;
            }
        }
    }
}