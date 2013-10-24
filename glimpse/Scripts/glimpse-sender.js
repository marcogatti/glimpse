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
        alert(data.message);
    }
}

function mailSendingConnectionFailed(jqXHR, textStatus, errorThrown) {
    alert("Actualmente tenemos problemas para enviar el email, por favor inténtelo de nuevo más tarde");
}

function sendEmailAsync(fromAccountId, toAddress, subject, body, circularProgress) {

    var sendInfo = {
        ToAddress: toAddress,
        Subject: subject,
        Body: body,
        mailAccountId: fromAccountId
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
        complete: function () {
            stopWorkingWidget(circularProgress);
        },
        data: sendInfo
    });
}

function prepareComposeDialog() {

    var compose_panel = $("#compose_pannel"),
        circularProgress = compose_panel.find('.progress-circular'),
        composePanelTitle;;

    compose_panel.dialog({
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
                startWorkingWidget(circularProgress);
                sendEmailAsync($('#email-from').html(),
                               $("#email-to").val(),
                               $("#email-subject").val(),
                               editor.getData(),
                               circularProgress);
            }
        }
        ]
    });

    composePanelTitle = $('.ui-dialog[aria-describedby="compose_pannel"]').find('.ui-dialog-title');
    circularProgress.remove();
    composePanelTitle.append(circularProgress);

    $("#compose").on("click", function () {
        var compose_panel = $("#compose_pannel"),
            mainMailAccountId = user_mailAccounts[getMainAccount(user_mailAccounts)].mailAccountId;

        compose_panel.find('#email-from').html(mainMailAccountId);

        displayComposeDialog();

    });
}


function initializeMailEditor() {
    editor = CKEDITOR.replace('text_editor');

    //$("#email-to").autocomplete('async/getuseddirections',
    //    {
    //        dataType: 'json',
    //        parse: function (data) {
    //            var rows = new Array();
    //            for (var i = 0; i < data.length; i++) {
    //                rows[i] = data[i];
    //            }
    //            return rows;
    //        },
    //        formatItem: function (row, i, max) {
    //            return row.Tag;
    //        },
    //        width: 300,
    //        highlight: false,
    //        multiple: true,
    //        multipleSeparator: ",",
    //        autofocus: "true",
    //        delay: 300,
    //        minLength: 2
    //    });
}

function displayComposeDialog() {
    $("#compose_pannel").dialog("open");
}

function setMailEditorText(text) {
    editor.setData(text);
}
