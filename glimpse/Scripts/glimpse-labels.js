var selectedLabel,
    labelToAddIsSet = false;

var label_trash = 'Trash',
    label_inbox = 'Inbox',
    label_all = 'All',
    label_spam = 'Junk',
    label_sent = 'Sent',
    label_important = 'Important',
    label_flagged = 'Flagged',
    label_draft = 'Drafts';

var unwantedSystemLabels = [label_important, label_flagged, label_draft],
    exclusive_labels = [label_trash, label_spam];


function populateLabelColors() {

    var i = 0;
    $(".custom-label:not(.custom-label[data-name^='others'])").each(function () {

        if ($(this).data("name") !== "others") {
            paintLabel($(this), $(this).data("color"));
            i++;
        }
    });

    setMailBoxSelection();
}

function paintLabel(labelElement, color) {
    labelElement.data("color", color);
    labelElement.css("background-color", color);
    labelColors[labelElement.data("name")] = color;
}

function labelDrag(ev) {
    //ev.dataTransfer.setData("Text", ev.target.id);
    ev.data.label.css('opacity', 0.4);
}

function setLabelsAdder(currentLabel) {

    if (currentLabel.hasClass("custom-label")) {

        if (currentLabel.data('name') === 'others') {
            currentLabel.mousedown(function (event) {
                event.preventDefault();
            });
            return;
        }

        currentLabel.attr("draggable", true);

        currentLabel.on('dragstart', { label: currentLabel }, function (ev) {

            ev.data.label.css('opacity', 0.4);
            ev.originalEvent.dataTransfer.setData("Text", ev.target.id);
            selectedLabel = ev.data.label.attr('data-name');
            labelToAddIsSet = true;
        }
        );

        currentLabel.on('dragend', { label: currentLabel }, function (ev) {
            ev.data.label.css('opacity', 1);
            labelToAddIsSet = false;
        }
        );

    } else {
        $(this).mousedown(function (downEvent) {
            downEvent.preventDefault();
        });
    }
}

function canReceiveThatLabel(label, circle) {

    var systemLabels = getSystemLabels(circle);

    if (hasExclusiveLabel(systemLabels))
        return false;
    else
        return !hasLabel(circle, label);
}

function prepareToReceiveLabels(circle) {

    $(circle).on('dragover', function (ev) {
        if (selectedLabel === 'others') return;
        if (labelToAddIsSet && canReceiveThatLabel(selectedLabel, $(this))) {
            ev.preventDefault();
            ev.stopPropagation();
        }
    }
    );

    $(circle).on('drop', function (ev) {
        if (labelToAddIsSet) {
            if (isClicked(this) == 'true') {
                $('[mail-clicked|=true]').each(function () {
                    addLabelToEmail(selectedLabel, $(this));
                }
                );
            } else {
                addLabelToEmail(selectedLabel, $(this));
            }
        }
    }
);
}

function addLabelToCircleData(circle, label) {
    var newCustomLabels = getCustomLabels(circle);
    newCustomLabels.push(label);
    circle.data("custom-labels", newCustomLabels);
}

function addCircleColor(circle, label) {

    addLabelToCircleData(circle, label);

    calculateEmailColor(circle);
    if (circle.hasClass("previewed")) {
        putLabelBalls(circle);
    }
}

function addSystemLabel(circle, systemLabel) {

    var systemLabels = getSystemLabels(circle);

    systemLabels.push(systemLabel);
    circle.data("system-labels", systemLabels);

    showCircleIfNeedBe(circle);
}

function addLabelToEmail(label, circle) {

    addCircleColor(circle, label);

    $.ajax({
        type: "POST",
        url: "async/AddLabel",
        dataType: 'json',
        data: { labelName: label, mailId: circle.data('id'), mailAccountId: circle.data('mailaccount') }
    }).fail(function () {
        alert("No fue posible etiquetar el email");
    });
}

function setQuickLabelRemoval() {
    $(".label-ball").on('click', function () {

        var circle = $(this).parent();
        removeLabelFromCircle(circle, $(this).data("label-name"));
        putLabelBalls(circle);
    });
}

function removeLabelFromCircleData(circle, label) {

    var circleLabels = getCustomLabels(circle),
    indexOfLabel;

    indexOfLabel = circleLabels.indexOf(label);

    if (indexOfLabel > -1) {
        circleLabels.splice(indexOfLabel, 1);
    }

    circle.data("custom-labels", circleLabels);
}

function removeLabelFromCircle(circle, label) {

    removeLabelFromCircleData(circle, label);

    showCircleIfNeedBe(circle);

    calculateEmailColor(circle);

    removeLabelFromCircleInServer(label, circle.data('id'), false, circle.data('mailaccount'));
}

