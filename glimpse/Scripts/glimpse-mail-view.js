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
                    setMailViewerActions(view_modal, circle, data.mail.body);

                } else alert(data.message);
            });
        });
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
}

function setConversationOfMail(circle, body, subjectPrefix) {

    var subject = circle.data('subject');

    if (subjectPrefix !== undefined) {
        subject = subjectPrefix + subject;
    }

    $("#email-subject").html(subject);
    setMailEditorText('<br /> <blockquote>' + body + '</blockquote>');
}