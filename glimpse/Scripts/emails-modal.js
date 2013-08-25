﻿
function calculateEmailsPosition() {

    var containerWidth = $("#email-container").width();
    var containerHeight = $("#email-container").height();

    $(".circle").each(function () {

        var maxSize = parseInt($(this).css('max-width'), 10);
        var left = $(this).attr('data-age') / maxAge;
        var top = ($(this).attr('data-from').charCodeAt(0) - "a".charCodeAt(0)) / 26;    

        $(this).css('top', function () {
            return top * (containerHeight - (maxSize * 0.6)) + 'px';
        });

        $(this).css('left', function () {
            return left * (containerWidth - (maxSize * 0.6)) + 'px';
        });
    })
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

            $(".coord").removeClass("hidden").addClass("visible");

            dateTime.css("left",currentCircle.css("left"));

            from.css("top", currentCircle.css("top"));

        }, function () {
            $(".coord").removeClass("visible").addClass("hidden");
        })

    dateTime.css("top", function () {
        return $("#email-container").height();
    });

    from.css("left", "-60px");
}

$(document).ready(function () {

    setModal();
    calculateEmailsPosition();
    $(window).resize(function () { calculateEmailsPosition() })
    configureCircleHover();

})

