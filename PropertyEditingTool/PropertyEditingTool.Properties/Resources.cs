using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace PropertyEditingTool.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				resourceMan = new ResourceManager("PropertyEditingTool.Properties.Resources", typeof(Resources).Assembly);
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static Icon app => (Icon)ResourceManager.GetObject("app", resourceCulture);

	internal static Bitmap asm => (Bitmap)ResourceManager.GetObject("asm", resourceCulture);

	internal static Bitmap document_pdf_32px_1 => (Bitmap)ResourceManager.GetObject("document_pdf_32px_1", resourceCulture);

	internal static Bitmap document_pdf_32px_2 => (Bitmap)ResourceManager.GetObject("document_pdf_32px_2", resourceCulture);

	internal static Bitmap Document_Remove_24px => (Bitmap)ResourceManager.GetObject("Document_Remove_24px", resourceCulture);

	internal static Bitmap dwg => (Bitmap)ResourceManager.GetObject("dwg", resourceCulture);

	internal static Bitmap file => (Bitmap)ResourceManager.GetObject("file", resourceCulture);

	internal static Bitmap folder_24 => (Bitmap)ResourceManager.GetObject("folder_24", resourceCulture);

	internal static Bitmap left_32px => (Bitmap)ResourceManager.GetObject("left_32px", resourceCulture);

	internal static Bitmap logo => (Bitmap)ResourceManager.GetObject("logo", resourceCulture);

	internal static Bitmap prt => (Bitmap)ResourceManager.GetObject("prt", resourceCulture);

	internal static Bitmap right_32px => (Bitmap)ResourceManager.GetObject("right_32px", resourceCulture);

	internal static Bitmap sw => (Bitmap)ResourceManager.GetObject("sw", resourceCulture);

	internal static Bitmap Video_32px => (Bitmap)ResourceManager.GetObject("Video_32px", resourceCulture);

	internal Resources()
	{
	}
}
