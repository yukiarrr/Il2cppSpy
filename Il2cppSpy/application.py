import sys
sys.path.append('../')
sys.path.append('./ui/')
from PySide2.QtCore import Qt
from PySide2.QtWidgets import QApplication, QMainWindow, QFileDialog, QProgressDialog
import qdarkstyle

from Il2cppSpy.presentation.presenter.action_presenter import ActionPresenter
from Il2cppSpy.presentation.view.explorer_view import ExplorerView
from Il2cppSpy.presentation.view.tab_view import TabView
from Il2cppSpy.ui.ui_mainwindow import Ui_MainWindow


class MainWindow(QMainWindow):
    def __init__(self):
        super(MainWindow, self).__init__()
        ui = Ui_MainWindow()
        ui.setupUi(self)
        tab_view = TabView(ui)
        explorer_view = ExplorerView(ui, tab_view)
        self.action_presenter = ActionPresenter(explorer_view)
        ui.actionOpenFile.triggered.connect(self.action_open_file)
        ui.actionCompareFiles.triggered.connect(self.action_compare_files)

    def action_open_file(self):
        file_path, _ = QFileDialog.getOpenFileName(self, 'Open Apk', '', 'Apk(*.apk)')
        if not file_path:
            return
        progress_dialog = QProgressDialog('Open Apk...', '', 0, 100, self)
        progress_dialog.setCancelButton(None)
        progress_dialog.setWindowModality(Qt.WindowModal)
        self.action_presenter.open_file(file_path, lambda value: progress_dialog.setValue(value * 100))

    def action_compare_files(self):
        before_file_path, _ = QFileDialog.getOpenFileName(self, 'Open Before Apk', '', 'Apk(*.apk)')
        if not before_file_path:
            return
        after_file_path, _ = QFileDialog.getOpenFileName(self, 'Open After Apk', '', 'Apk(*.apk)')
        if not after_file_path:
            return
        progress_dialog = QProgressDialog('Compare Apks...', '', 0, 100, self)
        progress_dialog.setCancelButton(None)
        progress_dialog.setWindowModality(Qt.WindowModal)
        self.action_presenter.compare_files(before_file_path, after_file_path, lambda value: progress_dialog.setValue(value * 100))


if __name__ == "__main__":
    app = QApplication(sys.argv)
    app.setStyleSheet(qdarkstyle.load_stylesheet_pyside())
    window = MainWindow()
    window.show()

    sys.exit(app.exec_())
