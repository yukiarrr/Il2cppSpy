from typing import List


class DumpAssembly:
    def __init__(self, address: int = 0, assembly: str = '', is_diff: bool = False):
        self.address = address
        self.assembly = assembly
        self.is_diff = is_diff


class DumpField:
    def __init__(self, attributes: List[str] = [], modifier: str = '', type_str: str = '', name: str = '', value_str: str = ''):
        self.attributes = attributes
        self.modifier = modifier
        self.type_str = type_str
        self.name = name
        self.value_str = value_str


class DumpProperty:
    def __init__(self, attributes: List[str] = [], modifier: str = '', type_str: str = '', name: str = '', access: str = ''):
        self.attributes = attributes
        self.modifier = modifier
        self.type_str = type_str
        self.name = name
        self.access = access


class DumpMethod:
    def __init__(self, attributes: List[str] = [], modifier: str = '', type_str: str = '', name: str = '', parameters: List[str] = [], assemblies: List[DumpAssembly] = []):
        self.attributes = attributes
        self.modifier = modifier
        self.type_str = type_str
        self.name = name
        self.parameters = parameters
        self.assemblies = assemblies


class DumpType:
    def __init__(self, namespace: str = '', attributes: List[str] = [], modifier: str = '', type_str: str = '', name: str = '', extends: List[str] = [], fields: List[DumpField] = [], properties: List[DumpProperty] = [], methods: List[DumpMethod] = []):
        self.namespace = namespace
        self.attributes = attributes
        self.modifier = modifier
        self.type_str = type_str
        self.name = name
        self.extends = extends
        self.fields = fields
        self.properties = properties
        self.methods = methods


class DumpFile:
    def __init__(self, file_path: str, dump_types: List[DumpType], is_diff: bool):
        self.file_path = file_path
        self.dump_types = dump_types
        self.is_diff = is_diff
