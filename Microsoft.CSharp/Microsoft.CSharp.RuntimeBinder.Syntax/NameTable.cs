namespace Microsoft.CSharp.RuntimeBinder.Syntax;

internal class NameTable
{
	private class Entry
	{
		internal readonly Name name;

		internal readonly int hashCode;

		internal Entry next;

		internal Entry(Name name, int hashCode, Entry next)
		{
			this.name = name;
			this.hashCode = hashCode;
			this.next = next;
		}
	}

	private Entry[] entries;

	private int count;

	private int mask;

	private int hashCodeRandomizer;

	internal NameTable()
	{
		mask = 31;
		entries = new Entry[mask + 1];
		hashCodeRandomizer = 0;
	}

	public Name Add(string key)
	{
		int num = ComputeHashCode(key);
		for (Entry entry = entries[num & mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == num && entry.name.Text.Equals(key))
			{
				return entry.name;
			}
		}
		return AddEntry(new Name(key), num);
	}

	internal void Add(Name name)
	{
		int num = ComputeHashCode(name.Text);
		for (Entry entry = entries[num & mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == num && entry.name.Text.Equals(name.Text))
			{
				throw Error.InternalCompilerError();
			}
		}
		AddEntry(name, num);
	}

	public Name Lookup(string key)
	{
		int num = ComputeHashCode(key);
		for (Entry entry = entries[num & mask]; entry != null; entry = entry.next)
		{
			if (entry.hashCode == num && entry.name.Text.Equals(key))
			{
				return entry.name;
			}
		}
		return null;
	}

	private int ComputeHashCode(string key)
	{
		int length = key.Length;
		int num = length + hashCodeRandomizer;
		for (int i = 0; i < key.Length; i++)
		{
			num += (num << 7) ^ key[i];
		}
		num -= num >> 17;
		num -= num >> 11;
		return num - (num >> 5);
	}

	private Name AddEntry(Name name, int hashCode)
	{
		int num = hashCode & mask;
		Entry entry = new Entry(name, hashCode, entries[num]);
		entries[num] = entry;
		if (count++ == mask)
		{
			Grow();
		}
		return entry.name;
	}

	private void Grow()
	{
		int num = mask * 2 + 1;
		Entry[] array = entries;
		Entry[] array2 = new Entry[num + 1];
		for (int i = 0; i < array.Length; i++)
		{
			Entry entry = array[i];
			while (entry != null)
			{
				int num2 = entry.hashCode & num;
				Entry next = entry.next;
				entry.next = array2[num2];
				array2[num2] = entry;
				entry = next;
			}
		}
		entries = array2;
		mask = num;
	}
}
