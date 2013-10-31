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
})

function initializeAboutUSModal() {

    $('#about-us-trigger').click(function () {
        $('#about-us-modal').modal('show');
    });
}
