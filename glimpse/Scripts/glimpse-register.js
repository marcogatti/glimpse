$(document).ready(function () {
    $("#registration-link").click(function () {
        $("#register-view").modal("show");
    });

    $("#register-user-form").submit(function (e) {

        e.preventDefault();

        var data = {
            firstname: $('#register-name').val(),
            lastname: $('#register-lastname').val(),
            username: $('#register-username').val(),
            password: $('#register-pass').val(),
            confirmationPassword: $('#register-confpass').val(),
        }

        $.ajax({
            type: "POST",
            url: "validateuserfields",
            dataType: 'json',
            data: data,
            success: function (data, textStatus, jqXHR) {
                if (data.success) {
                    switchform();
                } else {
                    alert(data.message);
                }
            }
        });
    });

    $("#registration-back-btn").click(function () {
        $('#first-screen').fadeIn().removeClass('hidden');
        $('#second-screen').fadeOut().addClass('hidden');
    });

    $("#forgot-password").click(function (e) {
        $.ajax({
            type: "POST",
            url: "resetpassword",
            dataType: 'json',
            data: {username:$('#username').val()},
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

function switchform() {

    $("#register-mailaccounts").addClass("active");
    $("#register-user").removeClass("active");
    
    $('#registration-title').val("Integrá tus cuentas de correo");
    $("#register-user-div").addClass("hidden");
    $("#register-mails-div").removeClass("hidden");
}

function registrationNext(data) {
    if (!data.success) {
        $('#first-validations').html(data.message);
    }
    else {
        renderMailAccountScreen();
    }
}

function renderMailAccountScreen() {
    $('#first-screen').fadeOut().addClass('hidden');
    $('#second-screen').fadeIn().removeClass('hidden');
}

function registrationFailure() {
    alert("No se pudo realizar la registración. Intentelo de nuevo más tarde.");
}

function checkbox1Changed() {
    if ($("#check1").is(':checked')) {
        ($("#check2").removeAttr('checked'));
        ($("#check3").removeAttr('checked'));
    }
}

function checkbox2Changed() {
    if ($("#check2").is(':checked')) {
        ($("#check1").removeAttr('checked'));
        ($("#check3").removeAttr('checked'));
    }
}

function checkbox3Changed() {
    if ($("#check3").is(':checked')) {
        ($("#check1").removeAttr('checked'));
        ($("#check2").removeAttr('checked'));
    }
}