using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class Servicio
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public int Duracion { get; set; }

        public decimal Precio { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [ValidateNever]
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    }
}
