using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime;
using GrEmit;
using Wist.Grammar;

namespace Wist;

public class CLanguageVisitor : CBaseVisitor<object>
{
    private readonly Dictionary<string, GroboIL.Local> _locals = [];
    private readonly TypeBuilder _mainType;
    private int _expressionLevel;
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


    public override object VisitFunctionDefinition(CParser.FunctionDefinitionContext context)
    {
        var method = _mainType.DefineMethod(
            "main",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(int),
            []);

        _il = new GroboIL(method);

        VisitChildren(context);

        return null!;
    }

    public override object VisitPrimaryExpression(CParser.PrimaryExpressionContext context)
    {
        if (_expressionLevel == 0) return context.GetText();

        if (context.Constant() is not null)
        {
            var text = context.Constant().GetText();
            var i = int.Parse(text);
            _il.Ldc_I4(i);
            return null!;
        }

        if (context.Identifier() is not null)
        {
            var text = context.Identifier().GetText();
            _il.Ldloc(_locals[text]);
        }

        return base.VisitPrimaryExpression(context);
    }

    public override object VisitJumpStatement(CParser.JumpStatementContext context)
    {
        if (context.GetChild(0).GetText() != "return") throw new InvalidOperationException();

        VisitChildren(context);

        _il.Ret();

        return null!;
    }

    public override object VisitAdditiveExpression(CParser.AdditiveExpressionContext context)
    {
        if (context.multiplicativeExpression().Length < 2)
            return base.VisitAdditiveExpression(context);

        // ReSharper disable once CoVariantArrayConversion
        EmitMathOps(context, context.multiplicativeExpression());

        return null!;
    }

    public override object VisitMultiplicativeExpression(CParser.MultiplicativeExpressionContext context)
    {
        if (context.castExpression().Length < 2)
            return base.VisitMultiplicativeExpression(context);

        // ReSharper disable once CoVariantArrayConversion
        EmitMathOps(context, context.castExpression());

        return null!;
    }

    private void EmitMathOps(ParserRuleContext context, ParserRuleContext[] contexts)
    {
        _expressionLevel++;
        Visit(contexts[0]);
        for (var i = 1; i < contexts.Length; i++)
        {
            Visit(contexts[i]);
            var expressionSignIndex = i != 1 ? i * 2 - 1 : 1;
            var opStr = context.GetChild(expressionSignIndex).GetText();
            EmitOp(opStr);
        }

        _expressionLevel--;
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

    public override object VisitDeclarationSpecifiers(CParser.DeclarationSpecifiersContext context)
    {
        if (context.declarationSpecifier().Length <= 1) return base.VisitDeclarationSpecifiers(context);

        var type = Visit(context.declarationSpecifier(0));
        var name = Visit(context.declarationSpecifier(1));
        _locals.Add((string)name, _il.DeclareLocal(((CType)type).ToSharpType(), (string)name));
        return null!;
    }

    public override object VisitAssignmentExpression(CParser.AssignmentExpressionContext context)
    {
        if (context.unaryExpression() is null) return base.VisitAssignmentExpression(context);
        if (context.assignmentExpression() is null) return base.VisitAssignmentExpression(context);

        var identifier = Visit(context.unaryExpression());
        _expressionLevel++;
        Visit(context.assignmentExpression());
        _expressionLevel--;

        _il.Stloc(_locals[(string)identifier]);

        return null!;
    }

    public override object VisitTypedefName(CParser.TypedefNameContext context)
    {
        if (CTypeConverter.Convert(context.GetText(), out var value))
            return value;
        return context.GetText();
    }
}