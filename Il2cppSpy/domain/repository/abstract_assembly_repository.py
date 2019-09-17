from typing import List
from abc import ABCMeta, abstractmethod

from Il2cppSpy.domain.model.dump_data import DumpAssembly


class AbstractAssemblyRepository(metaclass=ABCMeta):
    @abstractmethod
    def disassemble(self, code: bytes) -> List[DumpAssembly]:
        pass
