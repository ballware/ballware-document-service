window.BallwareReportingCustomization = (function () {
    'use strict';

    var localizationFiles = [
        'dx/Localization/dx-analytics-core.de.json',
        'dx/Localization/dx-reporting.de.json',
        'dx/Localization/dx-rich.de.json'
    ];

    var hiddenActionIds = [
        'dxrd-newreport',
        'dxrd-newreport-via-wizard',
        'dxrd-run-wizard',
        'dxrd-exit'
    ];

    // LoadMessages akzeptiert ein fertig geparstes Object (wird synchron uebernommen)
    // oder ein jQuery-Deferred, das von DevExpress vor dem Rendern des Controls
    // abgewartet wird. Ein fetch()-Promise erfuellt diesen Vertrag nicht (kein done()),
    // deshalb wurde die Uebersetzung bisher nachtraeglich und damit wirkungslos geladen.
    function loadLocalization(args, url) {
        var jq = window.jQuery || window.$;

        args.LoadMessages(jq.ajax({
            url: new URL(url, document.baseURI).href,
            dataType: 'json'
        }));
    }

    return {
        customizeLocalization: function (s, e) {
            var jq = window.jQuery || window.$;

            if (!jq || typeof jq.ajax !== 'function') {
                console.error('BallwareReportingCustomization: jQuery is not available, reporting localization was skipped.');
                return;
            }

            if (e.WidgetLocalization) {
                e.WidgetLocalization.locale('de');
            }

            for (var i = 0; i < localizationFiles.length; i++) {
                loadLocalization(e, localizationFiles[i]);
            }
        },
        customizeMenuActions: function (s, e) {
            for (var i = 0; i < hiddenActionIds.length; i++) {
                var action = e.GetById(hiddenActionIds[i]);

                if (action) {
                    action.visible = false;
                }
            }
        }
    };
})();
