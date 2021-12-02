var id_tally = { 'editor-tags': 10, 'editor-shares': 0 };

var homepage_state = {
    collection_open: -1,
    collection_name: '',
    search_open: false
};

function e(id) {
    return document.getElementById(id);
}

function confirmDialog(text, function_ok) {
    e('modalConfirm').style.display = 'block';
    e('modal-confirm-text').innerHTML = text;
    e('confirm-ok').setAttribute('onclick', `${function_ok}; modalClose();`);
}

function toolbar_deleteCollection(){
    confirmDialog(`Deleting this collection will destroy any items which belong to it. Are you sure you want to delete ${homepage_state.collection_name}?`, 'deleteCollection()');
}

function deleteCollection() {

    var data = { session_id: session, delete: true, id: homepage_state.collection_open };

    $.ajax({
        type: "POST",
        url: "api/collections/commitcollection",
        contentType: "application/json/",
        data: JSON.stringify(data),
        success: function (res) {
            if (!res['ok']) {
                warningDialog(res['error']);
            }
            else {
                getHomepage();
            }
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function toolbar_renameCollection() {
    e('ren_coll').value = homepage_state.collection_name;
    e('renameCat').style.display = 'block';
}

function renameCollection() {
    var new_name = e('ren_coll').value;

    var data = { session_id: session, append: true, name: new_name, id: homepage_state.collection_open };

    $.ajax({
        type: "POST",
        url: "api/collections/commitcollection",
        contentType: "application/json/",
        data: JSON.stringify(data),
        success: function (res) {
            if (!res['ok']) {
                warningDialog(res['error']);
            }
            else {
                getHomepage();
            }
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function createCollections(obj) {
var html = '';

for (var i = 0; i < obj.length; i++) {
    html += addCollection(obj[i], i);
}

    return html += `
    <div class="cat-box-add">
        <div class="add-button" onclick="addCat()">Add</div>
        </div>`;
}

function addCollection(item, i) {
    var html = ` <div class="cat-box ${item.shared ? "cat-box-shared" : ""}" id="category-${item.id}" onclick="viewCat(${item.id},'${item.name}')">
                    ${item.name}
                </div>`

return html;
}

function createItems(obj) {
var html = '';

for (var i = 0; i < obj.length; i++) {
    html += addItem(obj[i]);
}

return html;
}

function addItem(item) {

    var icon;
    var border_colour = 'border-top: 3px solid ';
    var shared = '';
    var completed = '';
    var done = item.todo_complete == 1;

    if (item.todo_complete == 1) {
        completed = 'item-completed';
    }

    if (item.item_type == 0) {
        icon = "";
    border_colour += '#54b0bf;';
}
if (item.item_type == 1) {
    icon = `<a><i class="item-tick material-icons" id="complete-btn-${item.id}" onclick="${done ? 'un' : ''}completeItem(${item.id})">${done ? 'undo' : 'done'}</i></a>`;
border_colour += '#d78787;';
}
if (item.item_type == 2) {
    icon = `<a><i class="item-tick material-icons" onclick="calendarSetCursor('${item.due}')">today</i></a>`;
border_colour += '#62a994;';
}

if (item.shared_with == 1) shared = 'item-shared';

    var html = `
                    <div id="item-${item.id}" class="item ${shared} ${completed}" style="${border_colour}">
                        <table class="item-table" style="height:100%;">
                            <colgroup>
                                <col style="width:20%" />
                                <col style="width:40%" />
                                <col style="width:20%" />
                                <col style="width:20%" />
                            </colgroup>
                            <tr style="border:none;">
                                <td onclick="quickView(${item.id})" colspan="4" class="item-title">
                                    <div class="item-title-container fadeout">
                                        ${item.title}
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2" class="item-content">
                                    ${item.due}
                                </td>
                                <td colspan="2" style="text-align:right;">
                                    <div class="cat-tag">
                                        ${item.collection}
                                    </div>
                                </td>
                            </tr>
                            <tr class="item-content">
                                <td>
                                    ${icon}
                                </td>
                                <td style="text-align: center;">
                                    <div class="date-tag">
                                        ${item.modified_on}
                                    </div>
                                </td>
                                <td style="text-align:right;">
                                    <a><i id="delete-btn-${item.id}" class="item-tick material-icons" onclick="deleteItem(${item.id})">delete</i></a>
                                </td>
                                <td style="text-align:right;">
                                    <a><i class="item-tick material-icons" onclick="editItem(${item.id})">create</i></a>

                                </td>
                            </tr>
                        </table>
                    </div>`
;
return html;
}

function htmlToElement(html) {
    var template = document.createElement('template');
    html = html.trim();
    template.innerHTML = html;
    return template.content.firstChild;
}


function setTags(list) {

    if (list == null || list.length == 0) {
        e('tags-header').style.setProperty('display', 'none');
        return;
    }
    else {
        e('tags-header').style.setProperty('display', 'block');

        var html = '';

        if (list != null) {
            list.forEach(function (element) {
                html +=
                    `<div onclick="viewTag('${element}')" class="chiptag">
                ${element}
             </div>`;
            });
        }


        document.getElementById('tags-header').innerHTML = html;
    }
}


function quickView(item) {

    var requestObj = { session_id: session, item_id: item };

    $.ajax({
        type: "POST",
        url: "api/item/getitem",
        contentType: "application/json/",
        data: JSON.stringify(requestObj),
        success: function (result) {
            var res = result['data'];

            if (!result['ok']) {
                warningDialog(result['error']);
                return;
            }
            console.log(res);

            let stringToParse = res['content'];
            quill.setContents(JSON.parse(stringToParse));
            e('modal-title').innerHTML = res['title'];
            e('quickview-cat').innerHTML = res['collection'];
            e('quickview-date').innerHTML = res['created_date'];
            if (res['due_date'] != null && res['due_date'] != '') {
                e('modal-date').innerHTML = res['friendly_due_date'];
                e('modal-date').style.display = 'block';
            }
            else {
                e('modal-date').style.display = 'none';
            }

            e('edit-item-modal').setAttribute('onclick', `editItem(${item}); modalClose();`);

            var content = quill.root.innerHTML;

            if (content.trim() == '') content = 'No content';

            e('modal-content').innerHTML = content;
            document.getElementById("myModal").style.display = "block";

        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function modalClose(){
    document.getElementById("myModal").style.display = "none";
    document.getElementById("renameCat").style.display = "none";
    document.getElementById("modalConfirm").style.display = "none";
}


window.onclick = function (event) {
    if (event.target == e('myModal')) {
        document.getElementById("myModal").style.display = "none";
    }

    if (event.target == e('newCat')) {
        document.getElementById("newCat").style.display = "none";
    }

    if (event.target == e('warningDialog')) {
        document.getElementById("warningDialog").style.display = "none";
    }
}

function viewTag(t) {

    var requestObj = { session_id: session, multiple: true, tag: t, order: 0, collection_id: -1};

    $.ajax({
        type: "POST",
        url: "api/item/getitem",
        contentType: "application/json/",
        data: JSON.stringify(requestObj),
        success: function (res) {
            homepage_state.collection_open = -1;
            homepage_state.collection_name = t;

            //Collection menu open
            e('collection-tbar-menu').style.display = 'inline-block';
            setTags(res['tags']);
            document.getElementById('upcoming-items').style.setProperty('display', 'none');
            document.getElementById('items-header').innerHTML = t;
            document.getElementById('upcoming-items-header').style.setProperty('display', 'none');
            e('recent-items').innerHTML = createItems(res['results']);
            e('collection-close').style.display = 'inline-block';
            console.log(res);

            var collections_parent = e('collections-grid');

            var nodes = collections_parent.children.length;

            [...collections_parent.children].forEach(function (element, index) {
                console.log(element); console.log(index);
                if (index < nodes - 1) {
                    element.classList.remove('cat-box-active');
                }
            });
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}


function search(t) {

    if (t.trim() == '' && homepage_state.search_open) {
        closeCollection();
        homepage_state.search_open = false;
        return;
    }
    else if(t.trim() == '') {
        return;
    }

    homepage_state.search_open = true;

    var requestObj = { session_id: session, multiple: true, search_term: t.trim(), order: 0, collection_id: -1 };

    $.ajax({
        type: "POST",
        url: "api/item/getitem",
        contentType: "application/json/",
        data: JSON.stringify(requestObj),
        success: function (res) {
            homepage_state.collection_open = -1;
            homepage_state.collection_name = '';

            //Collection menu open
            e('collection-tbar-menu').style.display = 'inline-block';
            setTags(null);
            document.getElementById('upcoming-items').style.setProperty('display', 'none');
            document.getElementById('items-header').innerHTML = 'Search results for: ' + t;
            document.getElementById('upcoming-items-header').style.setProperty('display', 'none');
            e('recent-items').innerHTML = createItems(res['results']);
            e('collection-close').style.display = 'inline-block';
            console.log(res);

            var collections_parent = e('collections-grid');

            var nodes = collections_parent.children.length;

            [...collections_parent.children].forEach(function (element, index) {
                console.log(element); console.log(index);
                if (index < nodes - 1) {
                    element.classList.remove('cat-box-active');
                }
            });
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}


function viewCat(i, name) {

    var requestObj = { session_id: session, multiple: true, collection_id: i, order: 0};

    $.ajax({
        type: "POST",
        url: "api/item/getitem",
        contentType: "application/json/",
        data: JSON.stringify(requestObj),
        success: function (res) {
            homepage_state.collection_open = i;
            homepage_state.collection_name = name;

            //Collection menu open
            e('collection-tbar-menu').style.display = 'inline-block';
            setTags(res['tags']);
            document.getElementById('upcoming-items').style.setProperty('display', 'none');
            document.getElementById('items-header').innerHTML = name;
            document.getElementById('upcoming-items-header').style.setProperty('display', 'none');
            e('recent-items').innerHTML = createItems(res['results']);
            e('collection-close').style.display = 'inline-block';
            console.log(res);

            var collections_parent = e('collections-grid');

            var nodes = collections_parent.children.length;

            [...collections_parent.children].forEach(function (element, index) {
                console.log(element); console.log(index);
                if (index < nodes - 1) {
                    if (element.id == 'category-' + i)
                        element.classList.add('cat-box-active');
                    else element.classList.remove('cat-box-active');
                }
            });
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function getHomepage() {
    $.ajax({
        type: "POST",
        url: "api/homepage/gethomepage/" + session,
        contentType: "application/json/",
        success: function (res) {
            console.log(res);
            e('items-header').innerHTML = 'Recent Items';
            e('collection-close').style.display = 'none';
            e('collection-tbar-menu').style.display = 'none';
            e('logged-in-as').innerHTML = 'Logged in as <span style="color: black;">' + res['username'] + '</span>';
            document.getElementById('collections-grid').innerHTML = createCollections(res['collections_list']);
            document.getElementById('recent-items').innerHTML = createItems(res['recents_list']);
            //document.getElementById('upcoming-items').innerHTML = createItems(res['upcoming_list']);
            setTags(res['tags']);

            homepageCache = res;
            e('nav-btm').style.display = 'block';
            $("#cover").fadeOut(200);
        },
        error: function (xhr, status, error) {
            if (status == '403') loginScreen();
        }
    });
}

function viewPage(x) {
    var hpg = e('homepage-container');
    var cal = e('calendar-container');
    var ibx = e('inbox-container');

    var hpg_btn = e('home-btn');
    var cal_btn = e('calendar-btn');
    var ibx_btn = e('inbox-btn');

    switch (x) {
        case 1:
            hpg.style.display = 'block';
            cal.style.display = 'none';
            ibx.style.display = 'none';
            hpg_btn.classList.add('active');
            cal_btn.classList.remove('active');
            ibx_btn.classList.remove('active');

            break;
        case 2:
            hpg.style.display = 'none';
            cal.style.display = 'block';
            ibx.style.display = 'none';
            hpg_btn.classList.remove('active');
            cal_btn.classList.add('active');
            ibx_btn.classList.remove('active');

            break;
        case 3:
            hpg.style.display = 'none';
            cal.style.display = 'none';
            ibx.style.display = 'block';
            hpg_btn.classList.remove('active');
            cal_btn.classList.remove('active');
            ibx_btn.classList.add('active');

            break;
    }
}

function addCat() {
    var box = e('newCat');

    box.style.display = 'block';
}

function uncompleteItem(id) {
    var itemAction = {
        session_id: session,
        uncomplete: true,
        item_id: id
    };

    $.ajax({
        type: "POST",
        url: "api/item/itemaction",
        contentType: "application/json/",
        data: JSON.stringify(itemAction),
        success: function (res) {
            if (res['ok']) {
                console.log('completed');
                e('item-' + id).classList.remove('item-completed');
                e('complete-btn-' + id).innerHTML = 'done';
                e('complete-btn-' + id).style.color = '#898989';
                e('complete-btn-' + id).setAttribute('onclick', `completeItem(${id})`);
            }
        },
        error: function (xhr, status, error) {
            console.log('error');
        }
    });
}

function completeItem(id) {
    var itemAction = {
        session_id: session,
        complete: true,
        item_id: id
    };

    $.ajax({
        type: "POST",
        url: "api/item/itemaction",
        contentType: "application/json/",
        data: JSON.stringify(itemAction),
        success: function (res) {
            if (res['ok']) {
                console.log('completed');
                e('item-' + id).classList.add('item-completed');
                e('complete-btn-' + id).innerHTML = 'undo';
                e('complete-btn-' + id).style.color = '#bfbfbf';
                e('complete-btn-' + id).setAttribute('onclick', `uncompleteItem(${id})`);
            }
        },
        error: function (xhr, status, error) {
            console.log('error');
        }
    });
}

function undeleteItem(id) {
    var itemAction = {
        session_id: session,
        undelete: true,
        item_id: id
    };

    $.ajax({
        type: "POST",
        url: "api/item/itemaction",
        contentType: "application/json/",
        data: JSON.stringify(itemAction),
        success: function (res) {
            if (res['ok']) {
                console.log('undeleted');
                e('delete-btn-' + id).innerHTML = 'delete';
                e('delete-btn-' + id).style.color = '#898989';
                e('delete-btn-' + id).setAttribute('onclick', `deleteItem(${id})`);
            }
        },
        error: function (xhr, status, error) {
            console.log('error');
        }
    });
}

function deleteItem(id) {
    var itemAction = {
        session_id: session,
        delete: true,
        complete: false,
        item_id: id
    };

    $.ajax({
        type: "POST",
        url: "api/item/itemaction",
        contentType: "application/json/",
        data: JSON.stringify(itemAction),
        success: function (res) {
            if (res['ok']) {
                console.log('deleted');
                e('delete-btn-' + id).innerHTML = 'undo';
                e('delete-btn-' + id).style.color = '#bfbfbf';
                e('delete-btn-' + id).setAttribute('onclick', `undeleteItem(${id})`);
            }
        },
        error: function (xhr, status, error) {
            console.log('error');
        }
    });
}

function createCollection(t) {
    var name = t.trim();

    if (!validateSlip(name)) {
        return false;
    }
    else {
        var collectionCommit = {
            session_id: session,
            append: false,
            delete: false,
            name: name
        };

        $.ajax({
            type: "POST",
            url: "api/collections/commitcollection",
            contentType: "application/json/",
            data: JSON.stringify(collectionCommit),
            success: function (res) {
                console.log('added');

                e('newCat').style.display = 'none';
                e('new_coll').value = '';
                onload();
            },
            error: function (xhr, status, error) {
                console.log('error');
            }
        });
    }
}

function closeCollection() {
    getHomepage();
}