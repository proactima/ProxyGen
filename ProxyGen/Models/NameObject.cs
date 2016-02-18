using Humanizer;

namespace ProxyGen.Models
{
    public class NameObject
    {
        public NameObject(InterfaceDefinition iFace)
        {
            InterfaceName = iFace.InterfaceName;
            var nonIFace = InterfaceName.Substring(1, InterfaceName.Length - 1);

            PrivateName = "_service";
            ConstructorArgName = nonIFace.Camelize();
            ClassName = $"{nonIFace}Proxy";
        }

        public string InterfaceName { get; set; }
        public string PrivateName { get; set; }
        public string ConstructorArgName { get; set; }
        public string ClassName { get; set; }
    }
}