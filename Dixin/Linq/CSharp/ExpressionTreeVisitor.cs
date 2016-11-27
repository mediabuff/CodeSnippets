﻿namespace Dixin.Linq.CSharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection.Emit;

    internal abstract class BinaryArithmeticExpressionVisitor<TResult>
    {
        internal TResult VisitBody(LambdaExpression expression) => this.VisitNode(expression.Body, expression);

        protected TResult VisitNode(Expression node, LambdaExpression expression)
        {
            // Processes the 6 types of node.
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    return this.VisitAdd((BinaryExpression)node, expression);

                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)node, expression);

                case ExpressionType.Divide:
                    return this.VisitDivide((BinaryExpression)node, expression);

                case ExpressionType.Multiply:
                    return this.VisitMultiply((BinaryExpression)node, expression);

                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)node, expression);

                case ExpressionType.Subtract:
                    return this.VisitSubtract((BinaryExpression)node, expression);

                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        protected abstract TResult VisitAdd(BinaryExpression add, LambdaExpression expression);

        protected abstract TResult VisitConstant(ConstantExpression constant, LambdaExpression expression);

        protected abstract TResult VisitDivide(BinaryExpression divide, LambdaExpression expression);

        protected abstract TResult VisitMultiply(BinaryExpression multiply, LambdaExpression expression);

        protected abstract TResult VisitParameter(ParameterExpression parameter, LambdaExpression expression);

        protected abstract TResult VisitSubtract(BinaryExpression subtract, LambdaExpression expression);
    }

    internal class PrefixVisitor : BinaryArithmeticExpressionVisitor<string>
    {
        protected override string VisitAdd
            (BinaryExpression add, LambdaExpression expression) => this.VisitBinary(add, "add", expression);

        protected override string VisitConstant
            (ConstantExpression constant, LambdaExpression expression) => constant.Value.ToString();

        protected override string VisitDivide
            (BinaryExpression divide, LambdaExpression expression) => this.VisitBinary(divide, "div", expression);

        protected override string VisitMultiply
            (BinaryExpression multiply, LambdaExpression expression) =>
                this.VisitBinary(multiply, "mul", expression);

        protected override string VisitParameter
            (ParameterExpression parameter, LambdaExpression expression) => parameter.Name;

        protected override string VisitSubtract
            (BinaryExpression subtract, LambdaExpression expression) =>
                this.VisitBinary(subtract, "sub", expression);

        private string VisitBinary( // Recursion: operator(left, right)
            BinaryExpression binary, string @operator, LambdaExpression expression) =>
                $"{@operator}({this.VisitNode(binary.Left, expression)}, {this.VisitNode(binary.Right, expression)})";
    }

    internal static partial class ExpressionTree
    {
        internal static void Prefix()
        {
            Expression<Func<double, double, double, double, double, double>> infix =
                (a, b, c, d, e) => a + b - c * d / 2 + e * 3;
            PrefixVisitor prefixVisitor = new PrefixVisitor();
            string prefix = prefixVisitor.VisitBody(infix); // add(sub(add(a, b), div(mul(c, d), 2)), mul(e, 3))
        }
    }

    internal class PostfixVisitor : BinaryArithmeticExpressionVisitor<List<Tuple<OpCode, double?>>>
    {
        protected override List<Tuple<OpCode, double?>> VisitAdd
            (BinaryExpression add, LambdaExpression expression) => this.VisitBinary(add, OpCodes.Add, expression);

        protected override List<Tuple<OpCode, double?>> VisitConstant(
            ConstantExpression constant, LambdaExpression expression) => 
                new List<Tuple<OpCode, double?>>() { Tuple.Create(OpCodes.Ldc_R8, (double?)constant.Value) };

        protected override List<Tuple<OpCode, double?>> VisitDivide
            (BinaryExpression divide, LambdaExpression expression) =>
                this.VisitBinary(divide, OpCodes.Div, expression);

        protected override List<Tuple<OpCode, double?>> VisitMultiply
            (BinaryExpression multiply, LambdaExpression expression) =>
                this.VisitBinary(multiply, OpCodes.Mul, expression);

        protected override List<Tuple<OpCode, double?>> VisitParameter(
            ParameterExpression parameter, LambdaExpression expression)
        {
            int index = expression.Parameters.IndexOf(parameter);
            return new List<Tuple<OpCode, double?>>() { Tuple.Create(OpCodes.Ldarg_S, (double?)index) };
        }

        protected override List<Tuple<OpCode, double?>> VisitSubtract
            (BinaryExpression subtract, LambdaExpression expression) =>
                this.VisitBinary(subtract, OpCodes.Sub, expression);

        private List<Tuple<OpCode, double?>> VisitBinary( // Recursion: left, right, operator
            BinaryExpression binary, OpCode postfix, LambdaExpression expression)
        {
            List < Tuple < OpCode, double?>>  cils = this.VisitNode(binary.Left, expression);
            cils.AddRange(this.VisitNode(binary.Right, expression));
            cils.Add(Tuple.Create(postfix, (double?)null));
            return cils;
        }
    }

