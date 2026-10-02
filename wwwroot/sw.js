self.addEventListener('push', function (event) {
    let data = { title: 'Ollin Barber', body: 'Tienes una novedad.' };
    try { data = event.data.json(); } catch (e) { }

    event.waitUntil(
        self.registration.showNotification(data.title || 'Ollin Barber', {
            body: data.body || '',
            icon: '/images/logo.png',
            badge: '/images/logo.png'
        })
    );
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    event.waitUntil(
        clients.matchAll({ type: 'window' }).then(function (clientList) {
            for (const client of clientList) {
                if (client.url.includes('/Citas') && 'focus' in client) return client.focus();
            }
            if (clients.openWindow) return clients.openWindow('/Citas/Index');
        })
    );
});
