using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TropiNailsPro.Models
{
    public class ModeloUnas
    {
        public int Id { get; set; }

        //=====================================================
        // RELACION MULTI-MANICURISTA
        //=====================================================

        [Required]
        public int ManicuristaId { get; set; }


        //=====================================================
        // DATOS DEL MODELO
        //=====================================================

        [Required(ErrorMessage = "El nombre del modelo es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;


        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;


        //=====================================================
        // PRECIO (AHORA OPCIONAL)
        //=====================================================

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Precio { get; set; }



        //=====================================================
        // ARCHIVO MULTIMEDIA
        //=====================================================

        [StringLength(500)]
        public string? ImagenUrl { get; set; }



        // imagen | video
        [StringLength(20)]
        public string TipoMedia { get; set; } = "imagen";



        //=====================================================
        // SUBIDA DESDE PC
        //=====================================================

        [NotMapped]
        public IFormFile? ImagenArchivo { get; set; }



        //=====================================================
        // URL SEGURA
        //=====================================================

        [NotMapped]
        public string ImagenSegura
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImagenUrl))
                    return "/uploads/default-modelo.png";


                if (
                    ImagenUrl.StartsWith("http://") ||
                    ImagenUrl.StartsWith("https://")
                   )
                    return ImagenUrl;


                var ruta = ImagenUrl.Replace("\\", "/");

                if (!ruta.StartsWith("/"))
                    ruta = "/" + ruta;


                return ruta;
            }
        }



        //=====================================================
        // DETECTAR SI ES VIDEO
        //=====================================================

        [NotMapped]
        public bool EsVideo
        {
            get
            {
                return TipoMedia == "video";
            }
        }

    }
}