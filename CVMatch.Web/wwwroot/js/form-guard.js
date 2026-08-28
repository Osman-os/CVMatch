// Çift tıklamada formun iki kez gönderilmesini engeller
(function () {
    'use strict';

    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            // Doğrulama gönderimi durdurduysa düğme kilitlenmemeli
            if (e.defaultPrevented) return;

            const dugme = form.querySelector('button[type=submit]');
            if (!dugme || dugme.disabled) return;

            setTimeout(function () {
                // Gecikme sırasında başka bir doğrulama araya girmiş olabilir
                if (e.defaultPrevented) return;

                dugme.disabled = true;
                dugme.dataset.eskiMetin = dugme.textContent;
                dugme.textContent = 'Gönderiliyor...';
            }, 0);
        });
    });
})();