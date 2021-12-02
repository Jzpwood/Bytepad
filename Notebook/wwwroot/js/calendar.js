const monthNames = ["January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December"
];

var calendar = {
    'current_month': 10,
    'current_year': 2021,
    'cache': null,
    'selected-day': null,
    'cursor-x': 0,
    'cursor-y': 0
}

function getCalendarDefault() {
    setCalendarDefault();
    getCalendar(true);
}

function dateToMonth(i) {
    return monthNames[i - 1];
}

function getCalendar(def) {
    session = getCookie("session_id");

    var requestObj = { session_id: session, month: calendar['current_month'], year: calendar['current_year'] };

    e('calendar-month-disp').innerHTML = dateToMonth(calendar['current_month']) + ' ' + calendar['current_year'];

    if (session != '') {
        $.ajax({
            type: "POST",
            url: "api/calendar/getcalendar",
            contentType: "application/json/",
            data: JSON.stringify(requestObj),
            success: function (res) {
                console.log(res);
                fillCalendar(res);
                if (def) loadThisWeek(res);
            },
            error: function (xhr, status, error) {
                console.log(error);
            }
        });

    }
    else {
        //No session
    }
}

function loadThisWeek(calendar) {
    var html = '';

    calendar['weeks'].forEach(function (element) {
        element['days'].forEach(function (element) {
            if (element['this_week']) {
                var slips = '';
                var fade = '';

                if (element['events'] != null) {
                    element['events'].forEach(function (element) {
                        slips += `<div onclick="quickView(${element.item_id})" class="day-card-slip">${element.title}</div>`;
                    });
                }
                else {
                    fade = 'faded';
                    slips += `<div class="day-card-empty">No events</div>`;
                }

                html += `                
                <div class="day-card ${fade}">
                    <div onclick="calendarSetCursor('${element.date}')" class="day-card-title">
                        ${element['full_date']}
                    </div>
                    <div class="day-card-content">
                        ${slips}
                    </div>
                </div>`;

            }
        });
    });

    e('upcoming-items').innerHTML = html;
}

function fillCalendar(cal) {

    calendar['cache'] = cal;

    var calendar_el = e('calendar-table');
    var calendar_hdr = e('calendar-day-hdr');

    var html = '';

    cal['weeks'].forEach(function (element) {
        var html_row = '<tr>';

        element['days'].forEach(function (element) {
            var cls = '';

            if (!element['relevant_month']) { cls += 'not ' };
            if (!(element['events'] == null)) { cls += 'has-event ' };
            if (element['today']) { cls += 'today ' };
            if (calendar["selected-day"] == element['date']) {
                cls += 'selected-day';
                calendar["cursor-x"] = element['day_of_week'];
                calendar["cursor-y"] = element['week_number'];
            };

            html_row += `
            <td id="calendar-day-${element['day_of_week'] + '-' + element['week_number']}" onclick = "calendarViewDay(${element['day_of_week']},${element['week_number']})"class="${cls}">
                ${element.day_number}
            </td>
            `;
        });

        html += html_row + '</tr>'
    });

    calendar_el.innerHTML = html;

    calendarViewDay(calendar["cursor-x"], calendar["cursor-y"]);
}

function setCalendarDefault() {
    var d = new Date();
    d.setHours(0, 0, 0, 0);

    calendar['current_month'] = d.getMonth() + 1;
    calendar['current_year'] = d.getFullYear();
    calendar["selected-day"] = d.toLocaleDateString('en-GB');
}

function calendarMove(i) {
    if (i == 1) {
        if (calendar['current_month'] == 12) {
            calendar['current_month'] = 1;
            calendar['current_year']++;
        }
        else {
            calendar['current_month']++;
        }
    }
    if (i == -1) {
        if (calendar['current_month'] == 1) {
            calendar['current_month'] = 12;
            calendar['current_year']--;
        }
        else {
            calendar['current_month']--;
        }
    }

    calendar['selected-day'] = new Date(calendar.current_year, calendar.current_month -1, 1);

    //Refresh Calendar
    getCalendar(false);
}

function calendarSetCursor(date) {
    var cursor = new Object();

    for (var w = 0; w < 6; w++) {
        for (var d = 0; d < 7; d++) {
            if (calendar.cache['weeks'][w]['days'][d].date == date) {
                viewPage(2);
                calendarViewDay(d, w);
                return;
            }
        }
    }
}

function calendarViewDay(day, week) {

    e('calendar-day-' + calendar['cursor-x'] + '-' + calendar['cursor-y']).classList.remove('selected-day');

    calendar['cursor-x'] = day;
    calendar['cursor-y'] = week;

    var d = calendar.cache['weeks'][week]['days'][day];

    console.log(d);

    e('calendar-current-date').innerHTML = `${d['full_date']}<i class="material-icons quickadd-icon">bookmark_add</i>`

    var html = '';

    if (d['events'] != null) {
        d['events'].forEach(function (element) {
            html += `<tr onclick="quickView(${element.item_id})">
                    <td class="calendar-items-icon-row">
                        <i class="material-icons calendar-items-icon">${element.todo ? 'done' : 'event'}</i>
                    </td>
                    <td>
                        ${element['title']}
                    </td>
                    <td class="time">
                        ${element['time']}
                    </td>
                </tr>`;
        });
    }
    else {
        html += `<tr>
                    <td>
                        No events
                    </td>
                </tr>`;
    }

    e('calendar-items').innerHTML = html;

    e('calendar-day-' + calendar['cursor-x'] + '-' + calendar['cursor-y']).classList.add('selected-day');
}