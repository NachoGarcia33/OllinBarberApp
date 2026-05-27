using Microsoft.AspNetCore.Identity;
using System;

namespace OllinBarberApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;

        public bool EsBarbero { get; set; }
        public bool Disponible { get; set; }
    }
}

