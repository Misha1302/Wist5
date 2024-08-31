using Antlr4.Runtime;
using Wist;
using Wist.Grammar;

var expression = "int main() { return 0 - (6 + 8 * 3 / 2); }";

var inputStream = new AntlrInputStream(expression);
var lexer = new CLexer(inputStream);
var tokenStream = new CommonTokenStream(lexer);
var parser = new CParser(tokenStream);

var visitor = new CLanguageVisitor();
var context = parser.program();

visitor.Visit(context);
visitor.Execute();