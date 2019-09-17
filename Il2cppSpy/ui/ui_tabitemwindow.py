# -*- coding: utf-8 -*-

# Form implementation generated from reading ui file 'tabitemwindow.ui',
# licensing of 'tabitemwindow.ui' applies.
#
# Created: Tue Sep 17 17:05:33 2019
#      by: pyside2-uic  running on PySide2 5.13.1
#
# WARNING! All changes made in this file will be lost!

from PySide2 import QtCore, QtGui, QtWidgets

class Ui_TabItemWindow(object):
    def setupUi(self, TabItemWindow):
        TabItemWindow.setObjectName("TabItemWindow")
        TabItemWindow.resize(400, 300)
        self.verticalLayout = QtWidgets.QVBoxLayout(TabItemWindow)
        self.verticalLayout.setSpacing(0)
        self.verticalLayout.setContentsMargins(0, 0, 0, 0)
        self.verticalLayout.setObjectName("verticalLayout")
        self.tableWidget = QtWidgets.QTableWidget(TabItemWindow)
        self.tableWidget.setColumnCount(1)
        self.tableWidget.setObjectName("tableWidget")
        self.tableWidget.setColumnCount(1)
        self.tableWidget.setRowCount(0)
        self.tableWidget.horizontalHeader().setVisible(False)
        self.tableWidget.horizontalHeader().setStretchLastSection(True)
        self.tableWidget.verticalHeader().setDefaultSectionSize(24)
        self.tableWidget.verticalHeader().setHighlightSections(False)
        self.verticalLayout.addWidget(self.tableWidget)

        self.retranslateUi(TabItemWindow)
        QtCore.QMetaObject.connectSlotsByName(TabItemWindow)

    def retranslateUi(self, TabItemWindow):
        TabItemWindow.setWindowTitle(QtWidgets.QApplication.translate("TabItemWindow", "Form", None, -1))

