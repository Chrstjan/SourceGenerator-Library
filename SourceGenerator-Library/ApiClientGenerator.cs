using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator_Library
{
	[Generator]
	public class ApiClientGenerator : IIncrementalGenerator
	{
		
		private const string AttributeSourceCode =
			"""
			using System;
			namespace SourceGenerator_Library
			{
				[AttributeUsage(AttributeTargets.Interface)]
				[global::Microsoft.CodeAnalysis.EmbeddedAttribute]
				internal class GenerateApiClientAttribute : Attribute
				{
					public string BaseUrl { get; }

					public GenerateApiClientAttribute(string baseUrl)
					{
						BaseUrl = baseUrl;
					}
				}
			}
			""";

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			// emitting the marker attribute
			context.RegisterPostInitializationOutput(ctx =>
			{
				ctx.AddEmbeddedAttributeDefinition();
				ctx.AddSource("GenerateApiClientAttribute.g.cs", AttributeSourceCode);
			});

			// Creating the pipeline
			var interfaceProvider =
				context.SyntaxProvider.ForAttributeWithMetadataName(
					"SourceGenerator_Library.GenerateApiClientAttribute",

					// Filtering syntax nodes
					static (node, _) => node is InterfaceDeclarationSyntax,

					// Getting the nodes
					static (ctx, _) =>
					{
						return (INamedTypeSymbol)ctx.TargetSymbol;
					}
				);

			context.RegisterSourceOutput(
				interfaceProvider,
				static (spc, interfaceSymbol) => {
					GenerateClient(spc, interfaceSymbol);
				}
			);
		}

		private static void GenerateClient(SourceProductionContext context, INamedTypeSymbol interfaceSymbol) {
			var className = interfaceSymbol.Name.StartsWith("I")
				? interfaceSymbol.Name.Substring(1) + "Client"
				: interfaceSymbol.Name + "Client";

			var namespaceName =
				interfaceSymbol.ContainingNamespace.IsGlobalNamespace
					? null
					: interfaceSymbol.ContainingNamespace.ToDisplayString();

			var methods = interfaceSymbol.GetMembers().OfType<IMethodSymbol>();

			var attribute = interfaceSymbol.GetAttributes()
				.FirstOrDefault(x => x.AttributeClass?.Name == "GenerateApiClientAttribute");
			var baseUrl = attribute?.ConstructorArguments[0].Value?.ToString()
				?? "https://jsonplaceholder.typicode.com";

			var methodsBuilder = new StringBuilder();
			foreach (var method in methods) {
				GenerateMethod(methodsBuilder, method);
			}

			// Generating the client class
			var source =
			$$"""
						using System.Net.Http;
						using System.Net.Http.Json;
						using System.Threading.Tasks;

						{{(namespaceName is not null ? $"namespace {namespaceName};" : "")}}
						
							public partial class {{className}} : {{interfaceSymbol.Name}}
							{
								private readonly HttpClient _httpClient;

								public {{className}}(HttpClient httpClient)
								{
									_httpClient = httpClient;
									_httpClient.BaseAddress = new System.Uri("{{baseUrl}}");
								}

								{{methodsBuilder}}
							}
						
					""";
			context.AddSource($"{className}.g.cs", source);
		}

		private static void GenerateMethod(StringBuilder builder, IMethodSymbol method) {

			var parameter = method.Parameters.FirstOrDefault();
			var parameterType = parameter?.Type.ToDisplayString();
			var parameterName = parameter?.Name;

			var returnType = GetTaskResultType(method.ReturnType)?.ToDisplayString() ?? "object";

			var route = method.Name.Replace("Async", "");
			builder.AppendLine(
				$$"""
					public async Task<{{returnType}}> {{method.Name}}({{parameterType}} {{parameterName}}) 
					{
						return await _httpClient.GetFromJsonAsync<{{returnType}}>
							($"{{route}}/{ {{parameterName}} }");
					}
				"""
			);
		}

		private static ITypeSymbol? GetTaskResultType(ITypeSymbol returnType) {

			if (returnType is not INamedTypeSymbol namedType) return null;
			if (namedType.Name != "Task") return null;
			if (namedType.TypeArguments.Length != 1) return null;

			return namedType.TypeArguments[0];
		}
	}
}
