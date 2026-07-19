using System;
using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models.ViewModels
{
    public class AgendarCitaViewModel
    {
        public int DisponibilidadId { get; set; }

        public int ManicuristaId { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; }


        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string NombreClienta { get; set; } = string.Empty;


        [Phone(ErrorMessage = "El teléfono no tiene un formato válido")]
        public string? TelefonoCliente { get; set; }


        [Required(ErrorMessage = "Indica el diseño que deseas")]
        public string Servicio { get; set; } = string.Empty;


        public string? NotasAdicionales { get; set; }
    }
}