function removeSystemLabelFromCircle(circle, label) {

    var circleLabels = getSystemLabels(circle),
        indexOfLabel;

    indexOfLabel = circleLabels.indexOf(label);

    if (indexOfLabel > -1) {
        circleLabels.splice(indexOfLabel, 1);
    }

    circle.data("system-labels", circleLabels);

    showCircleIfNeedBe(circle);

    removeLabelFromCircleInServer(label, circle.data('id'), true, circle.data('mailaccount'));
}


function setEverithingRelatedToAddLabelsToAMail() {
    setLabelPencil();
    preventSelectingSystemLabels();
    setLabelsMarker();
}

function preventSelectingSystemLabels() {

    $('#panel').find('.label.mailbox').mousedown(function (e) { e.preventDefault(); });
    $('#panel').find('.nav-header').addClass('unselectable');
}

function setLabelPencil() {

    var labelCreationIcon = $("#create-label");

    labelCreationIcon.popover({
        html: true,
        content: "<div id='label-creation-form' class='form-inline'><input type='text' id='create-label-textbox' class='input-small'>" +
            "<div id='create-label-submit' class='btn'>Crear</div></div>"
    });

    labelCreationIcon.on('click', function (event) {
        event.stopPropagation();
        setLabelCreationForm();
        $('#label-creation-form').parent().parent().parent().click(function (event) {
            event.stopPropagation();
        });
    });

    $('#label-creation-form').click(function (event) {
        event.stopPropagation();
    });

    $(document).click(function () {
        labelCreationIcon.popover('hide');
    });

}

function setLabelCreationForm() {

    $("#create-label-textbox").on('keyup', function (e) {
        e.preventDefault();
        if (e.keyCode === 13) {
            $('#create-label-submit').click();
        }
    });

    $('#create-label-submit').on('click', function () {
        var newLabel = $("#create-label-textbox").val();
        if (newLabel == null || newLabel == '')
            alert("La etiqueta debe tener un nombre.");
        else {
            createCustomLabel(newLabel);
            $("#create-label").popover('hide');
        }
    });
}

function chooseCirclesToBeShown() {

    var currentMailbox = getCurrentMailbox();

    $(".circle").each(function () {
        showCircleIfNeedBe($(this), currentMailbox);
    });
}

function getCurrentMailbox() {
    return $(".mailbox:not(.label-hidden)").data("name");
}

function showCircleIfNeedBe(circle, currentMailbox) {

    if (currentMailbox == null)
        currentMailbox = getCurrentMailbox();

    if (toBeHidden(circle, currentMailbox)) {
        circle.addClass("hidden");
    } else {
        circle.removeClass("hidden");
        calculateEmailColor(circle);
    }
}

function setLabelSelection(label) {
    label.on('click', function () {
        $(this).toggleClass('label-hidden');
        chooseCirclesToBeShown();
    });
}

function setMailBoxSelection() {
    $(".mailbox").on('click', function () {

        var clicked = $(this);

        if (clicked.hasClass("label-hidden")) {
            $(".mailbox").addClass("label-hidden");
            clicked.removeClass("label-hidden");
            chooseCirclesToBeShown();
        }
    });
}

function getCustomLabels(circle) {

    return getLabels(circle, "custom-labels");
}

function getSystemLabels(circle) {

    return getLabels(circle, "system-labels");
}

function getLabels(circle, labelsString) {
    var labelsArray = circle.data(labelsString).toString().split(",");
    if (labelsArray.length === 1 && labelsArray[0] === "") {
        labelsArray = [];
    }
    return labelsArray;
}

function toBeHidden(circle, currentMailbox) {

    var customLabels = getCustomLabels(circle),
        systemLabels = getSystemLabels(circle),
        activeMailBox = currentMailbox,
        circleMailAccountIsActive = activeMailAccounts[circle.data('mailaccount')];


    if (!circleMailAccountIsActive)
        return true;

    if (hasExclusiveLabel(systemLabels) && !isExclusive(activeMailBox)) {
        return true;
    }

    if (activeMailBox !== "INBOX") {
        if (systemLabels.indexOf(activeMailBox) === -1) {
            return true;
        }
    } else {
        if (systemLabels.length != 0 && systemLabels.indexOf(activeMailBox) === -1) {
            return true;
        }
    }

    if (customLabels.length === 0) {
        return !isActive("others");
    }
    else {
        return !customLabels.some(isActive);
    }
}

function isExclusive(systemLabel) {
    return exclusive_labels.indexOf(systemLabel) != -1 ? true : false;
}

