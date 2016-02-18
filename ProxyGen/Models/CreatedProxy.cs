using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProxyGen.Models
{
    public class CreatedProxy
    {
        public CompilationUnitSyntax CompilationUnitSyntax { get; set; }
        public string Name { get; set; }
    }
}