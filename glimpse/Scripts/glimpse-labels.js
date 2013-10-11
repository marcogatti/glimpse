var selectedLabel,
    labelToAddIsSet = false;
var unwantedSystemLabels = ["Important", "Flagged", "Drafts", "All"];

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
    "Indigo"
    ];

    var i = 0;
    $("#labels-header").children(".label").each(function () {

        var currentColor = glimpseColors[i];

        if (!$(this).data("system") && $(this).data("name") !== "others") {

            $(this).css("background-color", currentColor);
            labelColors[$(this).data("name")] = currentColor;
            i++;
        }
    });

    setLabelSelection();
}

function labelDrag(ev) {
    //ev.dataTransfer.setData("Text", ev.target.id);
    ev.data.label.css('opacity', 0.4);
}

function setLabelsAdder() {
    $.each($('.label'), function (index, actualLabel) {

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

function canReceiveThatLabel(label, mail) {
    return !hasLabel(mail, label);
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
            if (isClicked(this)) {
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
}

function addLabelToEmail(label, circle) {

    $.ajax({
        type: "POST",
        url: "async/AddLabel",
        dataType: 'json',
        error: function (jqXHR, textStatus, errorThrown) {
            alert("No se pudo agregar la etiqueta.");
        },
        success: function () {
            addCircleColor(circle, label);
        },
        data: { labelName: label, mailId: circle.data('id') }
    });
}

function removeLabelFromEmail(label, circle) {

    $.ajax({
        type: "POST",
        url: "async/RemoveLabel",
        dataType: 'json',
        error: function (jqXHR, textStatus, errorThrown) {
            alert("No se pudo quitar la etiqueta.");
        },
        success: function () {
            removeCircleColor(circle, label);
        },
        data: { labelName: label, mailId: circle.data('id') }
    });
}

function removeCircleColor(circle, label) {

    var index = getCustomLabels(circle).indexOf(label),
        newCustomLabels;

    if (index > -1) {
        newCustomLabels = getCustomLabels(circle).splice(index, 1);
    } else {
        alert("El Email no contenía la etiqueta.");
    }

    circle.data("custom-labels", newCustomLabels);
    calculateEmailColor(circle);
}

function setEverithingRelatedToAddLabelsToAMail() {
    setLabelsAdder();
}

function setLabelSelection() {
    $(".custom-label:not(.custom-label[data-name^='others'])").on('click', function () {
        $(this).toggleClass('label-hidden');

        $(".circle").each(function () {
            if (toBeHidden($(this))) {
                $(this).addClass("hidden");
            } else {
                $(this).removeClass("hidden");
            }
        });
    });
}

function getCustomLabels(circle) {

    var labelsArray = circle.data("custom-labels").toString().split(",");
    if (labelsArray[0] === "") {
        labelsArray = [];
    }
    return labelsArray;
}

function toBeHidden(circle) {
    return !getCustomLabels(circle).some(isActive);
}

function isActive(label) {
    return !$("li[data-name='" + label + "']").hasClass("label-hidden");
}

function hasLabel(circle, label) {
    return (getCustomLabels(circle).indexOf(label) != -1);
}

function loadLabels() {

    for (var i = labels.length - 1; i >= 0; i--) {

        var currentLabel = labels[i];
        if (currentLabel.systemName === null) {
            $(".nav-header:contains('Etiquetas')").after(
                "<li class='custom-label label label-glimpse' data-name='" + currentLabel.showName + "'>" + currentLabel.showName + "</li>"
            );
        } else
            if (unwantedSystemLabels.indexOf(currentLabel.systemName) === -1 && currentLabel.systemName != null) {

                $(".nav-header:contains('Carpetas')").after(
               "<li class='label label-glimpse label-hidden' data-name=" + currentLabel.showName +
               " data-system=" + currentLabel.systemName + ">" + currentLabel.showName + "</li>"
           );
            }
    }

    $(".label-glimpse:contains('INBOX')").removeClass("label-hidden");


    $(".nav-header:contains('Etiquetas')").after(
                "<li class='custom-label label label-glimpse' data-name='others'>Sin etiqueta</li>"
            );

}