var maxAge = 0;
var minAge = 0;
var containerBorder = parseInt($("#email-container").css("border-width"));

var labelColors = {};
var RGBaColors = [
    "rgba(255, 105, 0, 0.7)",
    "rgba(32, 178, 170, 0.7)",
    "rgba(160, 32, 240, 0.7)",
    "rgba(50, 205, 50, 0.7",
    "rgba(123, 104, 238, 0.7)",
    "rgba(255, 99, 71, 0.7"

];

function getContainerHeight() {
    return $("#email-container").height();
}

function getContainerWidth() {
    return $("#email-container").width();
}

function currentPeriod() {
    return maxAge - minAge;
}

function alphabetSize() {
    return "z".charCodeAt(0) - "a".charCodeAt(0) + 2;
}

function clearCanvas() {
    document.getElementById('gridCanvas').getContext('2d').clearRect(0, 0, getContainerWidth(), getContainerHeight())
}

function populateLabelColors() {

    var i = 0;
    for (var label in labelColors) {
        var currentColor = RGBaColors[i];
        labelColors[label] = currentColor;

         /* Armar listado de labels */
        var labelItem = $("<li style = 'font-weight: bold; color: " + currentColor + "'>" + label + "</li>");
        $("#labels").append(labelItem);

        i++;
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

function calculateEmailsPosition() {

    var containerWidth = getContainerWidth();
    var containerHeight = getContainerHeight();

    var offset = parseInt($(".circle").css('width'), 10);

    $(".circle").each(function () {

        var left = ($(this).attr('data-age') - minAge) / (maxAge - minAge);
        var top = ($(this).attr('data-from').charCodeAt(0) - "a".charCodeAt(0) + 2) / alphabetSize();    

        $(this).css('top', function () {
            return top * (containerHeight - offset) + 'px';
        });

        $(this).css('left', function () {
            return left * (containerWidth - offset) + 'px';
        });

       
    })
}

function zoom(factor) {
    maxAge -= currentPeriod() * factor;
    minAge += currentPeriod() * factor;
    calculateEmailsPosition();
}

function setDragging() {

    var startX, endX = 0;
    var isDragging = false;

    $("#email-container")
    .mousedown(function (e) {
        startX = e.pageX;
        $(window).mousemove(function () {
            isDragging = true;
            $(window).unbind("mousemove");
        });
    });

    $("body")
    .mouseup(function (e) {
        endX = e.pageX;
        var wasDragging = isDragging;
        isDragging = false;
        $(window).unbind("mousemove");
        if (wasDragging) { //was clicking
            var offset = (startX - endX) * currentPeriod() / 1000;
            minAge += offset;
            maxAge += offset;
            calculateEmailsPosition();
        }
    });
}

function setDateCoords() {
    $(".dateCoord").css("top", function () {
        return parseInt(getContainerHeight()) - parseInt($(".dateCoord").css("line-height")) + 'px';
    });
}

function setModal() {

    $(".circle").on("click", function () {

        var from = $('<h4>From: ' + $(this).data("from") + '</h4>');
        var subject = $('<h3>' + $(this).data("subject") + '</h3>');

        $(".modal-body").find("h4").remove();
        $(".modal-body").find(".bodyhtml").remove();
        $(".modal-header").find("h3").remove();

        $(".modal-body").append(from);
        $(".modal-header").append(subject);

        $.getJSON("async/GetMailBody/" + $(this).data("id"), function (data) {
            if (data.success == true) {
                $(".modal-body").append("<div class='bodyhtml'>" + data.body + "</div>");          
            } else alert(data.message);
        });
    });
}

function fetchMailBody(mailId) {

    $.getJSON("async/GetMailBody/" + mailId, function (data) {
        if (data.success == true) {
            return data.body;
        } else alert(data.message);
    });

}

function configureZoom() {

    var factor = 0.2;
    $('#zoom-in').click(function () { zoom(factor); return false; });
    $('#zoom-out').click(function () { zoom(-factor); return false; });
}

function configureCircleHover() {

    var dateTime = $("#dateTime");
    var from = $("#from");

    $(".circle").hover(

        function () {

            var currentCircle = $(this);

            dateTime.html(currentCircle.data("date"));
            from.html(currentCircle.data("from"));

            $(".hidable").removeClass("hidden");

            dateTime.css("left", function () {
                return currentCircle.css("left");
            });

            from.css("top", function () {
                return currentCircle.css("top");
            });

            currentCircle.addClass("selected");

            var currentTid = currentCircle.data("tid");

            $('.circle').each(
                function () {
                    if ($(this).data("tid") == currentTid) {
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

        if (data.success == true) {

            $.each(data.mails, function (index, value) {

                if (value.age > maxAge) {
                    maxAge = value.age;
                }

                var classes = "circle";

                if (!value.seen) {
                    classes += " new";
                }

                var date = new Date(parseInt(value.date.substr(6))).toLocaleDateString();

                var newCircle = $("<a data-toggle='modal' href='#example'><div class='" + classes +
                        "' data-id='" + value.id +
                        "' data-tid='" + value.tid +
                        "' data-subject='" + value.subject +
                        "' data-date='" + date +
                        "' data-from='" + value.from.address +
                        "' data-bodypeek='" + value.bodypeek +
                        "' data-label='" + value.labels[0].name +
                        "' data-age=" + value.age + ">" +
                        "<div class='centered'><p>" + value.subject + "</p></div></div></a>");

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
        setDateCoords();
        hideProgressBar();
        setRefreshOnResize();
        configureCircleHover();
        setModal();
        setDragging();
    });
}

function drawGrid() {

    clearCanvas();

    
    //grid width and height
    var bw = getContainerWidth() - containerBorder;
    var bh = getContainerHeight() - containerBorder;
    //padding around grid
    var p = 0;
    //size of canvas
    var cw = bw;
    var ch = bh;

    var canvas = $('canvas').attr({ width: cw, height: ch });

    var context = canvas.get(0).getContext("2d");
    
    function squareSize() {
        return getContainerHeight() / alphabetSize();
    }

    function drawBoard() {
        for (var x = 0; x <= bw; x += squareSize()) {
            context.moveTo(0.5 + x + p, p);
            context.lineTo(0.5 + x + p, bh + p);
        }


        for (var x = 0; x <= bh; x += squareSize()) {
            context.moveTo(p, 0.5 + x + p);
            context.lineTo(bw + p, 0.5 + x + p);
        }

        context.strokeStyle = "#CDCDCD";
        context.stroke();
    }

    drawBoard();
}

$(document).ready(function () {

    configureZoom();
    fetchMailsAsync();
    drawGrid();
})

