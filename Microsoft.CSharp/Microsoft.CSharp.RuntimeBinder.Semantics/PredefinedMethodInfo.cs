using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class PredefinedMethodInfo
{
	public PREDEFMETH method;

	public PredefinedType type;

	public PredefinedName name;

	public MethodCallingConventionEnum callingConvention;

	public ACCESS access;

	public int cTypeVars;

	public int[] signature;

	public PredefinedMethodInfo(PREDEFMETH method, MethodRequiredEnum required, PredefinedType type, PredefinedName name, MethodCallingConventionEnum callingConvention, ACCESS access, int cTypeVars, int[] signature)
	{
		this.method = method;
		this.type = type;
		this.name = name;
		this.callingConvention = callingConvention;
		this.access = access;
		this.cTypeVars = cTypeVars;
		this.signature = signature;
	}
}
