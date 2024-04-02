using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class DynamicMetaObjectProviderDebugView
{
	[DebuggerDisplay("{value}", Name = "{name, nq}", Type = "{type, nq}")]
	internal class DynamicProperty
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly string name;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly object value;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly string type;

		public DynamicProperty(string name, object value)
		{
			this.name = name;
			this.value = value;
			type = ((value == null) ? "<null>" : value.GetType().ToString());
		}
	}

	[Serializable]
	internal class DynamicDebugViewEmptyException : Exception
	{
		public string Empty => Strings.EmptyDynamicView;

		public DynamicDebugViewEmptyException()
		{
		}

		protected DynamicDebugViewEmptyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private IList<KeyValuePair<string, object>> results;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private object obj;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private static readonly Type ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private static readonly ParameterExpression parameter = Expression.Parameter(typeof(object), "debug");

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	internal DynamicProperty[] Items
	{
		get
		{
			if (results == null || results.Count == 0)
			{
				results = QueryDynamicObject(obj);
				if (results == null || results.Count == 0)
				{
					throw new DynamicDebugViewEmptyException();
				}
			}
			DynamicProperty[] array = new DynamicProperty[results.Count];
			for (int i = 0; i < results.Count; i++)
			{
				array[i] = new DynamicProperty(results[i].Key, results[i].Value);
			}
			return array;
		}
	}

	public DynamicMetaObjectProviderDebugView(object arg)
	{
		obj = arg;
	}

	private static bool IsComObject(object obj)
	{
		if (obj != null)
		{
			return ComObjectType.IsAssignableFrom(obj.GetType());
		}
		return false;
	}

	public static object TryEvalBinaryOperators<T1, T2>(T1 arg1, T2 arg2, CSharpArgumentInfoFlags arg1Flags, CSharpArgumentInfoFlags arg2Flags, ExpressionType opKind, Type accessibilityContext)
	{
		CSharpArgumentInfo cSharpArgumentInfo = CSharpArgumentInfo.Create(arg1Flags, null);
		CSharpArgumentInfo cSharpArgumentInfo2 = CSharpArgumentInfo.Create(arg2Flags, null);
		CSharpBinaryOperationBinder binder = new CSharpBinaryOperationBinder(opKind, isChecked: false, CSharpBinaryOperationFlags.None, accessibilityContext, new CSharpArgumentInfo[2] { cSharpArgumentInfo, cSharpArgumentInfo2 });
		CallSite<Func<CallSite, T1, T2, object>> callSite = CallSite<Func<CallSite, T1, T2, object>>.Create(binder);
		return callSite.Target(callSite, arg1, arg2);
	}

	public static object TryEvalUnaryOperators<T>(T obj, ExpressionType oper, Type accessibilityContext)
	{
		if (oper == ExpressionType.IsTrue || oper == ExpressionType.IsFalse)
		{
			CallSite<Func<CallSite, T, bool>> callSite = CallSite<Func<CallSite, T, bool>>.Create(new CSharpUnaryOperationBinder(oper, isChecked: false, accessibilityContext, new CSharpArgumentInfo[1] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }));
			return callSite.Target(callSite, obj);
		}
		CallSite<Func<CallSite, T, object>> callSite2 = CallSite<Func<CallSite, T, object>>.Create(new CSharpUnaryOperationBinder(oper, isChecked: false, accessibilityContext, new CSharpArgumentInfo[1] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }));
		return callSite2.Target(callSite2, obj);
	}

	public static K TryEvalCast<T, K>(T obj, Type type, CSharpBinderFlags kind, Type accessibilityContext)
	{
		CallSite<Func<CallSite, T, K>> callSite = CallSite<Func<CallSite, T, K>>.Create(Binder.Convert(kind, type, accessibilityContext));
		return callSite.Target(callSite, obj);
	}

	private static void CreateDelegateSignatureAndArgumentInfos(object[] args, Type[] argTypes, CSharpArgumentInfoFlags[] argFlags, out Type[] delegateSignatureTypes, out CSharpArgumentInfo[] argInfos)
	{
		int num = args.Length;
		delegateSignatureTypes = new Type[num + 2];
		delegateSignatureTypes[0] = typeof(CallSite);
		argInfos = new CSharpArgumentInfo[num];
		for (int i = 0; i < num; i++)
		{
			if (argTypes[i] != null)
			{
				delegateSignatureTypes[i + 1] = argTypes[i];
			}
			else if (args[i] != null)
			{
				delegateSignatureTypes[i + 1] = args[i].GetType();
			}
			else
			{
				delegateSignatureTypes[i + 1] = typeof(object);
			}
			argInfos[i] = CSharpArgumentInfo.Create(argFlags[i], null);
		}
		delegateSignatureTypes[num + 1] = typeof(object);
	}

	private static object CreateDelegateAndInvoke(Type[] delegateSignatureTypes, CallSiteBinder binder, object[] args)
	{
		Type delegateType = Expression.GetDelegateType(delegateSignatureTypes);
		CallSite callSite = CallSite.Create(delegateType, binder);
		Delegate @delegate = (Delegate)callSite.GetType().GetField("Target").GetValue(callSite);
		object[] array = new object[args.Length + 1];
		array[0] = callSite;
		args.CopyTo(array, 1);
		return @delegate.DynamicInvoke(array);
	}

	public static object TryEvalMethodVarArgs(object[] methodArgs, Type[] argTypes, CSharpArgumentInfoFlags[] argFlags, string methodName, Type accessibilityContext, Type[] typeArguments)
	{
		Type[] delegateSignatureTypes = null;
		CSharpArgumentInfo[] argInfos = null;
		CreateDelegateSignatureAndArgumentInfos(methodArgs, argTypes, argFlags, out delegateSignatureTypes, out argInfos);
		return CreateDelegateAndInvoke(binder: (!string.IsNullOrEmpty(methodName)) ? ((CallSiteBinder)new CSharpInvokeMemberBinder(CSharpCallFlags.ResultDiscarded, methodName, accessibilityContext, typeArguments, argInfos)) : ((CallSiteBinder)new CSharpInvokeBinder(CSharpCallFlags.ResultDiscarded, accessibilityContext, argInfos)), delegateSignatureTypes: delegateSignatureTypes, args: methodArgs);
	}

	public static object TryGetMemberValue<T>(T obj, string propName, Type accessibilityContext, bool isResultIndexed)
	{
		CSharpGetMemberBinder binder = new CSharpGetMemberBinder(propName, isResultIndexed, accessibilityContext, new CSharpArgumentInfo[1] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
		CallSite<Func<CallSite, T, object>> callSite = CallSite<Func<CallSite, T, object>>.Create(binder);
		return callSite.Target(callSite, obj);
	}

	public static object TryGetMemberValueVarArgs(object[] propArgs, Type[] argTypes, CSharpArgumentInfoFlags[] argFlags, Type accessibilityContext)
	{
		Type[] delegateSignatureTypes = null;
		CSharpArgumentInfo[] argInfos = null;
		CreateDelegateSignatureAndArgumentInfos(propArgs, argTypes, argFlags, out delegateSignatureTypes, out argInfos);
		CallSiteBinder binder = new CSharpGetIndexBinder(accessibilityContext, argInfos);
		return CreateDelegateAndInvoke(delegateSignatureTypes, binder, propArgs);
	}

	public static object TrySetMemberValue<TObject, TValue>(TObject obj, string propName, TValue value, CSharpArgumentInfoFlags valueFlags, Type accessibilityContext)
	{
		CSharpArgumentInfo cSharpArgumentInfo = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
		CSharpArgumentInfo cSharpArgumentInfo2 = CSharpArgumentInfo.Create(valueFlags, null);
		CSharpSetMemberBinder binder = new CSharpSetMemberBinder(propName, isCompoundAssignment: false, isChecked: false, accessibilityContext, new CSharpArgumentInfo[2] { cSharpArgumentInfo, cSharpArgumentInfo2 });
		CallSite<Func<CallSite, TObject, TValue, object>> callSite = CallSite<Func<CallSite, TObject, TValue, object>>.Create(binder);
		return callSite.Target(callSite, obj, value);
	}

	public static object TrySetMemberValueVarArgs(object[] propArgs, Type[] argTypes, CSharpArgumentInfoFlags[] argFlags, Type accessibilityContext)
	{
		Type[] delegateSignatureTypes = null;
		CSharpArgumentInfo[] argInfos = null;
		CreateDelegateSignatureAndArgumentInfos(propArgs, argTypes, argFlags, out delegateSignatureTypes, out argInfos);
		CallSiteBinder binder = new CSharpSetIndexBinder(isCompoundAssignment: false, isChecked: false, accessibilityContext, argInfos);
		return CreateDelegateAndInvoke(delegateSignatureTypes, binder, propArgs);
	}

	internal static object TryGetMemberValue(object obj, string name, bool ignoreException)
	{
		bool ignoreCase = false;
		object obj2 = null;
		CallSite<Func<CallSite, object, object>> callSite = CallSite<Func<CallSite, object, object>>.Create(new GetMemberValueBinder(name, ignoreCase));
		try
		{
			return callSite.Target(callSite, obj);
		}
		catch (DynamicBindingFailedException ex)
		{
			if (ignoreException)
			{
				return null;
			}
			throw ex;
		}
		catch (MissingMemberException ex2)
		{
			if (ignoreException)
			{
				return Strings.GetValueonWriteOnlyProperty;
			}
			throw ex2;
		}
	}

	private static IList<KeyValuePair<string, object>> QueryDynamicObject(object obj)
	{
		if (obj is IDynamicMetaObjectProvider dynamicMetaObjectProvider)
		{
			DynamicMetaObject metaObject = dynamicMetaObjectProvider.GetMetaObject(parameter);
			List<string> list = new List<string>(metaObject.GetDynamicMemberNames());
			list.Sort();
			if (list != null)
			{
				List<KeyValuePair<string, object>> list2 = new List<KeyValuePair<string, object>>();
				{
					foreach (string item in list)
					{
						object value;
						if ((value = TryGetMemberValue(obj, item, ignoreException: true)) != null)
						{
							list2.Add(new KeyValuePair<string, object>(item, value));
						}
					}
					return list2;
				}
			}
		}
		else if (IsComObject(obj))
		{
			string[] comExclusionList = new string[1] { "MailEnvelope" };
			IEnumerable<string> dynamicDataMemberNames = ComBinder.GetDynamicDataMemberNames(obj);
			dynamicDataMemberNames = dynamicDataMemberNames.Where((string name) => !comExclusionList.Contains(name));
			List<string> list3 = new List<string>(dynamicDataMemberNames);
			list3.Sort();
			return ComBinder.GetDynamicDataMembers(obj, list3);
		}
		return new KeyValuePair<string, object>[0];
	}
}
