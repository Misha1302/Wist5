using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime;
using GrEmit;
using Wist.Grammar;

namespace Wist;

public class CLanguageVisitor : CBaseVisitor<object?>
{
    private readonly TypeBuilder _mainType;
    private GroboIL _il = null!;

    public CLanguageVisitor()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DynAssembly"),
            AssemblyBuilderAccess.RunAndCollect
        );

        var module = assembly.DefineDynamicModule("MainModule");
        _mainType = module.DefineType("MainType", TypeAttributes.Public | TypeAttributes.Class);
    }

    public override object? VisitFunctionDefinition(CParser.FunctionDefinitionContext context)
    {
        var method = _mainType.DefineMethod(
            "main",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(int),
            []);

        _il = new GroboIL(method);

        VisitChildren(context);

        return null;
    }

    public override object? VisitPrimaryExpression(CParser.PrimaryExpressionContext context)
    {
        if (context.Constant() is null) return base.VisitPrimaryExpression(context);

        var text = context.Constant().GetText();
        var i = int.Parse(text);
        _il.Ldc_I4(i);
        return null;
    }

    public override object? VisitJumpStatement(CParser.JumpStatementContext context)
    {
        if (context.GetChild(0).GetText() != "return") throw new InvalidOperationException();

        VisitChildren(context);

        _il.Ret();

        return null;
    }

    public override object? VisitAdditiveExpression(CParser.AdditiveExpressionContext context)
    {
        if (context.multiplicativeExpression().Length < 2)
            return base.VisitAdditiveExpression(context);

        // ReSharper disable once CoVariantArrayConversion
        EmitMathOps(context, context.multiplicativeExpression());

        return null;
    }

    public override object? VisitMultiplicativeExpression(CParser.MultiplicativeExpressionContext context)
    {
        if (context.castExpression().Length < 2)
            return base.VisitMultiplicativeExpression(context);

        // ReSharper disable once CoVariantArrayConversion
        EmitMathOps(context, context.castExpression());

        return null;
    }

    private void EmitMathOps(ParserRuleContext context, ParserRuleContext[] contexts)
    {
        Visit(contexts[0]);
        for (var i = 1; i < contexts.Length; i++)
        {
            Visit(contexts[i]);
            var expressionSignIndex = i != 1 ? i * 2 - 1 : 1;
            var opStr = context.GetChild(expressionSignIndex).GetText();
            EmitOp(opStr);
        }
    }

    private void EmitOp(string opStr)
    {
        if (opStr == "*") _il.Mul();
        else if (opStr == "/") _il.Div(false);
        else if (opStr == "+") _il.Add();
        else if (opStr == "-") _il.Sub();
        else if (opStr == "%") _il.Rem(false);
        else throw new InvalidOperationException($"Invalid operation '{opStr}'");
    }

    public void Execute()
    {
        Console.WriteLine(_il.GetILCode());

        Console.WriteLine(_mainType.CreateType().GetMethod("main")!.Invoke(null, null));
    }
}