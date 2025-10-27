using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace ToyBox.Analyzer {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ToyBoxAnalyzer : DiagnosticAnalyzer {
        private const string Category = "Usage";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor("LOC001", "Replace String literal with Localized String", "Replace String literal with Localized String", Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor("LOC002", "UI String should be localized", "UI String should be localized", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Rule3 = new DiagnosticDescriptor("LOC003", "Key should resolve to valid identifier", "The Localized String key should resolve to a valid identifier", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Rule6 = new DiagnosticDescriptor("LOC004", "Name and Description should be localized", "A Feature should have a localized name and description", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Rule4 = new DiagnosticDescriptor("HAR001", "Missing Harmony attributes", "Class '{0}' must have [HarmonyPatch] and [ToyBoxPatchCategory(\"{0}\")] attributes", Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Rule5 = new DiagnosticDescriptor("HAR002", "Missing or incorrect HarmonyName property", "Class '{0}' must override HarmonyName to return \"{0}\"", Category, DiagnosticSeverity.Error, isEnabledByDefault: true);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create([Rule, Rule2, Rule3, Rule4, Rule5, Rule6]); } }

        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeNamedTypeLoc, SymbolKind.NamedType);
        }
        #region PatchFeatureAnalyzer
        private void AnalyzeNamedType(SymbolAnalysisContext context) {
            var namedType = (INamedTypeSymbol)context.Symbol;
            if (namedType.TypeKind != TypeKind.Class) {
                return;
            }            
            // Check if the class inherits from ToyBox.FeatureWithPatch.
            if (!InheritsFromFeatureWithPatch(namedType)) {
                return;
            }
            string fullName = namedType.ToDisplayString();
            bool hasHarmonyPatch = false;
            bool hasToyBoxPatchCategory = false;
            foreach (var attribute in namedType.GetAttributes()) {
                string attrName = attribute.AttributeClass.Name;
                if (attrName.EndsWith("HarmonyPatchAttribute") || attrName.EndsWith("HarmonyPatch")) {
                    hasHarmonyPatch = true;
                } else if (attrName.EndsWith("ToyBoxPatchCategoryAttribute") || attrName.EndsWith("ToyBoxPatchCategory")) {
                    hasToyBoxPatchCategory = true;
                    if (attribute.ConstructorArguments.Length == 1 &&
                        attribute.ConstructorArguments[0].Value is string categoryName &&
                        categoryName != fullName) {
                        // Wrong argument: report diagnostic on the attribute syntax.
                        if (attribute.ApplicationSyntaxReference != null) {
                            var attrSyntax = attribute.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
                            var diag = Diagnostic.Create(Rule4, attrSyntax.GetLocation(), fullName);
                            context.ReportDiagnostic(diag);
                        }
                    }
                }
            }
            if (!hasHarmonyPatch || !hasToyBoxPatchCategory) {
                // Report on the class declaration itself.
                var declRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
                if (declRef != null) {
                    var classDecl = declRef.GetSyntax(context.CancellationToken) as ClassDeclarationSyntax;
                    if (classDecl != null) {
                        var diag = Diagnostic.Create(Rule4, classDecl.Identifier.GetLocation(), fullName);
                        context.ReportDiagnostic(diag);
                    }
                }
            }
            bool foundHarmonyName = false;
            foreach (var member in namedType.GetMembers().OfType<IPropertySymbol>()) {
                if (member.Name != "HarmonyName")
                    continue;

                foundHarmonyName = true;
                // Must be an override, protected and of type string.
                if (!member.IsOverride ||
                    member.Type.SpecialType != SpecialType.System_String) {
                    var diag = Diagnostic.Create(Rule5, member.Locations[0], fullName);
                    context.ReportDiagnostic(diag);
                    continue;
                }

                // Get the syntax to inspect the getter.
                var syntaxRef = member.DeclaringSyntaxReferences.FirstOrDefault();
                if (syntaxRef == null)
                    continue;

                var propDecl = syntaxRef.GetSyntax(context.CancellationToken) as PropertyDeclarationSyntax;
                if (propDecl == null)
                    continue;

                // Look for an expression-bodied or block-bodied getter returning a literal.
                bool correctReturn = false;
                if (propDecl.ExpressionBody != null) {
                    if (propDecl.ExpressionBody.Expression is LiteralExpressionSyntax literal &&
                        literal.Token.ValueText == fullName) {
                        correctReturn = true;
                    }
                }
                if (!correctReturn && propDecl.AccessorList != null) {
                    var getter = propDecl.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
                    if (getter != null) {
                        // Try to find a return statement.
                        var returnStmt = getter.Body?.Statements.OfType<ReturnStatementSyntax>()?.FirstOrDefault();
                        if (returnStmt != null &&
                            returnStmt.Expression is LiteralExpressionSyntax literal &&
                            literal.Token.ValueText == fullName) {
                            correctReturn = true;
                        }
                    }
                }

                if (!correctReturn) {
                    var diag = Diagnostic.Create(Rule5, propDecl.GetLocation(), fullName);
                    context.ReportDiagnostic(diag);
                }
            }
            if (!foundHarmonyName) {
                var declRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
                if (declRef != null) {
                    var classDecl = declRef.GetSyntax(context.CancellationToken) as ClassDeclarationSyntax;
                    if (classDecl != null) {
                        var diag = Diagnostic.Create(Rule5, classDecl.Identifier.GetLocation(), fullName);
                        context.ReportDiagnostic(diag);
                    }
                }
            }
        }
        private bool InheritsFromFeatureWithPatch(INamedTypeSymbol type) {
            var baseType = type.BaseType;
            while (baseType != null) {
                if (baseType.ToString() == "ToyBox.FeatureWithPatch") {
                    return !type.IsAbstract;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }
        #endregion
        #region LocalizationAnalyzer
        private void AnalyzeNamedTypeLoc(SymbolAnalysisContext context) {
            var namedType = (INamedTypeSymbol)context.Symbol;
            if (namedType.TypeKind != TypeKind.Class || namedType.IsAbstract) {
                return;
            }
            if (!InheritsFromFeature(namedType)) {
                return;
            }
            bool hasLocalizedName = false;
            bool hasLocalizedDesc = false;
            foreach (var member in namedType.GetMembers().OfType<IPropertySymbol>()) {
                if (member.Name == "Name") {
                    hasLocalizedName |= member.GetAttributes().Any(attr =>
                        (attr.AttributeClass?.Name.Contains("ToyBox.LocalizedStringAttribute") ?? false) ||
                        (attr.AttributeClass?.ToDisplayString().Contains("ToyBox.LocalizedStringAttribute") ?? false)
                    );
                }
                if (member.Name == "Description") {
                    hasLocalizedDesc |= member.GetAttributes().Any(attr =>
                        (attr.AttributeClass?.Name.Contains("ToyBox.LocalizedStringAttribute") ?? false) ||
                        (attr.AttributeClass?.ToDisplayString().Contains("ToyBox.LocalizedStringAttribute") ?? false)
                    );
                }
            }
            if (!hasLocalizedDesc || !hasLocalizedName) {
                var declRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
                if (declRef != null) {
                    var classDecl = declRef.GetSyntax(context.CancellationToken) as ClassDeclarationSyntax;
                    if (classDecl != null) {
                        var diag = Diagnostic.Create(Rule6, classDecl.GetLocation(), namedType.ToDisplayString());
                        context.ReportDiagnostic(diag);
                    }
                }
            }
        }
        private bool InheritsFromFeature(INamedTypeSymbol type) {
            var baseType = type.BaseType;
            while (baseType != null) {
                if (baseType.ToString() == "ToyBox.Feature") {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }
        private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context) {
            var literal = (LiteralExpressionSyntax)context.Node;
            var stringValue = literal.Token.ValueText;

            var localizedAttr = literal.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault(attr => {
                var nameText = attr.Name.ToString();
                return nameText == "LocalizedString" || nameText.EndsWith(".LocalizedString");
            });

            if (localizedAttr != null) {
                AnalyzeAttributeStringLiteral(context);
                return;
            }

            ExpressionSyntax initializerExpr = null;
            string val = null;
            if (literal != null) {
                initializerExpr = (ExpressionSyntax)context.Node;
                val = literal.Token.ValueText;
            } else if (context.Node is ArgumentSyntax argument) {
                initializerExpr = argument.Expression;
                val = (argument.Expression as LiteralExpressionSyntax).Token.ValueText;
            }
            if (initializerExpr == null) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation(), stringValue));
        }
        private void AnalyzeAttributeStringLiteral(SyntaxNodeAnalysisContext context) {
            var literal = (LiteralExpressionSyntax)context.Node;
            if (!(literal.Parent is AttributeArgumentSyntax argument))
                return;

            if (!(argument.Parent is AttributeArgumentListSyntax argumentList) ||
                 argumentList.Arguments.First() != argument) {
                return;
            }

            var candidateIdentifier = literal.Token.ValueText;

            if (!SyntaxFacts.IsValidIdentifier(candidateIdentifier)) {
                context.ReportDiagnostic(Diagnostic.Create(Rule3, literal.GetLocation(), candidateIdentifier));
            }
        }
        private void AnalyzeInvocation(OperationAnalysisContext context) {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;

            // Very naive check for arguments of methods calling GUILayout
            if (targetMethod.ContainingType.Name == "GUILayout") {
                foreach (var argument in invocation.Arguments) {
                    if (argument.Value is ILiteralOperation literalOp && literalOp.ConstantValue.HasValue && literalOp.Type.SpecialType == SpecialType.System_String) {
                        context.ReportDiagnostic(Diagnostic.Create(Rule2, argument.Syntax.GetLocation(), argument.ConstantValue.Value));
                    }
                }
            }
        }
        #endregion
    }
}
