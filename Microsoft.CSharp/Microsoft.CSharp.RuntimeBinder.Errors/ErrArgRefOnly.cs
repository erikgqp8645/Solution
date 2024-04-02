using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class ErrArgRefOnly : ErrArgRef
{
	public ErrArgRefOnly(Symbol sym)
		: base(sym)
	{
		eaf = ErrArgFlags.RefOnly;
	}
}
