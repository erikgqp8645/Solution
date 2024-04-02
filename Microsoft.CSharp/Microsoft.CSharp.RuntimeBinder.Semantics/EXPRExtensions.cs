using System;
using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal static class EXPRExtensions
{
	public static EXPR Map(this EXPR expr, ExprFactory factory, Func<EXPR, EXPR> f)
	{
		if (expr == null)
		{
			return f(expr);
		}
		EXPR first = null;
		EXPR last = null;
		foreach (EXPR item in expr.ToEnumerable())
		{
			EXPR newItem = f(item);
			factory.AppendItemToList(newItem, ref first, ref last);
		}
		return first;
	}

	public static IEnumerable<EXPR> ToEnumerable(this EXPR expr)
	{
		EXPR exprCur = expr;
		while (exprCur != null)
		{
			if (exprCur.isLIST())
			{
				yield return exprCur.asLIST().GetOptionalElement();
				exprCur = exprCur.asLIST().GetOptionalNextListNode();
				continue;
			}
			yield return exprCur;
			break;
		}
	}

	public static bool isSTMT(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind < ExpressionKind.EK_BINOP;
		}
		return false;
	}

	public static EXPRSTMT asSTMT(this EXPR expr)
	{
		return (EXPRSTMT)expr;
	}

	public static bool isBIN(this EXPR expr)
	{
		if (expr != null)
		{
			if (expr.kind >= ExpressionKind.EK_COUNT)
			{
				return (expr.flags & EXPRFLAG.EXF_BINOP) != 0;
			}
			return false;
		}
		return false;
	}

	public static bool isUnaryOperator(this EXPR expr)
	{
		if (expr != null)
		{
			switch (expr.kind)
			{
			case ExpressionKind.EK_UNARYOP:
			case ExpressionKind.EK_TRUE:
			case ExpressionKind.EK_FALSE:
			case ExpressionKind.EK_INC:
			case ExpressionKind.EK_DEC:
			case ExpressionKind.EK_LOGNOT:
			case ExpressionKind.EK_NEG:
			case ExpressionKind.EK_UPLUS:
			case ExpressionKind.EK_BITNOT:
			case ExpressionKind.EK_ADDR:
			case ExpressionKind.EK_DECIMALNEG:
			case ExpressionKind.EK_DECIMALINC:
			case ExpressionKind.EK_DECIMALDEC:
				return true;
			}
		}
		return false;
	}

	public static bool isLvalue(this EXPR expr)
	{
		if (expr != null)
		{
			return (expr.flags & EXPRFLAG.EXF_LVALUE) != 0;
		}
		return false;
	}

	public static bool isChecked(this EXPR expr)
	{
		if (expr != null)
		{
			return (expr.flags & EXPRFLAG.EXF_CHECKOVERFLOW) != 0;
		}
		return false;
	}

	public static EXPRBINOP asBIN(this EXPR expr)
	{
		return (EXPRBINOP)expr;
	}

	public static EXPRUNARYOP asUnaryOperator(this EXPR expr)
	{
		return (EXPRUNARYOP)expr;
	}

	public static bool isANYLOCAL(this EXPR expr)
	{
		if (expr != null)
		{
			if (expr.kind != ExpressionKind.EK_LOCAL)
			{
				return expr.kind == ExpressionKind.EK_THISPOINTER;
			}
			return true;
		}
		return false;
	}

	public static EXPRLOCAL asANYLOCAL(this EXPR expr)
	{
		return (EXPRLOCAL)expr;
	}

	public static bool isANYLOCAL_OK(this EXPR expr)
	{
		if (expr.isANYLOCAL())
		{
			return expr.isOK();
		}
		return false;
	}

	public static bool isNull(this EXPR expr)
	{
		if (expr.isCONSTANT_OK() && expr.type.fundType() == FUNDTYPE.FT_REF)
		{
			return expr.asCONSTANT().Val.IsNullRef();
		}
		return false;
	}

	public static bool isZero(this EXPR expr)
	{
		if (expr.isCONSTANT_OK())
		{
			return expr.asCONSTANT().isZero();
		}
		return false;
	}

	public static EXPR GetSeqVal(this EXPR expr)
	{
		if (expr == null)
		{
			return null;
		}
		EXPR eXPR = expr;
		while (true)
		{
			switch (eXPR.kind)
			{
			default:
				return eXPR;
			case ExpressionKind.EK_SEQUENCE:
				eXPR = eXPR.asBIN().GetOptionalRightChild();
				break;
			case ExpressionKind.EK_SEQREV:
				eXPR = eXPR.asBIN().GetOptionalLeftChild();
				break;
			}
		}
	}

	public static EXPR GetConst(this EXPR expr)
	{
		EXPR seqVal = expr.GetSeqVal();
		if (seqVal == null || (!seqVal.isCONSTANT_OK() && seqVal.kind != ExpressionKind.EK_ZEROINIT))
		{
			return null;
		}
		return seqVal;
	}

	private static void RETAILVERIFY(bool f)
	{
	}

	public static EXPRRETURN asRETURN(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_RETURN);
		return (EXPRRETURN)expr;
	}

	public static EXPRBINOP asBINOP(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_BINOP);
		return (EXPRBINOP)expr;
	}

	public static EXPRLIST asLIST(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_LIST);
		return (EXPRLIST)expr;
	}

	public static EXPRARRAYINDEX asARRAYINDEX(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_ARRAYINDEX);
		return (EXPRARRAYINDEX)expr;
	}

	public static EXPRCALL asCALL(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_CALL);
		return (EXPRCALL)expr;
	}

	public static EXPREVENT asEVENT(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_EVENT);
		return (EXPREVENT)expr;
	}

	public static EXPRFIELD asFIELD(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_FIELD);
		return (EXPRFIELD)expr;
	}

	public static EXPRCONSTANT asCONSTANT(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_CONSTANT);
		return (EXPRCONSTANT)expr;
	}

	public static EXPRFUNCPTR asFUNCPTR(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_FUNCPTR);
		return (EXPRFUNCPTR)expr;
	}

	public static EXPRPROP asPROP(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_PROP);
		return (EXPRPROP)expr;
	}

	public static EXPRWRAP asWRAP(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_WRAP);
		return (EXPRWRAP)expr;
	}

	public static EXPRARRINIT asARRINIT(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_ARRINIT);
		return (EXPRARRINIT)expr;
	}

	public static EXPRCAST asCAST(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_CAST);
		return (EXPRCAST)expr;
	}

	public static EXPRUSERDEFINEDCONVERSION asUSERDEFINEDCONVERSION(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_USERDEFINEDCONVERSION);
		return (EXPRUSERDEFINEDCONVERSION)expr;
	}

	public static EXPRTYPEOF asTYPEOF(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_TYPEOF);
		return (EXPRTYPEOF)expr;
	}

	public static EXPRZEROINIT asZEROINIT(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_ZEROINIT);
		return (EXPRZEROINIT)expr;
	}

	public static EXPRUSERLOGOP asUSERLOGOP(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_USERLOGOP);
		return (EXPRUSERLOGOP)expr;
	}

	public static EXPRMEMGRP asMEMGRP(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_MEMGRP);
		return (EXPRMEMGRP)expr;
	}

	public static EXPRFIELDINFO asFIELDINFO(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_FIELDINFO);
		return (EXPRFIELDINFO)expr;
	}

	public static EXPRMETHODINFO asMETHODINFO(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_METHODINFO);
		return (EXPRMETHODINFO)expr;
	}

	public static EXPRPropertyInfo asPropertyInfo(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_PROPERTYINFO);
		return (EXPRPropertyInfo)expr;
	}

	public static EXPRNamedArgumentSpecification asNamedArgumentSpecification(this EXPR expr)
	{
		RETAILVERIFY(expr == null || expr.kind == ExpressionKind.EK_NamedArgumentSpecification);
		return (EXPRNamedArgumentSpecification)expr;
	}

	public static bool isCONSTANT_OK(this EXPR expr)
	{
		if (expr != null)
		{
			if (expr.kind == ExpressionKind.EK_CONSTANT)
			{
				return expr.isOK();
			}
			return false;
		}
		return false;
	}

	public static bool isRETURN(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_RETURN;
		}
		return false;
	}

	public static bool isLIST(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_LIST;
		}
		return false;
	}

	public static bool isARRAYINDEX(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_ARRAYINDEX;
		}
		return false;
	}

	public static bool isCALL(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_CALL;
		}
		return false;
	}

	public static bool isFIELD(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_FIELD;
		}
		return false;
	}

	public static bool isCONSTANT(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_CONSTANT;
		}
		return false;
	}

	public static bool isCLASS(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_CLASS;
		}
		return false;
	}

	public static bool isPROP(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_PROP;
		}
		return false;
	}

	public static bool isWRAP(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_WRAP;
		}
		return false;
	}

	public static bool isARRINIT(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_ARRINIT;
		}
		return false;
	}

	public static bool isCAST(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_CAST;
		}
		return false;
	}

	public static bool isUSERDEFINEDCONVERSION(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_USERDEFINEDCONVERSION;
		}
		return false;
	}

	public static bool isTYPEOF(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_TYPEOF;
		}
		return false;
	}

	public static bool isZEROINIT(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_ZEROINIT;
		}
		return false;
	}

	public static bool isMEMGRP(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_MEMGRP;
		}
		return false;
	}

	public static bool isBOUNDLAMBDA(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_BOUNDLAMBDA;
		}
		return false;
	}

	public static bool isUNBOUNDLAMBDA(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_UNBOUNDLAMBDA;
		}
		return false;
	}

	public static bool isMETHODINFO(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_METHODINFO;
		}
		return false;
	}

	public static bool isNamedArgumentSpecification(this EXPR expr)
	{
		if (expr != null)
		{
			return expr.kind == ExpressionKind.EK_NamedArgumentSpecification;
		}
		return false;
	}
}