function hasExclusiveLabel(labels) {

    for (var i = 0; i < labels.length; i++) {

        if (isExclusive(labels[i]))
            return true;
    }

    return false;
}

function isActive(label) {
    return !$("li[data-name='" + label + "']").hasClass("label-hidden");
}

function hasLabel(circle, label) {
    return hasGenericLabel(getCustomLabels, circle, label);
}

function hasSystemLabel(circle, systemLabel) {
    return hasGenericLabel(getSystemLabels, circle, systemLabel);
}

function hasGenericLabel(labelsFinder, circle, label) {
    return (labelsFinder(circle).indexOf(label) != -1);
}

function validSystemLabel(label) {
    return unwantedSystemLabels.indexOf(label.systemName) === -1 && label.systemName != null;
}

function loadLabels() {

    var others = $("<li class='custom-label label label-glimpse' data-name='others'>Sin Etiqueta</li>");
    setLabelSelection(others);
    $(".nav-header:contains('Etiquetas')").after(others);

    for (var i = labels.length - 1; i >= 0; i--) {

        var currentLabel = labels[i];
        if (currentLabel.systemName === null) {
            appendCustomLabel(currentLabel);

        } else
            if (validSystemLabel(currentLabel)) {

                $(".nav-header:contains('Carpetas')").after(
               "<li class='mailbox label label-glimpse label-hidden' data-name=" + currentLabel.systemName + ">" +
               currentLabel.showName + "</li>"
           );
            }
    }

    $(".mailbox:contains('INBOX')").removeClass("label-hidden");

}

function appendCustomLabel(label) {

    var name = label.showName;
    var color = label.Color;
    var labelToAppend = $("<li class='custom-label label label-glimpse' data-name='" + name + "'><p>" + name +
        '</p><span class="pull-right hidden"><div class="btn-group">' +
    '<button class="btn" title="Renombrar"><i class="icon-pencil"></i></button>' +
    '<button class="btn" title="Cambiar color"><i class="icon-edit"></i></button>' +
    '<button class="btn" title="Eliminar"><i class="icon-remove"></i></button>' +
    '</div></span></li>'
        );

    labelToAppend.find("span").on('click', function (e) {
        e.stopPropagation();
    });

    paintLabel(labelToAppend, color);

    setColorButton(labelToAppend, color);
    setRenameButton(labelToAppend, color);
    setRemoveButton(labelToAppend);

    setEditionButtonGroup(labelToAppend);

    setLabelSelection(labelToAppend);
    $(".custom-label:last-of-type").after(labelToAppend);
    setLabelsAdder(labelToAppend);
}

function setEditionButtonGroup(label) {

    label.hover(
        function () {
            label.find("span").removeClass("hidden");
        },
        function () {
            label.find("span").addClass("hidden");
        }
        );
}

function setColorButton(labelElement, currentColor) {

    labelElement.find(".btn[title='Cambiar color']").on('click', function (e) {

        var name = labelElement.attr("data-name"),
            edition_panel = $("#label-edition");

        $("#label-edition").html("</br>Color:  <input type='color' class='label-color-picker label-edition' value='" + currentColor +
            "' data-current-label='" + name + "'>");

        setDialogConfig(edition_panel, "Cambiar color", function () { }, changeLabelColor)

        edition_panel.dialog("open");
    });
}

function setRenameButton(labelElement) {

    labelElement.find(".btn[title='Renombrar']").on('click', function (e) {

        var name = labelElement.attr("data-name"),
            edition_panel = $("#label-edition");

        edition_panel.html("</br>Nombre:  <input type='text' class='label-rename-textbox label-edition' value='" + name +
            "' data-oldname='" + name + "'>");

        setDialogConfig(edition_panel, "Cambiar nombre", function () { }, renameLabel)

        edition_panel.dialog("open");
    });
}

function setDialogConfig(dialogElement, title, actionClose, actionSave) {

    dialogElement.dialog({
        title: title,
        buttons: [
        {
            text: "Cerrar",
            click: function (element) {
                actionClose(dialogElement);
                dialogElement.dialog("close");
            },
        },
        {
            id: 'label-editor-save',
            text: "Guardar",
            click: function () {
                actionSave(dialogElement);
                dialogElement.dialog("close");
            }
        }
        ]
    });
}

function showConfirmationModal(title, question, cancelText, confirmText, confirmAction) {

    var modal = $('#confirmation-modal'),
        cancelButton = modal.find('[data-action-name="cancel"]'),
        confirmButton = modal.find('[data-action-name="confirm"]');

    if (title != null)
        modal.find('.title').html(title);

    if (question != null)
        modal.find('p.question').html(question);

    if (cancelText != null) {
        cancelButton.html(cancelText);
    }

    if (confirmText != null)
        modal.find('[data-action-name="confirm"]').html(confirmText);


    confirmButton.unbind('click');
    confirmButton.one('click', null, confirmAction);

    modal.modal('show');
}


