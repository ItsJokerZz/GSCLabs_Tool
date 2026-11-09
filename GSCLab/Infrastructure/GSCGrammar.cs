using System;
using GSCLab.Infrastructure;
using Irony.Parsing;

namespace GSCLab.Helpers
{
    [Language("GSC", "0.3", "GSCLab Game Script Code || By LazyDev")]
    public class GSCGrammar : Grammar
    {
        public GSCGrammar() : base(caseSensitive: true)
        {
            var SingleLineComment = new CommentTerminal("SingleLineComment", "//", "\r", "\n");
            var DelimitedComment = new CommentTerminal("DelimitedComment", "/*", "*/");
            var DevBlock = new CommentTerminal("DevBlock", "/#", "#/");
            NonGrammarTerminals.Add(SingleLineComment);
            NonGrammarTerminals.Add(DelimitedComment);
            NonGrammarTerminals.Add(DevBlock);

            var Number = new NumberLiteral("Number");
            var StringLiteral = new StringLiteral("String", "\"", StringOptions.AllowsAllEscapes);
            var Identifier = new IdentifierTerminal("Identifier");
            var PathTerminal = new RegexBasedTerminal("Path", @"[a-zA-Z0-9_]+([\\/][a-zA-Z0-9_]+)+");

            var If = ToTerm("if"); var Else = ToTerm("else"); var While = ToTerm("while");
            var For = ToTerm("for"); var Foreach = ToTerm("foreach"); var In = ToTerm("in");
            var Do = ToTerm("do"); var Switch = ToTerm("switch"); var Case = ToTerm("case");
            var Default = ToTerm("default"); var Break = ToTerm("break"); var Continue = ToTerm("continue");
            var Return = ToTerm("return");

            var EndOn = ToTerm("endon"); var Notify = ToTerm("notify");
            var Wait = ToTerm("wait"); var Waittill = ToTerm("waittill");
            var WaittillMatch = ToTerm("waittillmatch"); var WaittillFrameEnd = ToTerm("waittillframeend");
            var Thread = ToTerm("thread"); var ChildThread = ToTerm("childthread");
            var Call = ToTerm("call");

            var True = ToTerm("true"); var False = ToTerm("false"); var Undefined = ToTerm("undefined");
            var Level = ToTerm("level"); var Self = ToTerm("self");
            var Game = ToTerm("game"); var Anim = ToTerm("anim");

            var LBrace = ToTerm("{"); var RBrace = ToTerm("}");
            var LParen = ToTerm("("); var RParen = ToTerm(")");
            var LBracket = ToTerm("["); var RBracket = ToTerm("]");
            var Comma = ToTerm(","); var Semi = ToTerm(";");
            var Colon = ToTerm(":"); var DoubleColon = ToTerm("::");
            var Dot = ToTerm("."); var Question = ToTerm("?");
            var Amp = ToTerm("&");

            this.MarkReservedWords("if", "else", "while", "for", "foreach", "in", "do", "switch", "case", "default", "break", "continue", "return", "wait", "waittill", "notify", "endon", "waittillmatch", "thread", "childthread", "call");

            var Program = new NonTerminal("Program");
            var Elements = new NonTerminal("Elements");
            var Element = new NonTerminal("Element");
            var FunctionDef = new NonTerminal("FunctionDef");
            var Parameters = new NonTerminal("Parameters");
            var Block = new NonTerminal("Block");
            var StmtList = new NonTerminal("StmtList");
            var Stmt = new NonTerminal("Stmt");
            var Expr = new NonTerminal("Expr");
            var ExprList = new NonTerminal("ExprList");
            var Primary = new NonTerminal("Primary");
            var Assignment = new NonTerminal("Assignment");
            var LValue = new NonTerminal("LValue");
            var Vector = new NonTerminal("Vector");
            var UnaryExpr = new NonTerminal("UnaryExpr");
            var ArrayInit = new NonTerminal("ArrayInit");
            var FuncPtr = new NonTerminal("FuncPtr");
            var SwitchBlock = new NonTerminal("SwitchBlock");
            var CaseList = new NonTerminal("CaseList");
            var CaseItem = new NonTerminal("CaseItem");

            var Arguments = new NonTerminal("Arguments");
            var CallPrefix = new NonTerminal("CallPrefix");
            var CallTarget = new NonTerminal("CallTarget");

            var DirectCall = new NonTerminal("DirectCall");
            var MethodCall = new NonTerminal("MethodCall");

            var IncludeDirective = new NonTerminal("IncludeDirective");
            var DefineDirective = new NonTerminal("DefineDirective");
            var UsingDirective = new NonTerminal("UsingDirective");
            var NamespaceDirective = new NonTerminal("NamespaceDirective");

            var IncludeTerm = ToTerm("#include");
            var DefineTerm = ToTerm("#define");
            var UsingTerm = ToTerm("#using");
            var NamespaceTerm = ToTerm("#namespace");

            Program.Rule = Elements;
            Elements.Rule = MakeStarRule(Elements, Element);

            if (ProjectManager.ActiveGame == "t7" || ProjectManager.ActiveGame == "t8" || ProjectManager.ActiveGame == "t9" || ProjectManager.ActiveGame == "t10" || ProjectManager.ActiveGame == "jup" || ProjectManager.ActiveGame == "sat")
            {
                IncludeDirective.Rule = IncludeTerm + (PathTerminal | Identifier) + Semi;
                DefineDirective.Rule = DefineTerm + Expr + "=" + Expr + Semi | DefineTerm + Identifier + Semi;
                UsingDirective.Rule = UsingTerm + (PathTerminal | Identifier) + Semi;
                NamespaceDirective.Rule = NamespaceTerm + (PathTerminal | Identifier) + Semi;

                Element.Rule = FunctionDef | Stmt | Semi | IncludeDirective | DefineDirective | UsingDirective | NamespaceDirective;
                MarkPunctuation("#include", "#define", "#using", "#namespace");
            }
            else
            {
                Element.Rule = FunctionDef | Stmt | Semi;
            }

            FunctionDef.Rule = Identifier + LParen + Parameters + RParen + Block;
            Parameters.Rule = MakeStarRule(Parameters, Comma, Identifier);
            Block.Rule = LBrace + StmtList + RBrace;
            StmtList.Rule = MakeStarRule(StmtList, Stmt);

            Stmt.Rule = Block | Semi | Expr + Semi | If + LParen + Expr + RParen + Stmt | If + LParen + Expr + RParen + Stmt + PreferShiftHere() + Else + Stmt | While + LParen + Expr + RParen + Stmt | For + LParen + (Expr | Empty) + Semi + (Expr | Empty) + Semi + (Expr | Empty) + RParen + Stmt | Foreach + LParen + Identifier + In + Expr + RParen + Stmt | Foreach + LParen + Identifier + Comma + Identifier + In + Expr + RParen + Stmt | Switch + LParen + Expr + RParen + SwitchBlock | Return + (Expr | Empty) + Semi | Break + Semi | Continue + Semi | Wait + Expr + Semi | WaittillFrameEnd + Semi | Expr + (Waittill | WaittillMatch | Notify | EndOn) + LParen + ExprList + RParen + Semi;

            SwitchBlock.Rule = LBrace + CaseList + RBrace;
            CaseList.Rule = MakeStarRule(CaseList, CaseItem);
            CaseItem.Rule = Case + Expr + Colon + StmtList | Default + Colon + StmtList;

            ArrayInit.Rule = LBracket + RBracket | LBracket + ExprList + RBracket;

            FuncPtr.Rule = LBracket + LBracket + LValue + RBracket + RBracket;

            LValue.Rule = Identifier | Self | Level | Game | Anim | PathTerminal | LValue + Dot + PreferShiftHere() + Identifier | LValue + LBracket + Expr + RBracket | LParen + Expr + RParen + Dot + Identifier | DirectCall + Dot + Identifier | MethodCall + Dot + Identifier | DirectCall + LBracket + Expr + RBracket | MethodCall + LBracket + Expr + RBracket;

            Arguments.Rule = LParen + ExprList + RParen;
            CallPrefix.Rule = Thread | ChildThread | Call;

            CallTarget.Rule = Identifier | Identifier + DoubleColon + Identifier | PathTerminal + DoubleColon + Identifier | DoubleColon + Identifier;

            DirectCall.Rule = CallTarget + Arguments | CallPrefix + CallTarget + Arguments | FuncPtr + Arguments | CallPrefix + FuncPtr + Arguments;

            MethodCall.Rule = LValue + CallTarget + Arguments | LValue + CallPrefix + CallTarget + Arguments | LValue + FuncPtr + Arguments | LValue + CallPrefix + FuncPtr + Arguments;

            Primary.Rule = LValue | Number | StringLiteral | Amp + StringLiteral | True | False | Undefined | LParen + Expr + RParen | Vector | ArrayInit | DoubleColon + Identifier | FuncPtr | DirectCall | MethodCall;

            Assignment.Rule = LValue + "=" + Expr | LValue + "+=" + Expr | LValue + "-=" + Expr | LValue + "*=" + Expr | LValue + "/=" + Expr | LValue + "++" | LValue + "--";

            UnaryExpr.Rule = ToTerm("!") + Expr | ToTerm("~") + Expr | ToTerm("-") + Expr;

            Expr.Rule = Assignment | UnaryExpr | Primary | Expr + "+" + Expr | Expr + "-" + Expr | Expr + "*" + Expr | Expr + "/" + Expr | Expr + "%" + Expr | Expr + "==" + Expr | Expr + "!=" + Expr | Expr + "<" + Expr | Expr + ">" + Expr | Expr + "<=" + Expr | Expr + ">=" + Expr | Expr + "&&" + Expr | Expr + "||" + Expr | Expr + Question + Expr + Colon + Expr;

            ExprList.Rule = MakeStarRule(ExprList, Comma, Expr);
            Vector.Rule = LParen + Expr + Comma + Expr + Comma + Expr + RParen;

            RegisterOperators(0, "?", ":");
            RegisterOperators(1, "||");
            RegisterOperators(2, "&&");
            RegisterOperators(6, "==", "!=");
            RegisterOperators(7, "<", ">", "<=", ">=");
            RegisterOperators(9, "+", "-");
            RegisterOperators(10, "*", "/", "%");
            RegisterOperators(11, Associativity.Right, "!", "~");
            RegisterOperators(12, Associativity.Left, ".");

            MarkPunctuation("(", ")", "{", "}", "[", "]", ",", ";", ".");
            MarkTransient(Element, Stmt, Expr, Primary, ArrayInit, UnaryExpr, SwitchBlock, Arguments, CallPrefix, CallTarget);

            this.Root = Program;
        }
    }
}