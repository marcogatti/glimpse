var ownedCircles = [];

function insertCircle(value) {

    if (ownedCircles.indexOf(value.id) === -1) {
        ownedCircles.push(value.id);

        if (value.age > maxAge) {
            maxAge = value.age;
        }

        var date = new Date(parseInt(value.date.substr(6))).toGMTString();

        var systemLabels = [],
            customLabels = [];

        for (var i = 0; i < value.labels.length; i++) {
            if (value.labels[i].system_name === null) {
                customLabels.push(value.labels[i].name);
            } else
                if (unwantedSystemLabels.indexOf(value.labels[i].system_name) === -1) {
                    systemLabels.push(value.labels[i].system_name);
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
            " data-mailaccount=", value.mailaccount,
            " data-seen=", value.seen,
            " data-has-attachments=", value.has_attachments
        ];

        var newCircle = $("<div class='circle'" + dataAttributes.join("'") + "' title='" + value.from.address + "'>" +
            "<div class='centered'><p content=true></p></div></div>");

        setSeenStatus(newCircle);
        calculateEmailColor(newCircle);
        newCircle.css("opacity", 0);
        $("#email-container").append(newCircle);

        if (toBeHidden(newCircle, getCurrentMailbox())) {
            newCircle.addClass("hidden");
        }

        setTimeout(function () {
            newCircle.css("opacity", 0.9);
        }, 100);

        setImportance(newCircle);
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

    circle.click(function (event) {
        event.preventDefault();
        event.stopPropagation();

        setMailClicked($(this));

        circle.unbind('click');

        circle.one('click', circle, function (innerEvent) {
            innerEvent.stopPropagation();
            unsetMailClicked(innerEvent.data);
            innerEvent.data.unbind('click');
            setPreviewDisplay(innerEvent.data);
        });

        $("#email-container").one('click', circle, function (innerEvent) {
            innerEvent.stopPropagation();
            unsetMailClicked(innerEvent.data);
            innerEvent.data.unbind('click');
            setPreviewDisplay(innerEvent.data);
        }
        );
    });
}

function rotation(deg) {
    return "-webkit-transform: rotate(" + deg + "deg); transform: rotate(" + deg + "deg);";
}

function removeImportance(circle) {
    circle.removeClass("importance1 importance2 importance3 importance4 importance5");
}
function setImportance(circle) {
    circle.addClass("importance" + circle.data("importance"));
}

function putLabelBalls(circle) {

    var customLabels = getCustomLabels(circle),
    labelsBalls = "";

    circle.find(".label-ball").remove();

    var deg = 135;
    for (var i = 0; i < customLabels.length; i++) {
        labelsBalls += "<div class='radial-button label-ball' title='" + customLabels[i] +
            "' data-label-name='" + customLabels[i] + "' style='" + rotation(deg) +
            " background-color: " + labelColors[customLabels[i]] + ";'></div>";
        deg += 20;
    }

    circle.prepend($(labelsBalls));

    circle.find(".radial-button").on('click dblclick', function (e) {
        e.stopPropagation();
    });

    setQuickLabelRemoval();
}

function putButtons(circle) {

    var buttons = $(
    "<div class='radial-button icon-plus-sign' title='Dar importacia' style='" + rotation(40) + "'></div>" +
    "<div class='radial-button icon-minus-sign' title='Quitar importancia' style='" + rotation(20) + "'></div>" +
    "<div class='radial-button icon-trash' title='Eliminar' style='" + rotation(-20) + "'></div>" +
    "<div class='radial-button icon-comment' title='Responder' style='" + rotation(-40) + "'></div>" +
    "<div class='radial-button icon-eye-close' title='Marcar como no leído' style='" + rotation(0) + "'></div>" +
    "<div class='radial-button icon-gift hidden' title='Este email tiene archivos adjuntos' style='" + rotation(60) + "'></div>"
    );

    circle.prepend(buttons);

    if (!circle.data("seen")) {
        var eye = circle.find(".icon-eye-close");
        toggleEye(eye);
        eye.attr("title", "Marcar como leído");
    }

    if (circle.data("has-attachments")) {
        circle.find(".icon-gift").removeClass("hidden");
    }

    circle.find(".icon-plus-sign").on('click', function () {
        changeImportance($(this).parent(), true);
    });

    circle.find(".icon-minus-sign").on('click', function () {
        changeImportance($(this).parent(), false);
    });

   circle.find(".icon-eye-open").on('click', function () {
        markAsRead(circle, true);
        $(this).attr("title", "Marcar como no leído");
        toggleEye($(this));
   });

    $(".circle > .icon-eye-close").on('click', function () {
        markAsRead(circle, false);
        $(this).attr("title", "Marcar como leído");
        toggleEye($(this));
    });

    $(".circle > .icon-trash").on('click', function () {
        currentCircle = circle;
        deleteCircleWithConfirmationWindow(circle);
    });

    $(".circle > .icon-comment").on('click', function () {
        replyMail(circle);
    });
}

function toggleEye(eyeIcon) {
    eyeIcon.toggleClass("icon-eye-close icon-eye-open");
}
    
function checkIconHide(currentImportance, limit, iconToHide) {
    if (currentImportance === limit) {
        iconToHide.addClass("hidden");
    } else {
        iconToHide.removeClass("hidden");
    }
}

function checkImportanceIconsVisibility(circle) {
    var currentImportance = circle.data("importance");
    checkIconHide(currentImportance, 1, circle.find(".icon-minus-sign"));
    checkIconHide(currentImportance, 5, circle.find(".icon-plus-sign"));
}

function changeSize(circle, step) {
    var currentImportance = circle.data("importance");
    var intVal = step ? 1 : -1;
    var newImportance = Math.min(Math.max(currentImportance + intVal, 1), 5);
    circle.data("importance", newImportance);
    checkImportanceIconsVisibility(circle);
}

function changeImportance(circle, step) {

    changeSize(circle, step);

    $.ajax({
        type: "POST",
        url: "async/ChangeImportance",
        dataType: 'json',
        data: { mailId: circle.data('id'), isIncrease: step, mailAccountId: circle.data("mailaccount") }
    });
}

function setMailClicked(circle) {

    removeImportance(circle);
    circle.attr('mail-clicked', true);
    circle.addClass('previewed');
    circle.find(".loading").css("visibility", "hidden");

    putButtons(circle);
    putLabelBalls(circle);
    checkImportanceIconsVisibility(circle);

    circle.animate({
        width: '150',
        height: '150',
        marginLeft: '-37.5',
        marginTop: '-37.5',
    }, 200);

    circle.find('[content=true]').text(circle.attr('data-bodypeek'));
}

function unsetMailClicked(circle) {

    circle.attr('mail-clicked', false);
    circle.removeClass('previewed');
    circle.find(".loading").css("visibility", "visible");
    circle.find(".radial-button").remove();
    setImportance(circle);

    circle.animate({
        width: '75',
        height: '75',
        marginLeft: '0',
        marginTop: '0'
    }, 200);

    circle.find('[content=true]').text($(circle).attr('data-subject').substr(0, 30));
}

function isClicked(circle) {
    return $(circle).attr('mail-clicked');
}

function configureCircleHover(circle) {

    var dateTime = $("#dateTime"),
        from = $("#from"),
        dateTimeWidth = parseInt($("#from").css("width"));

    circle.hover(

        function () {

            dateTime.html(circle.data("date"));
            from.html(circle.data("from").substr(0, 10) + "...");

            dateTime.css("left", function () {
                return wrapperLeftPadding + parseInt(circle.css("left")) - (dateTimeWidth/2) + 'px';
            });

            from.css({
                "top": function () { return parseInt(circle.css("top")) + parseInt(circle.css("height"))/2 + wrapperVerticalPadding; },
                "left": "0"
            }
            );

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

function markAsRead(circle, seenFlag) {
    circle.data("seen", seenFlag);
    setSeenStatus(circle);
    $.ajax({
        type: "POST",
        url: "async/SetReadFlag",
        data: { mailId: circle.data('id'), seenFlag: seenFlag, mailAccountId: circle.data("mailaccount") }
    });
}

function setSeenStatus(circle) {
    unsetMailClicked(circle);
    if (!circle.data("seen")) {
        var spinner = '<div class="loading"><div class="spinner"><div class="mask"><div class="maskedCircle"></div></div></div></div>';
        circle.prepend(spinner);
    } else {
        circle.find(".loading").remove();
    }
}

function calculateEmailColor(circle) {

    var customLabels = getCustomLabels(circle),
        filteredLabels = [];

    for (var i = 0; i < customLabels.length; i++) {
        if (isActive(customLabels[i])){
            filteredLabels.push(customLabels[i]);
        }
    }    

    var fill = "",
        i;

    for (i = 0; i < filteredLabels.length; i++) {

        if (isActive(filteredLabels[i])) {
            fill += ", ";
            fill += labelColors[filteredLabels[i]];
        }
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

    var left = (circle.data('age') - minAge) / currentPeriodShown(),
        top = (circle.data('from').charCodeAt(0) - "a".charCodeAt(0)) / alphabetSize();

    circle.css('top', function () {
        return top * containerHeight() + 'px';
    });

    circle.css('left', function () {
        return left * containerWidth() + 'px';
    });
}

function surroundingCircles(factor, whatToDo) {
    var containerChunk = parseInt(currentPeriodShown() * factor),
      furthestAgeRight = maxAge + containerChunk,
      furthestAgeLeft = minAge - containerChunk;

    $(".circle").each(function () {
        var currentAge = $(this).attr('data-age');
        if ((currentAge < furthestAgeRight) && (currentAge > furthestAgeLeft)) {
            whatToDo($(this));
        }
    });
}

function calculateEmailsLeft(containerChunk) {

    var r = $.Deferred();
    periodShown = currentPeriodShown();

    surroundingCircles(containerChunk, function (circle) {

        var left = (circle.data("age") - minAge) / periodShown;
        circle.css('left', function () {
            return left * containerWidth() + 'px';
        });
    });

    setTimeout(function () {
        r.resolve();
    }, 300);

    return r;
}

function archiveCircle(circle) {
    removeSystemLabelFromCircle(circle, label_inbox);
    showCircleIfNeedBe(circle);
}

function deleteCircle(circle) {

    circle.remove();
    deleteCircleInServer(circle);
}

function deleteCircleInServer(circle){
    $.ajax({
        type: "POST",
        url: "async/TrashMail",
        dataType: 'json',
        data: { id: circle.data('id'), mailAccountId: circle.data('mailaccount') }
    });
}