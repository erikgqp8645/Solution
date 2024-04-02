namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRCALL : EXPR
{
	private EXPR OptionalArguments;

	private EXPRMEMGRP MemberGroup;

	public MethWithInst mwi;

	public PREDEFMETH PredefinedMethod;

	public NullableCallLiftKind nubLiftKind;

	public EXPR pConversions;

	public EXPR castOfNonLiftedResultToLiftedType;

	public EXPR GetOptionalArguments()
	{
		return OptionalArguments;
	}

	public void SetOptionalArguments(EXPR value)
	{
		OptionalArguments = value;
	}

	public EXPRMEMGRP GetMemberGroup()
	{
		return MemberGroup;
	}

	public void SetMemberGroup(EXPRMEMGRP value)
	{
		MemberGroup = value;
	}
}
