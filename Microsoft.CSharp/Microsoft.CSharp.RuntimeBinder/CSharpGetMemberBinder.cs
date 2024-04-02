using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpGetMemberBinder : GetMemberBinder, IInvokeOnGetBinder
{
	private Type m_callingContext;

	private List<CSharpArgumentInfo> m_argumentInfo;

	private bool m_bResultIndexed;

	private RuntimeBinder m_binder;

	internal Type CallingContext => m_callingContext;

	internal IList<CSharpArgumentInfo> ArgumentInfo => m_argumentInfo.AsReadOnly();

	bool IInvokeOnGetBinder.InvokeOnGet => !m_bResultIndexed;

	internal bool ResultIndexed => m_bResultIndexed;

	public CSharpGetMemberBinder(string name, bool resultIndexed, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(name, ignoreCase: false)
	{
		m_bResultIndexed = resultIndexed;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
	{
		if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindGetMember(this, target, out var result, ResultIndexed))
		{
			return result;
		}
		return BinderHelper.Bind(this, m_binder, new DynamicMetaObject[1] { target }, m_argumentInfo, errorSuggestion);
	}
}
