using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProxyGen.Models;

namespace ProxyGen
{
    public class Extractor
    {
        public InterfaceDefinitionBase Extract(SyntaxNode document)
        {
            var namespaces = document.ChildNodes()
                .Where(x => x.IsKind(SyntaxKind.NamespaceDeclaration))
                .ToList();
            if (!namespaces.Any())
                return NotAnInterfaceDefinition.Create();

            var interfaceDeclaration = namespaces.First().ChildNodes()
                .Where(x => x.IsKind(SyntaxKind.InterfaceDeclaration))
                .ToList();

            if (!interfaceDeclaration.Any())
                return NotAnInterfaceDefinition.Create();

            var usingDirectives = document.ChildNodes()
                .Where(x => x.IsKind(SyntaxKind.UsingDirective))
                .Select(x => (UsingDirectiveSyntax)x);

            var interfaceNameNode = interfaceDeclaration.First();
            var interfaceName = interfaceNameNode.ChildTokens()
                .Single(x => x.IsKind(SyntaxKind.IdentifierToken))
                .ValueText;

            var methods = interfaceDeclaration.First().ChildNodes()
                .Where(x => x.IsKind(SyntaxKind.MethodDeclaration));

            var definition = new InterfaceDefinition
            {
                InterfaceMethods = methods,
                InterfaceName = interfaceName,
                UsingDirectives = usingDirectives
            };

            return definition;
        }
    }
}
