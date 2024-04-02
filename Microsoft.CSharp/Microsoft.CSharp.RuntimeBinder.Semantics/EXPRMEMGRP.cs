using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class EXPRMEMGRP : EXPR
{
	public Name name;

	public TypeArray typeArgs;

	public SYMKIND sk;

	public EXPR OptionalObject;

	public EXPR OptionalLHS;

	public CMemberLookupResults MemberLookupResults;

	public CType ParentType;

	public EXPR GetOptionalObject()
	{
		return OptionalObject;
	}

	public void SetOptionalObject(EXPR value)
	{
		OptionalObject = value;
	}

	public EXPR GetOptionalLHS()
	{
		return OptionalLHS;
	}

	public void SetOptionalLHS(EXPR lhs)
	{
		OptionalLHS = lhs;
	}

	public CMemberLookupResults GetMemberLookupResults()
	{
		return MemberLookupResults;
	}

	public void SetMemberLookupResults(CMemberLookupResults results)
	{
		MemberLookupResults = results;
	}

	public CType GetParentType()
	{
		return ParentType;
	}

	public void SetParentType(CType type)
	{
		ParentType = type;
	}

	public bool isDelegate()
	{
		return (flags & EXPRFLAG.EXF_GOTONOTBLOCKED) != 0;
	}
}
