namespace OllinBarberApp.Models
{
    public class VentaViewModel
    {
        public string ClienteNombre { get; set; } = string.Empty;

        public List<int> ProductosIds { get; set; } = new();

        public List<int> Cantidades { get; set; } = new();

        public List<Producto> ProductosDisponibles { get; set; } = new();
    }
}