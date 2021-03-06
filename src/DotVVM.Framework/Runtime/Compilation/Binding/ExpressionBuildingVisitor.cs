﻿using DotVVM.Framework.Parser.Binding.Parser;
using DotVVM.Framework.Parser.Binding.Tokenizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Framework.Runtime.Compilation.Binding
{
    public class ExpressionBuildingVisitor : BindingParserNodeVisitor<Expression>
    {
        public TypeRegistry Registry { get; set; }
        public Expression Scope { get; set; }
        private List<Exception> currentErrors;

        public ExpressionBuildingVisitor(TypeRegistry registry)
        {
            Registry = registry;
        }

        protected T HandleErrors<T, TNode>(TNode node, Func<TNode, T> action, string defaultErrorMessage = "Binding compilation failed", bool allowResultNull = true)
            where TNode : BindingParserNode
        {
            T result = default(T);
            try
            {
                result = action(node);
            }
            catch (BindingCompilationException exception)
            {
                if (currentErrors == null) currentErrors = new List<Exception>();
                currentErrors.Add(exception);
            }
            catch (Exception exception)
            {
                if (currentErrors == null) currentErrors = new List<Exception>();
                currentErrors.Add(new BindingCompilationException(defaultErrorMessage, exception, node));
            }
            if (!allowResultNull && result == null)
            {
                currentErrors.Add(new BindingCompilationException(defaultErrorMessage, node));
            }
            return result;
        }

        protected void AddError(params Exception[] errors)
        {
            if (currentErrors == null) currentErrors = new List<Exception>(errors);
            else currentErrors.AddRange(errors);
        }

        protected void ThrowOnErrors()
        {
            if (currentErrors != null && currentErrors.Count > 0)
            {
                var currentErrors = this.currentErrors;
                this.currentErrors = null;
                if (currentErrors.Count == 1)
                {
                    if (currentErrors[0].StackTrace == null
                        || (currentErrors[0] is BindingCompilationException && (currentErrors[0] as BindingCompilationException).Tokens == null)
                        || (currentErrors[0] is AggregateException && (currentErrors[0] as AggregateException).Message == null))
                        throw currentErrors[0];
                }
                throw new AggregateException(currentErrors);
            }
        }

        protected void RegisterSymbols(IEnumerable<KeyValuePair<string, Expression>> symbols)
        {
            Registry = Registry.AddSymbols(symbols);
        }

        public override Expression Visit(BindingParserNode node)
        {
            var regBackup = Registry;
            var errors = currentErrors;
            try
            {
                return base.Visit(node);
            }
            finally
            {
                currentErrors = errors;
                Registry = regBackup;
            }
        }

        protected override Expression VisitLiteralExpression(LiteralExpressionBindingParserNode node)
        {
            return Expression.Constant(node.Value);
        }

        protected override Expression VisitParenthesizedExpression(ParenthesizedExpressionBindingParserNode node)
        {
            // just visit content
            return Visit(node.InnerExpression);
        }

        protected override Expression VisitUnaryOperator(UnaryOperatorBindingParserNode node)
        {
            var operand = Visit(node.InnerExpression);
            ExpressionType eop;
            switch (node.Operator)
            {
                case BindingTokenType.AddOperator:
                    eop = ExpressionType.UnaryPlus;
                    break;
                case BindingTokenType.SubtractOperator:
                    eop = ExpressionType.Negate;
                    break;
                case BindingTokenType.NotOperator:
                    eop = ExpressionType.Not;
                    break;
                default:
                    throw new NotSupportedException($"unary operator { node.Operator } is not supported");
            }
            return ExpressionHelper.GetUnaryOperator(operand, eop);
        }

        protected override Expression VisitBinaryOperator(BinaryOperatorBindingParserNode node)
        {
            var left = HandleErrors(node.FirstExpression, Visit);
            var right = HandleErrors(node.SecondExpression, Visit);
            ThrowOnErrors();

            ExpressionType eop;

            switch (node.Operator)
            {
                case BindingTokenType.AddOperator:
                    eop = ExpressionType.Add;
                    break;
                case BindingTokenType.SubtractOperator:
                    eop = ExpressionType.Subtract;
                    break;
                case BindingTokenType.MultiplyOperator:
                    eop = ExpressionType.Multiply;
                    break;
                case BindingTokenType.DivideOperator:
                    eop = ExpressionType.Divide;
                    break;
                case BindingTokenType.ModulusOperator:
                    eop = ExpressionType.Modulo;
                    break;
                case BindingTokenType.EqualsEqualsOperator:
                    eop = ExpressionType.Equal;
                    break;
                case BindingTokenType.LessThanOperator:
                    eop = ExpressionType.LessThan;
                    break;
                case BindingTokenType.LessThanEqualsOperator:
                    eop = ExpressionType.LessThanOrEqual;
                    break;
                case BindingTokenType.GreaterThanOperator:
                    eop = ExpressionType.GreaterThan;
                    break;
                case BindingTokenType.GreaterThanEqualsOperator:
                    eop = ExpressionType.GreaterThanOrEqual;
                    break;
                case BindingTokenType.NotEqualsOperator:
                    eop = ExpressionType.NotEqual;
                    break;
                case BindingTokenType.NullCoalescingOperator:
                    eop = ExpressionType.Coalesce;
                    break;
                case BindingTokenType.AndOperator:
                    eop = ExpressionType.And;
                    break;
                case BindingTokenType.AndAlsoOperator:
                    eop = ExpressionType.AndAlso;
                    break;
                case BindingTokenType.OrOperator:
                    eop = ExpressionType.Or;
                    break;
                case BindingTokenType.OrElseOperator:
                    eop = ExpressionType.OrElse;
                    break;
                case BindingTokenType.AssignOperator:
                    eop = ExpressionType.Assign;
                    break;
                default:
                    throw new NotSupportedException($"unary operator { node.Operator } is not supported");
            }

            return ExpressionHelper.GetBinaryOperator(left, right, eop);
        }

        protected override Expression VisitArrayAccess(ArrayAccessBindingParserNode node)
        {
            var target = HandleErrors(node.TargetExpression, Visit);
            var index = HandleErrors(node.ArrayIndexExpression, Visit);
            ThrowOnErrors();

            return ExpressionHelper.GetIndexer(target, index);
        }

        protected override Expression VisitFunctionCall(FunctionCallBindingParserNode node)
        {
            var target = HandleErrors(node.TargetExpression, Visit);
            var args = new Expression[node.ArgumentExpressions.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = HandleErrors(node.ArgumentExpressions[i], Visit);
            }
            ThrowOnErrors();

            return ExpressionHelper.Call(target, args);
        }

        protected override Expression VisitIdentifierName(IdentifierNameBindingParserNode node)
        {
            var expr = ExpressionHelper.GetMember(Scope, node.Name, throwExceptions: false) ??
                Registry.Resolve(node.Name, false);
            if (expr == null) throw new BindingCompilationException($"The identifier '{ node.Name }' could not be resolved!", node);
            return expr;
        }

        protected override Expression VisitConditionalExpression(ConditionalExpressionBindingParserNode node)
        {
            var condition = HandleErrors(node.ConditionExpression, n => TypeConversion.ImplicitConversion(Visit(n), typeof(bool), true));
            var trueExpr = HandleErrors(node.TrueExpression, Visit);
            var falseExpr = HandleErrors(node.FalseExpression, Visit);
            ThrowOnErrors();

            return Expression.Condition(condition, trueExpr, falseExpr);
        }

        protected override Expression VisitMemberAccess(MemberAccessBindingParserNode node)
        {
            var identifier = node.MemberNameExpression.Name;
            var target = Visit(node.TargetExpression);
            return ExpressionHelper.GetMember(target, identifier);
        }
    }
}
