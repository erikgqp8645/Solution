using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder;

[EditorBrowsable(EditorBrowsableState.Never)]
[__DynamicallyInvokable]
public static class Binder
{
	[__DynamicallyInvokable]
	public static CallSiteBinder BinaryOperation(CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
		bool flag = (flags & CSharpBinderFlags.BinaryOperationLogical) != 0;
		CSharpBinaryOperationFlags cSharpBinaryOperationFlags = CSharpBinaryOperationFlags.None;
		if (flag)
		{
			cSharpBinaryOperationFlags |= CSharpBinaryOperationFlags.LogicalOperation;
		}
		return new CSharpBinaryOperationBinder(operation, isChecked, cSharpBinaryOperationFlags, context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder Convert(CSharpBinderFlags flags, Type type, Type context)
	{
		CSharpConversionKind conversionKind = (((flags & CSharpBinderFlags.ConvertExplicit) != 0) ? CSharpConversionKind.ExplicitConversion : (((flags & CSharpBinderFlags.ConvertArrayIndex) != 0) ? CSharpConversionKind.ArrayCreationConversion : CSharpConversionKind.ImplicitConversion));
		bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
		return new CSharpConvertBinder(type, conversionKind, isChecked, context);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder GetIndex(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		return new CSharpGetIndexBinder(context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder GetMember(CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool resultIndexed = (flags & CSharpBinderFlags.ResultIndexed) != 0;
		return new CSharpGetMemberBinder(name, resultIndexed, context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder Invoke(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool flag = (flags & CSharpBinderFlags.ResultDiscarded) != 0;
		CSharpCallFlags cSharpCallFlags = CSharpCallFlags.None;
		if (flag)
		{
			cSharpCallFlags |= CSharpCallFlags.ResultDiscarded;
		}
		return new CSharpInvokeBinder(cSharpCallFlags, context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder InvokeMember(CSharpBinderFlags flags, string name, IEnumerable<Type> typeArguments, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool flag = (flags & CSharpBinderFlags.InvokeSimpleName) != 0;
		bool flag2 = (flags & CSharpBinderFlags.InvokeSpecialName) != 0;
		bool flag3 = (flags & CSharpBinderFlags.ResultDiscarded) != 0;
		CSharpCallFlags cSharpCallFlags = CSharpCallFlags.None;
		if (flag)
		{
			cSharpCallFlags |= CSharpCallFlags.SimpleNameCall;
		}
		if (flag2)
		{
			cSharpCallFlags |= CSharpCallFlags.EventHookup;
		}
		if (flag3)
		{
			cSharpCallFlags |= CSharpCallFlags.ResultDiscarded;
		}
		return new CSharpInvokeMemberBinder(cSharpCallFlags, name, context, typeArguments, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder InvokeConstructor(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		return new CSharpInvokeConstructorBinder(CSharpCallFlags.None, context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder IsEvent(CSharpBinderFlags flags, string name, Type context)
	{
		return new CSharpIsEventBinder(name, context);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder SetIndex(CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool isCompoundAssignment = (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
		bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
		return new CSharpSetIndexBinder(isCompoundAssignment, isChecked, context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder SetMember(CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool isCompoundAssignment = (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
		bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
		return new CSharpSetMemberBinder(name, isCompoundAssignment, isChecked, context, argumentInfo);
	}

	[__DynamicallyInvokable]
	public static CallSiteBinder UnaryOperation(CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
		return new CSharpUnaryOperationBinder(operation, isChecked, context, argumentInfo);
	}
}
