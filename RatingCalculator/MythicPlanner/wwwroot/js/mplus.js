(function ($, window, document) {
    'use strict';

    // ---------- Spinner / loading ----------
    function toggleLoading(show) {
        if (show) {
            $(".loadingSpinner").show();
            $('[role="status"]').show();
            $('[role="button"]').hide();
            $("#CharInfoDiv").addClass("loader");
        } else {
            $(".loadingSpinner").hide();
            $('[role="status"]').hide();
            $('[role="button"]').show();
            $("#CharInfoDiv").removeClass("loader");
        }
        $(".btn").prop('disabled', show);
    }

    // ---------- Remember Me ----------
    const STORAGE_KEY = 'characterInfo';

    function readRememberMe() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            return raw ? JSON.parse(raw) : null;
        } catch { return null; }
    }

    function writeRememberMe(on) {
        if (window.MPLUS?.init?.fromUrl) return;     
        if (!on) { localStorage.removeItem(STORAGE_KEY); return; }
        const info = {
            region: $("#Region").val() || null,
            realm: $("#Realm").val() || null,
            name: $("#CharacterName").val() || null
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(info));
    }

    $('#RememberMe').on('change', function () {
        if (window.MPLUS?.init?.fromUrl) return; 
        if (!this.checked) localStorage.removeItem(STORAGE_KEY);
        else writeRememberMe(true);
    });

    // Keep your old helper name
    function updateStoredInfo(store) { writeRememberMe(store); }

    // ---------- Select helpers (your working version) ----------
    function populateSelect($sel, items, preselectValue) {
        // Clear old options
        $sel.empty();

        // Add new options
        (items || []).forEach(it => {
            const opt = document.createElement('option');
            opt.value = it.value ?? '';
            opt.text = it.text ?? '';
            if (it.title) opt.title = it.title;
            $sel[0].appendChild(opt);
        });

        // Reset bootstrap-select completely, then rebuild
        if ($sel.data('selectpicker')) {
            $sel.selectpicker('destroy');
            $sel.selectpicker();
        }

        // Apply preselect if given
        if (preselectValue != null && preselectValue !== '') {
            $sel.selectpicker
                ? $sel.selectpicker('val', preselectValue)
                : $sel.val(preselectValue);
        }
    }

    function nextLoadId($sel) {
        const id = ($sel.data('loadId') || 0) + 1;
        $sel.data('loadId', id);
        return id;
    }
    function isLatestLoad($sel, id) {
        return $sel.data('loadId') === id;
    }
    
    // Patch GetSelectItems to track the version
    function GetSelectItems(action, id, $select, { showId, done } = {}) {
        const $loader = showId ? $('#' + showId) : $();
        const loadId = nextLoadId($select); // mark a new request for this select

        return $.ajax({
            url: (window.MPLUS?.controllerBase || '') + '/' + action,
            type: 'GET',
            dataType: 'json',
            data: { id },
            beforeSend: function () {
                $loader.show();
                if ($select.data('selectpicker')) $select.selectpicker('hide');
            },
            complete: function () {
                if ($select.data('selectpicker')) {
                    $select.selectpicker('show').selectpicker('render');
                }
                $loader.hide();
                if (typeof done === 'function') done();
            }
        }).then(function (data) {
            // Only the latest call for this select may proceed
            if (!isLatestLoad($select, loadId)) return $.Deferred().reject('stale').promise();
            return data;
        });
    }

    // ---------- Page bootstrapping ----------
    $(async function () {
        const $form = $("form.needs-validation");
        const fromUrl = !!(window.MPLUS?.init?.fromUrl);  

        if(fromUrl) {
            $('#RememberMe').prop('checked', false).prop('disabled', true)
                .closest('.form-check').addClass('disabled'); // (optional) gray it out
        }

        // Keep your AJAX form submit exactly as-is
        $form.off("submit.mplus").on("submit.mplus", function (e) {
            e.preventDefault();

            var rememberMe = $('#RememberMe').is(':checked');
            updateStoredInfo(rememberMe);

            $.ajax({
                url: (window.MPLUS?.controllerBase || '') + "/ProcessCharacter",
                type: "get",
                dataType: "html",
                data: $form.serialize(),
                beforeSend: function () { toggleLoading(true); },
                complete: function () { toggleLoading(false); },
                success: function (result) {
                    $("#CharInfoDiv").html(result);
                    $('[data-toggle="tooltip"]').tooltip({ html: true });

                    // ---- PICK THE SHARE URL FROM THE PARTIAL ----
                    let share =
                        $("#CharInfoDiv #ShareableUrl").val?.() || "";
                    
                    if (share) {
                        // 1) Update the address bar without reloading
                        try {
                            window.history.replaceState({ share }, "", share);
                        } catch { /* ignore malformed URLs */ }

                        // 2) Populate the external field and show it
                        $("#shareableUrl").val(share);
                        $("#shareSection").removeClass("d-none");
                    }
                },
                error: function (xhr) {
                    $("#CharInfoDiv").html(
                        `<div class="alert alert-danger" role="alert"><div>${xhr.responseText}</div></div>`
                    );
                }
            });

            return false;
        });

        // Grab elements
        const $region = $('#Region');
        const $realm = $('#Realm');
        const $exp = $('#ExpansionId');
        const $season = $('#SeasonSlug');
        const $dungs = $('#AvoidDungeon');

        // Model/Razor-provided init
        const modelInit = (window.MPLUS && window.MPLUS.init) || {
            region: '', realm: '', expansionId: '', seasonSlug: '', avoidDungeon: []
        };

        // Local memory
        const mem = readRememberMe() || {};

        // Merge: model first, then memory, else blank
        const init = {
            region: modelInit.region || mem.region || '',
            realm: modelInit.realm || mem.realm || '',
            expansionId: modelInit.expansionId || '',
            seasonSlug: modelInit.seasonSlug || '',
            avoidDungeon: Array.isArray(modelInit.avoidDungeon) ? modelInit.avoidDungeon : []
        };

        // Ensure Region is set before fetching realms
        if (init.region) $region.val(init.region);

        // Defaults for numeric inputs when no model (your earlier ask)
        if (!$('#MaxKeyLevel').val()) $('#MaxKeyLevel').val(15);

        // ---- Region -> Realms ----
        try {            
            const realmsData = await GetSelectItems('GetRealms', $region.val(), $realm, { showId: 'LoadGetRealms' });
            populateSelect($realm, realmsData, init.realm || '');
        } catch (e) {
            console.error('Failed loading realms', e);
            populateSelect($realm, [], '');
        }

        // ---- Expansion (already rendered via ViewBag) ----
        if (init.expansionId && $exp.val() !== init.expansionId) {
            $exp.selectpicker ? $exp.selectpicker('val', init.expansionId) : $exp.val(init.expansionId);
        }

        // ---- Expansion -> Seasons ----
        try {
            const seasonsData = await GetSelectItems('GetSeasons', $exp.val(), $season, { showId: 'LoadGetSeasons' });
            let preselectSeason = init.seasonSlug;
            if (!preselectSeason && Array.isArray(seasonsData)) {
                const selectedItem = seasonsData.find(s => s.selected);
                if (selectedItem) preselectSeason = selectedItem.value;
            }
            populateSelect($season, seasonsData, preselectSeason || '');
        } catch (e) {
            console.error('Failed loading seasons', e);
            populateSelect($season, [], '');
        }

        // ---- Season/Expansion -> Dungeons ----
        async function loadDungeons(preselectArray) {
            try {
                const payload = JSON.stringify({ Season: $season.val(), Expansion: $exp.val() });
                const dungsData = await GetSelectItems('GetDungeons', payload, $dungs, { showId: 'LoadGetDungeons' });
                populateSelect($dungs, dungsData, null); // init UI
                if (Array.isArray(preselectArray) && preselectArray.length) {
                    $dungs.selectpicker ? $dungs.selectpicker('val', preselectArray) : $dungs.val(preselectArray);
                }
            } catch (e) {
                console.error('Failed loading dungeons', e);
                populateSelect($dungs, [], null);
            }
        }
        await loadDungeons(init.avoidDungeon);

        // Cascading changes
        $region.on('change', async function () {
            try {
                const realmsData = await GetSelectItems('GetRealms', $(this).val(), $realm, { showId: 'LoadGetRealms' });
                populateSelect($realm, realmsData, ''); // clear selection on region change
            } catch {
                populateSelect($realm, [], '');
            }
        });

        $exp.on('change', async function () {
            try {
                const seasonsData = await GetSelectItems('GetSeasons', $(this).val(), $season, { showId: 'LoadGetSeasons' });
                const selected = (seasonsData || []).find(s => s.selected)?.value || '';
                populateSelect($season, seasonsData, selected);
            } catch {
                populateSelect($season, [], '');
            }
            await loadDungeons([]); // refresh
        });

        $season.on('change', async function () {
            await loadDungeons($dungs.val() || []);
        });

        // RememberMe checkbox state + name
        if (mem && (mem.region || mem.realm || mem.name)) {
            $('#RememberMe').prop('checked', true);
            if (!$('#CharacterName').val() && mem.name) $('#CharacterName').val(mem.name);
        }

        $("#TargetRating").focus();
        if (fromUrl) {
            // Ensure the DOM has reflected selectpicker values before submit
            setTimeout(function () { $form.trigger('submit'); }, 0);
        }

        // Copy to clipboard functionality
        $('#copyShareUrl').on('click', function () {
            var urlInput = document.getElementById('shareableUrl');
            urlInput.select();
            urlInput.setSelectionRange(0, 99999); // For mobile devices

            try {
                document.execCommand('copy');
                $('#copySuccess').fadeIn().delay(2000).fadeOut();
            } catch (err) {
                // Fallback for browsers that don't support execCommand
                navigator.clipboard.writeText(urlInput.value).then(function () {
                    $('#copySuccess').fadeIn().delay(2000).fadeOut();
                }).catch(function (err) {
                    console.error('Failed to copy: ', err);
                });
            }
        });
    });

})(jQuery, window, document);
