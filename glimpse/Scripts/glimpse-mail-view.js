var currentCircle;

function initializeMailViewModal() {

    $('.modal-footer').mousedown(function (ev) {
        ev.preventDefault();
    });

    setAddressesDisplayer();

    setMailTraversingArrows();

    $('#mail-view-address-displayer').click(function (ev) {
        ev.stopPropagation();
        $(this).popover('show');
    });

    $(document).click(function () {
        $('#mail-view-address-displayer').popover('hide');
    });

    $("#mail-view-archive").click(function () {
        archiveCircle(currentCircle);
        $("#mail-view").modal('hide');
    });

    $("#mail-view-delete").click(function () {
        deleteCircleWithConfirmationWindow(currentCircle);
    });

    $('#deletion-confirmed').click(function () {
        deleteCircle(currentCircle);
        $("#mail-view").modal('hide');
    });
}

function setFullDisplay(circle) {

    circle.dblclick(

        function () {

            currentCircle = circle;

            var view_modal = $("#mail-view");
            from = circle.data("from"),
            subject = circle.data("subject"),
            cc = circle.data("cc"),
            to = circle.data("to"),
            date = new Date(circle.data('date')),
            ccContainer = $("#mail-view-cc-container");

            setViewMailBody(view_modal, '');
            view_modal.find("#mail-view-from").html(from);
            $("#mail-view-to").html(to);
            if (cc == "" || cc === null) {
                ccContainer.addClass('hidden');
            } else {
                ccContainer.removeClass('hidden');
                $("#mail-view-cc").html(cc);
            }
            view_modal.find("#mail-view-subject").html(subject);
            view_modal.find("#mail-view-date").html(date.toLocaleString());

            showProgressBar("#body-progress");

            view_modal.modal("show");

            $.getJSON("async/GetMailBody/" + circle.data("id"), function (data) {
                hideProgressBar("#body-progress");
                if (data.success == true) {
                    setViewMailBody(view_modal, data.mail.body);
                    setViewMailAttachments(view_modal, data.mail.extras)
                    markAsRead(circle);
                    setMailViewerActions(view_modal, circle, data.mail.body);

                } else alert(data.message);
            });
        });
}

function setViewMailAttachments(view_modal, attachments) {

    var listItem,
        attachmentsUL,
        attachmentsContainer;

    attachmentsContainer = view_modal.find("#mail-view-attachments");
    attachmentsContainer.html('');

    if (attachments.length === 0) {
        view_modal.find('#mail-view-attach-container').addClass('hidden');
        attachmentsContainer.addClass('hidden');
        return;
    } else {
        view_modal.find('#mail-view-attach-container').removeClass('hidden');
        attachmentsContainer.removeClass('hidden');
    }

    attachmentsUL = $('<ul class="list-group"></ul>');

    for (var i = 0; i < attachments.length; i++) {

        listItem = $('<li class="list-group-item"></li>');

        listItem.append($('<a href="/async/getfile/' + attachments[i].id + '">' + attachments[i].name + '    </a>'));
        listItem.append($('<span class="badge">' + Math.floor(attachments[i].size / 1024) + ' Kb</span>'));

        attachmentsUL.append(listItem);
    }

    attachmentsContainer.append(attachmentsUL);
}

function setViewMailBody(view_modal, body) {
    view_modal.find("#mail-view-body").html(body);
}

function setMailViewerActions(view_modal, circle, body) {

    var data = { circle: circle, body: body }

    view_modal.find(".mail-reply").one("click", data, function (event) {

        var circle = event.data.circle;

        $("#email-to").html(circle.data('from'));

        setConversationOfMail(circle, event.data.body, 'RE: ');

        displayComposeDialog();
    }
    );

    view_modal.find(".mail-replyall").one("click", data, function (event) {

        var circle = event.data.circle;

        $("#email-to").html(circle.data('from') + ', ' + circle.data('to') + ', ' + circle.data('cc'));

        setConversationOfMail(circle, event.data.body, 'RE: ');

        displayComposeDialog();
    }
);

    view_modal.find(".mail-forward").one("click", data, function (event) {

        var circle = event.data.circle;

        $("#email-to").html('');

        setConversationOfMail(circle, event.data.body, 'FW: ');

        displayComposeDialog();
    }
);
    setMailViewerLabels(view_modal, circle);
}

function setAddressesDisplayer() {

    var options =
    {
        html: true,
        placement: 'bottom',
        trigger: 'manual',
        content: function () {
            return $('#addresses-content').html();
        }
    };

    $('#mail-view-address-displayer').popover(options);
}

function setMailViewerLabels(view_modal, circle) {

    var labels = getCustomLabels(circle),
        labelElement;

    $('#mail-view-labels').html('');

    for (var i = 0; i < labels.length && i < 5; i++) {

        labelElement = $('.custom-label[data-name="' + labels[i] + '"]').clone();
        prepareLabelForMailView(labelElement);
        view_modal.find('#mail-view-labels').append(labelElement);
    }
}

