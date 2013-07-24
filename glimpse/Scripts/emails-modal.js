$(document).ready(function () {

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
    })


    //$(".circle").on("mouseenter", function () {
    //    $(this).css('left', function (index) { return $(this).attr("data-age") + 'px' })
    //})
})

function calculateEmailsPosition(maxAge) {
    $(".circle").each(function () {

        $(this).attr('data-age', function () {
            return $(this).attr('data-age') / maxAge;
        });

        $(this).css('left', function (index) { return $(this).attr('data-age') * 700 + 'px' });
    })
}

