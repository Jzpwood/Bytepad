function setCookie(cname, cvalue, exdays) {
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

function loginScreen() {
    window.location.replace("/Index");
}

function itemType(id) {
    switch (id) {
        case 0: return 'Note';
        case 1: return 'Todo';
        case 2: return 'Event';
        default: return '';
    }
}

function warningDialog(message) {
    e('modal-warning-text').innerHTML = message;
    e('warningDialog').style.display = 'block';
}