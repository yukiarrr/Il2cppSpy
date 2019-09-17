using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Il2CppDumper;
using static Wrapper.DefineConstants;

namespace Wrapper
{
    public static class DumpWrapper
    {
        private static Metadata metadata;
        private static Il2Cpp il2cpp;
        private static Config config;
        private static Dictionary<Il2CppMethodDefinition, string> methodModifiers = new Dictionary<Il2CppMethodDefinition, string>();
        private static Dictionary<Il2CppTypeDefinition, int> typeDefImageIndices = new Dictionary<Il2CppTypeDefinition, int>();

        public static DumpType[] Dump(string configPath, string metadataPath, string il2cppPath, string stringVersion)
        {
            // From Program.cs

            config = new JavaScriptSerializer().Deserialize<Config>(File.ReadAllText(configPath));

            var metadataBytes = File.ReadAllBytes(metadataPath);
            var il2cppBytes = File.ReadAllBytes(il2cppPath);

            var sanity = BitConverter.ToUInt32(metadataBytes, 0);
            if (sanity != 0xFAB11BAF)
            {
                throw new Exception("ERROR: Metadata file supplied is not valid metadata file.");
            }
            float fixedMetadataVersion;
            var metadataVersion = BitConverter.ToInt32(metadataBytes, 4);
            if (metadataVersion == 24)
            {
                var versionSplit = Array.ConvertAll(Regex.Replace(stringVersion, @"\D", ".").Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries), int.Parse);
                var unityVersion = new Version(versionSplit[0], versionSplit[1]);
                if (unityVersion >= Unity20191)
                {
                    fixedMetadataVersion = 24.2f;
                }
                else if (unityVersion >= Unity20183)
                {
                    fixedMetadataVersion = 24.1f;
                }
                else
                {
                    fixedMetadataVersion = metadataVersion;
                }
            }
            else
            {
                fixedMetadataVersion = metadataVersion;
            }
            Console.WriteLine("Initializing metadata...");
            metadata = new Metadata(new MemoryStream(metadataBytes), fixedMetadataVersion);
            //判断il2cpp的magic
            var il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
            var isElf = false;
            var isPE = false;
            var is64bit = false;
            var isNSO = false;
            switch (il2cppMagic)
            {
                default:
                    throw new Exception("ERROR: il2cpp file not supported.");
                case 0x304F534E:
                    isNSO = true;
                    is64bit = true;
                    break;
                case 0x905A4D: //PE
                    isPE = true;
                    break;
                case 0x464c457f: //ELF
                    isElf = true;
                    if (il2cppBytes[4] == 2) //ELF64
                    {
                        is64bit = true;
                    }
                    break;
                case 0xCAFEBABE: //FAT Mach-O
                case 0xBEBAFECA:
                // To 64bit
                case 0xFEEDFACF: // 64bit Mach-O
                    is64bit = true;
                    break;
                case 0xFEEDFACE: // 32bit Mach-O
                    break;
            }

            var version = config.ForceIl2CppVersion ? config.ForceVersion : metadata.version;
            if (isNSO)
            {
                var nso = new NSO(new MemoryStream(il2cppBytes), version, metadata.maxMetadataUsages);
                il2cpp = nso.UnCompress();
            }
            else if (isPE)
            {
                il2cpp = new PE(new MemoryStream(il2cppBytes), version, metadata.maxMetadataUsages);
            }
            else if (isElf)
            {
                if (is64bit)
                    il2cpp = new Elf64(new MemoryStream(il2cppBytes), version, metadata.maxMetadataUsages);
                else
                    il2cpp = new Elf(new MemoryStream(il2cppBytes), version, metadata.maxMetadataUsages);
            }
            else if (is64bit)
                il2cpp = new Macho64(new MemoryStream(il2cppBytes), version, metadata.maxMetadataUsages);
            else
                il2cpp = new Macho(new MemoryStream(il2cppBytes), version, metadata.maxMetadataUsages);
            Console.WriteLine("Searching...");
            try
            {
                // Select Auto(Plus)
                bool flag = il2cpp.PlusSearch(metadata.methodDefs.Count(x => x.methodIndex >= 0), metadata.typeDefs.Length);
                if (!flag)
                    throw new Exception();
            }
            catch
            {
                throw new Exception("ERROR: Can't use this mode to process file, try another mode.");
            }

