namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal class CandidateFunctionMember
{
	public MethPropWithInst mpwi;

	public TypeArray @params;

	public byte ctypeLift;

	public bool fExpanded;

	public CandidateFunctionMember(MethPropWithInst mpwi, TypeArray @params, byte ctypeLift, bool fExpanded)
	{
		this.mpwi = mpwi;
		this.@params = @params;
		this.ctypeLift = ctypeLift;
		this.fExpanded = fExpanded;
	}
}
