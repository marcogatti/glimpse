$(document).ready(function () {
    $("#registration-link").click(function () {
        $("#registration-view").modal("show");
    });
});

function registrationNext(data) {
    if (!data.success) {
        alert(data.message);
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

$(document).ready(function () {
    $("#registration-back-btn").click(function () {
        $('#first-screen').fadeIn().removeClass('hidden');
        $('#second-screen').fadeOut().addClass('hidden');
    });
});

function checkbox1Changed() {
    if ($("check1").checkbox == true) {
        ($("check2").checkbox = false);
        ($("check3").checkbox = false);
    }
}

function checkbox2Changed() {
    if ($("check2").checkbox == true) {
        ($("check1").checkbox = false);
        ($("check3").checkbox = false);
    }
}

function checkbox3Changed() {
    if ($("check3").checkbox == true) {
        ($("check1").checkbox = false);
        ($("check2").checkbox = false);
    }
}