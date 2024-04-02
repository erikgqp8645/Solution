namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal interface IErrorSink
{
	void SubmitError(CParameterizedError error);

	int ErrorCount();
}
