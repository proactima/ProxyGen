using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProxyGen.Models
{
    public abstract class InterfaceDefinitionBase
    {
        public IEnumerable<UsingDirectiveSyntax> UsingDirectives { get; set; }
        public string InterfaceName { get; set; }
        public IEnumerable<SyntaxNode> InterfaceMethods { get; set; }
    }
}