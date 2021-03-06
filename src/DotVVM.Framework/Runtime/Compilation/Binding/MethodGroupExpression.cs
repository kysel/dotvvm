﻿using DotVVM.Framework.Parser.Binding.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Framework.Runtime.Compilation.Binding
{

    public class MethodGroupExpression : Expression
    {
        public override Type Type
        {
            get
            {
                return typeof(Delegate);
            }
        }

        public Expression Target { get; set; }
        public string MethodName { get; set; }
        public Type[] TypeArgs { get; set; }

        public bool IsStatic => Target is StaticClassIdentifierExpression;

        private static MethodInfo CreateDelegateFromStringMethodInfo = typeof(Delegate).GetMethod("CreateDelegate", new[] { typeof(Type), typeof(object), typeof(string) });
        private static MethodInfo CreateDelegateMethodInfo = typeof(Delegate).GetMethod("CreateDelegate", new[] { typeof(Type), typeof(object), typeof(MethodInfo) });
        public Expression CreateDelegateExpression(Type delegateType, bool throwException = true)
        {
            if (delegateType == null || delegateType == typeof(object)) return CreateDelegateExpression();
            if (!typeof(Delegate).IsAssignableFrom(delegateType)) if (throwException) throw new Exception("Could not convert method group expression to a non delegate type."); else return null;
            var invokeMethod = delegateType.GetMethod("Invoke");
            var args = invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var method = Target.Type.GetMethods(BindingFlags.Public | (IsStatic ? BindingFlags.Static : BindingFlags.Instance))
                .FirstOrDefault(m => m.Name == MethodName && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(args) && m.ReturnType == invokeMethod.ReturnType);
            if (method == null)
                if (throwException) throw new Exception($"Could not convert method group '{Target.Type.Name}.{ MethodName }' to delegate '{ delegateType.FullName }'");
                else return null;
            if (IsStatic)
                return Expression.Constant(Delegate.CreateDelegate(delegateType, method));
            else
                return Expression.Call(CreateDelegateMethodInfo, Expression.Constant(delegateType), Target, Expression.Constant(method));
        }

        private static Type GetDelegateType(Type returnType, Type[] args)
        {
            if (returnType == null || returnType == typeof(void))
            {
                return Type.GetType("System.Action`" + args.Length).MakeGenericType(args);
            }
            else
            {
                return Type.GetType("System.Func`" + (args.Length + 1)).MakeGenericType(args.Concat(new[] { returnType }).ToArray());
            }
        }

        private static Type GetDelegateType(MethodInfo methodInfo)
        {
            return GetDelegateType(methodInfo.ReturnType, methodInfo.GetParameters().Select(a => a.ParameterType).ToArray());
        }

        protected MethodInfo GetMethod()
            => Target.Type.GetMethod(MethodName, BindingFlags.Public | (IsStatic ? BindingFlags.Static : BindingFlags.Instance));

        public Expression CreateDelegateExpression()
        {
            var methodInfo = GetMethod();
            if (methodInfo == null) throw new Exception($"can not create delegate from method '{ MethodName }' on type '{ Target.Type.FullName }'");

            if (IsStatic)
                return Expression.Constant(Delegate.CreateDelegate(GetDelegateType(methodInfo), methodInfo));
            else
                return Expression.Call(CreateDelegateMethodInfo, Expression.Constant(GetDelegateType(methodInfo)), Target, Expression.Constant(methodInfo));
        }
        public Expression CreateMethodCall(IEnumerable<Expression> args)
        {
            var argsArray = args.ToArray();
            if (IsStatic)
            {
                return ExpressionHelper.CallMethod((Target as StaticClassIdentifierExpression).Type, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy, MethodName, TypeArgs, argsArray);
            }
            else
            {
                return ExpressionHelper.CallMethod(Target, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy, MethodName, TypeArgs, argsArray);
            }
        }

        public override string ToString()
        {
            return $"{Target}.{MethodName}";
        }
    }
}
