using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class Barbero
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        // Ruta física guardada
        public string ImagenUrl { get; set; } = string.Empty;

        public bool Disponible { get; set; } = true;

        public bool Activo { get; set; } = true;

        [ValidateNever]
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    }
}