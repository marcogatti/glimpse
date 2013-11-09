
function fetchMailsAsync(initialDate, finalDate) {
    
    $.getJSON("async/GetMailsByDate", {

            initial: initialDate.getTime(),
            final: finalDate.getTime()

        }, function (data) {

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                insertCircle(value);
            });

        } else alert(data.message);

    });
}

function fetchRecentMails() {

    $.getJSON("async/GetMailsByAmount", {

        amountOfMails: 10

    }, function (data) {

        if (data.mails.length === 0) {
            //  dos semanas (en segundos)
            maxAge = minAge + 1209600;
        }

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                insertCircle(value);
            });

        } else alert(data.message);

    }).done(function () {

        setDateCoords();
        calculateEmailsLeft(1);

    });
}

function fetchMailsWithinActualPeriod() {
    fetchMailsAsync(ageToDate(maxAge), ageToDate(minAge));
}
