from PySide2.QtCore import Qt
from PySide2.QtGui import QColor
from PySide2.QtWidgets import QWidget, QTableWidgetItem, QUndoStack

from Il2cppSpy.domain.model.dump_data import DumpType
from Il2cppSpy.ui.ui_tabitemwindow import Ui_TabItemWindow
import Il2cppSpy.ui.ui_color as color


class TabItemView(QWidget):
    def __init__(self, parent: QWidget, dump_type: DumpType):
        super(TabItemView, self).__init__(parent)
        self.undo_stack = QUndoStack(self)
        self.current_item: QTableWidgetItem
        tab_ui = Ui_TabItemWindow()
        tab_ui.setupUi(self)
        self.table_widget = tab_ui.tableWidget
        row_count = 0
        for dump_attribute in dump_type.attributes:
            self.add_row(row_count, dump_attribute)
            row_count += 1
        name = ''
        if dump_type.modifier:
            name += f'{dump_type.modifier} '
        if dump_type.type_str:
            name += f'{dump_type.type_str} '
        if dump_type.name:
            name += f'{dump_type.name}'
        if dump_type.extends:
            name += f' : {", ".join(dump_type.extends)}'
        self.add_row(row_count, name)
        row_count += 1
        for dump_field in dump_type.fields:
            for dump_attribute in dump_field.attributes:
                self.add_row(row_count, dump_attribute)
                row_count += 1
            name = ''
            if dump_field.modifier:
                name += f'{dump_field.modifier} '
            if dump_field.type_str:
                name += f'{dump_field.type_str} '
            if dump_field.name:
                name += f'{dump_field.name}'
            if dump_field.value_str:
                name += f' = {dump_field.value_str}'
            name += ';'
            self.add_row(row_count, name)
            row_count += 1
        for dump_property in dump_type.properties:
            for dump_attribute in dump_property.attributes:
                self.add_row(row_count, dump_attribute)
                row_count += 1
            name = ''
            if dump_property.modifier:
                name += f'{dump_property.modifier} '
            if dump_property.type_str:
                name += f'{dump_property.type_str} '
            if dump_property.name:
                name += f'{dump_property.name}'
            if dump_property.access:
                name += f' {dump_property.access}'
            self.add_row(row_count, name)
            row_count += 1
        for dump_method in dump_type.methods:
            for dump_attribute in dump_method.attributes:
                self.add_row(row_count, dump_attribute)
                row_count += 1
            name = ''
            if dump_method.modifier:
                name += f'{dump_method.modifier} '
            if dump_method.type_str:
                name += f'{dump_method.type_str} '
            if dump_method.name:
                name += f'{dump_method.name}'
            if dump_method.parameters:
                name += f'({", ".join(dump_method.parameters)})'
            else:
                name += '()'
            self.add_row(row_count, name, color.TABLE_METHOD_CODE_TEXT_COLOR)
            row_count += 1
            for dump_assembly in dump_method.assemblies:
                text_color = color.TABLE_DIFF_TEXT_COLOR if dump_assembly.is_diff else color.TABLE_ASSEMBLY_TEXT_COLOR
                background_color = color.TABLE_DIFF_BACKGROUND_COLOR if dump_assembly.is_diff else color.TABLE_ASSEMBLY_BACKGROUND_COLOR
                self.add_row(row_count, dump_assembly.assembly, text_color, background_color, f'0x{dump_assembly.address:x}')
                row_count += 1

    def add_row(self, row: int, item_text: str, item_text_color: QColor = color.TABLE_CODE_TEXT_COLOR, item_background_color=color.TABLE_CODE_BACKGROUND_COLOR, header_text: str = ''):
        self.table_widget.setRowCount(row + 1)
        table_header = QTableWidgetItem(header_text)
        self.table_widget.setVerticalHeaderItem(row, table_header)
        table_item = QTableWidgetItem(item_text)
        table_item.setTextColor(item_text_color)
        table_item.setFlags(table_item.flags() ^ Qt.ItemIsEditable)
        table_item.setBackgroundColor(item_background_color)
        self.table_widget.setItem(row, 0, table_item)
