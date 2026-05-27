namespace OllinBarberApp.Models
{
    public class ConfiguracionSistema
    {
        public int Id { get; set; }

        // 🔥 ACTIVAR WHATSAPP
        public bool WhatsappActivo { get; set; } = true;

        // 🔥 ACTIVAR TIENDA
        public bool TiendaActiva { get; set; } = false;

        // 🔥 ACTIVAR DASHBOARD
        public bool DashboardActivo { get; set; } = false;
    }
}