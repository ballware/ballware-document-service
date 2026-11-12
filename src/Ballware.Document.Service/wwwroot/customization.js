window.BallwareReportingCustomization = {
    loadLocalization: function (e, url) {
        fetch(url)
            .then(function (response) {
                return response.json();
            })
            .then(function (messages) {
                e.LoadMessages(messages);
            });
    },
    customizeLocalization: function (s, e) {
        DevExpress.localization.locale('de');

        window.BallwareReportingCustomization.loadLocalization(e, '/dx/Localization/dx-analytics-core.de.json');
        window.BallwareReportingCustomization.loadLocalization(e, '/dx/Localization/dx-reporting.de.json');
        window.BallwareReportingCustomization.loadLocalization(e, '/dx/Localization/dx-rich.de.json');
    },
    customizeMenuActions: function (s, e) {
        var hiddenActionIds = [
            'dxrd-newreport',
            'dxrd-newreport-via-wizard',
            'dxrd-run-wizard',
            'dxrd-exit'
        ];

        for (var i = 0; i < hiddenActionIds.length; i++) {
            var action = e.GetById(hiddenActionIds[i]);

            if (action) {
                action.visible = false;
            }
        }
    }
};
