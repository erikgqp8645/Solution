namespace Microsoft.CSharp.RuntimeBinder.Semantics;

internal enum FUNDTYPE
{
	FT_NONE = 0,
	FT_I1 = 1,
	FT_I2 = 2,
	FT_I4 = 3,
	FT_U1 = 4,
	FT_U2 = 5,
	FT_U4 = 6,
	FT_LASTNONLONG = 6,
	FT_I8 = 7,
	FT_U8 = 8,
	FT_LASTINTEGRAL = 8,
	FT_R4 = 9,
	FT_R8 = 10,
	FT_LASTNUMERIC = 10,
	FT_REF = 11,
	FT_STRUCT = 12,
	FT_PTR = 13,
	FT_VAR = 14,
	FT_COUNT = 15
}
