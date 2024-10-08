#nullable enable
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace WizBot.Generators
{
    public readonly record struct MethodPermData
    {
        public readonly string Name;
        public readonly string Value;

        public MethodPermData(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }


    [Generator]
    public class GrpcApiPermGenerator : IIncrementalGenerator
    {
        public const string Attribute =
            """
            namespace WizBot.GrpcApi;

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class GrpcApiPermAttribute : System.Attribute
            {
                public GuildPerm Value { get; }
                public GrpcApiPermAttribute(GuildPerm value) => Value = value;
            }
            """;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource("GrpcApiPermAttribute.cs",
                SourceText.From(Attribute, Encoding.UTF8)));

            var enumsToGenerate = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "WizBot.GrpcApi.GrpcApiPermAttribute",
                    predicate: static (s, _) => s is MethodDeclarationSyntax,
                    transform: static (ctx, _) => GetMethodSemanticTargets(ctx.SemanticModel, ctx.TargetNode))
                .Where(static m => m is not null)
                .Select(static (x, _) => x!.Value)
                .Collect();

            context.RegisterSourceOutput(enumsToGenerate,
                static (spc, source) => Execute(source, spc));
        }

        private static MethodPermData? GetMethodSemanticTargets(SemanticModel model, SyntaxNode node)
        {
            var method = (MethodDeclarationSyntax)node;

            var name = method.Identifier.Text;
            var attr = method.AttributeLists
                .SelectMany(x => x.Attributes)
                .FirstOrDefault();
                // .FirstOrDefault(x => x.Name.ToString() == "GrpcApiPermAttribute");


            if (attr is null)
                return null;

            // if (model.GetSymbolInfo(attr).Symbol is not IMethodSymbol attrSymbol)
            //     return null;

            return new  MethodPermData(name, attr.ArgumentList?.Arguments[0].ToString() ?? "__missing_perm__");
            // return new MethodPermData(name, attrSymbol.Parameters[0].ContainingType.ToDisplayString() + "." + attrSymbol.Parameters[0].Name);
        }

        private static void Execute(ImmutableArray<MethodPermData> fields, SourceProductionContext ctx)
        {
            using (var stringWriter = new StringWriter())
            using (var sw = new IndentedTextWriter(stringWriter))
            {
                sw.WriteLine("using System.Collections.Frozen;");
                sw.WriteLine();
                sw.WriteLine("namespace WizBot.GrpcApi;");
                sw.WriteLine();

                sw.WriteLine("public partial class GrpcApiPermsInterceptor");
                sw.WriteLine("{");

                sw.Indent++;

                sw.WriteLine("public static FrozenDictionary<string, GuildPerm> perms = new Dictionary<string, GuildPerm>()");
                sw.WriteLine("{");

                sw.Indent++;
                foreach (var field in fields)
                {
                    sw.WriteLine("{{ \"{0}\", {1} }},", field.Name, field.Value);
                }

                sw.Indent--;
                sw.WriteLine("}.ToFrozenDictionary();");

                sw.Indent--;
                sw.WriteLine("}");

                sw.Flush();
                ctx.AddSource("GrpcApiInterceptor.g.cs", stringWriter.ToString());
            }
        }

        private List<TranslationPair> GetFields(string? dataText)
        {
            if (string.IsNullOrWhiteSpace(dataText))
                return new();

            Dictionary<string, string> data;
            try
            {
                var output = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataText!);
                if (output is null)
                    return new();

                data = output;
            }
            catch
            {
                Debug.WriteLine("Failed parsing responses file.");
                return new();
            }

            var list = new List<TranslationPair>();
            foreach (var entry in data)
            {
                list.Add(new(
                    entry.Key,
                    entry.Value
                ));
            }

            return list;
        }
    }
}