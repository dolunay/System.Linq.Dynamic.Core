﻿using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq.Dynamic.Core.Parser
{
    /// <summary>
    /// ExpressionPromoter
    /// </summary>
    public class ExpressionPromoter : IExpressionPromoter
    {
        private readonly NumberParser _numberParser;
        private readonly ConstantExpressionHelper _constantExpressionHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionPromoter"/> class.
        /// </summary>
        /// <param name="config">The ParsingConfig.</param>
        public ExpressionPromoter(ParsingConfig config)
        {
            _numberParser = new NumberParser(config);
            _constantExpressionHelper = ConstantExpressionHelperFactory.GetInstance(config);
        }

        /// <inheritdoc />
        public virtual Expression? Promote(Expression expr, Type type, bool exact, bool convertExpr)
        {
            Type returnType;
            if (expr is LambdaExpression lambdaExpression)
            {
                returnType = lambdaExpression.GetReturnType();
            }
            else
            {
                returnType = expr.Type;
            }

            if (returnType == type || type.IsGenericParameter)
            {
                return expr;
            }

            if (expr is ConstantExpression ce)
            {
                if (Constants.IsNull(ce))
                {
                    if (!type.GetTypeInfo().IsValueType || TypeHelper.IsNullableType(type))
                    {
                        return Expression.Constant(null, type);
                    }
                }
                else
                {
                    if (_constantExpressionHelper.TryGetText(ce, out var text))
                    {
                        Type target = TypeHelper.GetNonNullableType(type);
                        object? value = null;

#if !(UAP10_0 || NETSTANDARD)
                        switch (Type.GetTypeCode(ce.Type))
                        {
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                                value = _numberParser.ParseNumber(text, target);

                                // Make sure an enum value stays an enum value
                                if (target.IsEnum)
                                {
                                    value = Enum.ToObject(target, value!);
                                }
                                break;

                            case TypeCode.Double:
                                if (target == typeof(decimal) || target == typeof(double))
                                {
                                    value = _numberParser.ParseNumber(text, target);
                                }
                                break;

                            case TypeCode.String:
                                TypeHelper.TryParseEnum(text, target, out value);
                                break;
                        }
#else
                        if (ce.Type == typeof(int) || ce.Type == typeof(uint) || ce.Type == typeof(long) || ce.Type == typeof(ulong))
                        {
                            // If target is an enum value, just use the Value from the ConstantExpression
                            if (target.GetTypeInfo().IsEnum)
                            {
                                value = Enum.ToObject(target, ce.Value);
                            }
                            else
                            {
                                value = _numberParser.ParseNumber(text!, target);
                            }
                        }
                        else if (ce.Type == typeof(double))
                        {
                            if (target == typeof(decimal) || target == typeof(double))
                            {
                                value = _numberParser.ParseNumber(text, target);
                            }
                        }
                        else if (ce.Type == typeof(string) && TypeHelper.TryParseEnum(text, target, out value))
                        {
                            // Empty if
                        }
#endif
                        if (value != null)
                        {
                            return Expression.Constant(value, type);
                        }
                    }
                }
            }

            if (TypeHelper.IsCompatibleWith(returnType, type))
            {
                if (type == typeof(decimal) && TypeHelper.IsEnumType(expr.Type))
                {
                    return Expression.Convert(Expression.Convert(expr, Enum.GetUnderlyingType(expr.Type)), type);
                }

                if (type.GetTypeInfo().IsValueType || exact || expr.Type.GetTypeInfo().IsValueType && convertExpr)
                {
                    return Expression.Convert(expr, type);
                }

                return expr;
            }

            return null;
        }
    }
}