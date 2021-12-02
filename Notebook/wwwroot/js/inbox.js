function getInbox() {
    $.ajax({
        type: "POST",
        url: "api/inbox/getinbox/" + session,
        contentType: "application/json/",
        success: function (res) {
            console.log(res);
            e('inbox-table').innerHTML = createInbox(res);
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function createInbox(res) {
    var count = 0;
    var html = '';

    res.forEach(function (element) {
        var faded;

        if (element['seen']) {
            faded = 'faded-more';
        }
        else {
            count++;
        }

        html += `
                <tr class="${faded}" id="msg-${element.id}">
                    <td>
                        <div class="inbox-namebadge">${element.initial_a}<span style="font-size:60%">${element.initial_b}</span></div>
                    </td>
                    <td onclick="ignoreItem(${element.id}); viewItem(${element.item_id},${element.type});">
                        ${element.header} <span>${element.content}</span>
                    </td>
                    <td onclick="ignoreItem(${element.id}); viewItem(${element.item_id},${element.type});">
                        View
                    </td>
                    <td onclick="decline(${element.id})">
                        Ignore
                    </td>
                </tr>`;
    });

    if (count == 0) {
        e('inbox-counter').style.display = 'none';
    }
    else {
        e('inbox-counter').style.display = 'block';
        e('inbox-counter').innerHTML = count;
    }
    return html;
}

function decline(i) {

}

function ignoreItem(i) {

    var data = {
        session_id: session,
        message_id: i
    };

    $.ajax({
        type: "POST",
        url: "api/inbox/messageread",
        contentType: "application/json/",
        data: JSON.stringify(data),
        success: function (res) {
            if (res['ok']) {
                e('msg-' + i).style.opacity = 0.4;
            }
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function viewItem(i, type) {
    if (type == 0)
        quickView(i);
    else
        openCollection(i);
}