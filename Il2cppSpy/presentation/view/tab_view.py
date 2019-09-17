from typing import Dict
from PySide2.QtWidgets import QWidget

from Il2cppSpy.domain.model.dump_data import DumpType
from Il2cppSpy.presentation.view.tab_item_view import TabItemView
from Il2cppSpy.ui.ui_mainwindow import Ui_MainWindow


class TabView:
    def __init__(self, ui: Ui_MainWindow):
        self.ui = ui
        self.tabs: Dict[DumpType, QWidget] = {}
        self.ui.tabWidget.tabCloseRequested.connect(self.close_tab)

    def open_type(self, dump_type: DumpType):
        if dump_type in self.tabs.keys():
            self.ui.tabWidget.setCurrentWidget(self.tabs[dump_type])
            return
        tab_view = TabItemView(self.ui.tabWidget, dump_type)
        self.ui.tabWidget.addTab(tab_view, dump_type.name)
        self.ui.tabWidget.setCurrentWidget(tab_view)
        self.tabs[dump_type] = tab_view

    def close_tab(self, index):
        self.tabs = {k: v for k, v in self.tabs.items() if v != self.ui.tabWidget.widget(index)}
        self.ui.tabWidget.removeTab(index)
