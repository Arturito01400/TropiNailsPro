using System;
using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class Cita
    {
        public int Id { get; set; }

        // =============================
        // DATOS CLIENTA
        // =============================
        [Required(ErrorMessage = "El nombre de la clienta es obligatorio")]
        public string NombreClienta { get; set; } = string.Empty;

        // =============================
        // FECHA
        // =============================
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        // =============================
        // HORA INICIO (MySQL TIME)
        // =============================
        [Required(ErrorMessage = "La hora es obligatoria")]
        [DataType(DataType.Time)]
        public TimeSpan Hora { get; set; }

        // =============================
        // DURACIÓN DEL SERVICIO
        // =============================
        [Range(15, 600)]
        public int DuracionMinutos { get; set; } = 60;

        // =============================
        // 🔥 FIX → NULLABLE
        // =============================
        [DataType(DataType.Time)]
        public TimeSpan? HoraFin { get; set; }

        // =============================
        // SERVICIO
        // =============================
        [Required(ErrorMessage = "El servicio es obligatorio")]
        public string Servicio { get; set; } = string.Empty;

        public string? NotasAdicionales { get; set; }

        // =============================
        // RELACIONES
        // =============================
        public int ManicuristaId { get; set; }
        public int? ClienteId { get; set; }

        // =============================
        // LÓGICA DEL SISTEMA
        // =============================
        public int PosicionFila { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool CreadaPorManicurista { get; set; } = false;
    }
}