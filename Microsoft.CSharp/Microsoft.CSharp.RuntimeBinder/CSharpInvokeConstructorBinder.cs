using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpInvokeConstructorBinder : DynamicMetaObjectBinder, ICSharpInvokeOrInvokeMemberBinder
{
	private CSharpCallFlags m_flags;

	private Type m_callingContext;

	private List<CSharpArgumentInfo> m_argumentInfo;

	private RuntimeBinder m_binder;

	public CSharpCallFlags Flags => m_flags;

	public Type CallingContext => m_callingContext;

	public IList<CSharpArgumentInfo> ArgumentInfo => m_argumentInfo.AsReadOnly();

	public bool StaticCall => true;

	public IList<Type> TypeArguments => new Type[0];

	public string Name => ".ctor";

	bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded => false;

	public CSharpInvokeConstructorBinder(CSharpCallFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
	{
		m_flags = flags;
		m_callingContext = callingContext;
		m_argumentInfo = BinderHelper.ToList(argumentInfo);
		m_binder = RuntimeBinder.GetInstance();
	}

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		return BinderHelper.Bind(this, m_binder, BinderHelper.Cons(target, args), m_argumentInfo, null);
	}
}
