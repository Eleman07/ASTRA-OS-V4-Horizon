using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Text.RegularExpressions;

string root=Path.GetFullPath(args[0]);var keys=new HashSet<string>(StringComparer.Ordinal);
foreach(var file in Directory.EnumerateFiles(Path.Combine(root,"src","Astra.Desktop"),"*.cs",SearchOption.AllDirectories).Where(p=>!p.Contains(Path.DirectorySeparatorChar+"obj"+Path.DirectorySeparatorChar)&&!p.Contains(Path.DirectorySeparatorChar+"bin"+Path.DirectorySeparatorChar)).Concat(new[]{"StudyCatalog.cs","SimulationLab.cs","Widgets.cs","EncryptedWidgetStore.cs","MeshImport.cs","WorkspaceSessions.cs","ScientificScene.cs","ProfileLabs.cs","ThemeCursors.cs"}.Select(x=>Path.Combine(root,"src","Astra.Core",x))))
{
    var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.Preview));var rewriter=new UiRewriter(keys,Path.GetFileName(file));var changed=rewriter.Visit(tree.GetRoot())!;
    foreach(var call in changed.DescendantNodes().OfType<InvocationExpressionSyntax>())if((call.Expression.ToString().EndsWith("UiText.T")||call.Expression.ToString().EndsWith("UiText.F"))&&call.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal)keys.Add(literal.Token.ValueText);
    foreach(var literal in changed.DescendantNodes().OfType<LiteralExpressionSyntax>())if(literal.IsKind(SyntaxKind.StringLiteralExpression)&&literal.Ancestors().Any(a=>a is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax)&&literal.Ancestors().Any(a=>a is AssignmentExpressionSyntax assign&&assign.Left.ToString().EndsWith("ItemsSource")||a is VariableDeclaratorSyntax variable&&new[]{"kinds","kindNames","Categories"}.Contains(variable.Identifier.Text))&&UiRewriter.Display(literal.Token.ValueText))keys.Add(literal.Token.ValueText);
    if(changed.ToFullString()!=tree.GetRoot().ToFullString()){var backup=Path.Combine(root,"docs","evidence","localization-originals",Path.GetFileName(file));Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup))File.Copy(file,backup);File.WriteAllText(file,changed.ToFullString());}
}
string directory=Path.Combine(root,"src","Astra.Desktop","Localization");Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"sources-csharp.json"),JsonSerializer.Serialize(keys.Order().ToArray(),new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("UI catalog sources: "+keys.Count);

sealed class UiRewriter(HashSet<string> keys,string file):CSharpSyntaxRewriter
{
    private static readonly HashSet<string> Properties=new(){"Text","Header","Content","PlaceholderText","OnContent","OffContent","Title","CloseButtonText","PrimaryButtonText","SecondaryButtonText","summary","message"};
    bool Want(SyntaxNode node)
    {
        if(node.Ancestors().Any(a=>a is AttributeSyntax or ConstantPatternSyntax)||Invariant(node))return false;
        if(node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i=>i.Expression.ToString().EndsWith("UiText.T")||i.Expression.ToString().EndsWith("UiText.F")))return false;
        foreach(var a in node.Ancestors())
        {
            if(a is LambdaExpressionSyntax)return false;
            if(file=="WindowsToolCatalog.cs"&&a is ArgumentSyntax tupleArg&&tupleArg.Parent is TupleExpressionSyntax tuple&&tuple.Arguments.IndexOf(tupleArg)==0)return true;
            if(a is AssignmentExpressionSyntax assign)return Properties.Contains(assign.Left is MemberAccessExpressionSyntax assignedMember?assignedMember.Name.Identifier.Text:assign.Left.ToString());
            if(a is InvocationExpressionSyntax call)
            {
                string name=call.Expression is MemberAccessExpressionSyntax m?m.Name.Identifier.Text:call.Expression.ToString();
                if(name is "Label" or "Action" or "Details" or "Button" or "SetToolTip" or "SetName" or "LogWidgetEvent")return true;
                if(name=="P"&&node.Ancestors().OfType<ArgumentSyntax>().FirstOrDefault() is {} argument&&call.ArgumentList.Arguments.IndexOf(argument)==1)return true;
            }
            if(a is ObjectCreationExpressionSyntax obj&&obj.Type.ToString().EndsWith("Exception")&&obj.ArgumentList?.Arguments.FirstOrDefault()?.Expression.Span.Contains(node.Span)==true)return true;
            if(file=="StudyCatalog.cs"&&a is ArgumentSyntax arg&&arg.Parent is ArgumentListSyntax list&&list.Arguments.IndexOf(arg)==1)return true;
            if(file=="SimulationLab.cs"&&a is ArgumentSyntax t&&t.Parent is ArgumentListSyntax args&&(args.Parent is ImplicitObjectCreationExpressionSyntax || args.Parent is ObjectCreationExpressionSyntax {Type:IdentifierNameSyntax{Identifier.ValueText:"SimulationTool"}})&&args.Arguments.IndexOf(t) is 1 or 2 or 3)return true;
        }
        return false;
    }
    static bool Invariant(SyntaxNode node)=>node.Ancestors().Any(a=>a is ObjectCreationExpressionSyntax obj&&new[]{"FontFamily","Uri","Regex"}.Contains(obj.Type.ToString().Split('.').Last())||a is InvocationExpressionSyntax call&&new[]{"Token","GetProperty","TryGetProperty","GetField","GetMethod","FindName","ToString","GetValue","SetValue"}.Contains(call.Expression is MemberAccessExpressionSyntax member?member.Name.Identifier.Text:call.Expression.ToString()));
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if(node.Expression.ToString().EndsWith("UiText.T")&&node.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal&&(Invariant(node)||Regex.IsMatch(literal.Token.ValueText,@"\A#[0-9a-fA-F]{6}\z")))return literal.WithTriviaFrom(node);
        return base.VisitInvocationExpression(node);
    }
    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if(node.IsKind(SyntaxKind.StringLiteralExpression)&&Want(node)&&Display(node.Token.ValueText))
        {
            keys.Add(node.Token.ValueText);return SyntaxFactory.ParseExpression("AstraOS.Core.UiText.T("+node.WithoutTrivia().ToFullString()+")").WithTriviaFrom(node);
        }
        return base.VisitLiteralExpression(node);
    }
    public override SyntaxNode? VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
    {
        if(!Want(node))return base.VisitInterpolatedStringExpression(node);var format=new System.Text.StringBuilder();var expressions=new List<string>();
        foreach(var part in node.Contents)
        {
            if(part is InterpolatedStringTextSyntax text)format.Append(text.TextToken.ValueText);
            else if(part is InterpolationSyntax item){format.Append('{').Append(expressions.Count);if(item.AlignmentClause is {} alignment)format.Append(',').Append(alignment.Value.ToString());if(item.FormatClause is {} spec)format.Append(':').Append(spec.FormatStringToken.ValueText);format.Append('}');expressions.Add(item.Expression.WithoutTrivia().ToFullString());}
        }
        if(!Display(format.ToString()))return node;keys.Add(format.ToString());string literal=SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,SyntaxFactory.Literal(format.ToString())).ToFullString();
        return SyntaxFactory.ParseExpression("AstraOS.Core.UiText.F("+literal+(expressions.Count>0?", "+string.Join(", ",expressions):"")+")").WithTriviaFrom(node);
    }
    public static bool Display(string s)=>s.Length>=3&&s.Any(char.IsLetter)&&!Regex.IsMatch(s,@"\A[a-z0-9_.:/\\-]+\z")&&!s.Contains("xmlns=")&&!s.Contains("https://")&&!s.StartsWith("C:\\",StringComparison.Ordinal);
}
