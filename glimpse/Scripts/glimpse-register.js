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