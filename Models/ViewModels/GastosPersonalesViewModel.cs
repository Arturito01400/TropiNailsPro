using TropiNailsPro.Models;

namespace TropiNailsPro.Models.ViewModels
{
    public class GastosPersonalesViewModel
    {
        // Lista completa de gastos personales
        public IEnumerable<GastoPersonal> Gastos { get; set; } 
            = new List<GastoPersonal>();


        // Total gastado en el mes actual
        public decimal TotalMes { get; set; }


        // Cantidad de registros de gastos
        public int CantidadGastos { get; set; }


        // Último monto registrado
        public decimal UltimoMonto { get; set; }


        // Categoría donde más se ha gastado
        public string CategoriaPrincipal { get; set; } 
            = string.Empty;


        // Total general histórico
        public decimal TotalHistorico { get; set; }


        // Promedio de gasto
        public decimal PromedioGasto { get; set; }
    }
}