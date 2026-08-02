namespace TropiNailsPro.ViewModels
{
    public class ResumenMensualViewModel
    {
        public int Año { get; set; }

        public int Mes { get; set; }

        public string NombreMes { get; set; } = string.Empty;

        public decimal TotalIngresos { get; set; }

        public decimal TotalGastosNegocio { get; set; }

        public decimal TotalGastosPersonales { get; set; }

        public decimal Ganancia
        {
            get
            {
                return TotalIngresos
                     - TotalGastosNegocio
                     - TotalGastosPersonales;
            }
        }
    }
}