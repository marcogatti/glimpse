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
    setTransitionsCheckbox();
    setEverithingRelatedToAddLabelsToAMail();
    initializeMailEditor();
    initializeMailViewModal();
    initializeMainDropdownMenuActions();
    initializeMailAccountToggles();
})

