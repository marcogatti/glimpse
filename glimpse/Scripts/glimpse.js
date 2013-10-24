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
})

