namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum UnaOpMask
{
	None = 0,
	Plus = 1,
	Minus = 2,
	Tilde = 4,
	Bang = 8,
	IncDec = 16,
	Signed = 7,
	Unsigned = 5,
	Real = 3,
	Bool = 8
}
