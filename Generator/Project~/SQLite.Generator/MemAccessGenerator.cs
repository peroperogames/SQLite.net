using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace SQLite.Generator;

internal record struct SQLProperty(string Name, string GetMethodName, string SetMethodName, ITypeSymbol PropertyType, bool IsPK);

[Generator]
internal class MemAccessGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(static () => new MemAccessSyntaxContextReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not MemAccessSyntaxContextReceiver syntaxReceiver) return;
        foreach (var kv in syntaxReceiver.Properties)
        {
            context.AddSource($"{kv.Key.ToDisplayString()}.g.cs", BuildScript(kv.Key, kv.Value.ToArray()));
        }
    }

    private string BuildScript(ITypeSymbol type, SQLProperty[] properties)
    {
        var @namespace = type.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {type.ContainingNamespace.ToDisplayString()} {{";
        var sb = new StringBuilder();

        if (type.BaseType.IsGenericType && !type.GetMembers().Any(m => m is IMethodSymbol {Parameters.Length: 0, Name: "GetPrimaryKey"}))
        {
            var pk =  properties.FirstOrDefault(property => property.IsPK);
            if (pk != null)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Get the primary key of the current record.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public override {pk.PropertyType.ToDisplayString()} GetPrimaryKey() => {pk.Name};");
            }
        }

        // 为属性生成 ORM 方法
        foreach (var property in properties)
        {
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Get the value of the {property.Name} property.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public {property.PropertyType.ToDisplayString()} {property.GetMethodName}() => {property.Name};");
            if (string.IsNullOrEmpty(property.SetMethodName)) continue;
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Set the value of the {property.Name} property.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public void {property.SetMethodName}({property.PropertyType.ToDisplayString()} value)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var origin = {property.Name};");
            sb.AppendLine($"        {property.Name} = value;");
            sb.AppendLine($"        if (Connection.IsInTransaction)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            Connection.TryGetSavePoint(out var savepoint);");
            sb.AppendLine($"            Connection.RegisterRollbackHandler({property.Name}RollbackHandler.Require(this, origin), savepoint);");
            sb.AppendLine($"            Connection.Update(this);");
            sb.AppendLine($"        }}");
            sb.AppendLine($"        else");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            if (Connection.Update(this) <= 0) {property.Name} = origin;");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");

            sb.AppendLine($"    private class {property.Name}RollbackHandler : SQLite.IRollbackHandler");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        private static readonly System.Collections.Generic.Stack<{property.Name}RollbackHandler> m_Pool = new();");
            sb.AppendLine($"        private {type.ToDisplayString()} m_Provider;");
            sb.AppendLine($"        private {property.PropertyType.ToDisplayString()} m_Origin;");
            sb.AppendLine($"        public static {property.Name}RollbackHandler Require({type.ToDisplayString()} provider,  {property.PropertyType.ToDisplayString()} origin)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            if (!m_Pool.TryPop(out var handler))");
            sb.AppendLine($"                handler = new {property.Name}RollbackHandler();");
            sb.AppendLine($"            handler.m_Provider = provider;");
            sb.AppendLine($"            handler.m_Origin = origin;");
            sb.AppendLine($"            return handler;");
            sb.AppendLine($"        }}");
            sb.AppendLine($"        void SQLite.IRollbackHandler.OnRollback()");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            m_Provider.{property.Name} = m_Origin;");
            sb.AppendLine($"            m_Origin = default;");
            sb.AppendLine($"            m_Provider = null;");
            sb.AppendLine($"            m_Pool.Push(this);");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
        }

        return
            $$"""
            using SQLite;
            {{@namespace}}
            partial class {{type.Name}}
            {
            {{(ContainsEmptyCtor(type) ? string.Empty : $"    public {type.Name}() {{ }}")}}
            {{sb}}
            }{{(type.ContainingNamespace.IsGlobalNamespace ? "" : "}")}}
            """;
    }

    private bool ContainsEmptyCtor(ITypeSymbol type)
    {
        return type.GetMembers()
                       .Where(m => m is IMethodSymbol method && method.MethodKind == MethodKind.Constructor)
                       .Cast<IMethodSymbol>()
                       .Any(ctor => ctor.Parameters.Length == 0);
    }
}

internal class MemAccessSyntaxContextReceiver : ISyntaxContextReceiver
{
    private const           string                                     IGNORE_ATTRIBUTE_TYPE         = "SQLite.IgnoreAttribute";
    private const           string                                     AUTO_INCREMENT_ATTRIBUTE_TYPE = "SQLite.AutoIncrementAttribute";
    private const           string                                     PRIMARY_KEY_ATTRIBUTE_TYPE    = "SQLite.PrimaryKeyAttribute";
    private static readonly SymbolEqualityComparer                     s_SymbolComparer              = SymbolEqualityComparer.Default;
    internal readonly       Dictionary<ITypeSymbol, List<SQLProperty>> Properties                    = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;

        if (semanticModel.GetDeclaredSymbol(context.Node) is not ITypeSymbol
        {
            BaseType:
            {
                Name                    : "SQLObject",
                ContainingNamespace.Name: "SQLite"
            }
        } typeSymbol)
        {
            return;
        }

        var ignoreAttrType = compilation.GetTypeByMetadataName(IGNORE_ATTRIBUTE_TYPE);
        var pkAttrType = compilation.GetTypeByMetadataName(PRIMARY_KEY_ATTRIBUTE_TYPE);
        var autoIncrementAttrType = compilation.GetTypeByMetadataName(AUTO_INCREMENT_ATTRIBUTE_TYPE);
        foreach (var property in typeSymbol.GetMembers()
                                           .Where(m => m.Kind == SymbolKind.Property
                                               && !m.IsStatic
                                               && !m.GetAttributes()
                                                    .Any(a => s_SymbolComparer.Equals(a.AttributeClass, ignoreAttrType)))
                                           .Cast<IPropertySymbol>())
        {
            var ispk = property.GetAttributes().Any(a => s_SymbolComparer.Equals(a.AttributeClass, pkAttrType));
            var propertyFormatName = char.ToUpper(property.Name[0]) + property.Name.Substring(1);
            var getMethodName = "Get" + propertyFormatName;
            // 自增的主键不允许 Set
            var setMethodName = property.GetAttributes().Any(a => s_SymbolComparer.Equals(a.AttributeClass, autoIncrementAttrType))
                ? null
                : "Set" + propertyFormatName;

            var propertyType = property.Type;
            var sqlProperty = new SQLProperty(property.Name, getMethodName, setMethodName, propertyType, ispk);
            if (!Properties.TryGetValue(typeSymbol, out var sqlProperties))
            {
                sqlProperties = [];
                Properties.Add(typeSymbol, sqlProperties);
            }

            sqlProperties.Add(sqlProperty);
        }
    }
}