function setRemoveButton(labelElement) {

    labelElement.find(".btn[title='Eliminar']").on('click', function () {

        var currentLabel = $(this).parent().parent().parent();

        showConfirmationModal('Eliminar etiqueta',
            '¿Estás seguro que querés eliminar la etiqueta "' + currentLabel.attr("data-name") + '"?',
            'Cancelar',
            'Eliminar',
            function () {

                $("#label-edition").dialog('close');

                $.ajax({
                    type: "POST",
                    url: "async/DeleteLabel",
                    data: { labelName: currentLabel.attr("data-name") }
                }).fail(function () {
                    alert("No fue posible eliminar la etiqueta");
                });

                $(".circle").each(function () {
                    removeLabelFromCircle($(this), currentLabel.text());
                });
                currentLabel.remove();
            });
    });

}

function changeLabelColor(dialogElement) {
    var targetLabelName = dialogElement.find('input').attr('data-current-label'),
        newColor = dialogElement.find('input').val();
    var targetLabel = $(".custom-label[data-name='" + targetLabelName + "']");
    paintLabel(targetLabel, newColor);

    $.ajax({
        type: "POST",
        url: "async/RecolorLabel",
        data: { labelName: targetLabelName, color: newColor }
    });

    $(".circle").each(function () {
        if (hasLabel($(this), targetLabelName)) {
            calculateEmailColor($(this));
        }
    });

    $(".circle.previewed").each(
        function () {
            putLabelBalls($(this));
        });
}

function renameLabel(dialogElement) {
    var oldName = dialogElement.find('input').attr('data-oldname'),
       newName = dialogElement.find('input').val();

    if (oldName === newName)
        return;

    var targetLabel = $(".custom-label[data-name='" + oldName + "']");
    targetLabel.find('p').html(newName);
    targetLabel.attr("data-name", newName);
    labelColors[newName] = labelColors[oldName];

    $(getOwnedCircles()).each(function () {

        if (hasLabel($(this), oldName)) {
            removeLabelFromCircleData($(this), oldName);
            addLabelToCircleData($(this), newName);
        }
    });

    $.ajax({
        type: "POST",
        url: "async/RenameLabel",
        data: { oldLabelName: oldName, newLabelName: newName }
    });
}

function exists(label) {

    for (var i = 0; i < labels.length; i++) {
        if (labels[i].showName === label) {
            return true;
        }
    }
    return false;
}

function createCustomLabel(labelName) {

    if (!exists(labelName)) {

        $.ajax({
            type: "POST",
            url: "async/CreateLabel",
            dataType: 'json',
            data: { labelName: labelName }

        }).done(function (data) {
            if (data.success === true) {
                var newLabel = {};
                newLabel.showName = labelName;
                newLabel.Color = data.color;
                appendCustomLabel(newLabel);
            } else {
                alert(data.message);
            }
        });
    } else {
        alert("Ya hay una etiqueta con el mismo nombre");
    }
}

function removeLabelFromCircleInServer(labelName, mailId, isSystemLabel, mailAccountId) {
    $.ajax({
        type: "POST",
        url: "async/RemoveLabel",
        dataType: 'json',
        data: { labelName: labelName, mailId: mailId, isSystemLabel: isSystemLabel, mailAccountId: mailAccountId }
    });
}

function setLabelsMarker() {
    $('#mark-labels').click(function () {

        var oneLabelIsOn,
            classAction;

        $('.custom-label').each(function () {
            if (isActive($(this).data('name'))) {
                oneLabelIsOn = true;
                return false;
            }
        });

        $('.custom-label').each(function () {
            var label = $(this);

            if (oneLabelIsOn)
                label.addClass('label-hidden');
            else
                label.removeClass('label-hidden');
        });

        chooseCirclesToBeShown();
    });
}

function prepareLabelsEditor() {
    var edition_panel = $("#label-edition");

    edition_panel.dialog({
        autoOpen: false,
        closeOnEscape: true,
        draggable: true,
        height: 200,
        width: 250,
        minWidth: 250,
        minHeight: 200,
        resizable: false,
        title: "Modifica tu label",
        position: { my: "center center", at: "center center", of: window },
        buttons: [
        {
            text: "Cerrar",
            click: function () {
                $(this).dialog("close");
            }
        },
        {
            text: "Guardar",
            click: function () {
                $(this).dialog("close");
            }
        }
        ]
    });
}
