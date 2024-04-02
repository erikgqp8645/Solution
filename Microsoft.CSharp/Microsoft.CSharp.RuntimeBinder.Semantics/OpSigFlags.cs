namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum OpSigFlags
{
	None = 0,
	Convert = 1,
	CanLift = 2,
	AutoLift = 4,
	Value = 7,
	Reference = 1,
	BoolBit = 3
}
