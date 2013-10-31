var user_personal_fields

$(document).ready(function () {

    var registrationModal = $('#register-view');

    $("#registration-link").click(function () {
        $("#register-view").modal("show");
    });

    $("#register-user-form").submit(function (e) {

        e.preventDefault();

        var dataToSend = getFormData($(this));

        startWorkingWidget(registrationModal);

        $.ajax({
            type: "POST",
            url: "validateuserfields",
            dataType: 'json',
            data: dataToSend,
            success: function (data, textStatus, jqXHR) {
                if (data.success) {
                    user_personal_fields = dataToSend;
                    switchForm($("#register-user"),
                               $("#register-user-div"),
                               $("#register-mailaccounts"),
                               $("#register-mails-div"),
                               "Integrá tus cuentas");
                } else {
                    showError(data.message);
                }
            },
            complete: function () { stopWorkingWidget(registrationModal); }
        });
    });

    $("#register-mails-form").submit(function (e) {

        e.preventDefault();

        var dataToSend = getFormData($(this)); //mailaccounts
        dataToSend.firstname = user_personal_fields.firstname;
        dataToSend.lastname = user_personal_fields.lastname;
        dataToSend.username = user_personal_fields.username;
        dataToSend.userpassword = user_personal_fields.userpassword;
        dataToSend.userconfirmationpassword = user_personal_fields.userconfirmationpassword;

        startWorkingWidget(registrationModal);

        $.ajax({
            type: "POST",
            url: "createuser",
            dataType: 'json',
            data: dataToSend,
            success: function (data, textStatus, jqXHR) {
                if (data.success) {
                    $('#config-errors-cont').addClass('hidden');
                    $('#config-errors-list').html("");
                    window.location.assign(data.url);
                } else {
                    showError(data.message);
                }
            },
            complete: function () { stopWorkingWidget(registrationModal); }
        });
    });

    $("#config_mailaccount-goback").click(function () {
        switchForm($("#register-mailaccounts"),
                   $("#register-mails-div"),
                   $("#register-user"),
                   $("#register-user-div"),
                   "Ingresá tus datos");
    });

    $("#forgot-password").click(function (e) {
        $.ajax({
            type: "POST",
            url: "resetpassword",
            dataType: 'json',
            data: { username: $('#username').val() },
            success: function (data, textStatus, jqXHR) {
                if (data.success) {
                    alert("Se ha enviado un email con la nueva contraseña a su cuenta principal de mail.");
                } else {
                    alert(data.message);
                }
            }
        });
    });
});

function switchForm(oldForm, oldContent, newForm, newContent, titleMessage) {

    $('#registration-title').val(titleMessage);
    $('#config-errors-cont').addClass('hidden');
    $('#config-errors-list').html("");

    oldForm.removeClass("active");
    oldContent.addClass("hidden");
    newForm.addClass("active");
    newContent.removeClass("hidden");
}

function showError(errorMessage) {
    $('#config-errors-cont').removeClass('hidden');
    var errorList = $('#config-errors-list')
    errorList.html("");
    $("<li />").html(errorMessage).appendTo(errorList);
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

//function checkbox1Changed() {
//    if ($("#check1").is(':checked')) {
//        ($("#check2").removeAttr('checked'));
//        ($("#check3").removeAttr('checked'));
//    }
//}
