using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpSetMemberBinder : SetMemberBinder
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

	public CSharpSetMemberBinder(string name, bool isCompoundAssignment, bool isChecked, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(name, ignoreCase: false)
	{
		m_bIsCompoundAssignment = isCompoundAssignment;
		m_isChecked = isChecked;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
	{
		if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindSetMember(this, target, value, out var result))
		{
			return result;
		}
		return BinderHelper.Bind(this, m_binder, new DynamicMetaObject[2] { target, value }, m_argumentInfo, errorSuggestion);
	}
}
