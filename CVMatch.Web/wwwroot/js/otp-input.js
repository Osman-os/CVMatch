// Altı kutulu doğrulama kodu girişi.
// Kutular yalnızca arayüz; gönderilen değer gizli alanda birleştirilir.
(function () {
    'use strict';

    const kap = document.getElementById('otpBoxes');
    const gizli = document.getElementById('otpValue');
    const gonder = document.getElementById('otpSubmit');

    if (!kap || !gizli || !gonder) return;

    const kutular = Array.from(kap.querySelectorAll('.cvm-otp-box'));

    function kodBirlestir() {
        return kutular.map(k => k.value).join('');
    }

    function durumGuncelle() {
        const kod = kodBirlestir();
        gizli.value = kod;
        gonder.disabled = kod.length !== 6;
    }

    function hataTemizle() {
        kap.classList.remove('cvm-otp-error');
    }

    kutular.forEach(function (kutu, i) {
        kutu.addEventListener('input', function () {
            // Yalnızca rakam kabul edilir
            kutu.value = kutu.value.replace(/\D/g, '').slice(0, 1);

            if (kutu.value && i < kutular.length - 1) {
                kutular[i + 1].focus();
            }

            hataTemizle();
            durumGuncelle();
        });

        kutu.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && !kutu.value && i > 0) {
                e.preventDefault();
                kutular[i - 1].focus();
                kutular[i - 1].value = '';
                durumGuncelle();
                return;
            }

            if (e.key === 'ArrowLeft' && i > 0) {
                e.preventDefault();
                kutular[i - 1].focus();
            }

            if (e.key === 'ArrowRight' && i < kutular.length - 1) {
                e.preventDefault();
                kutular[i + 1].focus();
            }
        });

        kutu.addEventListener('focus', function () {
            kutu.select();
        });

        kutu.addEventListener('paste', function (e) {
            e.preventDefault();

            const yapistirilan = (e.clipboardData || window.clipboardData)
                .getData('text')
                .replace(/\D/g, '')
                .slice(0, 6);

            if (!yapistirilan) return;

            kutular.forEach(k => k.value = '');

            for (let j = 0; j < yapistirilan.length; j++) {
                kutular[j].value = yapistirilan[j];
            }

            const sonrakiBos = Math.min(yapistirilan.length, kutular.length - 1);
            kutular[sonrakiBos].focus();

            hataTemizle();
            durumGuncelle();
        });
    });

    // Sayfa açılışında ilk kutuya odaklan
    kutular[0].focus();
    durumGuncelle();
})();