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


        // Teléfono para contacto y WhatsApp
        [Phone(ErrorMessage = "El teléfono no tiene un formato válido")]
        public string? TelefonoCliente { get; set; }



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
        // HORA FINAL DEL SERVICIO
        // =============================

        [DataType(DataType.Time)]
        public TimeSpan? HoraFin { get; set; }



        // =============================
        // SERVICIO / DISEÑO
        // =============================

        [Required(ErrorMessage = "El servicio es obligatorio")]
        public string Servicio { get; set; } = string.Empty;



        // Información adicional del diseño
        public string? NotasAdicionales { get; set; }



        // =============================
        // RELACIONES
        // =============================

        public int ManicuristaId { get; set; }


        public int? ClienteId { get; set; }



        // =============================
        // CONTROL DE AGENDA
        // =============================

        public int PosicionFila { get; set; }



        // Pendiente / Confirmada / Cancelada / Completada
        public string Estado { get; set; } = "Pendiente";



        // Se llenará desde TimeService
        public DateTime FechaRegistro { get; set; }



        // true = creada desde agenda de manicurista
        // false = creada por clienta desde reservas
        public bool CreadaPorManicurista { get; set; } = false;
    }
}