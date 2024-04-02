namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRPROP : EXPR
{
	public EXPR OptionalArguments;

	public EXPRMEMGRP MemberGroup;

	public EXPR OptionalObjectThrough;

	public PropWithType pwtSlot;

	public MethWithType mwtSet;

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

	public EXPR GetOptionalObjectThrough()
	{
		return OptionalObjectThrough;
	}

	public void SetOptionalObjectThrough(EXPR value)
	{
		OptionalObjectThrough = value;
	}

	public bool isBaseCall()
	{
		return (flags & EXPRFLAG.EXF_ASFINALLYLEAVE) != 0;
	}
}
