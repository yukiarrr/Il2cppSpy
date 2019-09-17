import os
import tempfile
from typing import Callable

from Il2cppSpy.domain.model.dump_data import DumpFile
from Il2cppSpy.domain.repository.abstract_file_repository import AbstractFileRepository
from Il2cppSpy.domain.repository.abstract_il2cpp_repository import AbstractIl2cppRepository


class OpenFileUseCase:
    def __init__(self, file_repository: AbstractFileRepository, il2cpp_repository: AbstractIl2cppRepository):
        self.file_repository = file_repository
        self.il2cpp_repository = il2cpp_repository

    def execute(self, file_path: str, progress: Callable[[float], None]) -> DumpFile:
        with tempfile.TemporaryDirectory() as temp_dir:
            file_name = os.path.splitext(os.path.basename(file_path))[0]
            out_file_dir = f'{temp_dir}/{file_name}'
            self.file_repository.decode(file_path, out_file_dir)
            result = self.il2cpp_repository.dump(out_file_dir, progress)
            return DumpFile(file_path, result, False)
