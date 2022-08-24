public static class MultiDeviceReportHtmlManifest
{
    public static readonly string REPORT_HTML_TEMPLATE = @"
        <head>
            <script type='text/javascript' src='https://www.gstatic.com/charts/loader.js'></script>
            <script src='https://code.jquery.com/jquery-3.6.0.min.js' integrity='sha256-/xUj+3OJU5yExlq6GSYGSHk7tPXikynS7ogEvDej/m4=' crossorigin='anonymous'></script>
            <script type='text/javascript'>
                function ShowDetails(deviceName) {
                    let modal = $('.modal-popup.details');
                    modal.find('h2').text(deviceName);
                    let modal_details = modal.find('.details-area');
                    modal.css('display', 'block');
                    let modal_background = $('.modal-background');
                    modal_background.css('display', 'block');
                    modal_background.addClass('modal-background-show').css('animation-direction', 'normal');
                    modal.addClass('modal-show').css('animation-direction', 'normal'); 

                    let json = JSON.parse($('#' + deviceName.replace(/ /g, '_')).val());
                    $('#start_time').text(json.RunStartTime);
                    $('#run_time').text(json.RunTime);
                    $('#device_type').text(json.DeviceType);
                    $('#device_model').text(json.DeviceModel);
                    $('#device_udid').text(json.Udid);
                    $('#aspect_ratio').text(json.AspectRatio);
                    $('#resolution').text(json.Resolution);
                }
                function GetClassNameFromOperatingSystem(os) {
                    if(os.toLowerCase().includes('iphone')) {
                        return 'device-image ios-phone';
                    }
                    if(os.toLowerCase().includes('ipad')) {
                        return 'device-image ios-tablet';
                    }
                    // TODO: Implement check that determines if android tablet in ReportingManager? https://forum.unity.com/threads/detecting-between-a-tablet-and-mobile.367274/
                    if(os.toLowerCase().includes('android phone')) {
                        return 'device-image android-tablet';
                    }                    
                    if(os.toLowerCase().includes('android')) {
                        return 'device-image android-phone';
                    }
                    return '';
                }
                $(function ()  { 
                    var animateTime = 750;
                    var minBarWidth = 20;
                    
                    var device_runs = $('.test-results');
                    var device_paths = $('.test-path');
                    var devices_json = [];
                    var device_reports = [];
                    for(let d = 0; d < device_runs.length; d++) {
                        let json = JSON.parse(device_runs[d].value);
                        devices_json.push(json);
                        device_reports.push(device_paths[d].value);
                    }
                                        
                    var devices_with_warnings = [];
                    var devices_with_failures = [];                
                    for(let i = 0; i < devices_json.length; i++) {
                        let device = devices_json[i];
                        let device_id = device.DeviceModel.replace(/ /g,'');
                        let os = GetClassNameFromOperatingSystem(device.OperatingSystem);
                        let html = `
                                <div id='${device_id}'>
                                    <h2>${device.DeviceModel}</h2>
                                    <div class='device-details-area'>
                                        <div class='device-image-container'>
                                            <div class='${os}'></div>
                                        </div>
                                        <div class='report-button-group'>
                                            <button class='go-to-report-button' onclick='document.location.href = ""file:///${device_reports[i]}/report.html"";'>Report</button>
                                            <button class='extended-details-button' onclick='ShowDetails(""${device.DeviceModel}"");'>Details</button>
                                        </div>
                                        <div class='report-details'>
                                            <div class='bar pass'><div class='count pass'></div></div>
                                            <div class='bar warning'><div class='count warning'></div></div>
                                            <div class='bar fail'><div class='count fail'></div></div>
                                        </div>
                                    </div>
                                </div>
                        `;
                        $('.device-list-container').append(html);
                        
                        let width_for_bars_to_expand_in_pixels = window.innerWidth - $('.bar.pass')[0].getBoundingClientRect().x - 25;
                        
                        let passCount, failCount, warningCount;
                        passCount = failCount = warningCount = 0;
                        for(let t = 0; t < devices_json[i].Tests.length; t++) {
                
                            let test = devices_json[i].Tests[t];
                            switch(test.Status) {
                                case 'Pass':
                                    passCount++;
                                    break;
                                case 'Fail':
                                    failCount++;
                                    break;
                                case 'Warning':
                                    warningCount++;
                                    break;
                            }
                        }
                            
                        if(failCount > 0)
                        {
                            devices_with_failures.push(device.DeviceModel);
                        }
                        else if(warningCount > 0)
                        {
                            devices_with_warnings.push(device.DeviceModel);
                        }
                        
                        $(`#${device_id}`).find('.count.pass').text(passCount);
                        $(`#${device_id}`).find('.count.fail').text(failCount);
                        $(`#${device_id}`).find('.count.warning').text(warningCount);
                        let totalTestCount = passCount + failCount + warningCount;
                        
                        let passWidthMax = width_for_bars_to_expand_in_pixels * (passCount / totalTestCount);
                        let failWidthMax = width_for_bars_to_expand_in_pixels * (failCount / totalTestCount);
                        let warningWidthMax = width_for_bars_to_expand_in_pixels * (warningCount / totalTestCount);
                        $(`#${device_id}`).find('.bar.pass').animate({width: (passWidthMax < 20 ? 20 : passWidthMax) + 'px'}, animateTime);
                        $(`#${device_id}`).find('.bar.fail').animate({width: (failWidthMax < 20 ? 20 : failWidthMax) + 'px'}, animateTime);
                        $(`#${device_id}`).find('.bar.warning').animate({width: (warningWidthMax < 20 ? 20 : warningWidthMax) + 'px'}, animateTime);
                    }
                    
                    let container = $('.device-fails-container');
                    let html = '';
                    for(let x = 0; x < devices_with_failures.length; x++)
                    {
                        let device_id = devices_with_failures[x].replace(/ /g,'');
                        if(x == 0)
                        {
                            html += `<h3 class='fail-summary-header'>Devices With Failures</h3>`;
                            html += `<div class='list-anchors'>`;
                        }
                        html += `<a class='anchor fail' href='#${device_id}'>${devices_with_failures[x]}</a>`;
                        if(x == devices_with_failures.length)
                        {
                            html += `</div>`;
                        }                    
                    }
                    container.append(html);

                    container = $('.device-warnings-container');
                    html = '';
                    for(let x = 0; x < devices_with_warnings.length; x++)
                    {
                        let device_id = devices_with_warnings[x].replace(/ /g,'');
                        if(x == 0)
                        {
                            html += `<h3 class='fail-summary-header'>Devices With Only Warnings</h3>`;
                            html += `<div class='list-anchors'>`;
                        }
                        html += `<a class='anchor warning' href='#${device_id}'>${devices_with_warnings[x]}</a>`;
                        if(x == devices_with_warnings.length)
                        {
                            html += `</div>`;
                        }
                    }
                    container.append(html);
                });
            </script>
            <style>
                .anchor {
                    margin: 2px 20px 2px 2px;
                    display: block;
                    text-decoration: none;
                    cursor: pointer;
                    font-weight: bold;
                    flex: 1 1 20px;
                }
                .anchor.fail {
                    color: red;
                }    
                .anchor.warning {
                    color: orange;
                }                
                .bar {
                    position: absolute;
                    left: 20%;
                    height: 25%;
                    width: 10px;
                }
                .bar.fail {
                    top: 62.5%;
                    background-color: red;
                }
                .bar.pass {
                    top: 8%;
                    background-color: green;
                }
                .bar.warning {
                    top: 35%;
                    background-color: orange;            
                }
                body {
                    font-family: sans-serif;
                    overflow-x: hidden;
                    margin-bottom: 25px;
                }
                button {
                    display: block;
                    left: 100px;
                    height: 75px;
                    width:     150px;
                    margin: 15px 10px 20px 10px;
                    background-color: white;
                    border-radius: 6px;
                    border-color: #2196f3;
                    color: #2196f3;
                    font-size: 1.25em;
                    cursor: pointer;
                }
                button:hover {
                    background-color: #2196f3;
                    border-radius: 6px;
                    border-color: white;
                    color: white;                
                }
                .count {
                    color: white;
                    font-size: 1.25em;
                    position: relative;
                    top: 50%;
                    transform: translateY(-50%);
                    margin-left: 5px;
                }
                .device-details-area
                {
                    border: 2px solid black;
                    border-radius: 6px;
                    padding: 3px;
                    height: 200px;
                    overflow: hidden;
                }
                .device-image {
                    cursor: pointer;
                    border-radius: 6px 0 0 6px;
                    margin: 0 0 0 25px;
                    background-repeat: no-repeat;
                }
                .device-image.android-phone {
                    margin-bottom: -5px;
                    height: 195px;
                    width: 150px;
                    background-image: url('DeviceReports/imgs/android_phone.png');
                    background-size: 65%;
                }
                .device-image.android-tablet {        
                    width: 150px;
                    height: 150px;
                    margin-bottom: -5px;
                    background-image: url('DeviceReports/imgs/android_tablet.png');
                    background-size: 90%;
                }
                .device-image.ios-phone {
                    height: 195px;
                    width: 150px;
                    background-image: url('DeviceReports/imgs/ios_phone.png');
                    background-size: 65%;
                }
                .device-image.ios-tablet {
                    height: 170px;
                    width: 150px;
                    background-image: url('DeviceReports/imgs/ios_tablet.png');
                    background-size: 90%;
                }
                .device-image-container {
                    display: inline-block;
                }
                .device-list-container {
                    margin: 100px 20px 10px 20px;
                }
                .header-logo {
                    position: absolute;
                    top: 0;
                }    
                .header-region {
                    width: calc(100% - 125px);
                    height: 80px;
                    position: absolute;
                    background-color: black;
                    left: 125px;
                    top: 0;
                }
                .header-region::before {
                    position: absolute;
                    display: inline-block;
                    border-top: 40px solid transparent;
                    border-left: 40px solid white;
                    border-bottom: 40px solid transparent;
                    content: '';
                }
                .header-title {
                    color: white;
                    padding-left: 60px;
                    white-space: nowrap;
                }
                .list-anchors {
                    display: flex;
                    flex-direction: column;
                    flex-wrap: wrap;
                    height: 80px;
                    width: 400px;
                }
                .modal-background {
                    display: none;
                    z-index: 98;
                    position: fixed;
                    background-color: black;
                    width: 100%;
                    height: 100%;
                    top: 0;
                    left: 0;
                }
                .modal-close {
                    cursor: pointer;
                    position: absolute;
                    top: 0;
                    right: 0;
                    padding: 5px 10px 5px 10px;
                    border: 1px solid black;
                    border-radius: 6px;
                }
                .modal-popup {
                    display: none;
                    position: fixed;
                    width: 50%;
                    height: 50%;
                    left: 50%;
                    top: 50%;
                    transform: translate(-50%, -50%);
                    background-color: white;
                    border: 2px solid black;
                    border-radius: 6px;
                }
                .modal-popup.fps {
                    z-index: 99;
                    margin-left: 25px;
                    margin-right: 25px;
                    width: 95% !important;
                    height: 95% !important;
                    overflow: scroll;
                }
                .modal-title {
                    width: 250px;
                    padding: 20px;
                    margin-left: 50%;
                    transform: translate(-50%);
                }
                .modal-show {
                    z-index: 99;
                    animation-fill-mode: forwards;
                    animation-name: toggleVisible;
                    animation-duration: 1s;
                    animation-direction: normal;
                }
                .modal-background-show {
                    z-index: 98;
                    animation-fill-mode: forwards;
                    animation-name: toggleHalfVisible;
                    animation-duration: 1s;
                    animation-direction: normal;
                }
                .report-details {
                    height: 100%;
                    left: 175px;
                    position: relative;
                    display: inline-block;
                }
                .report-button-group {
                    display: inline-block;
                    position: absolute;
                }
                .test-run-data {
                    white-space: nowrap;
                    padding: 4px;
                }    
                .test-run-data > .data-label {
                    font-weight: bold;
                    display: inline-block;
                    margin-right: 10px;
                }
                .test-run-data > .data-value {
                    display: inline-block;
                }
                .test-run-data-region {
                    position: relative;
                    width: fit-content;
                    margin: auto;
                    padding: 10px;
                    border: 2px solid black;
                    border-radius: 6px;
                    z-index: 2;
                    vertical-align: top;
                }
                ::-webkit-scrollbar {
                    width: 20px;
                }
                ::-webkit-scrollbar-thumb {
                    background-color: black;
                    background-clip: content-box;
                    border-radius: 25px;
                    border: 2px solid transparent;
                }
                ::-webkit-scrollbar-track {
                    background-color: transparent;
                }
                @media screen and (max-width: 1000px) {
                    .modal-popup {
                        width: 70%;
                        height: 70%;
                    }
                }
                @media screen and (max-height: 1000px) {
                    .modal-popup {
                        height: 70%;
                    }
                }
                @media screen and (max-width: 700px) {
                    .modal-popup {
                        width: 90%;
                        height: 90%;
                    }
                    .test-run-data-region {
                        position: relative;
                        top: 20;
                        width: calc(100% - 50px);
                    }
                }
                @media screen and (max-height: 700px) {
                    .modal-popup {
                        height: 90%;
                    }
                }
                @keyframes toggleVisible {
                  0%   {opacity: 0; z-index: -99;}
                  1%   {opacity: 0; z-index: 99}
                  100% {opacity: 1; z-index: 99}
                }
                @keyframes toggleHalfVisible {
                  0%   {opacity: 0; z-index: -98}
                  1%   {opacity: 0; z-index: 98;}
                  100% {opacity: 0.5; z-index: 98}
                }
            </style>
        </head>

        <svg class='header-logo' width='100' height='80' viewBox='0 0 89 32' xmlns='http://www.w3.org/2000/svg'>
            <g fill='currentColor' fill-rule='evenodd'>
                <path d='M28.487 0L15.42 3.405l-1.933 3.318-3.924-.029L0 15.995l9.564 9.3 3.922-.03 1.938 3.317 13.063 3.405 3.5-12.702-1.989-3.29 1.989-3.29L28.487 0zM13.802 7.257l9.995-2.498-5.737 9.665H6.584l7.218-7.167zm0 17.474l-7.218-7.166H18.06l5.737 9.664-9.995-2.498zm12.792.927l-5.74-9.663 5.74-9.667 2.771 9.667-2.771 9.663zM58.123 9.424c-1.746 0-2.918.723-3.791 2.095h-.075V9.773h-3.055v12.795h3.13V15.31c0-1.746 1.097-2.943 2.594-2.943 1.421 0 2.48.843 2.48 2.345v7.856h3.131v-8.355c0-2.794-1.77-4.789-4.414-4.789M46.44 17.15c0 1.696-.973 2.893-2.57 2.893-1.446 0-2.356-.823-2.356-2.32V9.767h-3.13v8.53c0 2.793 1.596 4.614 4.44 4.614 1.795 0 2.793-.673 3.666-1.845h.074v1.496h3.01V9.767H46.44v7.383M64.178 22.568h3.131V9.773h-3.13zM64.178 8.354h3.131V5.783h-3.13zM85.002 9.773l-1.86 5.761c-.4 1.173-.748 2.794-.748 2.794h-.08s-.424-1.621-.823-2.794l-2.1-5.76h-3.347l3.442 9.102c.723 1.946.972 2.769.972 3.467 0 1.048-.548 1.746-1.895 1.746h-1.197v2.669h1.995c2.594 0 3.494-1.023 4.467-3.866l4.511-13.119h-3.337M73.142 18.802v-6.784h1.995V9.773h-1.995v-3.99h-3.11v3.99h-1.77v2.245h1.77v7.507c0 2.42 1.822 3.068 3.468 3.068 1.346 0 1.712-.05 1.712-.05v-2.474s-.374.005-.798.005c-.749 0-1.272-.325-1.272-1.272'></path>
            </g>
        </svg>

        <div class='header-region'>
            <h1 class='header-title'>Cloud Device Run Report</h1>
        </div>

        <div class='device-list-container'>        
            <div class='device-fails-container'></div>
            <div class='device-warnings-container'></div>
        </div>
        
        <div class='modal-background' onclick='$("".modal-close"").click();'></div>
        <div class='modal-popup details'>
            <div class='modal-close' onclick='$(this).parent().css(""animation-direction"", ""reverse"");$("".modal-background"").css(""animation-direction"", ""reverse"");'>X</div>
            <h2 class='modal-title'>Device Details</h2>
            <div class='details-area'>
                <div class='test-run-data-region'>
                    <div class='test-run-data'><div class='data-label'>Time Started UTC:</div><div id = 'start_time' class='data-value'></div></div>
                    <div class='test-run-data'><div class='data-label'>Total Run Time:</div><div id = 'run_time' class='data-value'></div></div>
                    <div class='test-run-data'><div class='data-label'>Device Type:</div><div id = 'device_type' class='data-value'></div></div>
                    <div class='test-run-data'><div class='data-label'>Device Model:</div><div id = 'device_model' class='data-value'></div></div>
                    <div class='test-run-data'><div class='data-label'>Device UDID:</div><div id = 'device_udid' class='data-value'></div></div>
                    <div class='test-run-data'><div class='data-label'>Aspect Ratio:</div><div id = 'aspect_ratio' class='data-value'></div></div>
                    <div class='test-run-data'><div class='data-label'>Resolution:</div><div id = 'resolution' class='data-value'></div></div>
                    <div class='test-run-data' id='custom_data_toggle' onclick='ToggleCustomData()' style='display: none;'><div id = 'custom_data' class='data-label'>▶ Custom Data:</div><div class='data-value'></div></div>
                    <div id = 'custom_data_element' class='custom-data-container'></div>
                </div>
            </div>
        </div>
        
    ";
}