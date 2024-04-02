using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;

namespace Microsoft.CSharp.RuntimeBinder;

internal static class BinderHelper
{
	internal static DynamicMetaObject Bind(DynamicMetaObjectBinder action, RuntimeBinder binder, IEnumerable<DynamicMetaObject> args, IEnumerable<CSharpArgumentInfo> arginfos, DynamicMetaObject onBindingError)
	{
		List<Expression> list = new List<Expression>();
		BindingRestrictions bindingRestrictions = BindingRestrictions.Empty;
		ICSharpInvokeOrInvokeMemberBinder callPayload = action as ICSharpInvokeOrInvokeMemberBinder;
		ParameterExpression parameterExpression = null;
		IEnumerator<CSharpArgumentInfo> enumerator = arginfos?.GetEnumerator();
		int num = 0;
		foreach (DynamicMetaObject arg in args)
		{
			if (!arg.HasValue)
			{
				throw Error.InternalCompilerError();
			}
			CSharpArgumentInfo cSharpArgumentInfo = null;
			if (enumerator != null && enumerator.MoveNext())
			{
				cSharpArgumentInfo = enumerator.Current;
			}
			if (num == 0 && IsIncrementOrDecrementActionOnLocal(action))
			{
				parameterExpression = Expression.Variable((arg.Value != null) ? arg.Value.GetType() : typeof(object), "t0");
				list.Add(parameterExpression);
			}
			else
			{
				list.Add(arg.Expression);
			}
			BindingRestrictions restrictions = DeduceArgumentRestriction(num, callPayload, arg, cSharpArgumentInfo);
			bindingRestrictions = bindingRestrictions.Merge(restrictions);
			if (cSharpArgumentInfo != null && cSharpArgumentInfo.LiteralConstant && (!(arg.Value is float) || !float.IsNaN((float)arg.Value)) && (!(arg.Value is double) || !double.IsNaN((double)arg.Value)))
			{
				Expression expression = Expression.Equal(arg.Expression, Expression.Constant(arg.Value, arg.Expression.Type));
				restrictions = BindingRestrictions.GetExpressionRestriction(expression);
				bindingRestrictions = bindingRestrictions.Merge(restrictions);
			}
			num++;
		}
		try
		{
			Expression expression2 = binder.Bind(action, list, args.ToArray(), out var deferredBinding);
			if (deferredBinding != null)
			{
				expression2 = ConvertResult(deferredBinding.Expression, action);
				bindingRestrictions = deferredBinding.Restrictions.Merge(bindingRestrictions);
				return new DynamicMetaObject(expression2, bindingRestrictions);
			}
			if (parameterExpression != null)
			{
				DynamicMetaObject dynamicMetaObject = args.First();
				Expression item = Expression.Assign(parameterExpression, Expression.Convert(dynamicMetaObject.Expression, dynamicMetaObject.Value.GetType()));
				Expression item2 = Expression.Assign(dynamicMetaObject.Expression, Expression.Convert(parameterExpression, dynamicMetaObject.Expression.Type));
				List<Expression> list2 = new List<Expression>();
				list2.Add(item);
				list2.Add(expression2);
				list2.Add(item2);
				expression2 = Expression.Block(new ParameterExpression[1] { parameterExpression }, list2);
			}
			expression2 = ConvertResult(expression2, action);
			return new DynamicMetaObject(expression2, bindingRestrictions);
		}
		catch (RuntimeBinderException ex)
		{
			if (onBindingError != null)
			{
				return onBindingError;
			}
			return new DynamicMetaObject(Expression.Throw(Expression.New(typeof(RuntimeBinderException).GetConstructor(new Type[1] { typeof(string) }), Expression.Constant(ex.Message)), GetTypeForErrorMetaObject(action, args.FirstOrDefault())), bindingRestrictions);
		}
	}

	private static bool IsTypeOfStaticCall(int parameterIndex, ICSharpInvokeOrInvokeMemberBinder callPayload)
	{
		if (parameterIndex == 0 && callPayload != null)
		{
			return callPayload.StaticCall;
		}
		return false;
	}

	private static bool IsComObject(object obj)
	{
		if (obj != null)
		{
			return Marshal.IsComObject(obj);
		}
		return false;
	}

