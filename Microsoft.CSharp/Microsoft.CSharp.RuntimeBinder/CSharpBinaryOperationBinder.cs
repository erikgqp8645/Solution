using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpBinaryOperationBinder : BinaryOperationBinder
{
	private bool m_isChecked;

	private CSharpBinaryOperationFlags m_binopFlags;

	private Type m_callingContext;

	private List<CSharpArgumentInfo> m_argumentInfo;

	private RuntimeBinder m_binder;

	internal bool IsChecked => m_isChecked;

	internal bool IsLogicalOperation => (m_binopFlags & CSharpBinaryOperationFlags.LogicalOperation) != 0;

	internal Type CallingContext => m_callingContext;

	internal IList<CSharpArgumentInfo> ArgumentInfo => m_argumentInfo.AsReadOnly();

	public CSharpBinaryOperationBinder(ExpressionType operation, bool isChecked, CSharpBinaryOperationFlags binaryOperationFlags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(operation)
	{
		m_isChecked = isChecked;
		m_binopFlags = binaryOperationFlags;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public sealed override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
	{
		return BinderHelper.Bind(this, m_binder, BinderHelper.Cons(target, null, arg), m_argumentInfo, errorSuggestion);
	}
}
