namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal enum ErrArgFlags
{
	None = 0,
	Ref = 1,
	NoStr = 2,
	RefOnly = 3,
	Unique = 4,
	UseGetErrorInfo = 8
}
