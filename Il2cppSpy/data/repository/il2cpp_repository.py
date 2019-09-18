import sys
import re
from typing import List, Callable

from Il2cppSpy.domain.model.dump_data import DumpType, DumpAssembly, DumpField, DumpProperty, DumpMethod
from Il2cppSpy.domain.repository.abstract_il2cpp_repository import AbstractIl2cppRepository
from Il2cppSpy.domain.repository.abstract_assembly_repository import AbstractAssemblyRepository
from Il2cppSpy.utils.file_utils import strings

import clr
sys.path.append('./bin/Release')
clr.AddReference('DumpWrapper')
import Wrapper
from Wrapper import DumpWrapper


class Il2cppRepository(AbstractIl2cppRepository):
    def __init__(self, assembly_repository: AbstractAssemblyRepository):
        self.assembly_repository = assembly_repository

    def dump(self, out_apk_dir: str, progress: Callable[[float], None]) -> List[DumpType]:
        result = DumpWrapper.Dump(self.config_path(), self.metadata_path(out_apk_dir), self.il2cpp_path(out_apk_dir), self.unity_version(out_apk_dir))
        return self.to_model(result, out_apk_dir, progress)

    def config_path(self) -> str:
        return "./bin/Release/config.json"

    def metadata_path(self, out_apk_dir: str) -> str:
        return f'{out_apk_dir}/assets/bin/Data/Managed/Metadata/global-metadata.dat'

    def il2cpp_path(self, out_apk_dir: str) -> str:
        return f'{out_apk_dir}/lib/armeabi-v7a/libil2cpp.so'

    def unity_version(self, out_apk_dir: str) -> str:
        for s in strings(f'{out_apk_dir}/assets/bin/Data/Resources/unity_builtin_extra'):
            result = re.search(r'20[0-9]{2}\.[0-9]', s)
            if result:
                return result.group()
        # Before prefab in prefab
        return '2018.2'

    def to_model(self, wrapper_dump_types: List[Wrapper.DumpType], out_apk_dir: str, progress: Callable[[float], None]) -> List[DumpType]:
        il2cpp_path = self.il2cpp_path(out_apk_dir)
        with open(il2cpp_path, 'rb') as f:
            il2cpp_bytes = f.read()
            dump_types = []
            pre_address = 0
            pre_assemblies: List[DumpAssembly] = []
            for i, wrapper_dump_type in enumerate(wrapper_dump_types):
                dump_attributes = []
                if wrapper_dump_type.Attributes:
                    for dumpAttribute in wrapper_dump_type.Attributes:
                        if dumpAttribute.Address and pre_address:
                            address = pre_address
                            code = il2cpp_bytes[address:dumpAttribute.Address]
                            pre_assemblies.extend(self.assembly_repository.disassemble(code, address))
                            pre_address = 0
                            pre_assemblies = []
                        dump_attributes.append(dumpAttribute.Name)
                dump_fields = []
                if wrapper_dump_type.Fields:
                    for dumpField in wrapper_dump_type.Fields:
                        dump_field_attributes = []
                        if dumpField.Attributes:
                            for dumpFieldAttribute in dumpField.Attributes:
                                if dumpFieldAttribute.Address and pre_address:
                                    address = pre_address
                                    code = il2cpp_bytes[address:dumpFieldAttribute.Address]
                                    pre_assemblies.extend(self.assembly_repository.disassemble(code, address))
                                    pre_address = 0
                                    pre_assemblies = []
                                dump_field_attributes.append(dumpFieldAttribute.Name)
                        dump_fields.append(DumpField(dump_field_attributes, dumpField.Modifier, dumpField.TypeStr, dumpField.Name, dumpField.ValueStr))
                dump_properties = []
                if wrapper_dump_type.Properties:
                    for dumpProperty in wrapper_dump_type.Properties:
                        dump_property_attributes = []
                        if dumpProperty.Attributes:
                            for dumpPropertyAttribute in dumpProperty.Attributes:
                                if dumpPropertyAttribute.Address and pre_address:
                                    address = pre_address
                                    code = il2cpp_bytes[address:dumpPropertyAttribute.Address]
                                    pre_assemblies.extend(self.assembly_repository.disassemble(code, address))
                                    pre_address = 0
                                    pre_assemblies = []
                                dump_property_attributes.append(dumpPropertyAttribute.Name)
                        dump_properties.append(DumpProperty(dump_property_attributes, dumpProperty.Modifier, dumpProperty.TypeStr, dumpProperty.Name, dumpProperty.Access))
                dump_methods = []
                if wrapper_dump_type.Methods:
                    for j, dumpMethod in enumerate(wrapper_dump_type.Methods):
                        dump_method_attributes = []
                        if dumpMethod.Attributes:
                            for dumpMethodAttribute in dumpMethod.Attributes:
                                if dumpMethodAttribute.Address and pre_address:
                                    address = pre_address
                                    code = il2cpp_bytes[address:dumpMethodAttribute.Address]
                                    pre_assemblies.extend(self.assembly_repository.disassemble(code, address))
                                    pre_address = 0
                                    pre_assemblies = []
                                dump_method_attributes.append(dumpMethodAttribute.Name)
                        if dumpMethod.Address:
                            if pre_address:
                                address = pre_address
                                code = il2cpp_bytes[address:dumpMethod.Address]
                                pre_assemblies.extend(self.assembly_repository.disassemble(code, address))
                            pre_address = dumpMethod.Address
                            pre_assemblies = []
                        dump_parameters = []
                        if dumpMethod.Parameters:
                            for dump_parameter in dumpMethod.Parameters:
                                dump_parameters.append(dump_parameter)
                        dump_methods.append(DumpMethod(dump_method_attributes, dumpMethod.Modifier, dumpMethod.TypeStr, dumpMethod.Name, dump_parameters, pre_assemblies))
                        progress((i + (j / len(wrapper_dump_type.Methods))) / len(wrapper_dump_types))
                dump_extends = []
                if wrapper_dump_type.Extends:
                    for dump_extend in wrapper_dump_type.Extends:
                        dump_extends.append(dump_extend)
                dump_types.append(DumpType(wrapper_dump_type.Namespace, dump_attributes, wrapper_dump_type.Modifier, wrapper_dump_type.TypeStr, wrapper_dump_type.Name, dump_extends, dump_fields, dump_properties, dump_methods))
                progress((i + 1) / len(wrapper_dump_types))
        return dump_types
