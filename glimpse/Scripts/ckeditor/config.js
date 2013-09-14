/**
 * @license Copyright (c) 2003-2013, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.html or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function (config) {

    config.toolbar = 'Full';

    config.toolbar_Full =
    [
        { name: 'editing', items: ['Undo', 'Redo'] },
        { name: 'spell', items: ['spellchecker'] },
        { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', 'Font', 'TextColor', '-', 'RemoveFormat'] },
        { name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent'] },
        { name: 'links', items: ['Link', 'Unlink'] },
    ];

    // Make dialogs simpler.
    config.removeDialogTabs = 'image:advanced;link:advanced';

    config.width = "100%";
};
