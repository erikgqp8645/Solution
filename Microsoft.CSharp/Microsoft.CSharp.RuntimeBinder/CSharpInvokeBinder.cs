using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpInvokeBinder : InvokeBinder, ICSharpInvokeOrInvokeMemberBinder
{
	private CSharpCallFlags m_flags;

	private Type m_callingContext;

	private List<CSharpArgumentInfo> m_argumentInfo;

	private RuntimeBinder m_binder;

	bool ICSharpInvokeOrInvokeMemberBinder.StaticCall
	{
		get
		{
			if (m_argumentInfo[0] != null)
			{
				return m_argumentInfo[0].IsStaticType;
			}
			return false;
		}
	}

	string ICSharpInvokeOrInvokeMemberBinder.Name => "Invoke";

	IList<Type> ICSharpInvokeOrInvokeMemberBinder.TypeArguments => new Type[0];

	CSharpCallFlags ICSharpInvokeOrInvokeMemberBinder.Flags => m_flags;

	Type ICSharpInvokeOrInvokeMemberBinder.CallingContext => m_callingContext;

	IList<CSharpArgumentInfo> ICSharpInvokeOrInvokeMemberBinder.ArgumentInfo => m_argumentInfo.AsReadOnly();

	bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded => (m_flags & CSharpCallFlags.ResultDiscarded) != 0;

	public CSharpInvokeBinder(CSharpCallFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(BinderHelper.CreateCallInfo(argumentInfo, 1))
	{
		m_flags = flags;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
	{
		if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindInvoke(this, target, args, out var result))
		{
			return result;
		}
		return BinderHelper.Bind(this, m_binder, BinderHelper.Cons(target, args), m_argumentInfo, errorSuggestion);
	}
}
