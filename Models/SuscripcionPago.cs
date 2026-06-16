using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class SuscripcionPago
    {
        [Key]
        public int Id { get; set; }

        // ======================================================
        // RELACIÓN CON MANICURISTA (MULTI-TENANT SaaS)
        // ======================================================

        [Required]
        public int ManicuristaId { get; set; }

        [ForeignKey("ManicuristaId")]
        public Manicurista Manicurista { get; set; } = default!;

        // ======================================================
        // DETALLES DEL PAGO
        // ======================================================

        public decimal Monto { get; set; } = 0m;

        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Required]
        public string Metodo { get; set; } = string.Empty;

        // ======================================================
        // LÓGICA AUXILIAR
        // ======================================================

        [NotMapped]
        public bool EsPagoValido => Monto > 0 && !string.IsNullOrWhiteSpace(Metodo);
    }
}