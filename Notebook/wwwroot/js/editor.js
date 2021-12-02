var editor_state = {
    tag_open: false,
    share_open: false,
    not_owned: false,
    locked_out: false
}

function refresh() {
    //If in new item mode, no need to check lock
    if (editor_mode == 1) {
        return;
    }

    //Otherwise

    //Update state
    var itemcommit = { session_id: session, item_id: editor_id };

    $.ajax({
        type: "POST",
        url: "api/lock/request",
        contentType: "application/json/",
        data: JSON.stringify(itemcommit),
        success: function (res) {
            console.log('Access granted: ' + res);
            if (!res) {
                editor_state.locked_out = true;
                editItem(editor_id);
            }
            else {
                if (editor_state.locked_out == true) {
                    editItem(editor_id);
                    editor_state.locked_out = false;
                }
                else {
                    submitAppend(false);
                }
            }
        },
        error: function (xhr, status, error) {

        }
    });
}

function addEditorTags(item) {
    if (validateSlip(item)) {
        var item_id = id_tally["editor-tags"];
        id_tally["editor-tags"]++;

        var element_add = document.getElementById('editor-tag-add');
        var tag_editor = document.getElementById('editor-tags');

        var tag = `<div class="chip" id="editor-tag-${item_id}">
                     ${item.toLowerCase()}
                     <i class="clearable material-icons" onclick="removeSlip('editor-tag-${item_id}')">close</i>
                  </div>`;

        tag_editor.insertBefore(htmlToElement(tag), element_add);

        document.getElementById('editor-tag-new').value = '';
    }
}

function resetEditorShares() {
    document.getElementById('editor-shares').innerHTML =
        `<div class="chip editor-tag-prompt">
        <i class="clearable material-icons" > perm_identity</i>
            Shared with
                </div>
        <div class="chip editor-tag-add" id="editor-shares-add">
            <input id="editor-shares-new" class="editor-tag-input" />
            <i style="font-size:15px;" class="clearable material-icons" onclick="addEditorShares(document.getElementById('editor-shares-new').value)">add</i>
        </div>`;
}

function resetEditorTags() {
    document.getElementById('editor-tags').innerHTML =
        `<div class="chip editor-tag-prompt">
         <i class="clearable material-icons">bookmark</i>
         Item Tags
         </div>
                <div class="chip editor-tag-add" id="editor-tag-add">
                    <input id="editor-tag-new" class="editor-tag-input" />
                    <i style="font-size:15px;" class="clearable material-icons" onclick="addEditorTags(document.getElementById('editor-tag-new').value)">add</i>
                </div>`;
}

function addEditorShares(item) {
    if (validateSlip(item)) {
        validateUsername(item);
    }
}

function insertEditorShares(item) {
    var item_id = id_tally["editor-shares"];
    id_tally["editor-shares"]++;

    var element_add = document.getElementById('editor-shares-add');
    var tag_editor = document.getElementById('editor-shares');

    var tag = `<div class="chip" id="editor-shares-${item_id}">
                     ${item.toLowerCase()}
                     <i class="clearable material-icons" onclick="removeSlip('editor-shares-${item_id}')">close</i>
                  </div>`;

    tag_editor.insertBefore(htmlToElement(tag), element_add);

    document.getElementById('editor-shares-new').value = '';
}

function validateSlip(item) {
    if (item == '') return false;
    if (item.length > 20) return false;
    return item.match('^[A-Za-z0-9 ]+$');
}

function removeSlip(element) {
    var e = document.getElementById(element);
    e.parentNode.removeChild(e);
}

function startEditor() {
    if (page_mode == 0) {
        e('tab-navigator').style.display = 'none';
        e('content-container').style.display = 'none';
        e('item-editor').style.display = 'block';
    }
}

function returnEditor() {
    if (page_mode == 0) {
        e('item-editor').style.display = 'none';
        e('content-container').style.display = 'inherit';
        //e('tab-navigator').style.display = 'inherit';
    }
}

function createNew() {
    editor_mode = 1;
    clearAll();

    editor_state.not_owned = false;

    e('editor-username').innerHTML = homepageCache['username'];

    var categoryselect = "";

    var selected_cat;

    homepageCache['collections_list'].forEach(function (element, index) {
        if (index == 0) selected_cat = element['id'];
        categoryselect += `<option value="${element['id']}">${element['name']}</option>`;
    });

    document.getElementById('category-select').innerHTML = categoryselect;
    $('#category-select').val(selected_cat);
    $('#category-select').formSelect();

    startEditor();
}

function tagsToArray() {

    //Fix

    var tagsparent = e('editor-tags');

    if (tagsparent.children.length == 2) {
        return null;
    }
    else {
        var tags = new Array(1);

        for (var i = 1; i < tagsparent.children.length - 1; i++) {
            tags.push(tagsparent.children[i].innerHTML.split('<')[0].trim());
        }

        return tags;
    }
}

function sharesToArray() {

    var sharesparent = e('editor-shares');

    if (sharesparent.children.length == 2) {
        return null;
    }
    else {
        var shares = new Array(0);

        for (var i = 1; i < sharesparent.children.length - 1; i++) {
            shares.push(sharesparent.children[i].innerHTML.split('<')[0].trim());
        }

        return shares;
    }
}

