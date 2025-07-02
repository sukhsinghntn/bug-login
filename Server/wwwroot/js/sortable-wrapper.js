globalThis.initSortable = (selector, dotnetHelper) => {
    const container = document.querySelector(selector);
    if (!container) return;

    container.querySelectorAll('.designer-row').forEach(row => {
        if (row.dataset.sortableInit === 'true') return;
        row.dataset.sortableInit = 'true';
        new Sortable(row, {
            group: 'rows',
            animation: 150,
            handle: '.drag-handle',
            onEnd: function (evt) {
                const fromRow = evt.from.getAttribute('data-row');
                const toRow = evt.to.getAttribute('data-row');
                dotnetHelper.invokeMethodAsync('OnSortUpdate', parseInt(fromRow), evt.oldIndex, parseInt(toRow), evt.newIndex);
            }
        });
    });
};

globalThis.initListSortable = (selector, dotnetHelper) => {
    const container = document.querySelector(selector);
    if (!container || container.dataset.sortableInit === 'true') return;
    container.dataset.sortableInit = 'true';
    new Sortable(container, {
        animation: 150,
        handle: '.move-handle',
        onEnd: evt => dotnetHelper.invokeMethodAsync('OnFieldReorder', evt.oldIndex, evt.newIndex)
    });
};

globalThis.initRowSortable = (selector, dotnetHelper) => {
    const container = document.querySelector(selector);
    if (!container || container.dataset.rowSortableInit === 'true') return;
    container.dataset.rowSortableInit = 'true';
    new Sortable(container, {
        animation: 150,
        handle: '.row-handle',
        draggable: '.row-wrapper',
        onEnd: evt => dotnetHelper.invokeMethodAsync('OnRowReorder', evt.oldIndex, evt.newIndex)
    });
};
