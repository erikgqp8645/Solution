namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class ExpressionIterator
{
	private EXPRLIST m_pList;

	private EXPR m_pCurrent;

	public ExpressionIterator(EXPR pExpr)
	{
		Init(pExpr);
	}

	public bool AtEnd()
	{
		if (m_pCurrent == null)
		{
			return m_pList == null;
		}
		return false;
	}

	public EXPR Current()
	{
		return m_pCurrent;
	}

	public void MoveNext()
	{
		if (!AtEnd())
		{
			if (m_pList == null)
			{
				m_pCurrent = null;
			}
			else
			{
				Init(m_pList.GetOptionalNextListNode());
			}
		}
	}

	public static int Count(EXPR pExpr)
	{
		int num = 0;
		ExpressionIterator expressionIterator = new ExpressionIterator(pExpr);
		while (!expressionIterator.AtEnd())
		{
			num++;
			expressionIterator.MoveNext();
		}
		return num;
	}

	private void Init(EXPR pExpr)
	{
		if (pExpr == null)
		{
			m_pList = null;
			m_pCurrent = null;
		}
		else if (pExpr.isLIST())
		{
			m_pList = pExpr.asLIST();
			m_pCurrent = m_pList.GetOptionalElement();
		}
		else
		{
			m_pList = null;
			m_pCurrent = pExpr;
		}
	}
}
