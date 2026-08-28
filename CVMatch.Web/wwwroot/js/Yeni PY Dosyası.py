// Çift tıklamada formun iki kez gönderilmesini engeller
(function () {
    'use strict';

    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            const dugme = form.querySelector('button[type=submit]');
            if (!dugme || dugme.disabled) return;

            setTimeout(function () {
                dugme.disabled = true;
                dugme.dataset.eskiMetin = dugme.textContent;
                dugme.textContent = 'Gönderiliyor...';
            }, 0);
        });
    });
})();