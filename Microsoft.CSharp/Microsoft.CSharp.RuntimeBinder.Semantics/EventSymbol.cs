using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EventSymbol : Symbol
{
	public EventInfo AssociatedEventInfo;

	public new bool isStatic;

	public bool isOverride;

	public CType type;

	public MethodSymbol methAdd;

	public MethodSymbol methRemove;

	public AggregateDeclaration declaration;

	public bool IsWindowsRuntimeEvent { get; set; }

	public AggregateDeclaration containingDeclaration()
	{
		return declaration;
	}
}
