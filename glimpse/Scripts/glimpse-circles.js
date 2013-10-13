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

        classes += " importance" + value.importance;

        if (!value.seen) {
            classes += " new";
        }

        var systemLabels = [],
            customLabels = [];

        for (var i = 0; i < value.labels.length; i++) {
            if (value.labels[i].system_name === null) {
                customLabels.push(value.labels[i].name);
            } else
                if (unwantedSystemLabels.indexOf(value.labels[i].system_name) === -1) {
                    systemLabels.push(value.labels[i].name);
                }
        }

        var dataAttributes = [
            " data-id=", value.id,
            " data-tid=", value.tid,
            " data-subject=", value.subject,
            " data-date=", date,
            " data-from=", value.from.address,
            " data-bodypeek=", value.bodypeek,
            " data-age=", value.age,
            " data-cc=", value.cc,
            " data-bcc=", value.bcc,
            " data-to=", value.to,
            " data-importance=", value.importance,
            " data-custom-labels=", customLabels,
            " data-system-labels=", systemLabels,
            " data-mailaccount=", value.mailaccount
        ];

        var newCircle = $("<div class='" + classes + "'" + dataAttributes.join("'") + "' title='" + value.from.address +
                            "'><div class='centered'><p content=true>" + value.subject.substr(0, 50) + "</p></div></div>");

        calculateEmailColor(newCircle);
        newCircle.css("opacity", 0);
        $("#email-container").append(newCircle);

        if (toBeHidden(newCircle)) {
            newCircle.addClass("hidden");
        }

        setTimeout(function () {
            newCircle.css("opacity", 0.9);
        }, 100);

        calculateEmailPosition(newCircle);
        prepareToReceiveLabels(newCircle);
        setPreviewDisplay(newCircle);
        setFullDisplay(newCircle);
        configureCircleHover(newCircle);
    }
}

function getCircleData(circle) {
    var circleData = new Object;

    circleData.id = circle.data('id');
    circleData.tid = circle.data('tid');
    circleData.subject = circle.data('subject');
    circleData.date = circle.data('date');
    circleData.address = circle.data('address');
    circleData.bodypeek = circle.data('bodypeek');
    circleData.age = circle.data('age');
    circleData.cc = circle.data('cc');
    circleData.bcc = circle.data('bcc');
    circleData.to = circle.data('to');
    circleData.from = circle.data('from');
    circleData.importance = circle.data('importance');
    circleData.customlabels = circle.data('customlabels');
    circleData.systemlabels = circle.data('systemlabels');
    circleData.mailaccount = circle.data('mailaccount');

    return circleData;
}

function setPreviewDisplay(circle) {

    $(circle).click(function (event) {
        event.preventDefault();
        event.stopPropagation();

        setMailClicked(this);

        $(circle).unbind('click');

        $(circle).one('click', circle, function (innerEvent) {
            innerEvent.stopPropagation();
            unsetMailClicked(innerEvent.data);
            innerEvent.data.unbind('click');
            setPreviewDisplay(innerEvent.data);
        }
        );

        $(document).one('click', circle, function (innerEvent) {
            innerEvent.stopPropagation();
            unsetMailClicked(innerEvent.data);
            innerEvent.data.unbind('click');
            setPreviewDisplay(innerEvent.data);
        }
        );

    }
    );

}

function setMailClicked(circle) {

    $(circle).attr('mail-clicked', true);
    $(circle).addClass('previewed');

    $(circle).animate({
        width: '150',
        height: '150',
        marginLeft: '-37.5',
        marginTop: '-37.5',
    }, 200);

    $(circle).find('[content=true]').text($(circle).attr('data-bodypeek'));
}

function unsetMailClicked(circle) {

    $(circle).attr('mail-clicked', false);
    $(circle).removeClass('previewed');

    $(circle).animate({
        width: '75',
        height: '75',
        marginLeft: '0',
        marginTop: '0'
    }, 200);

    $(circle).find('[content=true]').text($(circle).attr('data-subject'));
}

function isClicked(circle) {
    return $(circle).attr('mail-clicked');
}

function configureCircleHover(circle) {

    var dateTime = $("#dateTime"),
        from = $("#from");

    circle.hover(

        function () {

            dateTime.html(circle.data("date"));
            from.html(circle.data("from").substr(0, 10) + "...");

            dateTime.css("left", function () {
                return parseInt(circle.css("left")) - 25 + 'px';
            });

            from.css("top", function () {
                return circle.css("top");
            });

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

    var customLabels = getCustomLabels(circle);

    var fill = "",
        i;

    for (i = 0; i < customLabels.length; i++) {

        fill += ", ";
        fill += labelColors[customLabels[i]];
    }

    //  hack para que se muestren bien los mails con un solo label
    if (i === 1) {
        fill += fill;
    }

    if (i !== 0) {
        circle.css('background', '-webkit-radial-gradient(circle' + fill + ')');
    } else {
        circle.css('background', '');
    }
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

function surroundingCircles(factor, whatToDo) {
    var containerChunk = parseInt(currentPeriodShown() * factor),
      furthestAgeRight = maxAge + containerChunk,
      furthestAgeLeft = minAge - containerChunk;

    $(".circle").each(function () {
        var currentAge = $(this).attr('data-age');
        if (currentAge < furthestAgeRight && currentAge > furthestAgeLeft) {
            whatToDo($(this));
        }
    });
}

function calculateEmailsLeft(containerChunk) {

    var r = $.Deferred();

    var margin = parseInt($(".circle").css('width'), 10),
        periodShown = currentPeriodShown();

    surroundingCircles(containerChunk, function (circle) {

        var left = (circle.data("age") - minAge) / periodShown;
        circle.css('left', function () {
            return left * (containerWidth() - margin) + 'px';
        });
    });

    setTimeout(function () {
        r.resolve();
    }, 600);

    return r;
}
