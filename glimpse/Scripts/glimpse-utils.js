var maxAge = 0,
    minAge = 0,
    labelColors = {},
    editor,
    wrapperVerticalPadding = parseInt($("#container-wrapper").css("padding-top")),
    wrapperLeftPadding = parseInt($("#container-wrapper").css("padding-left")),
    cw = containerWidth(),
    ch = containerHeight(),
    historyOffset = 0,
    defaultZoom;

function preventSelectingNotUsefulThings() {
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

function clearCanvas() {
    document.getElementById('vertical-lines').getContext('2d').clearRect(0, 0, containerWidth(), containerHeight());
}

function drawLines(offset) {

    historyOffset -= offset;
    clearCanvas();

    var
     canvas = $('canvas').attr({ width: cw, height: ch }),
     context = canvas.get(0).getContext("2d");

    var padding = 150;
    context.moveTo(padding + historyOffset, 0);
    context.lineTo(padding + historyOffset, ch);
    context.moveTo(cw - padding + historyOffset, 0);
    context.lineTo(cw - padding + historyOffset, ch);
    context.lineWidth = 2;
    context.strokeStyle = "#CDCDCD";
    context.stroke();

}

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

    var maxAmountInScreen = 30,
        smallestPeriod = 60;

    if ((factor > 0 && currentPeriodShown() < smallestPeriod) || (factor < 0 && amountOfCirclesShown() > maxAmountInScreen)) {
        return;
    }

    var movement = currentPeriodShown() * factor * 0.0001;

    var offsetRight = Math.round((containerWidth() - zoomPoint) * movement);
    var offsetLeft = Math.round(zoomPoint * movement);

    if (maxAge - offsetRight > minAge + offsetLeft) {

        if (allowedMovementRight(-offsetRight)) {
            maxAge -= offsetRight;
        }
        if (allowedMovementLeft(offsetLeft)) {
            minAge += offsetLeft;
        }

        setDateCoords();
        fetchMailsWithinActualPeriod();

        moveSoftly(0.5);

    }

}

function moveSoftly(chunk) {

    var circlesProcessed;

    circlesProcessed = surroundingCircles(chunk, function (circle) {
        circle.addClass("transition");
    });
    calculateEmailsLeft(chunk).done(function () {

        for (index in circlesProcessed) {
            var circle = circlesProcessed[index];

            circle.removeClass("transition");
        }
    });
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
    $('#zoom-restore,#new-mail-arrow').click(function () { restoreZoom(); });
}

function restoreZoom() {
    var halfDiff = parseInt((currentPeriodShown() - defaultZoom) / 2);
    if (allowedMovementLeft(halfDiff)) {
        minAge += halfDiff;
    }
    if (allowedMovementRight(halfDiff)) {
        maxAge -= halfDiff;
    }
    maxAge -= minAge;
    minAge -= minAge;
    calculateEmailsLeft(null);
    setDateCoords();
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
    if (allowedMovementLeft(offset)) {
        minAge += offset;
        maxAge += offset;
        calculateEmailsLeft(0.3);
    } else {
        clearCanvas();
    }
}

function setDragging() {

    var startX, endX = 0,
        offset, ageOffset = 0,
        wasDragging = false;

    $("#email-container").mousedown(function (downEvent) {
        downEvent.preventDefault();
        startX = downEvent.pageX;
        $(window).mousemove(function (dragEvent) {
            offset = (startX - dragEvent.pageX);
            drawLines(offset);
            ageOffset = Math.round(offset * currentPeriodShown() / 1000);
            movePeriodShown(ageOffset);
            startX = dragEvent.pageX;
            wasDragging = true;
        });
    });

    //  Revisar (pedidos ajax innecesarios)
    $(window).mouseup(function () {
        $(window).unbind("mousemove");
        if (wasDragging) {
            setDateCoords();
            calculateEmailsLeft(1.1);       //Emparchau
            fetchMailsWithinActualPeriod();
            wasDragging = false;

            //  para lineas motion
            historyOffset = 0;
            clearCanvas();
        }
    });
}

function setDateCoordsPosition() {
    var
        coordLineHeight = parseInt($(".date-coord").css("line-height"), 10),
        dateLastWidth = parseInt($("#date-last").css("width")),
        dateLastPosition = containerWidth() - wrapperLeftPadding;

    $(".date-coord").css("top", function () {
        return containerHeight() + (2 * wrapperVerticalPadding) + 'px';
    });
    $("#date-last").css("left", function () {
        return dateLastPosition + 'px';
    });
}

function ageToDate(age) {
    var now = new Date().getTime(),
        jsAge = age * 1000;
    return new Date(now - jsAge);
}

function getDiffString(diff, singular) {

    if (diff === 1) { return " " + singular; }
    else { return " " + singular + "s"; }
}

function sameDay(date1, date2) {
    return date1.toLocaleDateString() === date2.toLocaleDateString();
}

function setDateCoords() {

    var newMinDate = ageToDate(minAge),
        newMaxDate = ageToDate(maxAge),
        lastString = elapsedTime(newMinDate, newMaxDate),
        now = new Date(),
        dateToShow;

    if (currentPeriodShown() > 60 * 60 * 24 * 2) {

        if (sameDay(now, newMinDate)) {
            dateToShow = "Hoy";
            $("#new-mail-arrow").addClass("hidden");
        } else {
            dateToShow = newMinDate.toLocaleDateString();
        }

    } else {

        dateToShow = newMinDate.toLocaleString();

        if (currentPeriodShown() > 60 * 60 * 2) {

            if (sameDay(now, newMinDate) && now.getHours() === newMinDate.getHours()) {
                dateToShow = "Ahora";
                $("#new-mail-arrow").addClass("hidden");
            }
        }
        else {

            if (sameDay(now, newMinDate) && now.getHours() === newMinDate.getHours() && now.getMinutes() === newMinDate.getMinutes()) {
                dateToShow = "Ahora";
                $("#new-mail-arrow").addClass("hidden");
            }
        }
    }

    $("#date-today").html(dateToShow);
    $("#date-last").html(lastString);
}

function elapsedTime(recentDate, oldDate) {

    var diff = recentDate - oldDate,
        effectiveDiff = Math.round(diff / 1000 / 60 / 60 / 24);

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
    return effectiveDiff.toString() + diffString + " atrás";
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

        cw = containerWidth();
        ch = containerHeight();
        setDateCoords();
        setDateCoordsPosition();

    });

}

function stopWorkingWidget(container) {
    container.find('.progress-circular').addClass('hidden');
}

function startWorkingWidget(container) {
    container.find('.progress-circular').removeClass('hidden');
}