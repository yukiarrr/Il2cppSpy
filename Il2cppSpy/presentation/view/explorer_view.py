from typing import Dict
from PySide2.QtCore import Qt
from PySide2.QtWidgets import QTreeWidgetItem

from Il2cppSpy.domain.model.dump_data import DumpFile, DumpType
from Il2cppSpy.ui.ui_mainwindow import Ui_MainWindow
from Il2cppSpy.presentation.view.tab_view import TabView
import Il2cppSpy.ui.ui_color as color


class ExplorerView:
    def __init__(self, ui: Ui_MainWindow, tab_view: TabView):
        self.ui = ui
        self.tab_view = tab_view
        self.dump_types: Dict[QTreeWidgetItem, DumpType] = {}

    def add_file(self, dump_file: DumpFile):
        file_item = QTreeWidgetItem(self.ui.treeWidget)
        item_text = f'<diff> {dump_file.file_path}' if dump_file.is_diff else dump_file.file_path
        file_item.setText(0, item_text)
        tree_items: Dict[str, QTreeWidgetItem] = {}
        for dump_type in dump_file.dump_types:
            if dump_type.name.startswith('<'):
                continue
            names = dump_type.namespace.split('.')
            parent = file_item
            key = ''
            for name in names:
                if not name:
                    continue
                key += name
                if key not in tree_items.keys():
                    tree_item = QTreeWidgetItem(parent)
                    tree_item.setText(0, name)
                    tree_items[key] = tree_item
                parent = tree_items[key]
            tree_item = QTreeWidgetItem(parent)
            tree_item.setText(0, dump_type.name)
            background_color = color.EXPLORER_DIFF_TEXT_COLOR if dump_file.is_diff else color.EXPLORER_TYPE_TEXT_COLOR
            tree_item.setTextColor(0, background_color)
            self.dump_types[tree_item] = dump_type
        file_item.sortChildren(0, Qt.AscendingOrder)
        self.ui.treeWidget.itemClicked.connect(self.item_clicked)

    def item_clicked(self, item, column):
        if item in self.dump_types.keys():
            self.tab_view.open_type(self.dump_types[item])
