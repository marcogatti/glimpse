var maxAge = 0;
var containerBorder = parseInt($("#email-container").css("border-width"));

function getContainerHeight() {
    return $("#email-container").height();
}

function getContainerWidth() {
    return $("#email-container").width();
}

function alphabetSize() {
    return "z".charCodeAt(0) - "a".charCodeAt(0) + 2;
}

function clearCanvas() {
    document.getElementById('gridCanvas').getContext('2d').clearRect(0, 0, getContainerWidth(), getContainerHeight())
}

function calculateEmailsPosition() {

    var containerWidth = getContainerWidth();
    var containerHeight = getContainerHeight();

    var offset = parseInt($(".circle").css('width'), 10);

    $(".circle").each(function () {

        var left = $(this).attr('data-age') / maxAge;
        var top = ($(this).attr('data-from').charCodeAt(0) - "a".charCodeAt(0) + 2) / alphabetSize();    

        $(this).css('top', function () {
            return top * (containerHeight - offset) + 'px';
        });

        $(this).css('left', function () {
            return left * (containerWidth - offset) + 'px';
        });
    })
}

function setDateCoords() {
    $(".dateCoord").css("top", getContainerHeight());
}

function setModal() {

    $(".circle").on("click", function () {

        var from = $('<h4>From: ' + $(this).data("from") + '</h4>');
        var body = $('<div class="bodyhtml">' + $(this).data("body") + '</div>');
        var subject = $('<h3>' + $(this).data("subject") + '</h3>');

        $(".modal-body").find("h4").remove();
        $(".modal-body").find(".bodyhtml").remove();
        $(".modal-header").find("h3").remove();

        $(".modal-body").append(from);
        $(".modal-body").append(body);
        $(".modal-header").append(subject);
    });
}

function configureCircleHover() {

    var dateTime = $("#dateTime");
    var from = $("#from");

    $(".circle").hover(

        function () {

            var currentCircle = $(this);

            dateTime.html(currentCircle.data("date"));
            from.html(currentCircle.data("from"));

            $(".hidable").removeClass("hidden").addClass("visible");

            dateTime.css("left",currentCircle.css("left"));

            from.css("top", currentCircle.css("top"));

        }, function () {
            $(".hidable").removeClass("visible").addClass("hidden");
        })

    from.css("left", "-60px");
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

    $.getJSON("async/InboxMails", function (data) {

        if (data.success == true) {

            $.each(data.mails, function (index, value) {

                if (value.age > maxAge) {
                    maxAge = value.age;
                }

                var date = new Date(parseInt(value.date.substr(6))).toLocaleDateString();

                var newCircle = "<a data-toggle='modal' href='#example'><div class='circle'" +
                        " data-subject='" + value.subject +
                        "' data-date='" + date +
                        "' data-from='" + value.from.address +
                        //"' data-body='" + value.body +    // REVISAR!!
                        "' data-age=" + value.age + ">" +
                        "<p class='subject'>" + value.subject + "</p></div></a>"

                $("#email-container").append(newCircle);
            });
        } else alert(data.message);

    }).done(function () {

        setDateCoords();
        hideProgressBar();
        calculateEmailsPosition();
        setRefreshOnResize()
        configureCircleHover();
        setModal();
        
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

        context.strokeStyle = "lightgrey";
        context.stroke();
    }

    drawBoard();
}

$(document).ready(function () {

    fetchMailsAsync();
    drawGrid();
})

