namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class ErrArgIds : ErrArgRef
{
	public ErrArgIds(MessageID ids)
	{
		eak = ErrArgKind.Ids;
		eaf = ErrArgFlags.None;
		base.ids = ids;
	}
}
