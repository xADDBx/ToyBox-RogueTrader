using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ToyBox.Analyzer {
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToyBoxAnalyzerLocalizationFixProvider)), Shared]
    public class ToyBoxAnalyzerLocalizationFixProvider : CodeFixProvider {
        public sealed override ImmutableArray<string> FixableDiagnosticIds {
            get { return ImmutableArray.Create(["LOC001", "LOC002"]); }
        }

        public sealed override FixAllProvider GetFixAllProvider() {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node == null) return;

            if (context.Document is null || context.Document is SourceGeneratedDocument)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Move to LocalizedString field",
                    createChangedDocument: c => MoveToLocalizedStringAsync(context.Document, node, c),
                    equivalenceKey: "MoveToLocalizedString"),
                diagnostic);
        }

        private string ReplaceBadChar(string s) {
            StringBuilder sb = new();
            bool first = true;
            foreach (var c in s) {
                if (first && !SyntaxFacts.IsIdentifierStartCharacter(c)) {
                    sb.Append('_');
                }
                first = false;
                if (SyntaxFacts.IsIdentifierPartCharacter(c)) {
                    sb.Append(c);
                } else {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
        private async Task<Document> MoveToLocalizedStringAsync(Document document, SyntaxNode node, CancellationToken cancellationToken) {
            try {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if (root == null)
                    return document;

                var classDeclaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDeclaration == null)
                    return document;

                // Create a new field that holds the string literal.
                ExpressionSyntax initializerExpr = null;
                string val = null;
                if (node is LiteralExpressionSyntax literal) {
                    initializerExpr = (ExpressionSyntax)node;
                    val = literal.Token.ValueText;
                } else if (node is ArgumentSyntax argument) {
                    initializerExpr = argument.Expression;
                    val = (argument.Expression as LiteralExpressionSyntax).Token.ValueText;
                }
                // Generate a unique field name.
                string pascalCased;
                if (!string.IsNullOrEmpty(val)) {
                    var capitalizedArray = val.Split(' ').Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => string.Concat(s[0].ToString().ToUpperInvariant(), new(s.Skip(1).ToArray())));
                    pascalCased = string.Join("", capitalizedArray);
                } else {
                    pascalCased = "";
                }
                string identifier = ReplaceBadChar(pascalCased.Substring(0, Math.Min(pascalCased.Length, 32)));
                identifier = $"m_{identifier}LocalizedText";
                var attribute = AttributeList(
                                            SingletonSeparatedList<AttributeSyntax>(
                                                Attribute(
                                                    IdentifierName("LocalizedString"))
                                                .WithArgumentList(
                                                    AttributeArgumentList(
                                                        SeparatedList<AttributeArgumentSyntax>(
                                                            new SyntaxNodeOrToken[]{
                                                            AttributeArgument(
                                                                LiteralExpression(
                                                                    SyntaxKind.StringLiteralExpression,
                                                                    Literal(ReplaceBadChar($"{GetNamespaceAndClassName(classDeclaration)}.{identifier}")))),
                                                            Token(SyntaxKind.CommaToken),
                                                            AttributeArgument(
                                                                initializerExpr)})))))
                                .WithTrailingTrivia(TriviaList(LineFeed));
                var newProperty = PropertyDeclaration(
                                    PredefinedType(
                                        Token(SyntaxKind.StringKeyword)),
                                    Identifier(identifier))
                                .WithAttributeLists(SingletonList(attribute))
                                .WithModifiers(
                                    TokenList(
                                        new[]{
                                        Token(SyntaxKind.PrivateKeyword),
                                        Token(SyntaxKind.StaticKeyword),
                                        Token(SyntaxKind.PartialKeyword)}))
                                .WithAccessorList(
                                    AccessorList(
                                        SingletonList(
                                            AccessorDeclaration(
                                                SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(
                                                Token(SyntaxKind.SemicolonToken)))))
                                .WithTrailingTrivia(TriviaList(LineFeed));
                var fieldReference = IdentifierName(identifier);
                var updatedClassDeclaration = classDeclaration.ReplaceNode(initializerExpr, fieldReference);
                var newClassDeclaration = updatedClassDeclaration.AddMembers(newProperty);
                if (!newClassDeclaration.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword))) {
                    newClassDeclaration = newClassDeclaration.AddModifiers(Token(SyntaxKind.PartialKeyword));
                }
                var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

                return document.WithSyntaxRoot(newRoot);
            } catch (Exception) {
                return document;
            }
        }
        private static string GetNamespaceAndClassName(ClassDeclarationSyntax classDeclaration) {
            var className = classDeclaration.Identifier.Text;
            var namespaceDeclaration = classDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            string namespaceName = namespaceDeclaration != null ? namespaceDeclaration.Name.ToString() : "Global";
            return $"{namespaceName}.{className}";
        }
    }
}
