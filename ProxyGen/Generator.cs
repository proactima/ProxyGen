using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProxyGen.Models;

namespace ProxyGen
{
    public class Generator
    {
        public CreatedProxy Generate(InterfaceDefinition definition)
        {
            var name = new NameObject(definition);
            var classNameBase = definition.InterfaceName.Substring(1, definition.InterfaceName.Length - 1);
            var className = $"{classNameBase}Proxy";
            var constructor = CreateConstructor(name);

            var privateMembers = CreatePrivateMembers(name);

            var proxyMethods = GenerateProxyMethods(definition.InterfaceMethods, name.PrivateName);

            var classDecl = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(definition.InterfaceName)))
                .AddMembers(privateMembers.ToArray())
                .AddMembers(constructor)
                .AddMembers(proxyMethods.ToArray());

            var nameSpace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("Core.Interfaces.Proxy"))
                .AddMembers(classDecl);

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(definition.UsingDirectives.ToArray())
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Core.Interfaces.Wrappers")))
                .AddMembers(nameSpace);

            var proxy = new CreatedProxy
            {
                CompilationUnitSyntax = compilationUnit,
                Name = className
            };

            return proxy;
        }

        private ConstructorDeclarationSyntax CreateConstructor(NameObject name)
        {
            var paramList = new SeparatedSyntaxList<ParameterSyntax>();

            var parameter = SyntaxFactory
                .Parameter(SyntaxFactory.Identifier(name.ConstructorArgName))
                .WithType(SyntaxFactory.IdentifierName(name.InterfaceName));

            paramList = paramList.Add(parameter);

            var statements = new List<StatementSyntax>();

            var statement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(name.PrivateName),
                SyntaxFactory.IdentifierName(name.ConstructorArgName))
                .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.EqualsToken)));

            statements.Add(statement);

            var constructor = SyntaxFactory.ConstructorDeclaration(name.ClassName)
                .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(paramList))
                .WithBody(SyntaxFactory.Block(statements));

            return constructor;
        }

        private List<MemberDeclarationSyntax> CreatePrivateMembers(NameObject name)
        {
            var members = new List<MemberDeclarationSyntax>();
            var privateReadonlyModifiers = SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            var diInterfaceIdentifierName = SyntaxFactory.IdentifierName(name.InterfaceName);
            var diPrivateIdentifier = SyntaxFactory.Identifier(name.PrivateName);

            var privateMember = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(
                diInterfaceIdentifierName)
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(diPrivateIdentifier))))
                .WithModifiers(privateReadonlyModifiers);

            members.Add(privateMember);

            return members;
        }

        private List<MemberDeclarationSyntax> GenerateProxyMethods(IEnumerable<SyntaxNode> interfaceMethods,
            string toCall)
        {
            var methods = new List<MemberDeclarationSyntax>();

            foreach (var interfaceMethod in interfaceMethods)
            {
                var asMethod = interfaceMethod as MethodDeclarationSyntax;
                if (asMethod == null) continue;

                var selfName = interfaceMethod.ChildTokens().First(x => x.IsKind(SyntaxKind.IdentifierToken)).ValueText;
                var selfArgs = interfaceMethod.ChildNodes().Where(x => x.IsKind(SyntaxKind.ParameterList)).ToList();
                methods.Add(GenerateProxyMethod(toCall, selfName, selfArgs, asMethod.ReturnType.WithoutTrivia()));
            }

            return methods;
        }

        private MethodDeclarationSyntax GenerateProxyMethod(string callingClassName, string callingMethodName,
            List<SyntaxNode> selfArgs, TypeSyntax returnType)
        {
            var callingClass = SyntaxFactory.IdentifierName(callingClassName);
            var callingMethod = SyntaxFactory.IdentifierName(callingMethodName);
            var configAwait = SyntaxFactory.IdentifierName("ConfigureAwait");
            var dotToken = SyntaxFactory.Token(SyntaxKind.DotToken);

            var paramList = SyntaxFactory.ParameterList();

            foreach (var syntaxNode in selfArgs)
            {
                var s = syntaxNode as ParameterListSyntax;
                if (s == null)
                    continue;

                paramList = s;
            }

            var argumentList = SyntaxFactory.ArgumentList();

            if (paramList.Parameters.Any())
            {
                argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(paramList.Parameters.First().Identifier.ValueText))
                    ));
            }

            var mainCall = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, callingClass,
                callingMethod)
                .WithOperatorToken(dotToken);

            var mainCallInvoke = SyntaxFactory.InvocationExpression(mainCall).WithArgumentList(argumentList);

            var falseBool = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            var configAwaitArgs = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] {falseBool}));

            var executeWithTimeout = SyntaxFactory.IdentifierName("Core.Interfaces.Wrappers.ServicePolicy");

            var executeArgs = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression(mainCallInvoke))));

            var executeInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    executeWithTimeout, SyntaxFactory.IdentifierName("ExecuteWithTimeoutAndRetry")));

            var awaitCall =
                SyntaxFactory.AwaitExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            executeInvocation.WithArgumentList(executeArgs),
                            configAwait))
                        .WithArgumentList(configAwaitArgs));

            var retStatement = SyntaxFactory.ReturnStatement(awaitCall);
            var bodyBlock = SyntaxFactory.Block(SyntaxFactory.SingletonList(retStatement));

            var method = SyntaxFactory.MethodDeclaration(returnType, callingMethodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .WithParameterList(paramList)
                .WithBody(bodyBlock);

            return method;
        }
    }
}