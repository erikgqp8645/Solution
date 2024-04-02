using System;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder.Errors;

internal class ErrorHandling
{
	private IErrorSink m_errorSink;

	private UserStringBuilder m_userStringBuilder;

	private CErrorFactory m_errorFactory;

	public void Error(ErrorCode id, params ErrArg[] args)
	{
		ErrorTreeArgs(id, args);
	}

	public void ErrorRef(ErrorCode id, params ErrArgRef[] args)
	{
		ErrorTreeArgs(id, args);
	}

	public void SubmitError(CParameterizedError error)
	{
		if (m_errorSink != null)
		{
			m_errorSink.SubmitError(error);
		}
	}

	public void MakeErrorLocArgs(out CParameterizedError error, ErrorCode id, ErrArg[] prgarg)
	{
		error = new CParameterizedError();
		error.Initialize(id, prgarg);
	}

	public virtual void AddRelatedSymLoc(CParameterizedError err, Symbol sym)
	{
	}

	public virtual void AddRelatedTypeLoc(CParameterizedError err, CType pType)
	{
	}

	private void MakeErrorTreeArgs(out CParameterizedError error, ErrorCode id, ErrArg[] prgarg)
	{
		MakeErrorLocArgs(out error, id, prgarg);
	}

	public void MakeError(out CParameterizedError error, ErrorCode id, params ErrArg[] args)
	{
		MakeErrorTreeArgs(out error, id, args);
	}

	public ErrorHandling(UserStringBuilder strBldr, IErrorSink sink, CErrorFactory factory)
	{
		m_userStringBuilder = strBldr;
		m_errorSink = sink;
		m_errorFactory = factory;
	}

	private CError CreateError(ErrorCode iErrorIndex, string[] args)
	{
		return m_errorFactory.CreateError(iErrorIndex, args);
	}

	private void ErrorTreeArgs(ErrorCode id, ErrArg[] prgarg)
	{
		MakeErrorTreeArgs(out var error, id, prgarg);
		SubmitError(error);
	}

	public CError RealizeError(CParameterizedError parameterizedError)
	{
		string[] array = new string[parameterizedError.GetParameterCount()];
		int[] array2 = new int[parameterizedError.GetParameterCount()];
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		m_userStringBuilder.ResetUndisplayableStringFlag();
		for (int i = 0; i < parameterizedError.GetParameterCount(); i++)
		{
			ErrArg parameter = parameterizedError.GetParameter(i);
			if ((parameter.eaf & ErrArgFlags.NoStr) == 0)
			{
				bool fUserStrings = false;
				if (!m_userStringBuilder.ErrArgToString(out array[num], parameter, out fUserStrings) && parameter.eak == ErrArgKind.Int)
				{
					array[num] = parameter.n.ToString(CultureInfo.InvariantCulture);
				}
				num++;
				int num4;
				if (!fUserStrings || (parameter.eaf & ErrArgFlags.Unique) == 0)
				{
					num4 = -1;
				}
				else
				{
					num4 = i;
					num3++;
				}
				array2[num2] = num4;
				num2++;
			}
		}
		if (m_userStringBuilder.HadUndisplayableString())
		{
			return null;
		}
		int num5 = num;
		if (num3 > 1)
		{
			string[] array3 = new string[num5];
			Array.Copy(array, 0, array3, 0, num5);
			for (int j = 0; j < num5; j++)
			{
				if (array2[j] < 0 || array3[j] != array[j])
				{
					continue;
				}
				ErrArg parameter2 = parameterizedError.GetParameter(array2[j]);
				Symbol symbol = null;
				CType cType = null;
				ErrArgKind eak = parameter2.eak;
				if (eak <= ErrArgKind.Type)
				{
					if (eak != ErrArgKind.Sym)
					{
						if (eak != ErrArgKind.Type)
						{
							continue;
						}
						cType = parameter2.pType;
					}
					else
					{
						symbol = parameter2.sym;
					}
				}
				else if (eak != ErrArgKind.SymWithType)
				{
					if (eak != ErrArgKind.MethWithInst)
					{
						continue;
					}
					symbol = parameter2.mpwiMemo.sym;
				}
				else
				{
					symbol = parameter2.swtMemo.sym;
				}
				bool flag = false;
				for (int k = j + 1; k < num5; k++)
				{
					if (array2[k] < 0 || array[j] != array[k])
					{
						continue;
					}
					if (array3[k] != array[k])
					{
						flag = true;
						continue;
					}
					ErrArg parameter3 = parameterizedError.GetParameter(array2[k]);
					Symbol symbol2 = null;
					CType cType2 = null;
					ErrArgKind eak2 = parameter3.eak;
					if (eak2 <= ErrArgKind.Type)
					{
						if (eak2 != ErrArgKind.Sym)
						{
							if (eak2 != ErrArgKind.Type)
							{
								continue;
							}
							cType2 = parameter3.pType;
						}
						else
						{
							symbol2 = parameter3.sym;
						}
					}
					else if (eak2 != ErrArgKind.SymWithType)
					{
						if (eak2 != ErrArgKind.MethWithInst)
						{
							continue;
						}
						symbol2 = parameter3.mpwiMemo.sym;
					}
					else
					{
						symbol2 = parameter3.swtMemo.sym;
					}
					if (symbol2 != symbol || cType2 != cType || flag)
					{
						array3[k] = array[k];
						flag = true;
					}
				}
				if (flag)
				{
					array3[j] = array[j];
				}
			}
			array = array3;
		}
		return CreateError(parameterizedError.GetErrorNumber(), array);
	}
}
