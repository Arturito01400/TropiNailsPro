using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Suscripcion
    {
        [Key]
        public int Id { get; set; }

        // =====================================================
        // 🔥 RELACIÓN MANICURISTA
        // =====================================================

        [Required]
        public int ManicuristaId { get; set; }

        [ForeignKey(nameof(ManicuristaId))]
        public Manicurista Manicurista { get; set; } = default!;

        // =====================================================
        // 🔥 PLAN ÚNICO
        // =====================================================

        [Required]
        [MaxLength(50)]
        public string Plan { get; set; } = "Premium";

        // =====================================================
        // 🔥 FECHAS
        // =====================================================

        public DateTime FechaInicio { get; set; }
            = DateTime.UtcNow;

        public DateTime FechaVencimiento { get; set; }

        public DateTime? FechaRenovacion { get; set; }

        // =====================================================
        // 🔥 ESTADO
        // =====================================================

        public bool Activa { get; set; } = true;

        public bool Cancelada { get; set; } = false;

        // =====================================================
        // 🔥 PAYPAL
        // =====================================================

        [MaxLength(100)]
        public string MetodoPago { get; set; } = "PayPal";

        [MaxLength(250)]
        public string? PayPalOrderId { get; set; }

        [MaxLength(100)]
        public string EstadoPago { get; set; } = "COMPLETED";

        // =====================================================
        // 🔥 INFORMACIÓN FINANCIERA
        // =====================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; } = 10.20m; // USD mensual

        [MaxLength(10)]
        public string Moneda { get; set; } = "USD";

        // =====================================================
        // 🔥 REFERENCIA EXTRA
        // =====================================================

        [MaxLength(150)]
        public string? ReferenciaPago { get; set; }

        // =====================================================
        // 🔥 VALIDACIÓN AUTOMÁTICA
        // =====================================================

        [NotMapped]
        public bool EstaVencida =>
            DateTime.UtcNow > FechaVencimiento;

        [NotMapped]
        public bool EstaActiva =>
            Activa &&
            !Cancelada &&
            FechaVencimiento > DateTime.UtcNow;
    }
}