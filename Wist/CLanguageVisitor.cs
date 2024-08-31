using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime;
using GrEmit;
using Wist.Grammar;

namespace Wist;

public class CLanguageVisitor : CBaseVisitor<List<object>>
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


    public override List<object> VisitFunctionDefinition(CParser.FunctionDefinitionContext context)
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

    public override List<object> VisitPrimaryExpression(CParser.PrimaryExpressionContext context)
    {
        if (_expressionLevel == 0) return [context.GetText()];

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

    public override List<object> VisitDirectDeclarator(CParser.DirectDeclaratorContext context)
    {
        return [context.GetText()];
    }

    public override List<object> VisitJumpStatement(CParser.JumpStatementContext context)
    {
        if (context.GetChild(0).GetText() != "return") throw new InvalidOperationException();

        _expressionLevel++;
        VisitChildren(context);
        _expressionLevel--;

        _il.Ret();

        return null!;
    }

    public override List<object> VisitAdditiveExpression(CParser.AdditiveExpressionContext context)
    {
        if (context.multiplicativeExpression().Length < 2)
            return base.VisitAdditiveExpression(context);

        // ReSharper disable once CoVariantArrayConversion
        EmitMathOps(context, context.multiplicativeExpression());

        return null!;
    }

    public override List<object> VisitMultiplicativeExpression(CParser.MultiplicativeExpressionContext context)
    {
        if (context.castExpression().Length < 2)
            return base.VisitMultiplicativeExpression(context);

        // ReSharper disable once CoVariantArrayConversion
        EmitMathOps(context, context.castExpression());

        return null!;
    }

    private void EmitMathOps(ParserRuleContext context, ParserRuleContext[] contexts)
    {
        if (_expressionLevel == 0) return;

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

    // returns array of variables to init
    public override List<object> VisitInitDeclaratorList(CParser.InitDeclaratorListContext context)
    {
        var selectMany = context.initDeclarator().SelectMany(Visit);
        return [..selectMany.Where(x => x is InitInfo)];
    }

    public override List<object> VisitInitDeclarator(CParser.InitDeclaratorContext context)
    {
        var identifier = Visit(context.declarator()).To<string>();

        if (context.initializer() is null)
            return [new InitInfo(identifier, false)];


        _expressionLevel++;
        Visit(context.initializer());
        _expressionLevel--;

        return [new InitInfo(identifier, true)];
    }

    public override List<object> VisitDeclaration(CParser.DeclarationContext context)
    {
        if (context.initDeclaratorList() is null) return base.VisitDeclaration(context);

        var type = Visit(context.declarationSpecifiers()).To<CType>().ToSharpType();
        var names = Visit(context.initDeclaratorList());

        foreach (InitInfo initInfo in names.Reversed())
        {
            _locals.Add(initInfo.Identifier, _il.DeclareLocal(type, initInfo.Identifier));
            if (initInfo.IsNeedToSet)
                _il.Stloc(_locals[initInfo.Identifier]);
        }

        return null!;
    }

    protected override List<object> AggregateResult(List<object>? aggregate, List<object>? nextResult)
    {
        (aggregate ??= []).AddRange(nextResult ?? []);
        return aggregate;
    }

    public override List<object> VisitAssignmentExpression(CParser.AssignmentExpressionContext context)
    {
        if (context.unaryExpression() is null) return base.VisitAssignmentExpression(context);
        if (context.assignmentExpression() is null) return base.VisitAssignmentExpression(context);

        var identifier = Visit(context.unaryExpression());
        _expressionLevel++;
        Visit(context.assignmentExpression());
        _expressionLevel--;

        _il.Stloc(_locals[identifier.To<string>()]);

        return null!;
    }

    public override List<object> VisitTypedefName(CParser.TypedefNameContext context)
    {
        if (CTypeConverter.Convert(context.GetText(), out var value))
            return [value];
        return [context.GetText()];
    }
}