var maxAge = 0,
    minAge = 0,
    labelColors = {},
    editor;

function containerHeight() {
    return $("#email-container").height();
}

function containerWidth() {
    return $("#email-container").width();
}

function alphabetSize() {
    return "z".charCodeAt(0) - "a".charCodeAt(0) + 2;
}

function clearCanvas() {
    document.getElementById('gridCanvas').getContext('2d').clearRect(0, 0, containerWidth(), containerHeight());
}

function drawGrid() {

    clearCanvas();

    var containerBorder = parseInt($("#email-container").css("border-width"), 10),
    //grid width and height
        bw = containerWidth() - containerBorder,
        bh = containerHeight() - containerBorder,

    //padding around grid
     p = 0,

    //size of canvas
     cw = bw,
     ch = bh,

     canvas = $('canvas').attr({ width: cw, height: ch }),

     context = canvas.get(0).getContext("2d");

    function squareSize() {
        return containerHeight() / alphabetSize();
    }

    function drawBoard() {
        var x = 0;

        for (x = 0; x <= bw; x += squareSize()) {
            context.moveTo(0.5 + x + p, p);
            context.lineTo(0.5 + x + p, bh + p);
        }

        for (x = 0; x <= bh; x += squareSize()) {
            context.moveTo(p, 0.5 + x + p);
            context.lineTo(bw + p, 0.5 + x + p);
        }

        context.strokeStyle = "#CDCDCD";
        context.stroke();
    }

    drawBoard();
}

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

        if (!$(this).data("system")) {

            $(this).css("background-color", currentColor);
            labelColors[$(this).data("name")] = currentColor;
            i++;
        }
    });

    setLabelSelection();
}

function setTransitionsCheckbox() {
    $("#transitions-check").click(function () {
        $(".circle").toggleClass("transition");
    });
}



function currentPeriodShown() {
    return maxAge - minAge;
}

function notNegative(value1, value2) {
    return value1 + value2 >= 0;
}

function zoom(factor, zoomPoint) {

    var movement = currentPeriodShown() * factor * 0.0002;

    maxAge -= (containerWidth() - zoomPoint) * movement;

    var offset = zoomPoint * movement;
    if (notNegative(minAge, offset)) {
        minAge += offset;
    }

    setDateCoords();
    fetchMailsWithinActualPeriod();
    calculateEmailsLeft();
}

function configureZoom() {
    setWheelZoom();
    setButtonZoom();
}

function setButtonZoom() {

    /*  zoom exactamente en el centro del contenedor    */
    var zoomPoint = containerWidth() / 2;
    $('#zoom-in').click(function () { zoom(1, zoomPoint); return false; });
    $('#zoom-out').click(function () { zoom(-1, zoomPoint); return false; });
}

function setWheelZoom() {

    /*  zoom donde apunta el mouse  */
    $('#email-container').on('mousewheel', function (event, delta, deltaX, deltaY) {
        event.preventDefault();
        zoom(deltaY, event.offsetX);
    });
}

function movePeriodShown(offset) {
    if (notNegative(minAge, offset)) {
        minAge += offset;
        maxAge += offset;
    }
    calculateEmailsLeft();
}

function setDragging() {

    var startX, endX = 0,
        wasDragging = false;

    $("#email-container").mousedown(function (downEvent) {
        downEvent.preventDefault();
        startX = downEvent.pageX;
        $(window).mousemove(function (dragEvent) {
            var offset = (startX - dragEvent.pageX) * currentPeriodShown() / 1000;
            movePeriodShown(offset);
            startX = dragEvent.pageX;
            wasDragging = true;
        });
    });

    //  Revisar (pedidos ajax innecesarios)
    $(window).mouseup(function () {
        $(window).unbind("mousemove");
        if (wasDragging) {
            setDateCoords();
            fetchMailsWithinActualPeriod();
            wasDragging = false;
        }
    });
}

function setDateCoordsPosition() {
    $(".date-coord").css("top", function () {
        return parseInt(containerHeight(), 10) - parseInt($(".date-coord").css("line-height"), 10) + 'px';
    });
    $("#date-last").css("left", function () {
        return parseInt(containerWidth(), 10) - parseInt($("#date-last").css("width")) + 'px';
    });
}

function ageToDate(age) {
    var now = new Date().getTime(),
        jsAge = Math.floor(age / 10000);
    return new Date(now - jsAge);
}

function setDateCoords() {

    newDateToday = ageToDate(minAge).toLocaleDateString(),
    newDateLast = ageToDate(maxAge).toLocaleDateString();

    if (newDateToday === new Date().toLocaleDateString()) {
        newDateToday = "Hoy";
    }

    $("#date-today").html(newDateToday);
    $("#date-last").html(newDateLast);
}

function isOnPreview(circle) {
    return circle.hasClass("preview");
}

function hideProgressBar(bar) {
    $(bar).css("visibility", "hidden");
}

function showProgressBar(bar) {
    $(bar).css("visibility", "visible");
}


function setRefreshOnResize() {
    $(window).resize(function () {
        $(".circle").each(function () {
            calculateEmailPosition($(this));
        });
    });

}

var selectedLabel,
    labelToAddIsSet = false;

function setLabelsAdder() {
    $.each($('.label'), function (index, actualLabel) {
        $(this).mousedown(function (downEvent) {
            downEvent.preventDefault();
            selectedLabel = $(this).attr('data-name');
            labelToAddIsSet = true;
        })
    }
    );
}

function clearLabelsToAdd() {
    $(document).mouseup(function () {
        labelToAddIsSet = false;
    }
    );
}

function prepareToReceiveLabels(circle) {
    circle.mouseup(function () {
        if (labelToAddIsSet) {
            addLabelToEmail(selectedLabel, $(this));
        }
    }
    );
}

function changeMailColour(mail, label) {

    if (mail.attr('data-label0') === "") {
        mail.data('label0', label);
        mail.attr('data-label0', label);
    } else if (mail.attr('data-label1') === "") {
        mail.data('label1', label);
        mail.attr('data-label1', label);
    } else if (mail.attr('data-label2') === "") {
        mail.data('label2', label);
        mail.attr('data-label2', label);
    } else {
        mail.data('label0', label);
        mail.attr('data-label0', label);
    }

    calculateEmailColor(mail);
}

function addLabelToEmail(label, mail) {

    // Validar que el label no sea un system label, igual tambien lo hace el server.

    $.ajax({
        type: "POST",
        url: "async/AddLabel",
        dataType: 'json',
        error: function (jqXHR, textStatus, errorThrown) {
            alert("No se pudo agregar el label.");
        },
        success: function () {
            changeMailColour(mail, label);
        },
        data: { labelName: label, mailId: mail.attr('data-id') }
    });
}


function setEverithingRelatedToAddLabelsToAMail() {
    setLabelsAdder();
    clearLabelsToAdd();
}
