using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

[Flags]
internal enum SubstTypeFlags
{
	NormNone = 0,
	NormClass = 1,
	NormMeth = 2,
	NormAll = 3,
	DenormClass = 4,
	DenormMeth = 8,
	DenormAll = 0xC,
	NoRefOutDifference = 0x10
}
