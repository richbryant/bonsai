﻿using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Scripting
{
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class ExpressionTransform : SingleArgumentExpressionBuilder
    {
        static readonly MethodInfo selectMethod = typeof(Observable).GetMethods()
                                                                    .Single(m => m.Name == "Select" &&
                                                                            m.GetParameters().Length == 2 &&
                                                                            m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

        public ExpressionTransform()
        {
            Expression = "it";
        }

        public string Expression { get; set; }

        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var source = arguments.First();
            var sourceType = source.Type.GetGenericArguments()[0];
            var selector = global::System.Linq.Dynamic.DynamicExpression.ParseLambda(sourceType, null, Expression);
            return System.Linq.Expressions.Expression.Call(selectMethod.MakeGenericMethod(sourceType, selector.ReturnType), source, selector);
        }
    }
}