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

    var glimpseColors = [

    //  algunos de los colores de Gmail
    "rgb(251, 76, 47)",   //  rojo
    "rgb(22, 167, 101)",  //  verde
    "rgb(255, 173, 70)",  //  naranja
    "rgb(73, 134, 231)",  //  azul

    //  otros
    "LimeGreen",
    "LightSeaGreen",
    "Crimson",
    "Indigo",
    'aqua',
    'blue',
    'fuchsia',
    'green',
    'gray',
    'lime',
    'maroon',
    'navy',
    'olive',
    'purple',
    'red',
    'silver',
    'teal',
    'yellow'];

    var i = 0;
    $(".custom-label:not(.custom-label[data-name^='others'])").each(function () {

        var currentColor = glimpseColors[i];

        if (!$(this).data("system") && $(this).data("name") !== "others") {

            $(this).css("background-color", currentColor);
            labelColors[$(this).data("name")] = currentColor;
            i++;
        }
    });

    setLabelSelection();
    setMailBoxSelection();
}

function labelDrag(ev) {
    //ev.dataTransfer.setData("Text", ev.target.id);
    ev.data.label.css('opacity', 0.4);
}

function setLabelsAdder() {
    $.each($(".label-glimpse"), function (index, actualLabel) {

        var currentLabel = $(this);
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
    });
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

function addCircleColor(circle, label) {

    var newCustomLabels = getCustomLabels(circle);
    newCustomLabels.push(label);
    circle.data("custom-labels", newCustomLabels);

    calculateEmailColor(circle);
    if (circle.hasClass("previewed")) {
        putLabelBalls(circle);
    }
}

function addSystemLabel(circle, systemLabel) {

    var systemLabels = getSystemLabels(circle);

    systemLabels.push(systemLabel);
    circle.data("system-labels", systemLabels);

    chooseCirclesToBeShown();
}

function addLabelToEmail(label, circle) {

    addCircleColor(circle, label);

    $.ajax({
        type: "POST",
        url: "async/AddLabel",
        dataType: 'json',
        data: { labelName: label, mailId: circle.data('id') }
    });
}

function setQuickLabelRemoval() {
    $(".label-ball").on('click', function () {

        var circle = $(this).parent();
        removeLabelFromCircle(circle, $(this).data("label-name"));
        putLabelBalls(circle);
    });
}

function removeLabelFromCircle(circle, label) {

    var circleLabels = getCustomLabels(circle),
        indexOfLabel;

    indexOfLabel = circleLabels.indexOf(label);

    if (indexOfLabel > -1) {
        circleLabels.splice(indexOfLabel, 1);
    }

    circle.data("custom-labels", circleLabels);
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
    calculateEmailColor(circle);

    removeLabelFromCircleInServer(label, circle.data('id'), true, circle.data('mailaccount'));
}


function setEverithingRelatedToAddLabelsToAMail() {
    setLabelsAdder();
}

function chooseCirclesToBeShown() {
    $(".circle").each(function () {
        if (toBeHidden($(this))) {
            $(this).addClass("hidden");
        } else {
            $(this).removeClass("hidden");
            calculateEmailColor($(this));
        }
    });
}

function setLabelSelection() {
    $(".custom-label").on('click', function () {
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

function toBeHidden(circle) {

    var customLabels = getCustomLabels(circle),
        systemLabels = getSystemLabels(circle),
        activeMailBox = $(".mailbox:not(.label-hidden)").data("name"),
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
    return (getCustomLabels(circle).indexOf(label) != -1);
}

function validSystemLabel(label) {
    return unwantedSystemLabels.indexOf(label.systemName) === -1 && label.systemName != null;
}

function loadLabels() {

    for (var i = labels.length - 1; i >= 0; i--) {

        var currentLabel = labels[i];
        if (currentLabel.systemName === null) {
            $(".nav-header:contains('Etiquetas')").after(
                "<li class='custom-label label label-glimpse' data-name='" + currentLabel.showName + "'>" + currentLabel.showName + "</li>"
            );
        } else
            if (validSystemLabel(currentLabel)) {

                $(".nav-header:contains('Carpetas')").after(
               "<li class='mailbox label label-glimpse label-hidden' data-name=" + currentLabel.systemName + ">" +
               currentLabel.showName + "</li>"
           );
            }
    }

    $(".mailbox:contains('INBOX')").removeClass("label-hidden");


    $(".nav-header:contains('Etiquetas')").after(
                "<li class='custom-label label label-glimpse' data-name='others'>Sin etiqueta</li>"
            );

}

function removeLabelFromCircleInServer(label, mailId, isSystemLabel, mailAccountId) {
    $.ajax({
        type: "POST",
        url: "async/RemoveLabel",
        dataType: 'json',
        data: { labelName: label, mailId: mailId, isSystemLabel: isSystemLabel, mailAccountId: mailAccountId }
    });
}

