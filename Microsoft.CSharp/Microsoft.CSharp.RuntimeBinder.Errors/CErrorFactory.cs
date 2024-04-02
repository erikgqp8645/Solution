namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class CErrorFactory
{
	public CError CreateError(ErrorCode iErrorIndex, params string[] args)
	{
		CError cError = new CError();
		cError.Initialize(iErrorIndex, args);
		return cError;
	}
}
