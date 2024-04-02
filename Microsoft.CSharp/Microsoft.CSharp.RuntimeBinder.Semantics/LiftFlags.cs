namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum LiftFlags
{
	None = 0,
	Lift1 = 1,
	Lift2 = 2,
	Convert1 = 4,
	Convert2 = 8
}
