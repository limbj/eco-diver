using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class CSharpSyntaxCheck
{
    private static int Main(string[] paths)
    {
        int errors = 0;
        var options = new CSharpParseOptions(LanguageVersion.CSharp9);
        foreach (string path in paths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path);
            foreach (Diagnostic diagnostic in tree.GetDiagnostics())
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error) continue;
                Console.Error.WriteLine(diagnostic);
                errors++;
            }
        }
        Console.WriteLine("C# syntax: " + paths.Length + " files, " + errors + " errors");
        return errors == 0 && paths.Length != 0 ? 0 : 1;
    }
}
