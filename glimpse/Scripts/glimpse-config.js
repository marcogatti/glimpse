var formActions = {
    changePassword: { validation: formChangePasswordValidation, url: 'edituserpassword' },
    manageMailAccounts: { validation: formManageMailAccountsValidation, url: 'edituseraccount' },
    editPersonalData: { validation: formAEditPersonalDataValidation, url: 'edituserpersonaldata' }
};

function initializeMainDropdownMenuActions() {

    $('#btn-config').click(function () {
        openConfigModal();
    });

    $('#config-view').on('hidden', function () {
        cleanConfigErrors();
    })

    $('#config-password, #config-mailaccount, #config-personaldata').click(function () {

        var modalBody = $('#config-view').find('.modal-body'),
            bodyId = $(this).data('body-id');

        cleanConfigErrors();
        cleanAllFormsData(modalBody);

        modalBody.find('.nav-tabs').find('li').each(function () {
            $(this).removeClass('active')
        });

        $(this).addClass('active');

        modalBody.find('div.nav-body').addClass('hidden');
        modalBody.find('#' + bodyId).removeClass('hidden');
    });

    $('#config-password-form, #config-mailaccount-form, #config-personaldata-form').submit(function (event) {

        var sendData, url, isValid, form;

        event.preventDefault();

        form = $(this);
        sendData = getFormData($(this));
        url = formActions[$(this).data('action')]['url'];
        isValid = formActions[$(this).data('action')]['validation'];

        if (!isValid(sendData))
            return;

        $.ajax({
            type: "POST",
            url: "account/" + url,
            dataType: 'json',
            data: sendData,
            success: function (data, textStatus, jqXHR) {
                serverPostActions(form, sendData, data)
            }
        });
    });
}

function getFormData(form) {

    var data = {};

    form.find('input').each(function () {
        data[$(this).data('name')] = $(this).val();
    });

    return data;
}

function formChangePasswordValidation(data) {

    cleanConfigErrors();

    if (hasEmptyValues(data)) {
        showConfigErrors("Todos los campos son obligatorios.");
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

function serverPostActions(form, sentData, receivedData) {

    cleanConfigErrors();

    if (!receivedData.success) {
        showConfigErrors(receivedData.message);
        return;
    }

    $('#config-errors-cont').addClass('hidden');
    alert("Datos actualizados correctamente.");

    closeConfigModal();

    cleanFormData(form);
}

function closeConfigModal() {
    $('#config-view').modal('hide');
}

function openConfigModal() {
    cleanConfigErrors();
    $('#config-view').modal();
}

function showConfigErrors(message) {

    var errorList = $('#config-errors-list');

    $('#config-errors-cont').removeClass('hidden');
    $("<li />").html(message).appendTo(errorList);
}

function cleanConfigErrors() {
    $('#config-errors-list').html('');
}

function cleanFormData(form) {
    setFormData(form, null);
}

function cleanAllFormsData(modalBody) {

    modalBody.find('form').each(function () {
        cleanFormData($(this));
    });
}

function setFormData(form, data) {

    if (data == null) {
        form.find('input').each(function () {
            $(this).val('');
        });
    }
}