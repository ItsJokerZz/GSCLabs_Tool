using GSCLab.Helpers;
using Irony.Parsing;
using System.Collections.Generic;
using System.Text;

namespace GSCLab.Infrastructure
{
    public static class SyntaxChecker
    {
        private static readonly Parser _parser = new Parser(new GSCGrammar());

        public static (bool IsValid, string ErrorMessage) Validate(string sourceCode, string fileName)
        {
            ParseTree tree = _parser.Parse(sourceCode);

            if (tree.HasErrors())
            {
                var error = tree.ParserMessages[0];
                string msg = $"^1[LOG] SYNTAX ERROR: {fileName} (Line {error.Location.Line + 1}, Col {error.Location.Column + 1}): {error.Message}";
                return (false, msg);
            }

            return (true, null);
        }
    }
}
