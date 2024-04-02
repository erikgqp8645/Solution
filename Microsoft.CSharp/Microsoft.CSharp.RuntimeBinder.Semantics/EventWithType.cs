namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EventWithType : SymWithType
{
	public EventWithType()
	{
	}

	public EventWithType(EventSymbol @event, AggregateType ats)
	{
		Set(@event, ats);
	}
}
