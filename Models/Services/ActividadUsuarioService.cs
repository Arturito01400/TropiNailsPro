using System;
using TropiNailsPro.Services;

namespace TropiNailsPro.Models.Services
{
    public class ActividadUsuarioService
    {
        private readonly TimeService _timeService;

        public ActividadUsuarioService(TimeService timeService)
        {
            _timeService = timeService;
        }

        public string ObtenerEstado(DateTime? ultimoAcceso)
        {
            if (!ultimoAcceso.HasValue)
                return "⚫ Nunca inició sesión";

            var ahora = _timeService.ObtenerHoraLocal();
            var tiempo = ahora - ultimoAcceso.Value;

            if (tiempo.TotalMinutes < 1)
                return "🟢 Activo ahora";

            if (tiempo.TotalMinutes < 60)
            {
                int minutos = (int)tiempo.TotalMinutes;
                return $"🟢 Activo hace {minutos} minuto{(minutos == 1 ? "" : "s")}";
            }

            if (tiempo.TotalHours < 24)
            {
                int horas = (int)tiempo.TotalHours;
                return $"🟢 Activo hace {horas} hora{(horas == 1 ? "" : "s")}";
            }

            if (tiempo.TotalDays < 2)
                return "🟡 Activo ayer";

            if (tiempo.TotalDays <= 7)
            {
                int dias = (int)tiempo.TotalDays;
                return $"🟡 Hace {dias} día{(dias == 1 ? "" : "s")}";
            }

            if (tiempo.TotalDays <= 30)
            {
                int dias = (int)tiempo.TotalDays;
                return $"🟠 Hace {dias} día{(dias == 1 ? "" : "s")}";
            }

            {
                int dias = (int)tiempo.TotalDays;
                return $"🔴 Hace {dias} día{(dias == 1 ? "" : "s")}";
            }
        }
    }
}