            Console.WriteLine("Dumping...");
            //dump type
            var dumpTypes = new List<DumpType>();
            for (var imageIndex = 0; imageIndex < metadata.imageDefs.Length; imageIndex++)
            {
                try
                {
                    var imageDef = metadata.imageDefs[imageIndex];
                    var typeEnd = imageDef.typeStart + imageDef.typeCount;
                    for (int idx = imageDef.typeStart; idx < typeEnd; idx++)
                    {
                        var dumpType = new DumpType();
                        var typeDef = metadata.typeDefs[idx];
                        typeDefImageIndices.Add(typeDef, imageIndex);
                        var isStruct = false;
                        var isEnum = false;
                        var extends = new List<string>();
                        if (typeDef.parentIndex >= 0)
                        {
                            var parent = il2cpp.types[typeDef.parentIndex];
                            var parentName = GetTypeName(parent);
                            if (parentName == "ValueType")
                                isStruct = true;
                            else if (parentName == "Enum")
                                isEnum = true;
                            else if (parentName != "object")
                                extends.Add(parentName);
                        }
                        //implementedInterfaces
                        if (typeDef.interfaces_count > 0)
                        {
                            for (int i = 0; i < typeDef.interfaces_count; i++)
                            {
                                var @interface = il2cpp.types[metadata.interfaceIndices[typeDef.interfacesStart + i]];
                                extends.Add(GetTypeName(@interface));
                            }
                        }
                        dumpType.Namespace = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                        var dumpTypeAttributes = new List<DumpAttribute>();
                        var typeAttributes = GetCustomAttributes(imageDef, typeDef.customAttributeIndex, typeDef.token);
                        if (typeAttributes != null)
                        {
                            dumpTypeAttributes.AddRange(typeAttributes);
                        }
                        if (config.DumpAttribute && (typeDef.flags & TYPE_ATTRIBUTE_SERIALIZABLE) != 0)
                            dumpTypeAttributes.Add(new DumpAttribute { Name = "[Serializable]" });
                        dumpType.Attributes = dumpTypeAttributes.ToArray();
                        var visibility = typeDef.flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;
                        switch (visibility)
                        {
                            case TYPE_ATTRIBUTE_PUBLIC:
                            case TYPE_ATTRIBUTE_NESTED_PUBLIC:
                                dumpType.Modifier += "public ";
                                break;
                            case TYPE_ATTRIBUTE_NOT_PUBLIC:
                            case TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM:
                            case TYPE_ATTRIBUTE_NESTED_ASSEMBLY:
                                dumpType.Modifier += "internal ";
                                break;
                            case TYPE_ATTRIBUTE_NESTED_PRIVATE:
                                dumpType.Modifier += "private ";
                                break;
                            case TYPE_ATTRIBUTE_NESTED_FAMILY:
                                dumpType.Modifier += "protected ";
                                break;
                            case TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM:
                                dumpType.Modifier += "protected internal ";
                                break;
                        }
                        if ((typeDef.flags & TYPE_ATTRIBUTE_ABSTRACT) != 0 && (typeDef.flags & TYPE_ATTRIBUTE_SEALED) != 0)
                            dumpType.Modifier += "static ";
                        else if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) == 0 && (typeDef.flags & TYPE_ATTRIBUTE_ABSTRACT) != 0)
                            dumpType.Modifier += "abstract ";
                        else if (!isStruct && !isEnum && (typeDef.flags & TYPE_ATTRIBUTE_SEALED) != 0)
                            dumpType.Modifier += "sealed ";
                        dumpType.Modifier.TrimEnd();
                        if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) != 0)
                            dumpType.TypeStr = "interface";
                        else if (isStruct)
                            dumpType.TypeStr = "struct";
                        else if (isEnum)
                            dumpType.TypeStr = "enum";
                        else
                            dumpType.TypeStr = "class";
                        var typeName = GetTypeName(typeDef);
                        dumpType.Name = $"{typeName}";
                        if (extends.Count > 0)
                            dumpType.Extends = extends.ToArray();
                        //dump field
                        var dumpFields = new List<DumpField>();
                        if (config.DumpField && typeDef.field_count > 0)
                        {
                            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
                            for (var i = typeDef.fieldStart; i < fieldEnd; ++i)
                            {
                                //dump_field(i, idx, i - typeDef.fieldStart);
                                var dumpField = new DumpField();
                                var fieldDef = metadata.fieldDefs[i];
                                var fieldType = il2cpp.types[fieldDef.typeIndex];
                                var fieldDefaultValue = metadata.GetFieldDefaultValueFromIndex(i);
                                var fieldAttributes = GetCustomAttributes(imageDef, fieldDef.customAttributeIndex, fieldDef.token);
                                if (fieldAttributes != null)
                                {
                                    dumpField.Attributes = fieldAttributes;
                                }
                                var access = fieldType.attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK;
                                switch (access)
                                {
                                    case FIELD_ATTRIBUTE_PRIVATE:
                                        dumpField.Modifier += "private ";
                                        break;
                                    case FIELD_ATTRIBUTE_PUBLIC:
                                        dumpField.Modifier += "public ";
                                        break;
                                    case FIELD_ATTRIBUTE_FAMILY:
                                        dumpField.Modifier += "protected ";
                                        break;
                                    case FIELD_ATTRIBUTE_ASSEMBLY:
                                    case FIELD_ATTRIBUTE_FAM_AND_ASSEM:
                                        dumpField.Modifier += "internal ";
                                        break;
                                    case FIELD_ATTRIBUTE_FAM_OR_ASSEM:
                                        dumpField.Modifier += "protected internal ";
                                        break;
                                }
                                if ((fieldType.attrs & FIELD_ATTRIBUTE_LITERAL) != 0)
                                {
                                    dumpField.Modifier += "const ";
                                }
                                else
                                {
                                    if ((fieldType.attrs & FIELD_ATTRIBUTE_STATIC) != 0)
                                        dumpField.Modifier += "static ";
                                    if ((fieldType.attrs & FIELD_ATTRIBUTE_INIT_ONLY) != 0)
                                        dumpField.Modifier += "readonly ";
                                }
                                dumpField.TypeStr = GetTypeName(fieldType);
                                dumpField.Name = metadata.GetStringFromIndex(fieldDef.nameIndex);
                                if (fieldDefaultValue != null && fieldDefaultValue.dataIndex != -1)
                                {
                                    var pointer = metadata.GetDefaultValueFromIndex(fieldDefaultValue.dataIndex);
                                    if (pointer > 0)
                                    {
                                        var fieldDefaultValueType = il2cpp.types[fieldDefaultValue.typeIndex];
                                        metadata.Position = pointer;
                                        object val = null;
                                        switch (fieldDefaultValueType.type)
                                        {
                                            case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
                                                val = metadata.ReadBoolean();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_U1:
                                                val = metadata.ReadByte();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_I1:
                                                val = metadata.ReadSByte();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
                                                val = BitConverter.ToChar(metadata.ReadBytes(2), 0);
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_U2:
                                                val = metadata.ReadUInt16();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_I2:
                                                val = metadata.ReadInt16();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_U4:
                                                val = metadata.ReadUInt32();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                                                val = metadata.ReadInt32();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_U8:
                                                val = metadata.ReadUInt64();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_I8:
                                                val = metadata.ReadInt64();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_R4:
                                                val = metadata.ReadSingle();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_R8:
                                                val = metadata.ReadDouble();
                                                break;
                                            case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                                                var len = metadata.ReadInt32();
                                                val = Encoding.UTF8.GetString(metadata.ReadBytes(len));
                                                break;
                                        }
                                        if (val is string str)
                                            dumpField.ValueStr = $"\"{ToEscapedString(str)}\"";
                                        else if (val is char c)
                                        {
                                            var v = (int)c;
                                            dumpField.ValueStr = $"'\\x{v:x}'";
                                        }
                                        else if (val != null)
                                            dumpField.ValueStr = $"{val}";
                                    }
                                }
                                dumpFields.Add(dumpField);
                            }
                        }
                        dumpType.Fields = dumpFields.ToArray();
                        //dump property
                        var dumpProperties = new List<DumpProperty>();
                        if (config.DumpProperty && typeDef.property_count > 0)
                        {
                            var propertyEnd = typeDef.propertyStart + typeDef.property_count;
                            for (var i = typeDef.propertyStart; i < propertyEnd; ++i)
                            {
                                var dumpProperty = new DumpProperty();
                                var propertyDef = metadata.propertyDefs[i];
                                var propertyAttributes = GetCustomAttributes(imageDef, propertyDef.customAttributeIndex, propertyDef.token);
                                if (propertyAttributes != null)
                                {
                                    dumpProperty.Attributes = propertyAttributes;
                                }
                                if (propertyDef.get >= 0)
                                {
                                    var methodDef = metadata.methodDefs[typeDef.methodStart + propertyDef.get];
                                    dumpProperty.Modifier = GetModifiers(methodDef);
                                    var propertyType = il2cpp.types[methodDef.returnType];
                                    dumpProperty.TypeStr = GetTypeName(propertyType);
                                    dumpProperty.Name = metadata.GetStringFromIndex(propertyDef.nameIndex);
                                }
                                else if (propertyDef.set > 0)
                                {
                                    var methodDef = metadata.methodDefs[typeDef.methodStart + propertyDef.set];
                                    dumpProperty.Modifier = GetModifiers(methodDef);
                                    var parameterDef = metadata.parameterDefs[methodDef.parameterStart];
                                    var propertyType = il2cpp.types[parameterDef.typeIndex];
                                    dumpProperty.TypeStr = GetTypeName(propertyType);
                                    dumpProperty.Name = metadata.GetStringFromIndex(propertyDef.nameIndex);
                                }
                                dumpProperty.Access += "{ ";
                                if (propertyDef.get >= 0)
                                    dumpProperty.Access += "get; ";
                                if (propertyDef.set >= 0)
                                    dumpProperty.Access += "set; ";
                                dumpProperty.Access += "}";
                                dumpProperties.Add(dumpProperty);
                            }
                        }
                        dumpType.Properties = dumpProperties.ToArray();
                        //dump method
                        var dumpMethods = new List<DumpMethod>();
                        if (config.DumpMethod && typeDef.method_count > 0)
                        {
                            var methodEnd = typeDef.methodStart + typeDef.method_count;
                            for (var i = typeDef.methodStart; i < methodEnd; ++i)
                            {
                                var dumpMethod = new DumpMethod();
                                var methodDef = metadata.methodDefs[i];
                                var methodAttributes = GetCustomAttributes(imageDef, methodDef.customAttributeIndex, methodDef.token);
                                if (methodAttributes != null)
                                {
                                    dumpMethod.Attributes = methodAttributes;
                                }
                                dumpMethod.Modifier = GetModifiers(methodDef);
                                var methodReturnType = il2cpp.types[methodDef.returnType];
                                var methodName = metadata.GetStringFromIndex(methodDef.nameIndex);
                                dumpMethod.TypeStr = GetTypeName(methodReturnType);
                                dumpMethod.Name = methodName;
                                for (var j = 0; j < methodDef.parameterCount; ++j)
                                {
                                    var parameterStr = "";
                                    var parameterDef = metadata.parameterDefs[methodDef.parameterStart + j];
                                    var parameterName = metadata.GetStringFromIndex(parameterDef.nameIndex);
                                    var parameterType = il2cpp.types[parameterDef.typeIndex];
                                    var parameterTypeName = GetTypeName(parameterType);
                                    if ((parameterType.attrs & PARAM_ATTRIBUTE_OPTIONAL) != 0)
                                        parameterStr += "optional ";
                                    if ((parameterType.attrs & PARAM_ATTRIBUTE_OUT) != 0)
                                        parameterStr += "out ";
                                    parameterStr += $"{parameterTypeName} {parameterName}";
                                    dumpMethod.Parameters = new string[] { parameterStr };
                                }
                                if (config.DumpMethodOffset)
                                {
                                    var methodPointer = il2cpp.GetMethodPointer(methodDef.methodIndex, i, imageIndex, methodDef.token);
                                    if (methodPointer > 0)
                                    {
                                        dumpMethod.Address = il2cpp.MapVATR(methodPointer);
                                    }
                                }
                                dumpMethods.Add(dumpMethod);
                            }
                        }
                        dumpType.Methods = dumpMethods.ToArray();
                        dumpTypes.Add(dumpType);
                    }
                }
                catch
                {
                    throw new Exception("ERROR: Some errors in dumping.");
                }
            }

            return dumpTypes.ToArray();
        }

        private static string GetTypeName(Il2CppType type, bool fullName = false)
        {
            string ret;
            switch (type.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                    {
                        var typeDef = metadata.typeDefs[type.data.klassIndex];
                        ret = string.Empty;
                        if (fullName)
                        {
                            ret = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                            if (ret != string.Empty)
                            {
                                ret += ".";
                            }
                        }
                        ret += GetTypeName(typeDef);
                        break;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    {
                        var generic_class = il2cpp.MapVATR<Il2CppGenericClass>(type.data.generic_class);
                        var typeDef = metadata.typeDefs[generic_class.typeDefinitionIndex];
                        ret = metadata.GetStringFromIndex(typeDef.nameIndex);
                        var genericInst = il2cpp.MapVATR<Il2CppGenericInst>(generic_class.context.class_inst);
                        ret += GetGenericTypeParams(genericInst);
                        break;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                    {
                        var arrayType = il2cpp.MapVATR<Il2CppArrayType>(type.data.array);
                        var oriType = il2cpp.GetIl2CppType(arrayType.etype);
                        ret = $"{GetTypeName(oriType)}[{new string(',', arrayType.rank - 1)}]";
                        break;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    {
                        var oriType = il2cpp.GetIl2CppType(type.data.type);
                        ret = $"{GetTypeName(oriType)}[]";
                        break;
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                    {
                        var oriType = il2cpp.GetIl2CppType(type.data.type);
                        ret = $"{GetTypeName(oriType)}*";
                        break;
                    }
                default:
                    ret = TypeString[(int)type.type];
                    break;
            }

            return ret;
        }

        private static string GetTypeName(Il2CppTypeDefinition typeDef)
        {
            var ret = string.Empty;
            if (typeDef.declaringTypeIndex != -1)
            {
                ret += GetTypeName(il2cpp.types[typeDef.declaringTypeIndex]) + ".";
            }
            ret += metadata.GetStringFromIndex(typeDef.nameIndex);
            return ret;
        }

        private static string GetGenericTypeParams(Il2CppGenericInst genericInst)
        {
            var typeNames = new List<string>();
            var pointers = il2cpp.GetPointers(genericInst.type_argv, (long)genericInst.type_argc);
            for (uint i = 0; i < genericInst.type_argc; ++i)
            {
                var oriType = il2cpp.GetIl2CppType(pointers[i]);
                typeNames.Add(GetTypeName(oriType));
            }
            return $"<{string.Join(", ", typeNames)}>";
        }

        private static DumpAttribute[] GetCustomAttributes(Il2CppImageDefinition image, int customAttributeIndex, uint token, string padding = "")
        {
            if (!config.DumpAttribute || il2cpp.version < 21)
                return null;
            var index = metadata.GetCustomAttributeIndex(image, customAttributeIndex, token);
            if (index >= 0)
            {
                var attributeTypeRange = metadata.attributeTypeRanges[index];
                var attributes = new List<DumpAttribute>();
                for (var i = 0; i < attributeTypeRange.count; i++)
                {
                    var typeIndex = metadata.attributeTypes[attributeTypeRange.start + i];
                    var methodPointer = il2cpp.customAttributeGenerators[index];
                    var address = il2cpp.MapVATR(methodPointer);
                    attributes.Add(new DumpAttribute { Address = address, Name = $"[{GetTypeName(il2cpp.types[typeIndex])}]" });
                }
                return attributes.ToArray();
            }
            else
            {
                return null;
            }
        }

        private static string GetModifiers(Il2CppMethodDefinition methodDef)
        {
            if (methodModifiers.TryGetValue(methodDef, out string str))
                return str;
            var access = methodDef.flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK;
            switch (access)
            {
                case METHOD_ATTRIBUTE_PRIVATE:
                    str += "private ";
                    break;
                case METHOD_ATTRIBUTE_PUBLIC:
                    str += "public ";
                    break;
                case METHOD_ATTRIBUTE_FAMILY:
                    str += "protected ";
                    break;
                case METHOD_ATTRIBUTE_ASSEM:
                case METHOD_ATTRIBUTE_FAM_AND_ASSEM:
                    str += "internal ";
                    break;
                case METHOD_ATTRIBUTE_FAM_OR_ASSEM:
                    str += "protected internal ";
                    break;
            }
            if ((methodDef.flags & METHOD_ATTRIBUTE_STATIC) != 0)
                str += "static ";
            if ((methodDef.flags & METHOD_ATTRIBUTE_ABSTRACT) != 0)
            {
                str += "abstract ";
                if ((methodDef.flags & METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == METHOD_ATTRIBUTE_REUSE_SLOT)
                    str += "override ";
            }
            else if ((methodDef.flags & METHOD_ATTRIBUTE_FINAL) != 0)
            {
                if ((methodDef.flags & METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == METHOD_ATTRIBUTE_REUSE_SLOT)
                    str += "sealed override ";
            }
            else if ((methodDef.flags & METHOD_ATTRIBUTE_VIRTUAL) != 0)
            {
                if ((methodDef.flags & METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == METHOD_ATTRIBUTE_NEW_SLOT)
                    str += "virtual ";
                else
                    str += "override ";
            }
            if ((methodDef.flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) != 0)
                str += "extern ";
            methodModifiers.Add(methodDef, str);
            return str;
        }

        private static string ToEscapedString(string s)
        {
            var re = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\'':
                        re.Append(@"\'");
                        break;
                    case '"':
                        re.Append(@"\""");
                        break;
                    case '\t':
                        re.Append(@"\t");
                        break;
                    case '\n':
                        re.Append(@"\n");
                        break;
                    case '\r':
                        re.Append(@"\r");
                        break;
                    case '\f':
                        re.Append(@"\f");
                        break;
                    case '\b':
                        re.Append(@"\b");
                        break;
                    case '\\':
                        re.Append(@"\\");
                        break;
                    case '\0':
                        re.Append(@"\0");
                        break;
                    case '\u0085':
                        re.Append(@"\u0085");
                        break;
                    case '\u2028':
                        re.Append(@"\u2028");
                        break;
                    case '\u2029':
                        re.Append(@"\u2029");
                        break;
                    default:
                        re.Append(c);
                        break;
                }
            }
            return re.ToString();
        }
    }
}
