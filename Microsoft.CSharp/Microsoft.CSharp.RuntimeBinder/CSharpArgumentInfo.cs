using System.ComponentModel;

namespace Microsoft.CSharp.RuntimeBinder;

[EditorBrowsable(EditorBrowsableState.Never)]
[__DynamicallyInvokable]
public sealed class CSharpArgumentInfo
{
	internal static readonly CSharpArgumentInfo None = new CSharpArgumentInfo(CSharpArgumentInfoFlags.None, null);

	private CSharpArgumentInfoFlags m_flags;

	private string m_name;

	internal CSharpArgumentInfoFlags Flags => m_flags;

	internal string Name => m_name;

	internal bool UseCompileTimeType => (Flags & CSharpArgumentInfoFlags.UseCompileTimeType) != 0;

	internal bool LiteralConstant => (Flags & CSharpArgumentInfoFlags.Constant) != 0;

	internal bool NamedArgument => (Flags & CSharpArgumentInfoFlags.NamedArgument) != 0;

	internal bool IsByRef => (Flags & CSharpArgumentInfoFlags.IsRef) != 0;

	internal bool IsOut => (Flags & CSharpArgumentInfoFlags.IsOut) != 0;

	internal bool IsStaticType => (Flags & CSharpArgumentInfoFlags.IsStaticType) != 0;

	private CSharpArgumentInfo(CSharpArgumentInfoFlags flags, string name)
	{
		m_flags = flags;
		m_name = name;
	}

	[__DynamicallyInvokable]
	public static CSharpArgumentInfo Create(CSharpArgumentInfoFlags flags, string name)
	{
		return new CSharpArgumentInfo(flags, name);
	}
}
