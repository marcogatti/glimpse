$(document).ready(function () {
    $("#registration-link").click(function () {
        $("#registration-view").modal("show");
    });

    $("#registration-next-btn").click(function updateModel() {
        var fnvalue = $("#firstname").val();
        $("#createFirstname").val(fnvalue);

        var lnvalue = $("#lastname").val();
        $("#createLastname").val(lnvalue);

        var unvalue = $("#username").val();
        $("#createUsername").val(unvalue);

        var pvalue = $("#password").val();
        $("#createPassword").val(pvalue);

        var cpvalue = $("#confirmation").val();
        $("#createConfirmation").val(cpvalue);
    });

    $("#registration-back-btn").click(function () {
        $('#first-screen').fadeIn().removeClass('hidden');
        $('#second-screen').fadeOut().addClass('hidden');
    });
});

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