import os
import tempfile
from typing import List, Callable

from Il2cppSpy.domain.model.dump_data import DumpFile, DumpType
from Il2cppSpy.domain.repository.abstract_file_repository import AbstractFileRepository
from Il2cppSpy.domain.repository.abstract_il2cpp_repository import AbstractIl2cppRepository


class CompareFilesUseCase:
    def __init__(self, file_repository: AbstractFileRepository, il2cpp_repository: AbstractIl2cppRepository):
        self.file_repository = file_repository
        self.il2cpp_repository = il2cpp_repository

    def execute(self, before_file_path: str, after_file_path: str, progress: Callable[[float], None]) -> DumpFile:
        with tempfile.TemporaryDirectory() as temp_dir:
            # Before
            before_file_name = os.path.splitext(os.path.basename(before_file_path))[0]
            out_before_file_dir = f'{temp_dir}/{before_file_name}-before'
            self.file_repository.decode(before_file_path, out_before_file_dir)
            before_result = self.il2cpp_repository.dump(out_before_file_dir, lambda value: progress(value * 0.49))
            # After
            after_file_name = os.path.splitext(os.path.basename(after_file_path))[0]
            out_after_file_dir = f'{temp_dir}/{after_file_name}-after'
            self.file_repository.decode(after_file_path, out_after_file_dir)
            after_result = self.il2cpp_repository.dump(out_after_file_dir, lambda value: progress(0.49 + value * 0.49))
            # Compare
            compare_result = self.compare(before_result, after_result)
            progress(1)
            return DumpFile(before_file_path, compare_result, True)

    def compare(self, before_dump_types: List[DumpType], after_dump_types: List[DumpType]) -> List[DumpType]:
        dump_type_diffs = []
        for before_dump_type, after_dump_type in zip(before_dump_types, after_dump_types):
            exists_diff = False
            for before_dump_method, after_dump_method in zip(before_dump_type.methods, after_dump_type.methods):
                for before_dump_assembly, after_dump_assembly in zip(before_dump_method.assemblies, after_dump_method.assemblies):
                    if before_dump_assembly.assembly != after_dump_assembly.assembly:
                        exists_diff = True
                        after_dump_assembly.is_diff = True
            if exists_diff:
                dump_type_diffs.append(after_dump_type)
        return dump_type_diffs
