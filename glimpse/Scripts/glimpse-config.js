var formActions = {
    changePassword: {
        validation: formChangePasswordValidation,
        url: 'edituserpassword',
        cleanup: formChangePasswordCleanup,
        preload: preloadPasswordForm
    },
    manageMailAccounts: {
        validation: formManageMailAccountsValidation,
        url: 'edituseraccounts',
        cleanup: formManageMailAccountsCleanup,
        preload: preloadMailAccountsForm
    },
    editPersonalData: {
        validation: formEditPersonalDataValidation,
        url: 'edituserpersonaldata',
        cleanup: formEditPersonalDataCleanup,
        preload: preloadPersonalDataForm
    }
};

function initializeMainDropdownMenuActions() {

    if (user_isGlimpseUser) {
        $('#btn-config').click(function () {
            openConfigModal();
        });
    }

    $('#config-view').on('hidden', function () {
        cleanConfigErrors();
        cleanAllFormsData($(this));
    })

    $('#config-password, #config-mailaccount, #config-personaldata').click(function () {

        var modalBody = $('#config-view').find('.modal-body'),
            bodyId = $(this).data('body-id'),
            form = $('#' + $(this).data('body-id')).find('form'),
            preloadFormAction = formActions[form.data('action')]['preload'];

        cleanConfigErrors();
        preloadFormAction(form);

        modalBody.find('.nav-tabs').find('li').each(function () {
            $(this).removeClass('active')
        });

        $(this).addClass('active');

        modalBody.find('div.nav-body').addClass('hidden');
        modalBody.find('#' + bodyId).removeClass('hidden');
    });

    $('#config-password-form, #config-mailaccount-form, #config-personaldata-form').submit(function (event) {

        var sendData, url, isValid, form, modal;

        event.preventDefault();

        form = $(this);
        sendData = getFormData($(this));
        url = formActions[$(this).data('action')]['url'];
        isValid = formActions[$(this).data('action')]['validation'];

        if (!isValid(sendData))
            return;

        modal = $('#config-view');

        startWorkingWidget(modal);

        $.ajax({
            type: "POST",
            url: "account/" + url,
            dataType: 'json',
            data: sendData,
            success: function (data, textStatus, jqXHR) {
                serverPostActions(form, sendData, data)
            },
            complete: function () {
                stopWorkingWidget(modal);
            }
        });
    });
}

function serverPostActions(form, sentData, receivedData) {

    var cleanupFormAction;

    cleanConfigErrors();

    if (!receivedData.success) {
        showConfigErrors(receivedData.message);
        return;
    }

    $('#config-errors-cont').addClass('hidden');
    alert("Datos actualizados correctamente.");

    closeConfigModal();

    cleanupFormAction = formActions[form.data('action')]['cleanup'];
    cleanupFormAction(form, receivedData);
}

function closeConfigModal() {
    $('#config-view').modal('hide');
}

function openConfigModal() {

    preloadMailAccountsForm($('#config-mailaccount-form'));
    preloadPersonalDataForm($('#config-personaldata-form'));
    preloadPasswordForm($('#config-password-form'));

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

function cleanAllFormsData(modalBody) {

    modalBody.find('form').each(function () {
        cleanFormData($(this));
    });
}

function getFormData(form) {

    var formField,
        data = {};

    form.find('input').each(function () {

        formField = $(this);

        if (formField.attr('type') == 'checkbox') {
            if (formField.prop('checked') == 'checked' || formField.prop('checked') == true)
                data[formField.data('name')] = true;
            else
                data[formField.data('name')] = false;
        } else {
            data[formField.data('name')] = formField.val();
        }
    });

    return data;
}

function setFormData(form, data) {

    var formField;

    if (data == null) {
        form.find('input').each(function () {
            $(this).val('');
        });
    } else {
        for (var index in data) {

            formField = form.find('[data-name="' + index + '"]');

            if (formField.attr('type') == 'checkbox') {

                if (data[index] == true) {
                    formField.attr('checked', true);
                    formField.prop('checked', true);
                } else {
                    formField.attr('checked', false);
                    formField.prop('checked', false);
                }
            } else {
                formField.val(data[index]);
            }
        }
    }
}

function cleanFormData(form) {
    setFormData(form, null);
}

function hasEmptyValues(array) {

    for (var index in array) {
        if (array[index] === "") {
            return true;
        }
    }
    return false;
}


////////////////////////////////////////
//          PreloadActions            //
////////////////////////////////////////

function preloadMailAccountsForm(form) {

    var loopPass = 1,
        formData = {};

    for (var index in user_mailAccounts) {

        formData['mailAccount' + loopPass] = user_mailAccounts[index].address;
        formData['isMainAccount' + loopPass] = user_mailAccounts[index].mainAccount;

        loopPass++;
    }

    setFormData(form, formData);
}

function preloadPersonalDataForm(form) {
    setFormData(form, user_personal_data);
}

function preloadPasswordForm(form) {
    cleanFormData(form);
}


////////////////////////////////////////
//          CleanupActions            //
////////////////////////////////////////

function formChangePasswordCleanup(form, serverData) {
    cleanFormData(form);
}

function formEditPersonalDataCleanup(form, serverData) {
    user_personal_data = getFormData(form);
}

function formManageMailAccountsCleanup(form, serverData) {
    window.location.assign(serverData.url);
}


////////////////////////////////////////
//        ValidationActions           //
////////////////////////////////////////

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

function formEditPersonalDataValidation(data) {

    if (hasEmptyValues({ firstname: data.firstName, lastname: data.lastName })) {
        showConfigErrors("Los campos Nombre y Apellido son obligatorios.");
        return false;
    }

    return true;
}