	internal static bool IsWindowsRuntimeObject(DynamicMetaObject obj)
	{
		if (obj != null && obj.RuntimeType != null)
		{
			Type type = obj.RuntimeType;
			while (type != null)
			{
				if (type.Attributes.HasFlag(TypeAttributes.WindowsRuntime))
				{
					return true;
				}
				if (type.Attributes.HasFlag(TypeAttributes.Import))
				{
					return false;
				}
				type = type.BaseType;
			}
		}
		return false;
	}

	private static bool IsTransparentProxy(object obj)
	{
		if (obj != null)
		{
			return RemotingServices.IsTransparentProxy(obj);
		}
		return false;
	}

	private static bool IsDynamicallyTypedRuntimeProxy(DynamicMetaObject argument, CSharpArgumentInfo info)
	{
		return info != null && !info.UseCompileTimeType && (IsComObject(argument.Value) || IsTransparentProxy(argument.Value));
	}

	private static BindingRestrictions DeduceArgumentRestriction(int parameterIndex, ICSharpInvokeOrInvokeMemberBinder callPayload, DynamicMetaObject argument, CSharpArgumentInfo info)
	{
		if (argument.Value != null && !IsTypeOfStaticCall(parameterIndex, callPayload) && !IsDynamicallyTypedRuntimeProxy(argument, info))
		{
			return BindingRestrictions.GetTypeRestriction(argument.Expression, argument.RuntimeType);
		}
		return BindingRestrictions.GetInstanceRestriction(argument.Expression, argument.Value);
	}

	private static Expression ConvertResult(Expression binding, DynamicMetaObjectBinder action)
	{
		if (action is CSharpInvokeConstructorBinder)
		{
			return binding;
		}
		if (binding.Type == typeof(void))
		{
			if (action is ICSharpInvokeOrInvokeMemberBinder { ResultDiscarded: not false })
			{
				return Expression.Block(binding, Expression.Default(action.ReturnType));
			}
			throw Error.BindToVoidMethodButExpectResult();
		}
		if (binding.Type.IsValueType && !action.ReturnType.IsValueType)
		{
			return Expression.Convert(binding, action.ReturnType);
		}
		return binding;
	}

	private static Type GetTypeForErrorMetaObject(DynamicMetaObjectBinder action, DynamicMetaObject arg0)
	{
		if (action is CSharpInvokeConstructorBinder)
		{
			if (arg0 == null || !(arg0.Value is Type))
			{
				return typeof(object);
			}
			return arg0.Value as Type;
		}
		return action.ReturnType;
	}

	private static bool IsIncrementOrDecrementActionOnLocal(DynamicMetaObjectBinder action)
	{
		if (action is CSharpUnaryOperationBinder cSharpUnaryOperationBinder)
		{
			if (cSharpUnaryOperationBinder.Operation != ExpressionType.Increment)
			{
				return cSharpUnaryOperationBinder.Operation == ExpressionType.Decrement;
			}
			return true;
		}
		return false;
	}

	internal static IEnumerable<T> Cons<T>(T sourceHead, IEnumerable<T> sourceTail)
	{
		yield return sourceHead;
		if (sourceTail == null)
		{
			yield break;
		}
		foreach (T item in sourceTail)
		{
			yield return item;
		}
	}

	internal static IEnumerable<T> Cons<T>(T sourceHead, IEnumerable<T> sourceMiddle, T sourceLast)
	{
		yield return sourceHead;
		if (sourceMiddle != null)
		{
			foreach (T item in sourceMiddle)
			{
				yield return item;
			}
		}
		yield return sourceLast;
	}

	internal static List<T> ToList<T>(IEnumerable<T> source)
	{
		if (source == null)
		{
			return new List<T>();
		}
		return source.ToList();
	}

	internal static CallInfo CreateCallInfo(IEnumerable<CSharpArgumentInfo> argInfos, int discard)
	{
		int num = 0;
		List<string> list = new List<string>();
		foreach (CSharpArgumentInfo argInfo in argInfos)
		{
			if (argInfo.NamedArgument)
			{
				list.Add(argInfo.Name);
			}
			num++;
		}
		return new CallInfo(num - discard, list);
	}
}
