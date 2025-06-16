import $ from 'jquery';
import ko from 'knockout';

import 'devextreme/dist/css/dx.light.css';
import '@devexpress/analytics-core/dist/css/dx-analytics.common.css';
import '@devexpress/analytics-core/dist/css/dx-analytics.light.compact.css';
import '@devexpress/analytics-core/dist/css/dx-querybuilder.css';
import 'devexpress-reporting/dist/css/dx-webdocumentviewer.css';
import 'devexpress-reporting/dist/css/dx-reportdesigner.css';

import 'devexpress-reporting/dx-webdocumentviewer';
import 'devexpress-reporting/dx-reportdesigner';

import { fetchSetup } from '@devexpress/analytics-core/analytics-utils';

var urlSearchParams = new URLSearchParams(window.location.search);
/*
fetchSetup.fetchSettings = {
    headers: { 'Authorization': 'Bearer ' + urlSearchParams.get('token') }
};

urlSearchParams.delete('token');
*/

var viewModel = {
    designerOptions: {
        fetchSetup: fetchSetup,
        reportUrl: 'new', //reportUrl, // The URL of a report that is opened in the Report Designer when the application starts.
        requestOptions: { // Options for processing requests from the Report Designer. 
            getDesignerModelAction: "/api/ReportDesigner/GetReportDesignerModel" // Action that returns the Report Designer model.
        },
        callbacks: {
            CustomizeLocalization: function (s, e) {
                e.LoadMessages($.get("/dx/dist/dx-analytics-core.de.json"));
                e.LoadMessages($.get("/dx/dist/dx-reporting.de.json"));
                e.LoadMessages($.get("/dx/dist/dx-rich.de.json"));
            },
            CustomizeMenuActions: function (s, e) {
                e.GetById('dxrd-newreport').visible = false;
                e.GetById('dxrd-newreport-via-wizard').visible = false;
                e.GetById('dxrd-run-wizard').visible = false;
                e.GetById('dxrd-exit').visible = false;
            }
        }
    }
};

ko.applyBindings(viewModel);