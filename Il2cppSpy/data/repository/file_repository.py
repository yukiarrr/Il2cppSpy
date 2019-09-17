import zipfile

from Il2cppSpy.domain.repository.abstract_file_repository import AbstractFileRepository


class FileRepository(AbstractFileRepository):
    def decode(self, file_path: str, out_file_dir: str):
        with zipfile.ZipFile(file_path) as file_file:
            file_file.extractall(out_file_dir)
