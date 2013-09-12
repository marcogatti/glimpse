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

    var RGBaColors = [

    //  algunos de los colores de Gmail
    "rgba(251, 76, 47, 0.7)",   //  rojo
    "rgba(22, 167, 101, 0.7)",  //  verde
    "rgba(255, 173, 70, 0.7)",  //  naranja
    "rgba(73, 134, 231, 0.7)",  //  azul

    //  otros
    "rgba(255, 105, 0, 0.7)",
    "rgba(32, 178, 170, 0.7)",
    "rgba(160, 32, 240, 0.7)",
    "rgba(50, 205, 50, 0.7",
    "rgba(123, 104, 238, 0.7)",
    "rgba(255, 99, 71, 0.7"
    ];

    var i = 0;
    for (var label in labelColors) {

        if (labelColors.hasOwnProperty(label)) {

            var currentColor = RGBaColors[i],
                labelItem = $("<li class='label glimpse-label' style = 'background-color: " + currentColor + "'>" + label + "</li>");

            labelColors[label] = currentColor;
            /* Armar listado de labels */
            $("#labels").append(labelItem);

            i++;
        }  
       
    }
}

function calculateEmailsColor() {

    $(".circle").each(function () {

        var color = labelColors[$(this).data('label')];

        $(this).css({
            'color': color,
            'background-color': color
        });
    })
}


function currentPeriodShown() {
    return maxAge - minAge;
}

function calculateEmailsPosition() {

    var margin = parseInt($(".circle").css('width'), 10);

    $(".circle").each(function () {

        var left = ($(this).attr('data-age') - minAge) / currentPeriodShown(),
            top = ($(this).attr('data-from').charCodeAt(0) - "a".charCodeAt(0) + 2) / alphabetSize();

        $(this).css('top', function () {
            return top * (containerHeight() - margin) + 'px';
        });

        $(this).css('left', function () {
            return left * (containerWidth() - margin) + 'px';
        });


    })
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

    $("body")
    .mouseup(function () {
        $(window).unbind("mousemove");
    });
}

function setDateCoords() {
    $(".dateCoord").css("top", function () {
        return parseInt(containerHeight(), 10) - parseInt($(".dateCoord").css("line-height"), 10) + 'px';
    });
}

function setModal() {

    $(".circle").on("click", function () {

        var from = $('<h4>From: ' + $(this).data("from") + '</h4>'),
            subject = $('<h3>' + $(this).data("subject") + '</h3>');

        $(".modal-body").find("h4").remove();
        $(".modal-body").find("#bodyhtml").remove();
        $(".modal-header").find("h3").remove();

        $(".modal-body").append(from);
        $(".modal-header").append(subject);

        /*  horrible, pero no encontré otra forma de hacerlo andar  */
        $.getJSON("async/GetMailBody/" + $(this).data("id"), function (data) {
            if (data.success == true) {
                $(".modal-body").append("<div id='bodyhtml'>" + data.body + "</div>");
            } else alert(data.message);
        });
    });
}

function configureCircleHover() {

    var dateTime = $("#dateTime"),
        from = $("#from");

    $(".circle").hover(

        function () {

            var currentCircle = $(this);

            dateTime.html(currentCircle.data("date"));
            from.html(currentCircle.data("from"));

            dateTime.css("left", function () {
                return currentCircle.css("left");
            });

            from.css("top", function () {
                return currentCircle.css("top");
            });

            $(".hidable").removeClass("hidden");

            currentCircle.addClass("selected");

            var currentTid = currentCircle.data("tid");

            $('.circle').each(
                function () {
                    if ($(this).data("tid") === currentTid) {
                        $(this).addClass("focused");
                    }
                });


        }, function () {
            $(".hidable").addClass("hidden");
            $(".selected").removeClass("selected");
            $(".focused").removeClass("focused");
        })
}

function hideProgressBar() {
    $(".progress").css("visibility", "hidden");
}

function setRefreshOnResize() {
    $(window).resize(function () {
        calculateEmailsPosition();
        drawGrid();
    });

}

function fetchMailsAsync() {

    $.getJSON("async/InboxMails/500", function (data) {

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                if (value.age > maxAge) {
                    maxAge = value.age;
                }

                var date = new Date(parseInt(value.date.substr(6))).toLocaleDateString(),
                    classes = "circle";

                if (!value.seen) {
                    classes += " new";
                }

                var dataAttributes = [
                    " data-id=", value.id,
                    " data-tid=", value.tid,
                    " data-subject=", value.subject,
                    " data-date=", date,
                    " data-from=", value.from.address,
                    " data-bodypeek=", value.bodypeek,
                    " data-label=", value.labels[0].name,
                    " data-age=", value.age
                ];

                var newCircle = $("<a data-toggle='modal' href='#example'><div class='" + classes + "'" +
                                    dataAttributes.join("'") +
                                    "'><div class='centered'><p>" + value.subject + "</p></div></div></a>");

                $("#email-container").append(newCircle);

                /* Create labels */
                for (var i = 0; i < value.labels.length; i++) {
                    if (value.labels[i].system_name == null)
                        labelColors[value.labels[i].name] = "";
                }

            });
        } else alert(data.message);

    }).done(function () {

        populateLabelColors();
        calculateEmailsColor();
        calculateEmailsPosition();
        hideProgressBar();
        configureCircleHover();
        setModal();

    });
}

function prepareComposeDialog() {

    $("#compose_pannel").dialog({
        autoOpen: false,
        closeOnEscape: true,
        draggable: true,
        height: 400,
        width: 600,
        minWidth: 400,
        minHeight: 200,
        resizable: true,
        title:"Redacta un email",
        position: { my: "left botton", at: "left bottom", of: window },
        buttons: [
        {
            text: "Cerrar",
            click: function () {
                $(this).dialog("close");
            }
        },
        {
            text: "Enviar"
        }
        ]
    });
    $("#compose").on("click", function () {
        $("#compose_pannel").dialog("open");
        editor = CKEDITOR.replace('text_editor');
    });
}

$(document).ready(function () {

    setDateCoords();
    fetchMailsAsync();
    setDragging();
    configureZoom();
    drawGrid();
    setRefreshOnResize();
    prepareComposeDialog();
})

