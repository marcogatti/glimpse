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
    for (var label in labelColors) {

        if (labelColors.hasOwnProperty(label)) {

            var currentColor = glimpseColors[i],
                labelItem = $("<li class='label label-glimpse' style = 'background-color: " + currentColor + "'>" + label + "</li>");

            labelColors[label] = currentColor;
            /* Armar listado de labels */
            $("#labels-header").append(labelItem);

            i++;
        }

    }

    setLabelSelection();
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
    calculateEmailsPosition();
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
    calculateEmailsPosition();
}

function setDragging() {

    var startX, endX = 0;

    $("#email-container")
    .mousedown(function (downEvent) {
        downEvent.preventDefault();
        startX = downEvent.pageX;
        $(window).mousemove(function (dragEvent) {
            var offset = (startX - dragEvent.pageX) * currentPeriodShown() / 1000;
            movePeriodShown(offset);
            startX = dragEvent.pageX;
        });
    });

    $(document)
    .mouseup(function () {
        $(window).unbind("mousemove");
        setDateCoords();
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

function setDateCoords() {
    var now = new Date().getTime(),
        jsMinAge = Math.floor(minAge / 10000),
        jsMaxAge = Math.floor(maxAge / 10000),
        newDateToday = new Date(now - jsMinAge).toLocaleDateString(),
        newDateLast = new Date(now - jsMaxAge).toLocaleDateString();

    if (newDateToday === new Date().toLocaleDateString()) {
        newDateToday = "Hoy";
    }
    //  selector mágico
    //$("#date-today+div").find(".tooltip-inner").html(newDateToday.toLocaleDateString());
    $("#date-today").html(newDateToday);
    $("#date-last").html(newDateLast);
}

function setModal() {

    $(".circle").on("click", function () {

        var from = 'From: ' + $(this).data("from"),
            subject = $(this).data("subject"),
            currentCircle = $(this);

        $(".modal-body").find("h4").html(from);
        $(".modal-header").find("h3").html(subject);

        $(".modal-body").find("#bodyhtml").html("");
        showProgressBar("#body-progress");

        $.getJSON("async/GetMailBody/" + currentCircle.data("id"), function (data) {
            if (data.success == true) {
                hideProgressBar("#body-progress");
                $(".modal-body").find("#bodyhtml").html(data.body);
                markAsRead(currentCircle);

            } else alert(data.message);
        });
    });
}

function hideProgressBar(bar) {
    $(bar).css("visibility", "hidden");
}

function showProgressBar(bar) {
    $(bar).css("visibility", "visible");
}


function setRefreshOnResize() {
    $(window).resize(function () {
        calculateEmailsPosition();
    });

}