function tooltip(id) {
    ttip = e(id);

    ttip.style.visibility = "visible";
    ttip.style.opacity = 1;

    setTimeout(function () { tooltip_hide(id) }, 3000);
}

function tooltip_hide(id) {
    ttip = e(id);

    ttip.style.opacity = 0;
    ttip.style.visibility = "hidden";
}

function editorSubmit() {

    if (e('editor-title').value.trim() == '') {
        tooltip('ttp-title');
        return;
    }

    if (editor_mode == 0) {
        submitAppend(true);
        editor_mode = 1;
    }
    else if (editor_mode == 1) {
        submitNew();
    }
}

function submitAppend(close) {
    var i = {
        tags: tagsToArray(),
        shared_users: sharesToArray(),
        id: editor_id,
        type: parseInt(e('type-select').value),
        collection_id: parseInt(e('category-select').value),
        title: e('editor-title').value,
        content: JSON.stringify(quill.getContents()),
        due_date: e('event-datetime').value,
        recurrence_type: parseInt(e('reccuring-select').value),
       //remind: e('remind-select').checked == true ? 1 : 0,
        remind_before_hrs: parseInt(e('reminder-type').value),
        completed: 0
    };

    var itemcommit = {
        session_id: session,
        append: true,
        item: i,
        close: close,
    };

    console.log(itemcommit);

    $.ajax({
        type: "POST",
        url: "api/item/putitem",
        contentType: "application/json/",
        data: JSON.stringify(itemcommit),
        success: function (res) {
            if (res['ok']) {
                if (close) {
                    clearAll();
                    onload();
                    returnEditor();
                }
            }
            else {
                warningDialog(res['error']);
            }
        },
        error: function (xhr, status, error) {
            console.log('Update failed with status ' + status)
        }
    });
}

function submitNew() {
    var i = {
        tags: tagsToArray(),
        shared_users: sharesToArray(),
        type: parseInt(e('type-select').value),
        collection_id: parseInt(e('category-select').value),
        title: e('editor-title').value,
        content: JSON.stringify(quill.getContents()),
        due_date: e('event-datetime').value,
        recurrence_type: parseInt(e('reccuring-select').value),
        //remind: e('remind-select').checked == true ? 1 : 0,
        remind_before_hrs: parseInt(e('reminder-type').value),
        completed: 0
    };

    var itemcommit = {
        session_id: session,
        append: false,
        item: i
    };

    console.log(itemcommit);

    $.ajax({
        type: "POST",
        url: "api/item/putitem",
        contentType: "application/json/",
        data: JSON.stringify(itemcommit),
        success: function (res) {
            if (res['ok']) {
                clearAll();
                onload();
                returnEditor();
            }
            else {
                warningDialog(res['error']);
            }
        },
        error: function (xhr, status, error) {
            console.log('Update failed with status ' + status)
        }
    });
}

function clearAll() {
    resetEditorShares();
    resetEditorTags();

    //document.getElementById('category-select').innerHTML = categoryselect;

    document.getElementById('editor-username').innerHTML = 'usr';
    document.getElementById('editor-created').innerHTML = '';
    $('#type-select').val(0);
    $('#type-select').formSelect();
    $('#category-select').val(0);
    $('#category-select').formSelect();
    $('#reccuring-select').val(0);
    $('#reccuring-select').formSelect();
    document.getElementById('editor-modified').innerHTML = '';
    document.getElementById('editor-title').value = '';
    $('#reminder-type').val(0);
    $('#reminder-type').formSelect();
    //if ($('#remind-select').prop('checked') != false) {
    //    $('#remind-select').click();
    //}
    document.getElementById('event-datetime').value = '';

    editor_state.locked_out = false;
    e('editor-lock').style.display = 'none';
    e('lock-cover').style.display = 'none';

    quill.setContents('');
    M.updateTextFields();
}

function selectTags(str) {
    console.log('Selected tag: ' + str);
    //Needs completing
}

function typeChanged(i) {
    e('event-options').style.display = (i == 2 ? 'table-row' : 'none');
}

function setSharesOpen(b) {
    editor_state.share_open = b;
    var s = e('editor-shares');

    if (b) {
        s.style.display = 'block';
    } else {
        s.style.display = 'none';
    }
}

function toggleShares() {
    if (editor_state.not_owned) {
        tooltip('ttp-sharing-editor');
        return;
    }

    editor_state.share_open = !editor_state.share_open;

    var s = e('editor-shares');

    if (editor_state.share_open) {
        s.style.display = 'block';
    }
    else {
        s.style.display = 'none';
    }
}

function toggleTags() {
    editor_state.tag_open = !editor_state.tag_open;

    var s = e('editor-tags');

    if (editor_state.tag_open) {
        s.style.display = 'block';
    }
    else {
        s.style.display = 'none';
    }
}

function validateUsername(u) {
    var itemcommit = {
        session_id: session,
        username: u
    };

    console.log(itemcommit);

    $.ajax({
        type: "POST",
        url: "api/share/validate",
        contentType: "application/json/",
        data: JSON.stringify(itemcommit),
        success: function (res) {
            if (res['ok'] == true) {
                insertEditorShares(u);
            }
            else {
                warningDialog(`The user ${u} does not exist.`);
            }
        },
        error: function (xhr, status, error) {

        }
    });
}