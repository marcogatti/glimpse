
function fetchMailsAsync(initialDate, finalDate) {
    
    $.getJSON("async/GetMailsByDate", {

            initial: initialDate.getTime(),
            final: finalDate.getTime()

        }, function (data) {

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                insertCircle(value);
            });

        } else console.log(data.message);

    });
}

function fetchRecentMails(onComplete) {

    $.getJSON("async/GetMailsByAmount", {

        amountOfMails: 10

    }, function (data) {

        if (data.mails.length === 0) {
            //  dos semanas (en segundos)
            maxAge = minAge + 60*60*24*14;
        }

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                insertCircle(value);
            });

        } else console.log(data.message);

    }).done(function () {

        if (onComplete != null) {
            onComplete();
        }
    });
}

function fetchMailsWithinActualPeriod() {
    fetchMailsAsync(ageToDate(maxAge), ageToDate(minAge));
}

function firstTimeFetching() {

    setDateCoords();
    calculateEmailsLeft(1);
    defaultZoom = currentPeriodShown();
}
