from typing import List
from capstone import Cs, CS_ARCH_ARM, CS_MODE_ARM

from Il2cppSpy.domain.repository.abstract_assembly_repository import AbstractAssemblyRepository
from Il2cppSpy.domain.model.dump_data import DumpAssembly


class AssemblyRepository(AbstractAssemblyRepository):
    def disassemble(self, code: bytes, address: int) -> List[DumpAssembly]:
        dump_assemblies = []
        md = Cs(CS_ARCH_ARM, CS_MODE_ARM)
        for i in md.disasm(code, address):
            dump_assemblies.append(DumpAssembly(i.address, f'{i.mnemonic}\t{i.op_str}'))
        return dump_assemblies
