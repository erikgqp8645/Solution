using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class NullableType : CType
{
	public AggregateType ats;

	public BSYMMGR symmgr;

	public TypeManager typeManager;

	public CType UnderlyingType;

	public AggregateType GetAts(ErrorHandling errorContext)
	{
		AggregateSymbol nullable = typeManager.GetNullable();
		if (nullable == null)
		{
			throw Error.InternalCompilerError();
		}
		if (ats == null)
		{
			if (nullable == null)
			{
				typeManager.ReportMissingPredefTypeError(errorContext, PredefinedType.PT_G_OPTIONAL);
				return null;
			}
			CType cType = GetUnderlyingType();
			CType[] prgtype = new CType[1] { cType };
			TypeArray typeArgsAll = symmgr.AllocParams(1, prgtype);
			ats = typeManager.GetAggregate(nullable, typeArgsAll);
		}
		return ats;
	}

	public CType GetUnderlyingType()
	{
		return UnderlyingType;
	}

	public void SetUnderlyingType(CType pType)
	{
		UnderlyingType = pType;
	}
}
