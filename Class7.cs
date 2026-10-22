using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

[SuppressIldasm]
internal sealed class Class7
{
	[SuppressIldasm]
	public enum Enum0
	{
		Binary,
		Text,
		Xml,
		Image,
		ResX
	}

	[DebuggerNonUserCode]
	public Class7()
	{
	}

	public static Enum0 smethod_0(string string_0, byte[] byte_0)
	{
		string_0 = "." + string_0.Substring(checked(string_0.LastIndexOf(".") + 1));
		switch (Path.GetExtension(string_0).ToLower())
		{
		case ".resources":
			if (byte_0.Length > 10 && ((byte_0[0] == 206) & (byte_0[1] == 202) & (byte_0[2] == 239) & (byte_0[3] == 190) & (byte_0[4] == 1)))
			{
				return Enum0.ResX;
			}
			break;
		case ".png":
		case ".gif":
		case ".bmp":
		case ".jpg":
			try
			{
				Image image = smethod_5(byte_0);
				image.Dispose();
				return Enum0.Image;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			break;
		}
		return smethod_1(byte_0);
	}

	public static Enum0 smethod_1(byte[] byte_0)
	{
		if (byte_0.Length < 2)
		{
			return Enum0.Binary;
		}
		StreamReader input;
		switch ((byte_0[0] << 8) | byte_0[1])
		{
		case 61371:
			if (byte_0.Length >= 3 && byte_0[2] == 191)
			{
				Stream stream = new MemoryStream(byte_0, 2, checked(byte_0.Length - 2));
				input = new StreamReader(stream, Encoding.UTF8);
				break;
			}
			return Enum0.Binary;
		default:
		{
			if (!smethod_2(byte_0))
			{
				return Enum0.Binary;
			}
			Stream stream = new MemoryStream(byte_0);
			input = new StreamReader(stream, Encoding.UTF8);
			break;
		}
		case 65279:
		case 65534:
		{
			Stream stream = new MemoryStream(byte_0);
			input = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
			break;
		}
		}
		Enum0 result;
		try
		{
			XmlTextReader xmlTextReader = new XmlTextReader(input);
			xmlTextReader.XmlResolver = null;
			xmlTextReader.MoveToContent();
			result = Enum0.Xml;
		}
		catch (XmlException projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = Enum0.Text;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	private static bool smethod_2(byte[] byte_0)
	{
		int num = Math.Min(byte_0.Length, 500000);
		int num2 = 0;
		int num3 = 0;
		checked
		{
			int num4 = num - 1;
			for (int i = 0; i <= num4; i++)
			{
				byte b = byte_0[i];
				if (b < 128)
				{
					if (num2 == 3)
					{
						num2 = 1;
						break;
					}
				}
				else if (b < 192)
				{
					if (num2 != 3)
					{
						num2 = 1;
						break;
					}
					num3--;
					if (num3 < 0)
					{
						num2 = 1;
						break;
					}
					if (num3 == 0)
					{
						num2 = 2;
					}
				}
				else
				{
					if (b < 194 || b >= 245)
					{
						num2 = 1;
						break;
					}
					if (num2 != 2 && num2 != 0)
					{
						num2 = 1;
						break;
					}
					num2 = 3;
					num3 = ((b < 224) ? 1 : ((b >= 240) ? 3 : 2));
				}
			}
			return num2 != 1;
		}
	}

	public static string smethod_3(byte[] byte_0)
	{
		string result;
		try
		{
			Stream stream;
			StreamReader streamReader;
			switch ((byte_0[0] << 8) | byte_0[1])
			{
			case 61371:
				if (byte_0.Length >= 3 && byte_0[2] == 191)
				{
					stream = new MemoryStream(byte_0, 2, checked(byte_0.Length - 2));
					streamReader = new StreamReader(stream, Encoding.UTF8);
					break;
				}
				result = Conversions.ToString(0);
				goto end_IL_0001;
			default:
				if (smethod_2(byte_0))
				{
					stream = new MemoryStream(byte_0);
					streamReader = new StreamReader(stream, Encoding.UTF8);
					break;
				}
				result = Conversions.ToString(0);
				goto end_IL_0001;
			case 65279:
			case 65534:
				stream = new MemoryStream(byte_0);
				streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
				break;
			}
			string text = streamReader.ReadToEnd();
			streamReader.Close();
			stream.Close();
			result = text;
			end_IL_0001:;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = smethod_4(byte_0);
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static string smethod_4(byte[] byte_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		checked
		{
			int num = byte_0.Length - 1;
			for (int i = 0; i <= num; i += 16)
			{
				stringBuilder.Append($"{Convert.ToString(i, 16).PadLeft(6, '0')}: ");
				if (i < 4096)
				{
					int num2 = i;
					int num3 = Math.Min(i + 15, byte_0.Length - 1);
					for (int j = num2; j <= num3; j++)
					{
						stringBuilder.Append(Convert.ToString(byte_0[j], 16).PadLeft(2, '0') + " ");
					}
					stringBuilder.Append("   ");
					if (byte_0.Length < i + 16)
					{
						stringBuilder.Append("".PadLeft((i + 16 - byte_0.Length) * 3, ' '));
					}
					int num4 = i;
					int num5 = Math.Min(i + 15, byte_0.Length - 1);
					for (int k = num4; k <= num5; k++)
					{
						if (byte_0[k] == 0)
						{
							stringBuilder.Append(".");
						}
						else if (Operators.CompareString(Conversions.ToString(Strings.Chr(byte_0[k])), "\t", TextCompare: false) == 0)
						{
							stringBuilder.Append(" ");
						}
						else
						{
							stringBuilder.Append(Strings.Chr(byte_0[k]));
						}
					}
					stringBuilder.AppendLine();
					continue;
				}
				stringBuilder.Append("......");
				break;
			}
			return stringBuilder.ToString();
		}
	}

	public static Image smethod_5(byte[] byte_0)
	{
		Image result;
		try
		{
			MemoryStream memoryStream = new MemoryStream(byte_0);
			Bitmap bitmap = new Bitmap(memoryStream);
			memoryStream.Close();
			result = bitmap;
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = null;
			ProjectData.ClearProjectError();
		}
		return result;
	}

	public static string _003C(byte[] byte_0)
	{
		string result;
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			MemoryStream memoryStream = new MemoryStream(byte_0);
			ResourceReader resourceReader = new ResourceReader(memoryStream);
			IDictionaryEnumerator enumerator = resourceReader.GetEnumerator();
			DictionaryEntry dictionaryEntry2 = default;
			while (enumerator.MoveNext())
			{
				object current = enumerator.Current;
				DictionaryEntry dictionaryEntry = ((current != null) ? ((DictionaryEntry)current) : dictionaryEntry2);
				stringBuilder.Append(Conversions.ToString(dictionaryEntry.Key).PadRight(64, ' '));
				if (dictionaryEntry.Value != null)
				{
					if (dictionaryEntry.Value is string)
					{
						stringBuilder.Append(Conversions.ToString(dictionaryEntry.Value));
					}
					else if (dictionaryEntry.Value is int)
					{
						stringBuilder.Append(Conversions.ToString(dictionaryEntry.Value));
					}
					else
					{
						stringBuilder.Append(string.Format(Class8.smethod_305(), dictionaryEntry.Value.GetType().FullName));
					}
				}
				stringBuilder.AppendLine();
			}
			memoryStream.Close();
			resourceReader.Close();
			result = stringBuilder.ToString();
		}
		catch (Exception projectError)
		{
			ProjectData.SetProjectError(projectError);
			result = smethod_4(byte_0);
			ProjectData.ClearProjectError();
		}
		return result;
	}
}
