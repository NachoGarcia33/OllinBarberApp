namespace OllinBarberApp.Models
{
    public class ClienteAdminViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public bool TieneCuenta { get; set; }
        public bool CuentaBloqueada { get; set; }
        public int CantidadPedidos { get; set; }
        public DateTimeOffset? UltimoPedido { get; set; }
        public decimal TotalEntregado { get; set; }
    }
}
