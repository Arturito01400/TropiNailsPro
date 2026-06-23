using System;

namespace TropiNailsPro.Services
{
    public class TimeService
    {
        private readonly TimeZoneInfo _zonaDominicana;

        public TimeService()
        {
            _zonaDominicana = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
        }

        public DateTime ObtenerHoraLocal()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaDominicana);
        }
    }
}