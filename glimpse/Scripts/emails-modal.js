var maxAge = 0;

function calculateEmailsPosition() {

    var containerWidth = $("#email-container").width();
    var containerHeight = $("#email-container").height();

    var offset = parseInt($(".circle").css('width'), 10);

    $(".circle").each(function () {

        var left = $(this).attr('data-age') / maxAge;
        var top = ($(this).attr('data-from').charCodeAt(0) - "a".charCodeAt(0)) / 26;    

        $(this).css('top', function () {
            return top * (containerHeight - offset) + 'px';
        });

        $(this).css('left', function () {
            return left * (containerWidth - offset) + 'px';
        });
    })
}

function setDateCoords() {
    $(".dateCoord").css("top", function () {
        return $("#email-container").height();
    });
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

function setRefreshPosition() {
    $(window).resize(function () { calculateEmailsPosition() });
}

function fetchMailsAsync() {

    $.getJSON("async/InboxMails", function (data) {

        if (data.success == true) {

            $.each(data.mails, function (index, value) {

                if (value.age > maxAge) {
                    maxAge = value.age;
                    oldest = new Date(parseInt(value.date.substr(6))).toLocaleDateString();
                }

                var date = new Date(parseInt(value.date.substr(6))).toLocaleDateString();

                var newCircle = "<a data-toggle='modal' href='#example'><div class='circle'"+
                        " data-date=" + date +
                        " data-from='" + value.from.address +
                        "' data-age='" + value.age + "'>" +
                        "<p class='subject'>" + value.subject + "</p></div></a>"

                $("#email-container").append(newCircle);
            });
        } else alert(data.message);

    }).done(function () {

        setDateCoords();
        hideProgressBar();
        calculateEmailsPosition();
        setRefreshPosition()
        configureCircleHover();
        setModal();
        
    });
}

$(document).ready(function () {

    fetchMailsAsync();
})

