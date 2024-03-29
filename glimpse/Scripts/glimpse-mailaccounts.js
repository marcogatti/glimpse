﻿var activeMailAccounts = {};

function initializeMailAccountToggles() {

    var mailAccountsContainer = $('#mailaccounts-container');

    prepareMailAccountToggles(mailAccountsContainer);
    initializeMailAccountActivenessVector(mailAccountsContainer);
    setTogglesAction(mailAccountsContainer);

}

function prepareMailAccountToggles(mailAccountsContainer) {

    var mailaccounts = user_mailAccounts,
        mainAccountIndex = getMainAccount(mailaccounts);

    for (var index in mailaccounts) {

        var isMainAccount = (index === mainAccountIndex);

        mailAccountToggleAdd(mailAccountsContainer, mailaccounts[index], isMainAccount);
    }
}

function setTogglesAction(mailAccountsContainer) {

    mailAccountsContainer.find('li').each(function () {

        $(this).click(function () {

            var mailAccountToggle = $(this),
                mailAccountId = mailAccountToggle.data('mailaccount-id');

            if (mailAccountToggle.hasClass('active')) {
                mailAccountToggle.removeClass('active');
            } else {
                mailAccountToggle.addClass('active');
            }

            activeMailAccounts[mailAccountToggle.data('mailaccount-id')] = mailAccountToggle.hasClass('active');

            chooseCirclesToBeShown();
        });
    });

}

function getMainAccount(mailaccounts) {

    for (var index in mailaccounts) {
        if (mailaccounts[index].mainAccount)
            return index;
    }
    return index;
}

function mailAccountToggleAdd(container, mailAccount, isMainAccount) {

    var accountItem = $('<li></li>'),
        accountToggle = $('<a href="#"></a>');

    if (isMainAccount) {
        var mainMarker = $('<i></i>').addClass('icon-star').attr('title', 'Cuenta principal');

        accountItem.addClass('active');
        accountToggle.prepend(' ');
        accountToggle.prepend(mainMarker);
    }

    accountToggle.append(mailAccount.address);
    accountItem.append(accountToggle);
    accountItem.attr("data-mailaccount-id", mailAccount.mailAccountId);
    container.find('ul').append(accountItem);
}

function initializeMailAccountActivenessVector(container) {

    container.find('li').each(function () {

        var accountItem = $(this);

        activeMailAccounts[accountItem.data('mailaccount-id')] = accountItem.hasClass('active');
    });
}