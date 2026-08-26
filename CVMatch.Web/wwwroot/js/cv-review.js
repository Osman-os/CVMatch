(function () {
    'use strict';

    // ---------- Satır ekleme / silme ----------

    function reindex(container, prefix) {
        const rows = container.children;

        for (let i = 0; i < rows.length; i++) {
            rows[i].querySelectorAll('[name]').forEach(function (input) {
                input.name = input.name.replace(
                    new RegExp('^' + prefix + '\\[\\d+\\]'),
                    prefix + '[' + i + ']');
            });

            rows[i].querySelectorAll('[id]').forEach(function (el) {
                el.id = el.id.replace(
                    new RegExp('^' + prefix + '\\[\\d+\\]'),
                    prefix + '[' + i + ']');
            });

            rows[i].querySelectorAll('label[for]').forEach(function (label) {
                label.htmlFor = label.htmlFor.replace(
                    new RegExp('^' + prefix + '\\[\\d+\\]'),
                    prefix + '[' + i + ']');
            });
        }
    }

    function cloneRow(container, prefix) {
        if (container.children.length === 0) return;

        const template = container.children[0].cloneNode(true);

        template.querySelectorAll('input').forEach(function (input) {
            if (input.type === 'checkbox') {
                input.checked = false;
            } else if (input.type !== 'hidden') {
                input.value = '';
                input.disabled = false;
            }
        });

        template.querySelectorAll('textarea').forEach(function (t) { t.value = ''; });
        template.querySelectorAll('select').forEach(function (s) { s.selectedIndex = 0; });

        container.appendChild(template);
        reindex(container, prefix);
    }

    function setupList(containerId, addButtonId, prefix) {
        const container = document.getElementById(containerId);
        const addButton = document.getElementById(addButtonId);
        if (!container || !addButton) return;

        addButton.addEventListener('click', function () {
            cloneRow(container, prefix);
        });

        container.addEventListener('click', function (e) {
            if (!e.target.classList.contains('remove-row')) return;

            if (container.children.length === 1) {
                const row = container.children[0];

                row.querySelectorAll('input, textarea').forEach(function (el) {
                    if (el.type === 'checkbox') el.checked = false;
                    else if (el.type !== 'hidden') el.value = '';
                });

                row.querySelectorAll('select').forEach(function (s) {
                    s.selectedIndex = 0;
                });

                // "Devam Ediyor" işaretliyken silinirse bitiş tarihi pasif kalıyordu
                const endDate = row.querySelector('.end-date');
                if (endDate) endDate.disabled = false;

                return;
            }

            e.target.closest('.education-row, .experience-row, .project-row').remove();
            reindex(container, prefix);
        });

        // "Devam Ediyor" işaretlenince bitiş tarihi kapansın
        container.addEventListener('change', function (e) {
            if (!e.target.classList.contains('is-current')) return;

            const row = e.target.closest('.education-row, .experience-row, .project-row');
            const endDate = row.querySelector('.end-date');
            if (!endDate) return;

            endDate.disabled = e.target.checked;
            if (e.target.checked) endDate.value = '';
        });
    }

    setupList('educationList', 'addEducationTop', 'Educations');
    setupList('experienceList', 'addExperienceTop', 'WorkExperiences');
    setupList('projectList', 'addProjectTop', 'Projects');

    // ---------- Yetenek etiketleri ----------

    const skills = Array.isArray(window.initialSkills) ? window.initialSkills.slice() : [];
    const tagContainer = document.getElementById('skillTags');
    const skillInput = document.getElementById('skillInput');
    const addSkillButton = document.getElementById('addSkill');
    const skillsCsv = document.getElementById('skillsCsv');

    function renderSkills() {
        if (!tagContainer) return;

        tagContainer.innerHTML = '';

        skills.forEach(function (skill, index) {
            const tag = document.createElement('span');
            tag.className = 'badge text-bg-light border d-flex align-items-center gap-2 py-2 px-3';
            tag.textContent = skill;

            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn-close btn-close-sm';
            remove.setAttribute('aria-label', skill + ' yeteneğini kaldır');
            remove.addEventListener('click', function () {
                skills.splice(index, 1);
                renderSkills();
            });

            tag.appendChild(remove);
            tagContainer.appendChild(tag);
        });

        if (skillsCsv) skillsCsv.value = skills.join(',');
    }

    function addSkill() {
        if (!skillInput) return;

        const value = skillInput.value.trim();
        if (value.length === 0) return;

        // Skill.Name veritabanında 100 karakterle sınırlı
        if (value.length > 100) {
            skillInput.setCustomValidity('Yetenek adı en fazla 100 karakter olabilir.');
            skillInput.reportValidity();
            return;
        }

        skillInput.setCustomValidity('');

        const exists = skills.some(function (s) {
            return s.toLocaleLowerCase('tr') === value.toLocaleLowerCase('tr');
        });

        if (!exists) skills.push(value);

        skillInput.value = '';
        renderSkills();
    }

    if (addSkillButton) addSkillButton.addEventListener('click', addSkill);

    if (skillInput) {
        skillInput.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') return;
            e.preventDefault();   // Enter formu göndermesin
            addSkill();
        });
    }

    renderSkills();

    (function () {
        const alanlar = window.uncertainFields || [];
        if (alanlar.length === 0) return;

        // Alan adı -> sayfadaki karşılığı
        const eslesme = {
            fullName: '[name="FullName"]',
            email: '[name="Email"]',
            phone: '[name="Phone"]',
            city: '[name="CityId"]',
            address: '[name="Address"]',
            totalExperienceMonths: '[name="ExperienceYears"], [name="ExperienceMonths"]',
            educations: '#educationList',
            workExperiences: '#experienceList',
            projects: '#projectList',
            skills: '#skillTags'
        };

        alanlar.forEach(function (ad) {
            const secici = eslesme[ad];
            if (!secici) return;

            document.querySelectorAll(secici).forEach(function (el) {
                el.classList.add('cvm-uncertain');
            });
        });
    })();
})();