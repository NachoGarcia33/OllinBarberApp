namespace OllinBarberApp.Models
{
    public class Venta
    {
        public int Id { get; set; }

        public string ClienteNombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public decimal Total { get; set; }

        public List<VentaDetalle> Detalles { get; set; } = new();
    }
}