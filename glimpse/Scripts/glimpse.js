$(document).ready(function () {

    preventSelectingNotUsefulThings();
    loadLabels();
    setDateCoordsPosition();
    populateLabelColors();
    fetchRecentMails();
    setAutomaticFetching();
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