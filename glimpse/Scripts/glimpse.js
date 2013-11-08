$(document).ready(function () {

    preventSelectingNotUsefulThings();
    loadLabels();
    setDateCoordsPosition();
    populateLabelColors();
    fetchRecentMails();
    setDragging();
    configureZoom();
    setRefreshOnResize();
    prepareComposeDialog();
    setEverithingRelatedToAddLabelsToAMail();
    initializeMailEditor();
    initializeMailViewModal();
    initializeMainDropdownMenuActions();
    initializeMailAccountToggles();
    initializeAboutUSModal();
    checkMailAccountCredentials();
    prepareLabelsEditor();
    initializeGenericConfirmationModal();
    setSincronizeAccountsCaller();
})

function initializeAboutUSModal() {

    $('#about-us-trigger').click(function () {
        $('#about-us-modal').modal('show');
    });
}

function checkMailAccountCredentials() {

    var configView;

    if (user_accounts_errors !== '') {

        configView = $('#config-view');

        configView.find('#config-mailaccount').trigger('click');

        configView.find('#config-personaldata').addClass('hidden');
        configView.find('#config-password').addClass('hidden');

        configView.find('.close').addClass('hidden');

        configView.find('.modal-header').find('h4').html('Debes actualizar los datos de tus cuentas para seguir utilizando Glimpse');

        configView.modal('show');

        alert('Atencion: ' + user_accounts_errors);
    }

}

function initializeGenericConfirmationModal() {
    $('#confirmation-modal').modal({
        backdrop: 'static',
        show: false
    });
}

function setSincronizeAccountsCaller() {

    window.setInterval(function () { // Horrible, solo para la presentacion en la facu

        $.ajax({
            type: "POST",
            url: "async/SynchronizeAccount",
        });

        fetchMailsWithinActualPeriod();
        var i = 0;
        for (i = 0; i < reallyOwnedCircles.length; i++) {
            reallyOwnedCircles[i].data()["age"] += 10;
        }

    }, 15000);

    window.setInterval(function () {
        fetchRecentMails();
    }, 5000);




}