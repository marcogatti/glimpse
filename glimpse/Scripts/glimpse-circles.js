var ownedCircles = [];

function insertCircle(value) {

    if (ownedCircles.indexOf(value.id) === -1) {
        ownedCircles.push(value.id);

        if (value.age > maxAge) {
            maxAge = value.age;
        }

        var date = new Date(parseInt(value.date.substr(6))).toGMTString(),
            classes = "circle";

        if (!$("#transitions-check").prop("checked")) {
            classes += " transition";
        }

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
            " data-age=", value.age,
        ];

        var newCircle = $("<div class='" + classes + "'" +
                            dataAttributes.join("'") +
                            "'><div class='centered'><p>" + value.subject + "</p></div></div>");

        calculateEmailColor(newCircle);
        $("#email-container").append(newCircle);
        calculateEmailPosition(newCircle);
        prepareToReceiveLabels(newCircle);
        setFullDisplay(newCircle);
        configureCircleHover(newCircle);
        newCircle.popover({
            "placement": "left",
            "trigger": "hover",
            "content": value.bodypeek,
            "title": value.from.address
        });
    }
}

function setFullDisplay(circle) {
    circle.click(
        function () {

                var from = 'From: ' + circle.data("from"),
                    subject = circle.data("subject");

                $(".modal-body").find("h4").html(from);
                $(".modal-header").find("h3").html(subject);

                $(".modal-body").find("#bodyhtml").html("");
                showProgressBar("#body-progress");

                $("#body-modal").modal("show");

                $.getJSON("async/GetMailBody/" + circle.data("id"), function (data) {
                    hideProgressBar("#body-progress");
                    if (data.success == true) {
                        $(".modal-body").find("#bodyhtml").html(data.body);
                        markAsRead(circle);

                    } else alert(data.message);
                });
            }
        )
}

function configureCircleHover(circle) {

    var dateTime = $("#dateTime"),
        from = $("#from");

    circle.hover(

        function () {

            dateTime.html(circle.data("date"));
            //from.html(circle.data("from"));

            dateTime.css("left", function () {
                return parseInt(circle.css("left")) - 25 + 'px';
            });

            //from.css("top", function () {
            //    return circle.css("top");
            //});

            $(".hidable").removeClass("hidden");

            circle.addClass("selected");

            var currentTid = circle.data("tid");

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

    var margin = parseInt($(".circle").css('width'), 10);

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
    var containerChunk = parseInt(currentPeriodShown() / 2),
        furthestAgeRight = maxAge + containerChunk,
        furthestAgeLeft = minAge - containerChunk;

    $(".circle").each(function () {
        var currentAge = $(this).attr('data-age');

        if (currentAge < furthestAgeRight && currentAge > furthestAgeLeft) {

            var left = (currentAge - minAge) / currentPeriodShown();

            $(this).css('left', function () {
                return left * (containerWidth() - margin) + 'px';
            });
        }


    });
}
