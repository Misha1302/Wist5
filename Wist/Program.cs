using Antlr4.Runtime;
using Wist;
using Wist.Grammar;

const string expression =
    """
    i32 main() {
        i32 b, c;
        i32 a = 6, d, f = 3;
        i32 zero;
        zero = 0;
        d = 8;
        a = a + 10;
        c = 2;
        return zero - (a + d * f / c);
    }
    """;

var inputStream = new AntlrInputStream(expression);
var lexer = new CLexer(inputStream);
var tokenStream = new CommonTokenStream(lexer);
var parser = new CParser(tokenStream);

var visitor = new CLanguageVisitor();
var context = parser.program();

visitor.Visit(context);
visitor.Execute();