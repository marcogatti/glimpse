var maxAge = 0,
    minAge = 0,
    labelColors = {},
    editor,
    wrapperVerticalPadding = parseInt($("#container-wrapper").css("padding-top")),
    wrapperLeftPadding = parseInt($("#container-wrapper").css("padding-left"));


function preventSelectingNotUsefulThings() {
    //$('body').mousedown(function (downEvent) {
    //    downEvent.preventDefault();
    //}
    //);
}

function containerHeight() {
    return $("#email-container").height();
}

function containerWidth() {
    return $("#email-container").width();
}

function alphabetSize() {
    return "z".charCodeAt(0) - "a".charCodeAt(0) + 2;
}

//function clearCanvas() {
//    document.getElementById('gridCanvas').getContext('2d').clearRect(0, 0, containerWidth(), containerHeight());
//}

//function drawGrid() {

//    clearCanvas();

//    var containerBorder = parseInt($("#email-container").css("border-width"), 10),
//    //grid width and height
//        bw = containerWidth() - containerBorder,
//        bh = containerHeight() - containerBorder,

//    //padding around grid
//     p = 0,

//    //size of canvas
//     cw = bw,
//     ch = bh,

//     canvas = $('canvas').attr({ width: cw, height: ch }),

//     context = canvas.get(0).getContext("2d");

//    function squareSize() {
//        return containerHeight() / alphabetSize();
//    }

//    function drawBoard() {
//        var x = 0;

//        for (x = 0; x <= bw; x += squareSize()) {
//            context.moveTo(0.5 + x + p, p);
//            context.lineTo(0.5 + x + p, bh + p);
//        }

//        for (x = 0; x <= bh; x += squareSize()) {
//            context.moveTo(p, 0.5 + x + p);
//            context.lineTo(bw + p, 0.5 + x + p);
//        }

//        context.strokeStyle = "#CDCDCD";
//        context.stroke();
//    }

//    drawBoard();
//}

function amountOfCirclesShown() {

    var circles = [];

    $(".circle").each(function () {
        var currentAge = $(this).attr('data-age');
        if (currentAge < maxAge && currentAge > minAge) {
            circles.push($(this).data('id'));
        }
    });
    return circles.length;
}

function currentPeriodShown() {
    return parseInt(maxAge - minAge);
}

function zoom(factor, zoomPoint) {

    if (amountOfCirclesShown() < $("#max-amount").val() || (factor > 0)) {

        var movement = currentPeriodShown() * factor * 0.0001;

        var offsetRight = (containerWidth() - zoomPoint) * movement;
        var offsetLeft = zoomPoint * movement;

        if (maxAge - offsetRight > minAge + offsetLeft) {

            if (allowedMovementRight(-offsetRight)) {
                maxAge -= offsetRight;
            }
            if (allowedMovementLeft(offsetLeft)) {
                minAge += offsetLeft;
            }

            setDateCoords();
            fetchMailsWithinActualPeriod();

            surroundingCircles(0.5, function (circle) {
                circle.addClass("transition");
            });
            calculateEmailsLeft(2).done(function () {
                $(".circle.transition").removeClass("transition");
            });
        }
    }

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

        var sign = deltaY ? deltaY < 0 ? -1 : 1 : 0;

        zoom(sign, event.offsetX);

    });
}

function allowedMovementRight(offset) {
    return maxAge + offset < oldestAge;
}
function allowedMovementLeft(offset) {
    return minAge + offset > 0;
}

function movePeriodShown(offset) {
    if (allowedMovementLeft(offset) && allowedMovementRight(offset)) {
        minAge += offset;
        maxAge += offset;
        calculateEmailsLeft(0.3);
    }
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
            calculateEmailsLeft(15);       //Emparchau
            fetchMailsWithinActualPeriod();
            wasDragging = false;
        }
    });
}

function setDateCoordsPosition() {
    var 
        coordLineHeight = parseInt($(".date-coord").css("line-height"), 10),
        dateLastWidth = parseInt($("#date-last").css("width"));

    $(".date-coord").css("top", function () {
        return containerHeight() + (2 * wrapperVerticalPadding) + coordLineHeight + 'px';
    });
    $("#date-last").css("left", function () {
        return containerWidth() - wrapperLeftPadding + 'px';
    });
}

function ageToDate(age) {
    var now = new Date().getTime(),
        jsAge = Math.floor(age / 10000);
    return new Date(now - jsAge);
}

function getDiffString(diff, singular) {

    if (diff === 1) { return " " + singular; }
    else { return " " + singular + "s"; }
}

function setDateCoords() {

    var newMinDate = ageToDate(minAge),
        newMaxDate = ageToDate(maxAge),
        diff = newMinDate - newMaxDate,
        diffString;

    newMinDate = newMinDate.toLocaleDateString();
    if (newMinDate === new Date().toLocaleDateString()) {
        newMinDate = "Hoy";
    }

    var effectiveDiff = Math.round(diff / 1000 / 60 / 60 / 24);

    if (effectiveDiff !== 0) {
        diffString = getDiffString(effectiveDiff, "día");
    } else {
        effectiveDiff = Math.round(diff / 1000 / 60 / 60);
        if (effectiveDiff !== 0) {
            diffString = getDiffString(effectiveDiff, "hora");
        } else {
            effectiveDiff = Math.round(diff / 1000 / 60);
            diffString = getDiffString(effectiveDiff, "minuto");
        }
    }

    $("#date-today").html(newMinDate);
    $("#date-last").html(effectiveDiff.toString() + diffString + " atrás");
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

function stopWorkingWidget(container) {
    container.find('.progress-circular').addClass('hidden');
}

function startWorkingWidget(container) {
    container.find('.progress-circular').removeClass('hidden');
}