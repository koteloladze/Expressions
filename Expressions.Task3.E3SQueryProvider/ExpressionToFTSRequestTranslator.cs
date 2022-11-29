using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Expression predicate = null;
            switch (node.Method.Name)
            {
                case "Where":
                    predicate = node.Arguments[1];
                    break;
                case "Equals":
                    var constantEqual = node.Arguments[0];
                    var memberAccessEqual = node.Object;
                    predicate = Expression.Equal(constantEqual, memberAccessEqual);
                    break;
                case "StartsWith":
                    var constantStartsWith = node.Arguments[0];
                    var startsWithMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
                    var sumExpressionStartsWith = Expression.Add(constantStartsWith, Expression.Constant("*"), startsWithMethod);

                    predicate = Expression.Equal(node.Object, sumExpressionStartsWith);
                    break;
                case "EndsWith":
                    var constantEndsWith = node.Arguments[0];
                    var endsWithMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
                    var sumExpressionEndsWith = Expression.Add(Expression.Constant("*"), constantEndsWith, endsWithMethod);

                    predicate = Expression.Equal(node.Object, sumExpressionEndsWith);
                    break;
                case "Contains":
                    var constantBoth = node.Arguments[0];
                    var bothMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
                    var sumExpressionLeftSide = Expression.Add(Expression.Constant("*"), constantBoth, bothMethod);
                    var sumExpressionBoth = Expression.Add(sumExpressionLeftSide, Expression.Constant("*"), bothMethod);

                    predicate = Expression.Equal(node.Object, sumExpressionBoth);
                    break;
                default:
                    return base.VisitMethodCall(node);
            }

            Visit(predicate);

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:

                    if (node.Left.NodeType == ExpressionType.MemberAccess)
                    {
                        Visit(node.Left);
                        _resultStringBuilder.Append("(");
                        Visit(node.Right);

                    }
                    else if (node.Left.NodeType == ExpressionType.Constant)
                    {
                        Visit(node.Right);
                        _resultStringBuilder.Append("(");
                        Visit(node.Left);
                    }

                    _resultStringBuilder.Append(")");
                    break;
                case ExpressionType.Add:
                    if (node.Method.Name.Equals("Concat"))
                    {
                        Visit(node.Left);
                        Visit(node.Right);
                    }

                    break;
                case ExpressionType.AndAlso:
                    Visit(node.Left);
                    _resultStringBuilder.Append("%AND%");
                    Visit(node.Right);
                    break;
                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
