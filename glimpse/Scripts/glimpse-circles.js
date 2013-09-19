var ownedCircles = [];

function insertCircle(value) {
    if (value.age > maxAge) {
        maxAge = value.age;
    }

    var date = new Date(parseInt(value.date.substr(6))).toGMTString(),
        classes = "circle transition";

    if (!value.seen) {
        classes += " new";
    }

    var label0, label1, label2;

    if (value.labels[0] !== undefined) {
        label0 = value.labels[0].name;
    }
    if (value.labels[1] !== undefined) {
        label1 = value.labels[1].name;
    }
    if (value.labels[2] !== undefined) {
        label2 = value.labels[2].name;
    }

    var dataAttributes = [
        " data-id=", value.id,
        " data-tid=", value.tid,
        " data-subject=", value.subject,
        " data-date=", date,
        " data-from=", value.from.address,
        " data-bodypeek=", value.bodypeek,
        " data-label0=", label0,
        " data-label1=", label1,
        " data-label2=", label2,
        " data-age=", value.age
    ];

    var newCircle = $("<div class='" + classes + "'" +
                        dataAttributes.join("'") +
                        "'><div class='centered'><p>" + value.subject + "</p></div></div>");

    calculateEmailColor(newCircle);
    $("#email-container").append(newCircle);
    calculateEmailPosition(newCircle);

}

function setCirclePre() {
    $(".circle").click(
        function () {
            if (!isOnPreview($(this))) {
                $(this).addClass("preview");
                $(this).find(".centered").append("<div class='pre'>" + $(this).data("bodypeek") + "</div>");
            } else {
                $(this).find(".pre").remove();
                $(this).removeClass("preview");
                
                var from = 'From: ' + $(this).data("from"),
                    subject = $(this).data("subject"),
                    currentCircle = $(this);

                $(".modal-body").find("h4").html(from);
                $(".modal-header").find("h3").html(subject);

                $(".modal-body").find("#bodyhtml").html("");
                showProgressBar("#body-progress");

                $("#body-modal").modal("show");

                $.getJSON("async/GetMailBody/" + currentCircle.data("id"), function (data) {
                    if (data.success == true) {
                        hideProgressBar("#body-progress");
                        $(".modal-body").find("#bodyhtml").html(data.body);
                        markAsRead(currentCircle);

                    } else alert(data.message);
                });
            }
        })
}

function fetchMailsAsync(initialDate, finalDate) {

    $.getJSON("async/GetMailsByDate?initial=" + initialDate.getTime() + "&final=" + finalDate.getTime(), function(data){
        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                if (ownedCircles.indexOf(value.id) === -1) {
                    ownedCircles.push(value.id);
                    insertCircle(value);
                }              
            });

        } else alert(data.message);

    }).done(function () {

        hideProgressBar("#circles-progress");
        configureCircleHover();
        setCirclePre();
        setDateCoords();

    });
}

function fetchRecentMails() {

    var dateBefore = new Date(),
        dateToday = new Date();
    dateBefore.setDate(dateBefore.getDate() - 30);

    fetchMailsAsync(dateBefore, dateToday);
}

function fetchMailsWithinActualPeriod() {
    fetchMailsAsync(ageToDate(maxAge), ageToDate(minAge));
}


function configureCircleHover() {

    var dateTime = $("#dateTime"),
        from = $("#from");

    $(".circle").hover(

        function () {

            var currentCircle = $(this);

            dateTime.html(currentCircle.data("date"));
            //from.html(currentCircle.data("from"));

            dateTime.css("left", function () {
                return parseInt(currentCircle.css("left")) - 25 + 'px';
            });

            //from.css("top", function () {
            //    return currentCircle.css("top");
            //});

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

function markAsRead(circle) {
    circle.removeClass("new");
}

function calculateEmailColor(circle) {

        var innerColor,
            ringColor,
            outsetColor,
            shadow;

        if (circle.data('label0') !== "") {
            innerColor = labelColors[circle.data('label0')];
        }
        if (circle.data('label1') !== "") {
            ringColor = labelColors[circle.data('label1')];
            shadow = 'inset 0 0 0 12px ' + ringColor;
        }

        shadow += ', 0 0 0 8px ';

        if (circle.data('label2') !== "") {
            outsetColor = labelColors[circle.data('label2')];
            shadow += outsetColor;
        }

        circle.css({
            'color': innerColor,
            'background-color': innerColor,
            'box-shadow': shadow,
            '-webkit-box-shadow': shadow,
        });
}

function calculateEmailPosition(circle) {

    var margin = parseInt(circle.css('width'), 10);

        var left = (circle.attr('data-age') - minAge) / currentPeriodShown(),
            top = (circle.attr('data-from').charCodeAt(0) - "a".charCodeAt(0) + 2) / alphabetSize();

        circle.css('top', function () {
            return top * (containerHeight() - margin) + 'px';
        });

        circle.css('left', function () {
            return left * (containerWidth() - margin) + 'px';
        });
}

function calculateEmailsLeft() {

    var margin = parseInt($(".circle").css('width'), 10);

    $(".circle").each(function () {

        var left = ($(this).attr('data-age') - minAge) / currentPeriodShown();

        $(this).css('left', function () {
            return left * (containerWidth() - margin) + 'px';
        });


    });
}

function setLabelSelection() {
    $(".label-glimpse").on('click', function () {
        $(this).toggleClass('label-hidden');
        var currentLabel = $(this).html();

        $(".circle").each(function () {
            if (hasLabel($(this), currentLabel)) {
                $(this).toggleClass("hidden");
            }
        });
    });
}

function hasLabel(circle, label) {
    return ([circle.data("label0"), circle.data("label1"), circle.data("label2")].indexOf(label) != -1);
}
