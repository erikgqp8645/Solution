using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class ErrArgNoRef : ErrArgRef
{
	public ErrArgNoRef(CType pType)
	{
		eak = ErrArgKind.Type;
		eaf = ErrArgFlags.None;
		base.pType = pType;
	}
}