function prepareLabelForMailView(labelElement) {

    var timeoutId;

    labelElement.addClass('mail-view-labels');
    labelElement.removeAttr('draggable');
    labelElement.attr('title', labelElement.data('name'));

    labelElement.mousedown(function (ev) {
        ev.preventDefault();
        timeoutId = setTimeout(function () {
            removeLabelFromMailView(labelElement);
            removeLabelFromCircle(currentCircle, labelElement.data('name'));
        }, 1000);
    }).bind('mouseup mouseleave', function () {
        clearTimeout(timeoutId);
    });
}

function removeLabelFromMailView(label) {
    label.remove();
}

function setMailTraversingArrows() {

    var view_modal = $('#mail-view');

    view_modal.find("#mail-goback").on("click", function (event) {
        moveToFollowingMail(view_modal, currentCircle, getFollowingMailBackward);
    }
    );

    view_modal.find("#mail-goforw").on("click", function (event) {
        moveToFollowingMail(view_modal, currentCircle, getFollowingMailForward);
    }
    );

}

function moveToFollowingMail(view_modal, circle, nextMailSearcher) {
    var nextCircle;

    nextCircle = nextMailSearcher(circle);

    if (nextCircle.data('id') === circle.data('id') &&
        nextCircle.data('mailaccount') === circle.data('mailaccount')) {
        return false;
    }

    currentCircle = nextCircle;

    setMailViewModalHeadData(view_modal, nextCircle);
    setMailViewModalBodyData(view_modal, nextCircle);

    return true;
}

function getFollowingMailForward(circle) {
    return getFollowingMail(circle, criteriaForward);
}

function getFollowingMailBackward(circle) {
    return getFollowingMail(circle, criteriaBackward);
}

function followingMailWithCriteria(circle, circle_collection, criteria) {

    var current_id = circle.data('id'),
    current_age = circle.data('age'),
    next_circle,
    actual_circle;

    for (var i = 0; i < circle_collection.length; i++) {

        actual_circle = $(circle_collection[i]);

        if ((actual_circle.data('id') !== current_id) &&
            criteria(current_id, current_age, actual_circle, next_circle)) {
            next_circle = actual_circle;
        }
    }

    return next_circle != null ? next_circle : circle;
}

function criteriaBackward(current_id, current_age, actual_circle, next_circle) {
    return (actual_circle.data('age') <= current_age) &&
           ((next_circle == null) || (actual_circle.data('age') > next_circle.data('age')))
}

function criteriaForward(current_id, current_age, actual_circle, next_circle) {
    return (actual_circle.data('age') >= current_age) &&
           ((next_circle == null) || (actual_circle.data('age') < next_circle.data('age')))
}

function getFollowingMail(circle, followingCriteria) {
    var tid, mailaccount_id, circles_in_thread, nextCircle;

    mailaccount_id = circle.data('mailaccount');
    tid = circle.data('tid');

    circles_in_thread = $('.circle[data-mailaccount="' + mailaccount_id + '"][data-tid=' + tid + ']');

    nextCircle = followingMailWithCriteria(circle, circles_in_thread, followingCriteria);

    return nextCircle;
}

function setMailViewModalHeadData(view_modal, circle) {

    var ccContainer = $("#mail-view-cc-container"),
        data = getCircleData(circle),
        date = new Date(data.date);


    view_modal.find("#mail-view-from").html(data.from);
    $("#mail-view-to").html(data.to);
    if (data.cc == "" || data.cc === null) {
        ccContainer.addClass('hidden');
    } else {
        ccContainer.removeClass('hidden');
        $("#mail-view-cc").html(data.cc);
    }
    view_modal.find("#mail-view-subject").html(data.subject);
    view_modal.find("#mail-view-date").html(date.toLocaleString());
}

function setMailViewModalBodyData(view_modal, circle) {

    setViewMailBody(view_modal, '');

    showProgressBar("#body-progress");

    $.getJSON("async/GetMailBody/" + circle.data('id'), function (data) {
        hideProgressBar("#body-progress");
        if (data.success == true) {
            setViewMailBody(view_modal, data.mail.body);
            setViewMailAttachments(view_modal, data.mail.extras)
            markAsRead(circle);
            setMailViewerActions(view_modal, circle, data.mail.body);

        } else alert(data.message);
    });
}

function setConversationOfMail(circle, body, subjectPrefix) {

    var subject = circle.data('subject');

    if (subjectPrefix !== undefined) {
        subject = subjectPrefix + subject;
    }

    $("#email-subject").html(subject);
    setMailEditorText('<br /> <blockquote>' + body + '</blockquote>');
}

function deleteCircleWithConfirmationWindow(circle) {

    var currentLabels = getSystemLabels(circle);

    if (currentLabels.indexOf(label_trash) != -1) {
        $('#deletion-confirmation').modal();
        return;
    }

    addSystemLabel(circle, label_trash);

    deleteCircleInServer(circle);

    $("#mail-view").modal('hide');
}