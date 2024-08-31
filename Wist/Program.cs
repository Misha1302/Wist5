using Antlr4.Runtime;
using Wist;
using Wist.Grammar;

const string expression =
    """
    i32 main() {
        i32 a;
        a = 6;
        a = a + 10;
        return 0 - (a + 8 * 3 / 2);
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