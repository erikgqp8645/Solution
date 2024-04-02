using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpUnaryOperationBinder : UnaryOperationBinder
{
	private bool m_isChecked;

	private Type m_callingContext;

	private List<CSharpArgumentInfo> m_argumentInfo;

	private RuntimeBinder m_binder;

	internal bool IsChecked => m_isChecked;

	internal Type CallingContext => m_callingContext;

	internal IList<CSharpArgumentInfo> ArgumentInfo => m_argumentInfo.AsReadOnly();

	public CSharpUnaryOperationBinder(ExpressionType operation, bool isChecked, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(operation)
	{
		m_isChecked = isChecked;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public sealed override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
	{
		return BinderHelper.Bind(this, m_binder, BinderHelper.Cons(target, null), m_argumentInfo, errorSuggestion);
	}
}
