namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum CONVERTTYPE
{
	NOUDC = 1,
	STANDARD = 2,
	ISEXPLICIT = 4,
	CHECKOVERFLOW = 8,
	FORCECAST = 16,
	STANDARDANDNOUDC = 3
}
