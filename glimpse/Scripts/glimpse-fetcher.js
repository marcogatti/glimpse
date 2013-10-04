
function fetchMailsAsync(initialDate, finalDate) {

    showProgressBar("#circles-progress");

    $.getJSON("async/GetMailsByDate?initial=" + initialDate.getTime() + "&final=" + finalDate.getTime(), function (data) {

        hideProgressBar("#circles-progress");

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                insertCircle(value);
            });

        } else alert(data.message);

    });
}

function fetchRecentMails() {

    showProgressBar("#circles-progress");

    $.getJSON("async/GetMailsByAmount?amountOfMails=15", function (data) {

        hideProgressBar("#circles-progress");

        if (data.mails.length === 0) {
            maxAge = minAge + 5000000000000;
        }

        if (data.success === true) {

            $.each(data.mails, function (index, value) {

                insertCircle(value);
            });

        } else alert(data.message);

    }).done(function () {

        setDateCoords();
        calculateEmailsLeft();

    });
}

function fetchMailsWithinActualPeriod() {
    fetchMailsAsync(ageToDate(maxAge), ageToDate(minAge));
}

function setAutomaticFetching() {
    setInterval(function () { fetchMailsWithinActualPeriod(); }, 3000);
}
