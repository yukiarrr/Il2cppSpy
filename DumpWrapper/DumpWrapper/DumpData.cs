namespace Wrapper
{
    public class DumpType
    {
        public string Namespace { get; set; }
        public DumpAttribute[] Attributes { get; set; }
        public string Modifier { get; set; }
        public string TypeStr { get; set; }
        public string Name { get; set; }
        public string[] Extends { get; set; }
        public DumpField[] Fields { get; set; }
        public DumpProperty[] Properties { get; set; }
        public DumpMethod[] Methods { get; set; }
    }

    public class DumpAttribute
    {
        public string Name { get; set; }
        public ulong Address { get; set; }
    }

    public class DumpField
    {
        public DumpAttribute[] Attributes { get; set; }
        public string Modifier { get; set; }
        public string TypeStr { get; set; }
        public string Name { get; set; }
        public string ValueStr { get; set; }
    }

    public class DumpProperty
    {
        public DumpAttribute[] Attributes { get; set; }
        public string Modifier { get; set; }
        public string TypeStr { get; set; }
        public string Name { get; set; }
        public string Access { get; set; }
    }

    public class DumpMethod
    {
        public DumpAttribute[] Attributes { get; set; }
        public string Modifier { get; set; }
        public string TypeStr { get; set; }
        public string Name { get; set; }
        public string[] Parameters { get; set; }
        public ulong Address { get; set; }
    }
}
