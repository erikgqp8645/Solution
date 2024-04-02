using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder;

internal class RuntimeBinderController : CController
{
	public override void SubmitError(CError pError)
	{
		throw new RuntimeBinderException(pError.Text);
	}
}
