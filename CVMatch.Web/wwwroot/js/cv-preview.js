// CV önizlemesinde yakınlaştırma ve hata alanına kaydırma
(function () {
    'use strict';

    // ---- Yakınlaştırma ----
    const gorsel = document.getElementById('previewImg');
    const etiket = document.getElementById('zoomLabel');

    if (gorsel && etiket) {
        const enAz = 100, enCok = 250, adim = 25;
        let oran = 100;

        function uygula() {
            gorsel.style.width = oran + '%';
            etiket.textContent = oran + '%';
            document.getElementById('zoomOut').disabled = oran <= enAz;
            document.getElementById('zoomIn').disabled = oran >= enCok;
        }

        document.getElementById('zoomIn').addEventListener('click', function () {
            oran = Math.min(enCok, oran + adim);
            uygula();
        });

        document.getElementById('zoomOut').addEventListener('click', function () {
            oran = Math.max(enAz, oran - adim);
            uygula();
        });

        document.getElementById('zoomReset').addEventListener('click', function () {
            oran = 100;
            document.getElementById('previewFrame').scrollTo(0, 0);
            uygula();
        });

        uygula();
    }

    // Alan bazında mesaj varsa oraya; yoksa özet kutusundaki ilk hataya
    const alanHatasi = Array.from(
        document.querySelectorAll('#reviewForm span[data-valmsg-for]'))
        .find(e => e.textContent.trim().length > 0);

    const ozet = document.querySelector('.validation-summary-errors');

    const hedefEleman = alanHatasi
        ? alanHatasi.closest('.mb-3, .mb-0, .col-md-6, .card')
        : (ozet ? ilkHataliSatir() : null);

    if (hedefEleman) {
        hedefEleman.scrollIntoView({ behavior: 'smooth', block: 'center' });

        const alan = hedefEleman.querySelector('input:not([type=hidden]), select, textarea');
        if (alan) {
            setTimeout(() => alan.focus({ preventScroll: true }), 350);
        }
    }

    // Özet hatası hangi bölümden geliyorsa o bölümün ilk boş alanını bulur
    function ilkHataliSatir() {
        const metin = ozet.textContent;

        const eslesme = [
            { anahtar: 'Proje adını',  liste: '#projectList',    alan: '[name$=".Name"]' },
            { anahtar: 'Kurum adını',  liste: '#experienceList', alan: '[name$=".CompanyName"]' },
            { anahtar: 'Okul adını',   liste: '#educationList',  alan: '[name$=".School"]' }
        ].find(x => metin.includes(x.anahtar));

        if (!eslesme) return ozet;

        const kap = document.querySelector(eslesme.liste);
        if (!kap) return ozet;

        // Boş bırakılmış ilk satır
        const bos = Array.from(kap.querySelectorAll(eslesme.alan))
            .find(i => !i.value.trim());

        return bos ? bos.closest('.education-row, .experience-row, .project-row') || bos : ozet;
    }
})();