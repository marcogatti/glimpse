function resetComposeDialog() {
    $("#compose_pannel").dialog("close");
    editor.setData("");
    $("#email-to").val("");
    $("#email-subject").val("");
}

function mailSendingConnectionOK(data, textStatus, jqXHR) {

    if (data.success == true) {
        alert('Mail enviado correctamente a ' + data.address + '.');
        resetComposeDialog();
    } else {
        alert('Falló el envío del mail a "' + data.address + '". Verifica la dirección de correo.');
    }
}

function mailSendingConnectionFailed(jqXHR, textStatus, errorThrown) {
    alert("Falló el envío del mail, por favor intentelo nuevamente más tarde.");
}

function sendEmailAsync(toAddres, subject, body) {

    var sendInfo = {
        ToAddress: toAddres,
        Subject: subject,
        Body: body
    };

    $.ajax({
        type: "POST",
        url: "async/sendEmail",
        dataType: 'json',
        success: function (data, textStatus, jqXHR) {
            mailSendingConnectionOK(data, textStatus, jqXHR)
        },
        error: function (jqXHR, textStatus, errorThrown) {
            mailSendingConnectionFailed(jqXHR, textStatus, errorThrown)
        },
        data: sendInfo
    });
}

function prepareComposeDialog() {

    $("#compose_pannel").dialog({
        autoOpen: false,
        closeOnEscape: true,
        draggable: true,
        height: 500,
        width: 600,
        minWidth: 400,
        minHeight: 200,
        resizable: true,
        title: "Redacta un email",
        position: { my: "left botton", at: "left bottom", of: window },
        buttons: [
        {
            text: "Cerrar",
            click: function () {
                $(this).dialog("close");
            }
        },
        {
            text: "Enviar",
            click: function () {
                sendEmailAsync($("#email-to").val(), $("#email-subject").val(), editor.getData());
            }
        }
        ]
    });
    $("#compose").on("click", function () {
        $("#compose_pannel").dialog("open");      
    });
}


function initializeMailEditor() {
    editor = CKEDITOR.replace('text_editor');
}

function displayComposeDialog() {
    $("#compose_pannel").dialog("open");
}

function setMailEditorText(text) {
    editor.setData(text);
}
