function resetComposeDialog() {
    $("#compose_pannel").dialog("close");
    editor.setData("");
    $("#email-to").val("");
    $("#email-from").val("");
    $("#attachments-ids").val("");
    $("#email-subject").val("");
    $("#compose_pannel").find('input[type="file"]').val('');
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

function sendEmailAsync(fromAccountId, toAddress, subject, body, attachmentIds, compose_panel) {

    var sendInfo = {
        ToAddress: toAddress,
        Subject: subject,
        Body: body,
        mailAccountId: fromAccountId,
        AttachmentsIds: attachmentIds
    };

    $.ajax({
        type: "POST",
        url: "async/sendEmail",
        dataType: 'json',
        traditional: true,
        success: function (data, textStatus, jqXHR) {
            mailSendingConnectionOK(data, textStatus, jqXHR)
        },
        error: function (jqXHR, textStatus, errorThrown) {
            mailSendingConnectionFailed(jqXHR, textStatus, errorThrown)
        },
        complete: function () {
            stopWorkingWidget(compose_panel.parent());
        },
        data: sendInfo
    });
}

function prepareToUploadAttachedFiles(compose_panel) {

    compose_panel.find('.upload-file').click(function () {
        var formData = new FormData($('#upload-file-form')[0]);
        $.ajax({
            url: 'Async/UploadFile',  //Server script to process data
            type: 'POST',
            xhr: function () {  // Custom XMLHttpRequest
                var myXhr = $.ajaxSettings.xhr();
                return myXhr;
            },
            success: function (data, textStatus, jqXHR) {
                if (data.success)
                    saveAttachmentId(data.id);
                else
                    alert(data.message);
            },
            error: function () {
                alert("Tuvimos problemas para adjuntar el archivo. Por favor intentalo de nuevo más tarde.")
                /* Remover adjunto del form*/
            },
            beforeSend: function () {
                $(".ui-dialog-buttonpane button:contains('Enviar')").button('disable');
                startWorkingWidget(compose_panel.parent());
            },
            complete: function () {
                $(".ui-dialog-buttonpane button:contains('Enviar')").button('enable');
                stopWorkingWidget(compose_panel.parent());
            },
            // Form data
            data: formData,
            cache: false,
            contentType: false,
            processData: false
        });
    });

}

function saveAttachmentId(id) {

    var currentIds = getAttachmentIds();

    currentIds.push(id);

    saveAttachmentIds(currentIds);
}

function saveAttachmentIds(ids) {
    $('#attachments-ids').data('ids', ids);
}

function getAttachmentIds() {

    var currentIds = $('#attachments-ids').data('ids').toString(),
        currentIdsArray;

    currentIdsArray = currentIds.split(',');
    if (currentIdsArray.length === 1 && currentIdsArray[0] === "") {
        currentIdsArray = [];
    }

    return currentIdsArray;
}

function prepareComposeDialog() {

    var compose_panel = $("#compose_pannel"),
        circularProgress = compose_panel.find('.progress-circular'),
        composePanelTitle;

    prepareToUploadAttachedFiles(compose_panel);

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
                startWorkingWidget(compose_panel.parent());
                sendEmailAsync($('#email-from').html(),
                               $("#email-to").val(),
                               $("#email-subject").val(),
                               editor.getData(),
                               getAttachmentIds(),
                               compose_panel);
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

