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
            " data-cc=", value.cc,
            " data-bcc=", value.bcc,
            " data-to=", value.to,
            " data-importance=", value.importance
        ];

        var newCircle = $("<div class='" + classes + "'" +
                            dataAttributes.join("'") +
                            "'><div class='centered'><p content=true>" + value.subject + "</p></div></div>");

        calculateEmailColor(newCircle);
        newCircle.css("opacity", 0);
        $("#email-container").append(newCircle);

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

    $(circle).find('[content|=true]').text($(circle).attr('data-bodypeek'));
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

    $(circle).find('[content|=true]').text($(circle).attr('data-subject'));
}

function isClicked(circle) {
    return $(circle).attr('mail-clicked');
}

function setFullDisplay(circle) {
    circle.dblclick(
        function () {

            var view_modal = $("#mail-view");
            from = 'From: ' + circle.data("from"),
            subject = circle.data("subject"),
            cc = 'CC: ' + circle.data("cc"),
            to = 'To: ' + circle.data("to");

            view_modal.find("#mail-view-from").html(from);
            view_modal.find("#mail-view-to").html(to);
            view_modal.find("#mail-view-cc").html(cc);
            view_modal.find("#mail-view-subject").html(subject);

            showProgressBar("#body-progress");

            view_modal.modal("show");

            $.getJSON("async/GetMailBody/" + circle.data("id"), function (data) {
                hideProgressBar("#body-progress");
                if (data.success == true) {
                    view_modal.find("#mail-view-body").html(data.mail.body);
                    markAsRead(circle);

                } else alert(data.message);
            });
        });
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

    var innerColor = '',
        midColor = '',
        outsetColor = '';

    if (circle.data('label0') !== "") {
        innerColor = labelColors[circle.data('label0')];

        //  para que se muestren bien los de único label
        midColor = ', ' + innerColor;
        circle.css('color', innerColor);
    }

    if (circle.data('label1') !== "") {
        midColor = ', ';
        midColor += labelColors[circle.data('label1')];
    }

    if (circle.data('label2') !== "") {
        outsetColor = ', ';
        outsetColor += labelColors[circle.data('label2')];
    }

    circle.css('background', '-webkit-radial-gradient(circle, ' + innerColor + midColor + outsetColor + ')');

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
