using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpSetIndexBinder : SetIndexBinder
{
	private bool m_bIsCompoundAssignment;

	private bool m_isChecked;

	private Type m_callingContext;

	private List<CSharpArgumentInfo> m_argumentInfo;

	private RuntimeBinder m_binder;

	internal bool IsCompoundAssignment => m_bIsCompoundAssignment;

	internal bool IsChecked => m_isChecked;

	internal Type CallingContext => m_callingContext;

	internal IList<CSharpArgumentInfo> ArgumentInfo => m_argumentInfo.AsReadOnly();

	public CSharpSetIndexBinder(bool isCompoundAssignment, bool isChecked, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(BinderHelper.CreateCallInfo(argumentInfo, 2))
	{
		m_bIsCompoundAssignment = isCompoundAssignment;
		m_isChecked = isChecked;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
	{
		if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindSetIndex(this, target, indexes, value, out var result))
		{
			return result;
		}
		return BinderHelper.Bind(this, m_binder, BinderHelper.Cons(target, indexes, value), m_argumentInfo, errorSuggestion);
	}
}
