using System.Collections.Generic;
using TropiNailsPro.Models;

namespace TropiNailsPro.Models.ViewModels
{
    public class DashboardVM
    {
        public string UsuarioNombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool EsClienta { get; set; }

        // =========================
        // LISTAS PRINCIPALES
        // =========================
        public List<Usuario> Clientas { get; set; } = new();
        public List<Cita> CitasHoy { get; set; } = new();
        public List<Chat> Chats { get; set; } = new();
        public List<ModeloUnas> ModelosUnas { get; set; } = new();

        // =========================
        // INGRESOS
        // =========================
        public decimal IngresosHoy { get; set; }
        public decimal IngresosMes { get; set; }
        public decimal IngresosAnio { get; set; }

        public List<Pago> ClientasDeudoras { get; set; } = new();

        public List<GraficaIngresoVM> GraficaIngresos { get; set; } = new();

        // =========================
        // DATOS MANICURISTA
        // =========================
        public string? Plan { get; set; }
        public string? MensajeBienvenida { get; set; }
        public string? LinkRegistro { get; set; }

        // =========================
        // PRODUCTOS
        // =========================
        public int TotalProductos { get; set; }
        public int StockBajo { get; set; }
        public int ProductosAgotados { get; set; }

        // =========================
        // PERMISOS PREMIUM
        // =========================
        public bool InventarioDesbloqueado { get; set; }
        public bool PagosDesbloqueados { get; set; }
        public bool EstadisticasDesbloqueadas { get; set; }
        public bool MostrarSuscripcion { get; set; }

        // =========================
        // FEED SOCIAL
        // =========================
        public List<Publicacion> Feed { get; set; } = new();
    }

    public class GraficaIngresoVM
    {
        public int Mes { get; set; }
        public decimal Total { get; set; }
    }
}