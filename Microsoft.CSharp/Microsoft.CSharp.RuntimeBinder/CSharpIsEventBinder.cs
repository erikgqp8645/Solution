using System;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder;

internal sealed class CSharpIsEventBinder : DynamicMetaObjectBinder
{
	private string m_name;

	private Type m_callingContext;

	private RuntimeBinder m_binder;

	internal string Name => m_name;

	internal Type CallingContext => m_callingContext;

	public sealed override Type ReturnType => typeof(bool);

	public CSharpIsEventBinder(string name, Type callingContext)
	{
		m_name = name;
		m_callingContext = callingContext;
		m_binder = RuntimeBinder.GetInstance();
	}

	public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
	{
		return BinderHelper.Bind(this, m_binder, new DynamicMetaObject[1] { target }, null, null);
	}
}
