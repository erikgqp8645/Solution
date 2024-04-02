using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpInvokeMemberBinder : InvokeMemberBinder, ICSharpInvokeOrInvokeMemberBinder
{
	private CSharpCallFlags m_flags;

	private Type m_callingContext;

	private List<Type> m_typeArguments;

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

	CSharpCallFlags ICSharpInvokeOrInvokeMemberBinder.Flags => m_flags;

	Type ICSharpInvokeOrInvokeMemberBinder.CallingContext => m_callingContext;

	IList<Type> ICSharpInvokeOrInvokeMemberBinder.TypeArguments => m_typeArguments.AsReadOnly();

	IList<CSharpArgumentInfo> ICSharpInvokeOrInvokeMemberBinder.ArgumentInfo => m_argumentInfo.AsReadOnly();

	bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded => (m_flags & CSharpCallFlags.ResultDiscarded) != 0;

	public CSharpInvokeMemberBinder(CSharpCallFlags flags, string name, Type callingContext, IEnumerable<Type> typeArguments, IEnumerable<CSharpArgumentInfo> argumentInfo)
		: base(name, ignoreCase: false, BinderHelper.CreateCallInfo(argumentInfo, 1))
	{
		m_flags = flags;
		m_callingContext = callingContext;
		m_typeArguments = BinderHelper.ToList(typeArguments);
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
	{
		if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindInvokeMember(this, target, args, out var result))
		{
			return result;
		}
		return BinderHelper.Bind(this, m_binder, BinderHelper.Cons(target, args), m_argumentInfo, errorSuggestion);
	}

	public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
	{
		CSharpInvokeBinder cSharpInvokeBinder = new CSharpInvokeBinder(m_flags, m_callingContext, m_argumentInfo);
		return cSharpInvokeBinder.Defer(target, args);
	}

	[SpecialName]
	string ICSharpInvokeOrInvokeMemberBinder.get_Name()
	{
		return base.Name;
	}
}
