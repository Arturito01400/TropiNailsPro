namespace TropiNailsPro.Services
{
    public static class TimeZoneMapper
    {
        public static string Convertir(string zona)
        {
            return zona switch
            {
                "America/Santo_Domingo" => "SA Western Standard Time",
                "America/Mexico_City" => "Central Standard Time (Mexico)",
                "America/Bogota" => "SA Pacific Standard Time",
                "Europe/Madrid" => "Romance Standard Time",
                _ => "UTC"
            };
        }
    }
}