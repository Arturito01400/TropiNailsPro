using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TropiNailsPro.Models
{
    public class Producto
    {
        public int Id { get; set; }

        // ======================================================
        // 🔹 RELACIÓN CON LA MANICURISTA (NUEVO - MULTIUSUARIO)
        // ======================================================
        [Required]
        public int ManicuristaId { get; set; }

        public Usuario? Manicurista { get; set; }

        // ======================================================
        // 🔹 TUS CAMPOS ORIGINALES (NO TOCADOS)
        // ======================================================
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser 0 o mayor.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que cero.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Precio Unitario")]
        public decimal PrecioUnitario { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // ======================================================
        // 🔥 CAMPOS MODERNOS NUEVOS (SaaS Pro)
        // ======================================================
        [Display(Name = "Stock mínimo")]
        public int StockMinimo { get; set; } = 5;

        public bool Activo { get; set; } = true;

        // Imagen guardada en BD (ruta)
        public string? ImagenUrl { get; set; }

        // 🔹 PROPIEDAD NUEVA PARA EVITAR IMÁGENES ROTAS
        [NotMapped]
        public string ImagenSegura
        {
            get
            {
                if (string.IsNullOrEmpty(ImagenUrl))
                    return "/uploads/default-product.png"; // ruta de imagen por defecto
                return "/" + ImagenUrl.Replace("\\", "/");
            }
        }

        // ======================================================
        // 🆕 NUEVOS CAMPOS PROFESIONALES (AGREGADOS)
        // ======================================================
        [NotMapped]
        public IFormFile? ImagenFile { get; set; }

        [StringLength(50)]
        [Display(Name = "Código de Barras")]
        public string? CodigoBarras { get; set; }

        [StringLength(100)]
        public string? Categoria { get; set; }

        [Display(Name = "Venta automática")]
        public bool VentaAutomatica { get; set; } = true;

        public bool PermitirStockNegativo { get; set; } = false;

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // ======================================================
        // 🔹 PROPIEDADES CALCULADAS (UX moderna)
        // ======================================================
        [NotMapped]
        public bool StockBajo => Cantidad <= StockMinimo;

        [NotMapped]
        public decimal ValorInventario => Cantidad * PrecioUnitario;

        [NotMapped]
        public int Stock => Cantidad;

        [NotMapped]
        public decimal TotalCalculado => Cantidad * PrecioUnitario;
    }
}