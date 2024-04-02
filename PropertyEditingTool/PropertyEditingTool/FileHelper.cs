using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace PropertyEditingTool;

public static class FileHelper
{
	public static T Clone<T>(T RealObject)
	{
		using Stream objectStream = new MemoryStream();
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		((IFormatter)binaryFormatter).Serialize(objectStream, (object)RealObject);
		objectStream.Seek(0L, SeekOrigin.Begin);
		return (T)((IFormatter)binaryFormatter).Deserialize(objectStream);
	}

	public static void WriteTxt(string path, string txt)
	{
		FileStream fileStream = new FileStream(path, FileMode.Create);
		StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.Default);
		streamWriter.WriteLine(txt);
		streamWriter.Flush();
		streamWriter.Close();
		fileStream.Close();
	}

	public static void WriteTxtAdd(string path, string txt)
	{
		StreamWriter streamWriter = new StreamWriter(path, append: true);
		streamWriter.WriteLine(txt);
		streamWriter.Flush();
		streamWriter.Close();
	}

	public static string ReadTxt(string path)
	{
		FileStream fs = new FileStream(path, FileMode.Open);
		StreamReader streamReader = new StreamReader(fs, Encoding.Default);
		string txt = streamReader.ReadToEnd();
		fs.Close();
		streamReader.Close();
		return txt;
	}
}
