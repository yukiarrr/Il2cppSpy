from typing import Callable

from Il2cppSpy.data.repository.assembly_repository import AssemblyRepository
from Il2cppSpy.data.repository.il2cpp_repository import Il2cppRepository
from Il2cppSpy.data.repository.file_repository import FileRepository
from Il2cppSpy.domain.use_case.open_file_use_case import OpenFileUseCase
from Il2cppSpy.domain.use_case.compare_files_use_case import CompareFilesUseCase
from Il2cppSpy.presentation.view.explorer_view import ExplorerView


class ActionPresenter:
    def __init__(self, explorer_view: ExplorerView):
        self.explorer_view = explorer_view

    def open_file(self, file_path: str, progress: Callable[[float], None]):
        open_file = OpenFileUseCase(FileRepository(), Il2cppRepository(AssemblyRepository()))
        result = open_file.execute(file_path, progress)
        self.explorer_view.add_file(result)

    def compare_files(self, before_file_path: str, after_file_path: str, progress: Callable[[float], None]):
        compare_files = CompareFilesUseCase(FileRepository(), Il2cppRepository(AssemblyRepository()))
        result = compare_files.execute(before_file_path, after_file_path, progress)
        self.explorer_view.add_file(result)
