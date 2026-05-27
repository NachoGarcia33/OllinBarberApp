namespace OllinBarberApp.Models
{
    public class DashboardFinancieroViewModel
    {
        public decimal IngresosDiarios { get; set; }

        public decimal IngresosSemanales { get; set; }

        public decimal IngresosMensuales { get; set; }

        public decimal IngresosAnuales { get; set; }

        public decimal TicketPromedio { get; set; }

        public int CantidadCitas { get; set; }

        public int CantidadClientes { get; set; }

        public int CitasPendientes { get; set; }

        public int CitasCanceladas { get; set; }

        public int PorcentajeOcupacion { get; set; }

        public string ServicioTopRentable { get; set; } = string.Empty;

        public List<ServicioVendidoResumen> ServiciosMasVendidos { get; set; } = new();

        public BarberoResumen? BarberoConMasCitas { get; set; }

        public bool DashboardActivo { get; set; }
    }

    public class ServicioVendidoResumen
    {
        public string Nombre { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal Total { get; set; }
    }

    public class BarberoResumen
    {
        public string Nombre { get; set; } = string.Empty;

        public int Cantidad { get; set; }
    }
}