#if DEMO
    internal class PostfixVisitor : BinaryArithmeticExpressionVisitor<IEnumerable<Tuple<OpCode, double?>>>
    {
        protected override IEnumerable<Tuple<OpCode, double?>> VisitAdd
            (BinaryExpression add, LambdaExpression expression) => this.VisitBinary(add, OpCodes.Add, expression);

        protected override IEnumerable<Tuple<OpCode, double?>> VisitConstant(
            ConstantExpression constant, LambdaExpression expression)
        {
            yield return Tuple.Create(OpCodes.Ldc_R8, (double?)constant.Value);
        }

        protected override IEnumerable<Tuple<OpCode, double?>> VisitDivide
            (BinaryExpression divide, LambdaExpression expression) =>
                this.VisitBinary(divide, OpCodes.Div, expression);

        protected override IEnumerable<Tuple<OpCode, double?>> VisitMultiply
            (BinaryExpression multiply, LambdaExpression expression) =>
                this.VisitBinary(multiply, OpCodes.Mul, expression);

        protected override IEnumerable<Tuple<OpCode, double?>> VisitParameter(
            ParameterExpression parameter, LambdaExpression expression)
        {
            int index = expression.Parameters.IndexOf(parameter);
            yield return Tuple.Create(OpCodes.Ldarg_S, (double?)index);
        }

        protected override IEnumerable<Tuple<OpCode, double?>> VisitSubtract
            (BinaryExpression subtract, LambdaExpression expression) =>
                this.VisitBinary(subtract, OpCodes.Sub, expression);

        private IEnumerable<Tuple<OpCode, double?>> VisitBinary( // Recursion: left, right, operator
            BinaryExpression binary, OpCode postfix, LambdaExpression expression) =>
                this.VisitNode(binary.Left, expression)
                    .Concat(this.VisitNode(binary.Right, expression))
                    .Concat(EnumerableEx.Return(Tuple.Create(postfix, (double?)null))); // left, right, postfix
    }
#endif

    internal static partial class ExpressionTree
    {
        internal static void Cil()
        {
            Expression<Func<double, double, double, double, double, double>> infix =
                (a, b, c, d, e) => a + b - c * d / 2 + e * 3;

            PostfixVisitor postfixVisitor = new PostfixVisitor();
            IEnumerable<Tuple<OpCode, double?>> postfix = postfixVisitor.VisitBody(infix);
            foreach (Tuple<OpCode, double?> code in postfix)
            {
                Trace.WriteLine($"{code.Item1} {code.Item2}");
            }
            // ldarg.s 0
            // ldarg.s 1
            // add
            // ldarg.s 2
            // ldarg.s 3 
            // mul 
            // ldc.r8 2 
            // div 
            // sub 
            // ldarg.s 4 
            // ldc.r8 3 
            // mul 
            // add
        }
    }

    internal static class BinaryArithmeticCompiler
    {
        internal static TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        {
            DynamicMethod dynamicFunction = new DynamicMethod(
                name: string.Empty,
                returnType: expression.ReturnType,
                parameterTypes: expression.Parameters.Select(parameter => parameter.Type).ToArray(),
                m: typeof(BinaryArithmeticCompiler).Module);
            EmitIL(dynamicFunction.GetILGenerator(), new PostfixVisitor().VisitBody(expression));
            return (TDelegate)(object)dynamicFunction.CreateDelegate(typeof(TDelegate));
        }

        private static void EmitIL(ILGenerator ilGenerator, IEnumerable<Tuple<OpCode, double?>> codes)
        {
            foreach (Tuple<OpCode, double?> code in codes)
            {
                if (code.Item2.HasValue)
                {
                    if (code.Item1 == OpCodes.Ldarg_S)
                    {
                        ilGenerator.Emit(code.Item1, (int)code.Item2.Value); // ldarg.s (int)index
                    }
                    else
                    {
                        ilGenerator.Emit(code.Item1, code.Item2.Value); // ldc.r8 (double)constant
                    }
                }
                else
                {
                    ilGenerator.Emit(code.Item1); // add, sub, mul, div
                }
            }

            ilGenerator.Emit(OpCodes.Ret); // Returns the result.
        }
    }

    internal static partial class ExpressionTree
    {
        internal static void Compile()
        {
            Expression<Func<double, double, double, double, double, double>> expression =
                (a, b, c, d, e) => a + b - c * d / 2 + e * 3;
            Func<double, double, double, double, double, double> function =
                BinaryArithmeticCompiler.Compile(expression);
            double result = function(1, 2, 3, 4, 5); // 12
        }

        internal static void BuiltInCompile()
        {
            Expression<Func<double, double, double, double, double, double>> infix =
                (a, b, c, d, e) => a + b - c * d / 2 + e * 3;
            Func<double, double, double, double, double, double> function = infix.Compile();
            double result = function(1, 2, 3, 4, 5); // 12
        }
    }
}
