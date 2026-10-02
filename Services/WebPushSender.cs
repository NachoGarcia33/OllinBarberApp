using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Data;
using WebPush;

namespace OllinBarberApp.Services
{
    public class WebPushSender
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly WebPushClient _client = new();

        public WebPushSender(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task EnviarATodasLasSuscripcionesAsync(int barberoId, string titulo, string mensaje)
        {
            var publicKey = _config["WebPush:PublicKey"];
            var privateKey = _config["WebPush:PrivateKey"];
            var subject = _config["WebPush:Subject"];

            if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(subject))
                return;

            var vapid = new VapidDetails(subject, publicKey, privateKey);
            var suscripciones = await _context.SuscripcionesPush
                .Where(s => s.BarberoId == barberoId)
                .ToListAsync();

            if (!suscripciones.Any()) return;

            var payload = System.Text.Json.JsonSerializer.Serialize(new { title = titulo, body = mensaje });

            foreach (var s in suscripciones)
            {
                var pushSubscription = new PushSubscription(s.Endpoint, s.P256dh, s.Auth);
                try
                {
                    await _client.SendNotificationAsync(pushSubscription, payload, vapid);
                }
                catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // El navegador ya no existe o el barbero desinstaló/reinstaló: borramos esa suscripción vieja
                    _context.SuscripcionesPush.Remove(s);
                }
                catch (WebPushException)
                {
                    // No dejamos que un error de envío rompa la creación de la cita
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
