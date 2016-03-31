using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using ProxyGen.Models;

namespace ProxyGen
{
    public class ProxyGenerator
    {
        private readonly Extractor _extractor;
        private readonly Generator _generator;

        public ProxyGenerator(Extractor extractor, Generator generator)
        {
            _extractor = extractor;
            _generator = generator;
        }

        public async Task RunAsync(CancellationToken cancellationToken, string[] args)
        {
            var workspace = MSBuildWorkspace.Create();
            var path = args[0];

            Console.WriteLine("Loading solution...");
            var solution = await workspace.OpenSolutionAsync(path, cancellationToken).ConfigureAwait(false);
            var newSolution = solution;

            var interfaceProject = solution.Projects.Single(x => x.Name.Equals("Core.Interfaces"));

            foreach (var documentId in interfaceProject.DocumentIds)
            {
                var document = newSolution.GetDocument(documentId);
                if(document.Name.Contains("IResolveServiceReferences"))
                    continue;

                Console.WriteLine($"Reading {document.Name}");
                var documentSyntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var def = _extractor.Extract(documentSyntaxRoot);

                if (def is NotAnInterfaceDefinition)
                    continue;

                var interfaceDefinition = def as InterfaceDefinition;

                Console.WriteLine($"Generating proxy for {document.Name}");
                var generatedProxy = _generator.Generate(interfaceDefinition);

                var proxyFileName = $"{generatedProxy.Name}.cs";

                DocumentId docId;
                var existingProxy = interfaceProject.Documents
                    .Where(x => x.Name.Equals(proxyFileName))
                    .ToList();

                if (!existingProxy.Any())
                {
                    docId = DocumentId.CreateNewId(document.Project.Id);
                }
                else
                {
                    var firstDoc = existingProxy.First();
                    docId = firstDoc.Id;
                    newSolution = newSolution.RemoveDocument(docId);
                }

                newSolution = newSolution.AddDocument(docId, proxyFileName, generatedProxy.CompilationUnitSyntax,
                    new[] {"Proxy"});

                var tempDoc = newSolution.GetDocument(docId);
                var formattedDoc =
                    await Formatter.FormatAsync(tempDoc, cancellationToken: cancellationToken).ConfigureAwait(false);

                newSolution = formattedDoc.Project.Solution;
            }

            var saveResult = workspace.TryApplyChanges(newSolution);
            if (!saveResult)
            {
                Console.WriteLine("Failed to save...");
                return;
            }

            Console.WriteLine("Solution updated!");
        }
    }
}