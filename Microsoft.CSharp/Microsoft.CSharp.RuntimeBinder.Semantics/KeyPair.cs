using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal struct KeyPair<Key1, Key2> : IEquatable<KeyPair<Key1, Key2>>
{
	private Key1 m_pKey1;

	private Key2 m_pKey2;

	public KeyPair(Key1 pKey1, Key2 pKey2)
	{
		m_pKey1 = pKey1;
		m_pKey2 = pKey2;
	}

	public bool Equals(KeyPair<Key1, Key2> other)
	{
		if (object.Equals(m_pKey1, other.m_pKey1))
		{
			return object.Equals(m_pKey2, other.m_pKey2);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is KeyPair<Key1, Key2>))
		{
			return false;
		}
		return Equals((KeyPair<Key1, Key2>)obj);
	}

	public override int GetHashCode()
	{
		return ((m_pKey1 != null) ? m_pKey1.GetHashCode() : 0) + ((m_pKey2 != null) ? m_pKey2.GetHashCode() : 0);
	}
}
