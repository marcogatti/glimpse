function initializeMainDropdownMenuActions() {

    $('#btn-config').click(function () {
        $('#config-view').modal();
    });

    $('#config-password, #config-mailaccount, #config-personaldata').click(function () {

        var modalBody = $('#config-view').find('.modal-body'),
            bodyId = $(this).data('body-id');

        modalBody.find('.nav-tabs').find('li').each(function () {
            $(this).removeClass('active')
        });

        $(this).addClass('active');

        modalBody.find('div.nav-body').addClass('hidden');
        modalBody.find('#' + bodyId).removeClass('hidden');
    });

    $('#config-password-form, #config-mailaccount-form, #config-personaldata-form').submit(function (event) {
        event.preventDefault();
        alert("No implementado jeje");
    });


}