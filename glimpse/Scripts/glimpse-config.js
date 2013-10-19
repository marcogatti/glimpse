var formActions = {
    changePassword: { validation: formChangePasswordValidation, url: 'edituserpassword' },
    manageMailAccounts: { validation: formManageMailAccountsValidation, url: 'edituseraccount' },
    editPersonalData: { validation: formAEditPersonalDataValidation, url: 'edituserpersonaldata' }
};

function initializeMainDropdownMenuActions() {

    $('#btn-config').click(function () {
        $('#config-view').modal();
    });

    $('#config-password, #config-mailaccount, #config-personaldata').click(function () {

        var modalBody = $('#config-view').find('.modal-body'),
            bodyId = $(this).data('body-id');

        modalBody.find('.nav-tabs').find('li').each(function () {
            $(this).removeClass('active')
        });

        $(this).addClass('active');

        modalBody.find('div.nav-body').addClass('hidden');
        modalBody.find('#' + bodyId).removeClass('hidden');
    });

    $('#config-password-form, #config-mailaccount-form, #config-personaldata-form').submit(function (event) {

        var sendData, url, isValid;

        event.preventDefault();

        sendData = getFormData($(this));
        url = formActions[$(this).data('action')]['url'];
        isValid = formActions[$(this).data('action')]['validation'];

        if (!isValid(sendData))
            return;

        $.ajax({
            type: "POST",
            url: "async/" + url,
            dataType: 'json',
            data: sendData,
            success: function (data, textStatus, jqXHR) {
                serverPostActions(sendData, data)
            }
        });
    });
}

function getFormData(form) {

    var data = [];

    form.find('input').each(function () {
        data[$(this).data('name')] = $(this).val();
    });

    return data;
}

function formChangePasswordValidation(data) {

    if (hasEmptyValues(data)) {
        alert("Todos los campos son obligatorios.");
        return false;
    }
    return true;
}

function formManageMailAccountsValidation(data) {
    return true;
}

function formAEditPersonalDataValidation(data) {
    return true;
}

function hasEmptyValues(array) {

    for (var index in array) {
        if (array[index] === "") {
            return true;
        }
    }
    return false;
}

function serverPostActions(sentData, receivedData) {

    if (receivedData.success != true) {
        alert(receivedData.message);
    }
}