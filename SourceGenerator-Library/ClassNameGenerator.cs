using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace SourceGenerator_Library
{
	[Generator]
	public class ClassNameGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var provider = context.SyntaxProvider.CreateSyntaxProvider(
				predicate: (c, _) => c is ClassDeclarationSyntax,
				transform: (n, _) => (ClassDeclarationSyntax)n.Node
			).Where(x => x != null);

			var compilation = context.CompilationProvider.Combine(provider.Collect());

			context.RegisterSourceOutput(compilation,
				(spc, source) => Execute(spc, source.Left, source.Right));
		}

		private void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> typeList)
		{
			//if (!Debugger.IsAttached) Debugger.Launch();	Note, only used when debugging
			if (typeList.Length == 0) {
				var desc = new DiagnosticDescriptor("SG0001", "No Classes Found", "No classes declared in the actual project.", "Problem", DiagnosticSeverity.Warning, true);

				context.ReportDiagnostic(Diagnostic.Create(desc, Location.None));
			}

			var sb = new StringBuilder();

			foreach (var syntax in typeList)
			{
				var symbol = compilation
					.GetSemanticModel(syntax.SyntaxTree)
					.GetDeclaredSymbol(syntax) as INamedTypeSymbol;
				if (symbol != null)
				{
					sb.AppendLine();
					sb.Append($"      \"{symbol.ToDisplayString()}\",");
				}
			}

			if (sb.Length > 0) sb.Length--;

			var code = $$"""
				namespace SourceGenerator_Library 
				{
					public static class ClassNames
					{
						public static List<string> Names = new List<string>()
						{
							{{sb.ToString()}}
						};
					}
				}
				""";

			context.AddSource("ClassNames.g.cs", code);
		}
	}
}
