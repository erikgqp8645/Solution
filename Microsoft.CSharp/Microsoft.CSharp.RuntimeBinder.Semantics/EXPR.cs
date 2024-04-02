namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal abstract class EXPR
{
	internal object RuntimeObject;

	internal CType RuntimeObjectActualType;

	public ExpressionKind kind;

	public EXPRFLAG flags;

	public bool IsError;

	public bool IsOptionalArgument;

	public string errorString;

	public CType type;

	protected static void RETAILVERIFY(bool f)
	{
	}

	public void SetInaccessibleBit()
	{
		IsError = true;
	}

	public void SetMismatchedStaticBit()
	{
		ExpressionKind expressionKind = kind;
		if (expressionKind == ExpressionKind.EK_CALL && this.asCALL().GetMemberGroup() != null)
		{
			this.asCALL().GetMemberGroup().SetMismatchedStaticBit();
		}
		IsError = true;
	}

	public void setType(CType t)
	{
		type = t;
	}

	public void setAssignment()
	{
		flags |= EXPRFLAG.EXF_ASSGOP;
	}

	public bool isOK()
	{
		return !HasError();
	}

	public bool HasError()
	{
		return IsError;
	}

	public void SetError()
	{
		IsError = true;
	}

	public bool HasObject()
	{
		ExpressionKind expressionKind = kind;
		if ((uint)(expressionKind - 11) <= 2u || (uint)(expressionKind - 21) <= 1u || expressionKind == ExpressionKind.EK_MEMGRP)
		{
			return true;
		}
		return false;
	}

	public EXPR getArgs()
	{
		RETAILVERIFY(this.isCALL() || this.isPROP() || this.isFIELD() || this.isARRAYINDEX());
		if (this.isFIELD())
		{
			return null;
		}
		return kind switch
		{
			ExpressionKind.EK_CALL => this.asCALL().GetOptionalArguments(), 
			ExpressionKind.EK_PROP => this.asPROP().GetOptionalArguments(), 
			ExpressionKind.EK_ARRAYINDEX => this.asARRAYINDEX().GetIndex(), 
			_ => null, 
		};
	}

	public void setArgs(EXPR args)
	{
		RETAILVERIFY(this.isCALL() || this.isPROP() || this.isFIELD() || this.isARRAYINDEX());
		if (!this.isFIELD())
		{
			switch (kind)
			{
			case ExpressionKind.EK_CALL:
				this.asCALL().SetOptionalArguments(args);
				break;
			case ExpressionKind.EK_PROP:
				this.asPROP().SetOptionalArguments(args);
				break;
			case ExpressionKind.EK_ARRAYINDEX:
				this.asARRAYINDEX().SetIndex(args);
				break;
			}
		}
	}

	public EXPR getObject()
	{
		RETAILVERIFY(HasObject());
		return kind switch
		{
			ExpressionKind.EK_FIELD => this.asFIELD().OptionalObject, 
			ExpressionKind.EK_PROP => this.asPROP().GetMemberGroup().OptionalObject, 
			ExpressionKind.EK_CALL => this.asCALL().GetMemberGroup().OptionalObject, 
			ExpressionKind.EK_MEMGRP => this.asMEMGRP().OptionalObject, 
			ExpressionKind.EK_EVENT => this.asEVENT().OptionalObject, 
			ExpressionKind.EK_FUNCPTR => this.asFUNCPTR().OptionalObject, 
			_ => null, 
		};
	}

	public void SetObject(EXPR pExpr)
	{
		RETAILVERIFY(HasObject());
		switch (kind)
		{
		case ExpressionKind.EK_FIELD:
			this.asFIELD().OptionalObject = pExpr;
			break;
		case ExpressionKind.EK_PROP:
			this.asPROP().GetMemberGroup().OptionalObject = pExpr;
			break;
		case ExpressionKind.EK_CALL:
			this.asCALL().GetMemberGroup().OptionalObject = pExpr;
			break;
		case ExpressionKind.EK_MEMGRP:
			this.asMEMGRP().OptionalObject = pExpr;
			break;
		case ExpressionKind.EK_EVENT:
			this.asEVENT().OptionalObject = pExpr;
			break;
		case ExpressionKind.EK_FUNCPTR:
			this.asFUNCPTR().OptionalObject = pExpr;
			break;
		}
	}

	public SymWithType GetSymWithType()
	{
		return kind switch
		{
			ExpressionKind.EK_CALL => ((EXPRCALL)this).mwi, 
			ExpressionKind.EK_PROP => ((EXPRPROP)this).pwtSlot, 
			ExpressionKind.EK_FIELD => ((EXPRFIELD)this).fwt, 
			ExpressionKind.EK_EVENT => ((EXPREVENT)this).ewt, 
			_ => ((EXPRCALL)this).mwi, 
		};
	}
}
