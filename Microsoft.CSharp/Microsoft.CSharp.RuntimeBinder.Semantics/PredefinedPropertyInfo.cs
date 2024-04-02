using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PredefinedPropertyInfo
{
	public PREDEFPROP property;

	public PredefinedName name;

	public PREDEFMETH getter;

	public PREDEFMETH setter;

	public PredefinedPropertyInfo(PREDEFPROP property, MethodRequiredEnum required, PredefinedName name, PREDEFMETH getter, PREDEFMETH setter)
	{
		this.property = property;
		this.name = name;
		this.getter = getter;
		this.setter = setter;
	}
}
