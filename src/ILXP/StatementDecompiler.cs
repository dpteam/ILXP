using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace ILXP
{
    public sealed class StatementDecompiler
    {
    	private static TypeDefinition typeDefinition_0 = new TypeDefinition("", "", TypeAttributes.NotPublic);

    	[SpecialName]
    	private static object object_0;

    	[SpecialName]
    	private static StaticLocalInitFlag staticLocalInitFlag_0 = new StaticLocalInitFlag();

    	[SpecialName]
    	private static bool bool_0;

    	[SpecialName]
    	private static StaticLocalInitFlag staticLocalInitFlag_1 = new StaticLocalInitFlag();

    	[SpecialName]
    	private static StaticLocalInitFlag _003C = new StaticLocalInitFlag();

    	[SpecialName]
    	private static StaticLocalInitFlag staticLocalInitFlag_2 = new StaticLocalInitFlag();

    	[SpecialName]
    	private static bool bool_1;

    	[SpecialName]
    	private static StaticLocalInitFlag _0024 = new StaticLocalInitFlag();

    	[SpecialName]
    	private static bool bool_2;

    	[SpecialName]
    	private static bool bool_3;

    	[SpecialName]
    	private static object object_1;

    	[SpecialName]
    	private static object object_2;

    	[SpecialName]
    	private static object object_3;

    	[SpecialName]
    	private static List<Instruction> list_0;

    	[DebuggerNonUserCode]
    	public StatementDecompiler()
    	{
    	}

    	public static void WriteVisualBasicBody(StringBuilder stringBuilder_0, MethodDefinition methodDefinition_0, int int_0)
    	{
    		string string_ = smethod_19(methodDefinition_0, int_0);
    		Dictionary<string, string> dictionary = new Dictionary<string, string>();
    		dictionary.Add("<EXITLOOP>", "<<b>>Exit Loop<<>>");
    		dictionary.Add("<GOTO>", "<<b>>GoTo<<>>");
    		dictionary.Add("<GENERICCLOSE>", ")");
    		dictionary.Add("<GENERICOPEN>", "(Of ");
    		dictionary.Add("<VALUE>", "value");
    		dictionary.Add("<SELECTCASESTART>", "<<b>>Select Case<<>>");
    		dictionary.Add("<SELECTCASEEND>", "<<b>>End Select<<>>");
    		dictionary.Add("<CASE>", "<<b>>Case<<>>");
    		dictionary.Add("<CASEELSE>", "<<b>>Case Else<<>>");
    		dictionary.Add("<CASEEXIT>", "<<b>>Exit Select<<>>");
    		dictionary.Add("<SELECTCASESTARTMARKER>", "");
    		dictionary.Add("<CASEMARKER>", "");
    		dictionary.Add("<FINALLYSTART>", "<<b>>Finally<<>>");
    		dictionary.Add("<FINALLYEND>", "<<b>>End Try<<>>");
    		dictionary.Add("<TRYSTART>", "<<b>>Try<<>>");
    		dictionary.Add("<TRYEND>", "<<b>>End Try<<>>");
    		dictionary.Add("<CATCHEND>", "<<b>>End Try<<>>");
    		dictionary.Add("<PLAINSTRING>", "");
    		dictionary.Add("<OBJECTADDRESS>", "");
    		dictionary.Add("<TRUE>", "<<b>>True<<>>");
    		dictionary.Add("<FALSE>", "<<b>>False<<>>");
    		dictionary.Add("<OBJECTREF>", "");
    		dictionary.Add("<REF>", "");
    		dictionary.Add("<OUT>", "");
    		dictionary.Add("<ADDRESSOF>", "<<b>>AddressOf<<>> ");
    		dictionary.Add("<LINEEND>", "");
    		dictionary.Add("<MOD>", "<<b>>Mod<<>>");
    		dictionary.Add("<NOT>", "<<b>>Not<<>> ");
    		dictionary.Add("<NOTEND>", "");
    		dictionary.Add("<AND>", "<<b>>And<<>>");
    		dictionary.Add("<OR>", "<<b>>Or<<>>");
    		dictionary.Add("<ORELSE>", "<<b>>OrElse<<>>");
    		dictionary.Add("<ANDALSO>", "<<b>>AndAlso<<>>");
    		dictionary.Add("<XOR>", "<<b>>Xor<<>>");
    		dictionary.Add("<EQUALS>", "=");
    		dictionary.Add("<NOTEQUALS>", "<>");
    		dictionary.Add("<GREATER>", ">");
    		dictionary.Add("<GREATEROREQUAL>", ">=");
    		dictionary.Add("<LESS>", "<");
    		dictionary.Add("<LESSOREQUAL>", "<=");
    		dictionary.Add("<YES>", "");
    		dictionary.Add("<YESEND>", "");
    		dictionary.Add("<IS>", "<<b>>Is<<>>");
    		dictionary.Add("<ISEQUALS>", "<<b>>Is<<>>");
    		dictionary.Add("<ISNOTEQUALS>", "<<b>>Is Not<<>>");
    		dictionary.Add("<TYPEOF>", "<<b>>TypeOf<<>> ");
    		dictionary.Add("<ME>", "<<b>>Me<<>>");
    		dictionary.Add("<MYBASE>", "<<b>>MyBase<<>>");
    		dictionary.Add("<NOTHING>", "<<b>>Nothing<<>>");
    		dictionary.Add("<STRINGCONCAT>", "&");
    		dictionary.Add("<NEW>", "<<b>>New<<>>");
    		dictionary.Add("<ARRAYBRACKETOPEN>", "(");
    		dictionary.Add("<ARRAYBRACKETCLOSE>", ")");
    		dictionary.Add("<GETTYPE>", "<<b>>GetType<<>>");
    		dictionary.Add("<RETURN>", "<<b>>Return<<>>");
    		dictionary.Add("<THROW>", "<<b>>Throw<<>>");
    		dictionary.Add("<RETHROW>", "<<b>>Throw<<>>");
    		dictionary.Add("<COMMENT>", "'");
    		dictionary.Add("<ADDHANDLER1>", "<<b>>AddHandler<<>> ");
    		dictionary.Add("<ADDHANDLER2>", ", ");
    		dictionary.Add("<REMOVEHANDLER1>", "<<b>>RemoveHandler<<>> ");
    		dictionary.Add("<REMOVEHANDLER2>", ", ");
    		dictionary.Add("<IF>", "<<b>>If<<>>");
    		dictionary.Add("<ELSE>", "<<b>>Else<<>>");
    		dictionary.Add("<ELSEIF>", "<<b>>ElseIf<<>>");
    		dictionary.Add("<IIFOPEN>", "<<b>>IIf<<>>(");
    		dictionary.Add("<IIFPART1>", ", ");
    		dictionary.Add("<IIFPART2>", ", ");
    		dictionary.Add("<IIFCLOSE>", ")");
    		dictionary.Add("<IFOPEN>", "<<b>>Then<<>>");
    		dictionary.Add("<IFCLOSE>", "<<b>>End If<<>>");
    		dictionary.Add("<DOWHILEOPEN>", "<<b>>Do While<<>>");
    		dictionary.Add("<DOWHILECLOSE>", "<<b>>Loop<<>>");
    		dictionary.Add("<LOOPWHILEOPEN>", "<<b>>Do<<>>");
    		dictionary.Add("<LOOPWHILECLOSE>", "<<b>>Loop While<<>>");
    		dictionary.Add("<LOOPBRACKETOPEN>", "");
    		dictionary.Add("<LOOPBRACKETCLOSE>", "");
    		dictionary.Add("<TYPETYPE>", "<<b>>Type<<>>");
    		dictionary.Add("<TYPESBYTE>", "<<b>>SByte<<>>");
    		dictionary.Add("<TYPEBYTE>", "<<b>>Byte<<>>");
    		dictionary.Add("<TYPEDOUBLE>", "<<b>>Double<<>>");
    		dictionary.Add("<TYPESINGLE>", "<<b>>Single<<>>");
    		dictionary.Add("<TYPEUINT64>", "<<b>>ULong<<>>");
    		dictionary.Add("<TYPEUINT32>", "<<b>>UInteger<<>>");
    		dictionary.Add("<TYPEUINT16>", "<<b>>UShort<<>>");
    		dictionary.Add("<TYPEINT64>", "<<b>>Long<<>>");
    		dictionary.Add("<TYPEINT32>", "<<b>>Integer<<>>");
    		dictionary.Add("<TYPEINT16>", "<<b>>Short<<>>");
    		dictionary.Add("<TYPEOBJECT>", "<<b>>Object<<>>");
    		dictionary.Add("<TYPEBOOLEAN>", "<<b>>Boolean<<>>");
    		dictionary.Add("<TYPECHAR>", "<<b>>Char<<>>");
    		dictionary.Add("<TYPESTRING>", "<<b>>String<<>>");
    		dictionary.Add("<TYPEVOID>", "<<b>>Void<<>>");
    		dictionary.Add("<IFANDEXTEND>", "");
    		dictionary.Add("<IFOREXTEND>", "");
    		dictionary.Add("<IFANDEXTENDGROUP>", "");
    		dictionary.Add("<IFOREXTENDGROUP>", "");
    		dictionary.Add("<FOREACHEND>", "<<b>>Next<<>>");
    		dictionary.Add("<FOREACHSTART=", "");
    		dictionary.Add("<DECLARE=", "");
    		dictionary.Add("<CTYPE=", "");
    		dictionary.Add("<DIRECTCAST=", "");
    		dictionary.Add("<CATCHSTART=", "");
    		dictionary.Add("<CONSTRUCTOR=", "");
    		stringBuilder_0.AppendLine(smethod_1(string_, dictionary, methodDefinition_0));
    	}

    	private static string smethod_1(string string_0, Dictionary<string, string> dictionary_0, MethodDefinition methodDefinition_0)
    	{
    		string_0 = string_0.Replace("<LDLOCA>", "");
    		string_0 = string_0.Replace("<EQUALS> <NOTHING>", "<ISEQUALS> <NOTHING>");
    		string_0 = string_0.Replace("<NOTEQUALS> <NOTHING>", "<ISNOTEQUALS> <NOTHING>");
    		StringBuilder stringBuilder = new StringBuilder();
    		bool flag = false;
    		int num = 0;
    		checked
    		{
    			while (num < string_0.Length)
    			{
    				if (Operators.CompareString(Conversions.ToString(string_0[num]), "\"", TextCompare: false) == 0)
    				{
    					flag = !flag;
    				}
    				string text = "";
    				if (!flag && Operators.CompareString(Conversions.ToString(string_0[num]), "<", TextCompare: false) == 0)
    				{
    					int num2 = num;
    					while (num2 < string_0.Length)
    					{
    						num2++;
    						if ((Operators.CompareString(Conversions.ToString(string_0[num2]), ">", TextCompare: false) == 0) | (Operators.CompareString(Conversions.ToString(string_0[num2]), "=", TextCompare: false) == 0))
    						{
    							if (dictionary_0.ContainsKey(string_0.Substring(num, num2 - num + 1)))
    							{
    								text = string_0.Substring(num, num2 - num + 1);
    							}
    							break;
    						}
    					}
    				}
    				if (Operators.CompareString(text, "", TextCompare: false) == 0)
    				{
    					stringBuilder.Append(string_0[num]);
    					num++;
    				}
    				else if (text.EndsWith("="))
    				{
    					string text2 = string_0.Substring(num + text.Length, string_0.Replace(">>", "££").IndexOf("<END>", num) - num - text.Length);
    					num += text.Length + 5 + text2.Length;
    					switch (text)
    					{
    					case "<DECLARE=":
    					{
    						int index = int.Parse(text2);
    						string string_7 = smethod_29(methodDefinition_0.Body.Variables[index].VariableType);
    						string_7 = smethod_1(string_7, dictionary_0, methodDefinition_0);
    						stringBuilder.Append($"<<b>>Dim<<>> {methodDefinition_0.Body.Variables[index].Name} <<b>>As<<>> {string_7}");
    						break;
    					}
    					case "<CTYPE=":
    					{
    						string string_5 = text2.Substring(0, text2.IndexOf("<SEP>"));
    						string string_6 = text2.Substring(text2.IndexOf("<SEP>") + 5);
    						string_5 = smethod_1(string_5, dictionary_0, methodDefinition_0);
    						string_6 = smethod_1(string_6, dictionary_0, methodDefinition_0);
    						switch (string_5)
    						{
    						case "System.String":
    							stringBuilder.Append($"<<b>>CStr<<>>({string_6})");
    							break;
    						default:
    							stringBuilder.Append($"<<b>>CType<<>>({string_6}, {string_5})");
    							break;
    						case "System.Integer":
    						case "System.Int32":
    							stringBuilder.Append($"<<b>>CInt<<>>({string_6})");
    							break;
    						}
    						break;
    					}
    					case "<DIRECTCAST=":
    					{
    						string string_3 = text2.Substring(0, text2.IndexOf("<SEP>"));
    						string string_4 = text2.Substring(text2.IndexOf("<SEP>") + 5);
    						string_3 = smethod_1(string_3, dictionary_0, methodDefinition_0);
    						string_4 = smethod_1(string_4, dictionary_0, methodDefinition_0);
    						stringBuilder.Append($"<<b>>DirectCast<<>>({string_4}, {string_3})");
    						break;
    					}
    					case "<CATCHSTART=":
    					{
    						string string_1 = text2.Substring(0, text2.IndexOf("<SEP>"));
    						string string_2 = text2.Substring(text2.IndexOf("<SEP>") + 5);
    						string_2 = smethod_1(string_2, dictionary_0, methodDefinition_0);
    						string_1 = smethod_1(string_1, dictionary_0, methodDefinition_0);
    						stringBuilder.Append($"<<b>>Catch<<>> {string_1} <<b>>As<<>> {string_2}");
    						break;
    					}
    					case "<CONSTRUCTOR=":
    						stringBuilder.Append(string.Format("{0}New<<>>", text2.Substring(0, text2.Substring(0, text2.Length - 4).LastIndexOf(">>") + 2)));
    						break;
    					case "<FOREACHSTART=":
    					{
    						string text3 = stringBuilder.ToString();
    						int num3 = text3.LastIndexOf(Environment.NewLine);
    						int num4 = text3.LastIndexOf(Environment.NewLine, num3 - 1);
    						text3 = text3.Substring(num4, num3 - num4).Trim();
    						stringBuilder.Remove(num4, num3 - num4);
    						string arg = smethod_1(text2.Substring(0, text2.LastIndexOf(">>") + 2), dictionary_0, methodDefinition_0);
    						stringBuilder.Append(string.Format("<<b>>For Each<<>> {0} <<b>>As<<>> {1} <<b>>In<<>> {2}", text2.Substring(text2.LastIndexOf(">>") + 2), arg, text3));
    						break;
    					}
    					}
    				}
    				else if ((Operators.CompareString(text, "<TRYEND>", TextCompare: false) == 0) | (Operators.CompareString(text, "<CATCHEND>", TextCompare: false) == 0))
    				{
    					string text4 = string_0.Substring(num + text.Length).TrimStart();
    					if (!(text4.StartsWith("<CATCHSTART=") | text4.StartsWith("<FINALLYSTART>")))
    					{
    						stringBuilder.Append(dictionary_0[text]);
    					}
    					num += text.Length;
    				}
    				else
    				{
    					stringBuilder.Append(dictionary_0[text]);
    					num += text.Length;
    				}
    			}
    			return stringBuilder.ToString().TrimEnd();
    		}
    	}

    	public static void WriteCSharpBody(StringBuilder stringBuilder_0, MethodDefinition methodDefinition_0, int int_0)
    	{
    		string string_ = smethod_19(methodDefinition_0, int_0);
    		Dictionary<string, string> dictionary = new Dictionary<string, string>();
    		dictionary.Add("<EXITLOOP>", "<<b>>break<<>>");
    		dictionary.Add("<GOTO>", "<<b>>goto<<>>");
    		dictionary.Add("<SELECTCASESTART>", "<<b>>switch<<>>");
    		dictionary.Add("<SELECTCASEEND>", "<<b>>}<<>>");
    		dictionary.Add("<CASE>", "<<b>>case<<>>");
    		dictionary.Add("<CASEELSE>", "<<b>>default<<>>");
    		dictionary.Add("<CASEEXIT>", "<<b>>break<<>>");
    		dictionary.Add("<SELECTCASESTARTMARKER>", "\\L{");
    		dictionary.Add("<CASEMARKER>", ":");
    		dictionary.Add("<GENERICCLOSE>", ">");
    		dictionary.Add("<GENERICOPEN>", "<");
    		dictionary.Add("<FINALLYSTART>", "<<b>>finally<<>>\\L{");
    		dictionary.Add("<FINALLYEND>", "}");
    		dictionary.Add("<TRYSTART>", "<<b>>try<<>>\\L{");
    		dictionary.Add("<TRYEND>", "}");
    		dictionary.Add("<VALUE>", "value");
    		dictionary.Add("<CATCHEND>", "}");
    		dictionary.Add("<PLAINSTRING>", "@");
    		dictionary.Add("<OBJECTADDRESS>", "&");
    		dictionary.Add("<TRUE>", "<<b>>true<<>>");
    		dictionary.Add("<FALSE>", "<<b>>false<<>>");
    		dictionary.Add("<OBJECTREF>", "*");
    		dictionary.Add("<REF>", "<<b>>ref<<>> ");
    		dictionary.Add("<OUT>", "<<b>>out<<>> ");
    		dictionary.Add("<ADDRESSOF>", "");
    		dictionary.Add("<LINEEND>", ";");
    		dictionary.Add("<MOD>", "%");
    		dictionary.Add("<NOT>", "!");
    		dictionary.Add("<NOTEND>", "");
    		dictionary.Add("<AND>", "&");
    		dictionary.Add("<OR>", "|");
    		dictionary.Add("<ORELSE>", "||");
    		dictionary.Add("<ANDALSO>", "&&");
    		dictionary.Add("<XOR>", "^");
    		dictionary.Add("<EQUALS>", "==");
    		dictionary.Add("<NOTEQUALS>", "!=");
    		dictionary.Add("<GREATER>", ">");
    		dictionary.Add("<GREATEROREQUAL>", ">=");
    		dictionary.Add("<LESS>", "<");
    		dictionary.Add("<LESSOREQUAL>", "<=");
    		dictionary.Add("<YES>", "");
    		dictionary.Add("<YESEND>", "");
    		dictionary.Add("<IS>", "is");
    		dictionary.Add("<ISEQUALS>", "==");
    		dictionary.Add("<ISNOTEQUALS>", "!=");
    		dictionary.Add("<TYPEOF>", "");
    		dictionary.Add("<ME>", "<<b>>this<<>>");
    		dictionary.Add("<MYBASE>", "<<b>>base<<>>");
    		dictionary.Add("<NOTHING>", "<<b>>null<<>>");
    		dictionary.Add("<STRINGCONCAT>", "+");
    		dictionary.Add("<NEW>", "<<b>>new<<>>");
    		dictionary.Add("<ARRAYBRACKETOPEN>", "[");
    		dictionary.Add("<ARRAYBRACKETCLOSE>", "]");
    		dictionary.Add("<GETTYPE>", "<<b>>typeof<<>>");
    		dictionary.Add("<RETURN>", "<<b>>return<<>>");
    		dictionary.Add("<THROW>", "<<b>>throw<<>>");
    		dictionary.Add("<RETHROW>", "<<b>>throw<<>>");
    		dictionary.Add("<COMMENT>", "//");
    		dictionary.Add("<ADDHANDLER1>", "");
    		dictionary.Add("<ADDHANDLER2>", " += ");
    		dictionary.Add("<REMOVEHANDLER1>", "");
    		dictionary.Add("<REMOVEHANDLER2>", " -= ");
    		dictionary.Add("<IF>", "<<b>>if<<>>");
    		dictionary.Add("<ELSE>", "}\\L<<b>>else<<>>\\L{");
    		dictionary.Add("<ELSEIF>", "}\\L<<b>>else if<<>>");
    		dictionary.Add("<IIFOPEN>", "");
    		dictionary.Add("<IIFPART1>", " ? ");
    		dictionary.Add("<IIFPART2>", " : ");
    		dictionary.Add("<IIFCLOSE>", "");
    		dictionary.Add("<IFOPEN>", "\\L{");
    		dictionary.Add("<IFCLOSE>", "}");
    		dictionary.Add("<DOWHILEOPEN>", "<<b>>while<<>>");
    		dictionary.Add("<DOWHILECLOSE>", "");
    		dictionary.Add("<LOOPWHILEOPEN>", "<<b>>do<<>>");
    		dictionary.Add("<LOOPWHILECLOSE>", "<<b>>while<<>>");
    		dictionary.Add("<LOOPBRACKETOPEN>", "\\L{");
    		dictionary.Add("<LOOPBRACKETCLOSE>", "}");
    		dictionary.Add("<TYPETYPE>", "<<b>>type<<>>");
    		dictionary.Add("<TYPESBYTE>", "<<b>>sbyte<<>>");
    		dictionary.Add("<TYPEBYTE>", "<<b>>byte<<>>");
    		dictionary.Add("<TYPEDOUBLE>", "<<b>>double<<>>");
    		dictionary.Add("<TYPESINGLE>", "<<b>>float<<>>");
    		dictionary.Add("<TYPEUINT64>", "<<b>>ulong<<>>");
    		dictionary.Add("<TYPEUINT32>", "<<b>>uint<<>>");
    		dictionary.Add("<TYPEUINT16>", "<<b>>ushort<<>>");
    		dictionary.Add("<TYPEINT64>", "<<b>>long<<>>");
    		dictionary.Add("<TYPEINT32>", "<<b>>int<<>>");
    		dictionary.Add("<TYPEINT16>", "<<b>>short<<>>");
    		dictionary.Add("<TYPEOBJECT>", "<<b>>object<<>>");
    		dictionary.Add("<TYPEBOOLEAN>", "<<b>>bool<<>>");
    		dictionary.Add("<TYPECHAR>", "<<b>>char<<>>");
    		dictionary.Add("<TYPESTRING>", "<<b>>string<<>>");
    		dictionary.Add("<TYPEVOID>", "<<b>>void<<>>");
    		dictionary.Add("<IFANDEXTEND>", "");
    		dictionary.Add("<IFOREXTEND>", "");
    		dictionary.Add("<IFANDEXTENDGROUP>", "");
    		dictionary.Add("<IFOREXTENDGROUP>", "");
    		dictionary.Add("<FOREACHEND>", "}");
    		dictionary.Add("<FOREACHSTART=", "");
    		dictionary.Add("<DECLARE=", "");
    		dictionary.Add("<CTYPE=", "");
    		dictionary.Add("<DIRECTCAST=", "");
    		dictionary.Add("<CATCHSTART=", "");
    		dictionary.Add("<CONSTRUCTOR=", "");
    		stringBuilder_0.AppendLine(smethod_3(string_, dictionary, methodDefinition_0));
    	}

    	private static string smethod_3(string string_0, Dictionary<string, string> dictionary_0, MethodDefinition methodDefinition_0)
    	{
    		string_0 = string_0.Replace("<LDLOCA>", "");
    		bool flag = false;
    		StringBuilder stringBuilder = new StringBuilder(string_0.Length);
    		int num = 0;
    		checked
    		{
    			while (num < string_0.Length)
    			{
    				if (Operators.CompareString(Conversions.ToString(string_0[num]), "\"", TextCompare: false) == 0)
    				{
    					flag = !flag;
    				}
    				string text = "";
    				if (!flag && Operators.CompareString(Conversions.ToString(string_0[num]), "<", TextCompare: false) == 0)
    				{
    					int num2 = num;
    					while (num2 < string_0.Length)
    					{
    						num2++;
    						if ((Operators.CompareString(Conversions.ToString(string_0[num2]), ">", TextCompare: false) == 0) | (Operators.CompareString(Conversions.ToString(string_0[num2]), "=", TextCompare: false) == 0))
    						{
    							if (dictionary_0.ContainsKey(string_0.Substring(num, num2 - num + 1)))
    							{
    								text = string_0.Substring(num, num2 - num + 1);
    							}
    							break;
    						}
    					}
    				}
    				if (Operators.CompareString(text, "", TextCompare: false) == 0)
    				{
    					stringBuilder.Append(string_0[num]);
    					num++;
    					continue;
    				}
    				if (text.EndsWith("="))
    				{
    					string text2 = string_0.Substring(num + text.Length, string_0.Replace(">>", "££").IndexOf("<END>", num) - num - text.Length);
    					num += text.Length + 5 + text2.Length;
    					switch (text)
    					{
    					case "<DECLARE=":
    					{
    						int index = int.Parse(text2);
    						string string_7 = smethod_29(methodDefinition_0.Body.Variables[index].VariableType);
    						string_7 = smethod_1(string_7, dictionary_0, methodDefinition_0);
    						stringBuilder.Append(string.Format("{1} {0}", methodDefinition_0.Body.Variables[index].Name, string_7));
    						break;
    					}
    					case "<CTYPE=":
    					{
    						string string_3 = text2.Substring(0, text2.IndexOf("<SEP>"));
    						string string_4 = text2.Substring(text2.IndexOf("<SEP>") + 5);
    						string_3 = smethod_1(string_3, dictionary_0, methodDefinition_0);
    						string_4 = smethod_1(string_4, dictionary_0, methodDefinition_0);
    						switch (string_3)
    						{
    						case "System.String":
    							stringBuilder.Append($"(string)({string_4})");
    							break;
    						default:
    							stringBuilder.Append(string.Format("({1})({0})", string_4, string_3));
    							break;
    						case "System.Integer":
    						case "System.Int32":
    							stringBuilder.Append($"(int)({string_4})");
    							break;
    						}
    						break;
    					}
    					case "<DIRECTCAST=":
    					{
    						string string_1 = text2.Substring(0, text2.IndexOf("<SEP>"));
    						string string_2 = text2.Substring(text2.IndexOf("<SEP>") + 5);
    						string_1 = smethod_1(string_1, dictionary_0, methodDefinition_0);
    						string_2 = smethod_1(string_2, dictionary_0, methodDefinition_0);
    						stringBuilder.Append(string.Format("({1})({0})", string_2, string_1));
    						break;
    					}
    					case "<CATCHSTART=":
    					{
    						string text5 = stringBuilder.ToString();
    						int num6 = text5.LastIndexOf(Environment.NewLine);
    						if (num6 < 0)
    						{
    							num6 = -2;
    						}
    						text5 = text5.Substring(num6 + 2);
    						text5 = Environment.NewLine + text5.Substring(0, text5.Length - text5.TrimStart().Length);
    						string string_5 = text2.Substring(0, text2.IndexOf("<SEP>"));
    						string string_6 = text2.Substring(text2.IndexOf("<SEP>") + 5);
    						string_6 = smethod_3(string_6, dictionary_0, methodDefinition_0);
    						string_5 = smethod_3(string_5, dictionary_0, methodDefinition_0);
    						stringBuilder.Append(string.Format("<<b>>catch<<>> ({1} {0}) ", string_5, string_6) + text5 + "{");
    						break;
    					}
    					case "<CONSTRUCTOR=":
    						stringBuilder.Append(text2);
    						break;
    					case "<FOREACHSTART=":
    					{
    						string text3 = stringBuilder.ToString();
    						int num3 = text3.LastIndexOf(Environment.NewLine);
    						if (num3 < 0)
    						{
    							num3 = -2;
    						}
    						string text4 = text3.Substring(num3 + 2);
    						text4 = Environment.NewLine + text4.Substring(0, text4.Length - text4.TrimStart().Length);
    						int num4 = text3.LastIndexOf(Environment.NewLine);
    						int num5 = text3.LastIndexOf(Environment.NewLine, num4 - 1);
    						text3 = text3.Substring(num5, num4 - num5).Trim();
    						stringBuilder.Remove(num5, num4 - num5);
    						string arg = smethod_3(text2.Substring(0, text2.LastIndexOf(">>") + 2), dictionary_0, methodDefinition_0);
    						stringBuilder.Append(string.Format("<<b>>foreach<<>> ({1} {0} <<b>>in<<>> {2}) ", text2.Substring(text2.LastIndexOf(">>") + 2), arg, text3) + text4 + "{");
    						break;
    					}
    					}
    					continue;
    				}
    				string text6 = dictionary_0[text];
    				if (text6.Contains("\\L"))
    				{
    					string text7 = stringBuilder.ToString();
    					int num7 = text7.LastIndexOf(Environment.NewLine);
    					if (num7 < 0)
    					{
    						num7 = -2;
    					}
    					text7 = text7.Substring(num7 + 2);
    					text7 = Environment.NewLine + text7.Substring(0, text7.Length - text7.TrimStart().Length);
    					text6 = text6.Replace("\\L", text7);
    				}
    				stringBuilder.Append(text6);
    				num += text.Length;
    			}
    			string_0 = Regex.Replace(stringBuilder.ToString(), "^[\\ ]*[\\;]", "", RegexOptions.Multiline);
    			string_0 = string_0.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
    			return string_0.TrimEnd();
    		}
    	}

    	private static void smethod_4(MethodDefinition methodDefinition_0, ArrayList arrayList_0)
    	{
    		if (arrayList_0.Count <= 2)
    		{
    			return;
    		}
    		object obj = null;
    		checked
    		{
    			try
    			{
    				int num = -1;
    				while (num < arrayList_0.Count - 1)
    				{
    					num++;
    					if (!(arrayList_0[num] is Instruction))
    					{
    						continue;
    					}
    					Instruction instruction = (Instruction)arrayList_0[num];
    					if (instruction.Operand != arrayList_0[arrayList_0.Count - 2])
    					{
    						continue;
    					}
    					VariableDefinition variableDefinition = null;
    					switch (((Instruction)instruction.Operand).OpCode.Code)
    					{
    					case Code.Ldloc_0:
    						variableDefinition = methodDefinition_0.Body.Variables[0];
    						break;
    					case Code.Ldloc_1:
    						variableDefinition = methodDefinition_0.Body.Variables[1];
    						break;
    					case Code.Ldloc_2:
    						variableDefinition = methodDefinition_0.Body.Variables[2];
    						break;
    					case Code.Ldloc_3:
    						variableDefinition = methodDefinition_0.Body.Variables[3];
    						break;
    					case Code.Ldloc_S:
    					case Code.Ldloc:
    						variableDefinition = (VariableDefinition)((Instruction)instruction.Operand).Operand;
    						break;
    					}
    					if (variableDefinition != null)
    					{
    						VariableDefinition variableDefinition2 = null;
    						switch (((Instruction)arrayList_0[num - 1]).OpCode.Code)
    						{
    						case Code.Stloc_0:
    							variableDefinition2 = methodDefinition_0.Body.Variables[0];
    							break;
    						case Code.Stloc_1:
    							variableDefinition2 = methodDefinition_0.Body.Variables[1];
    							break;
    						case Code.Stloc_2:
    							variableDefinition2 = methodDefinition_0.Body.Variables[2];
    							break;
    						case Code.Stloc_3:
    							variableDefinition2 = methodDefinition_0.Body.Variables[3];
    							break;
    						case Code.Ldloc_S:
    						case Code.Stloc:
    							variableDefinition2 = (VariableDefinition)((Instruction)arrayList_0[num - 1]).Operand;
    							break;
    						}
    						if (variableDefinition == variableDefinition2)
    						{
    							arrayList_0.RemoveAt(num);
    							arrayList_0[num - 1] = methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Ret);
    							num--;
    						}
    					}
    				}
    			}
    			finally
    			{
    				if (obj != null)
    				{
    					((IDisposable)obj).Dispose();
    				}
    			}
    		}
    	}

    	private static void smethod_5(ArrayList arrayList_0, MethodDefinition methodDefinition_0)
    	{
    		List<Instruction> list = new List<Instruction>();
    		List<Instruction> list2 = new List<Instruction>();
    		object obj = null;
    		try
    		{
    			foreach (Instruction item in arrayList_0)
    			{
    				if (item.Operand is Instruction)
    				{
    					list.Add(item);
    				}
    				if (item.Operand is Instruction[])
    				{
    					list2.Add(item);
    				}
    			}
    		}
    		finally
    		{
    			if (obj != null)
    			{
    				((IDisposable)obj).Dispose();
    			}
    		}
    		object obj2 = null;
    		checked
    		{
    			try
    			{
    				int num = 0;
    				while (num < arrayList_0.Count - 2)
    				{
    					Instruction instruction2 = (Instruction)arrayList_0[num];
    					if (instruction2.OpCode.Code == Code.Call && Operators.CompareString(((MethodReference)instruction2.Operand).FullName, "System.Void Microsoft.VisualBasic.CompilerServices.ProjectData::ClearProjectError()", TextCompare: false) == 0)
    					{
    						arrayList_0.RemoveAt(num);
    					}
    					else
    					{
    						num++;
    					}
    				}
    			}
    			finally
    			{
    				if (obj2 != null)
    				{
    					((IDisposable)obj2).Dispose();
    				}
    			}
    			object obj3 = null;
    			try
    			{
    				for (int i = 0; i < arrayList_0.Count - 2; i++)
    				{
    					Instruction instruction3 = (Instruction)arrayList_0[i];
    					if (((instruction3.OpCode.Code == Code.Br) | (instruction3.OpCode.Code == Code.Br_S)) && instruction3.Operand == arrayList_0[i + 1])
    					{
    						instruction3.Operand = instruction3;
    					}
    				}
    			}
    			finally
    			{
    				if (obj3 != null)
    				{
    					((IDisposable)obj3).Dispose();
    				}
    			}
    			object obj4 = null;
    			try
    			{
    				int num2 = default;
    				while (num2 < arrayList_0.Count)
    				{
    					Instruction instruction4 = (Instruction)arrayList_0[num2];
    					if (instruction4.OpCode.Code == Code.Nop)
    					{
    						foreach (Instruction item2 in list)
    						{
    							if (item2.Operand == instruction4)
    							{
    								item2.Operand = RuntimeHelpers.GetObjectValue(arrayList_0[num2 + 1]);
    							}
    						}
    						foreach (Instruction item3 in list2)
    						{
    							Instruction[] array = (Instruction[])item3.Operand;
    							int num3 = array.Length - 1;
    							for (int j = 0; j <= num3; j++)
    							{
    								if (array[j] == instruction4)
    								{
    									array[j] = (Instruction)arrayList_0[num2 + 1];
    								}
    							}
    						}
    						arrayList_0.RemoveAt(num2);
    					}
    					else
    					{
    						num2++;
    					}
    				}
    			}
    			finally
    			{
    				if (obj4 != null)
    				{
    					((IDisposable)obj4).Dispose();
    				}
    			}
    			object obj5 = null;
    			try
    			{
    				int k = default;
    				for (; k < arrayList_0.Count - 2; k++)
    				{
    					Instruction instruction5 = (Instruction)arrayList_0[k];
    					if (instruction5.Operand == arrayList_0[k + 2] && ((Instruction)arrayList_0[k + 1]).Operand is Instruction && ((((Instruction)arrayList_0[k + 1]).OpCode.Code == Code.Br) | (((Instruction)arrayList_0[k + 1]).OpCode.Code == Code.Br_S)))
    					{
    						instruction5.Operand = RuntimeHelpers.GetObjectValue(((Instruction)arrayList_0[k + 1]).Operand);
    						arrayList_0.RemoveAt(k + 1);
    						switch (instruction5.OpCode.Code)
    						{
    						case Code.Brfalse_S:
    						case Code.Brfalse:
    							instruction5.OpCode = OpCodes.Brtrue;
    							break;
    						case Code.Brtrue_S:
    						case Code.Brtrue:
    							instruction5.OpCode = OpCodes.Brfalse;
    							break;
    						case Code.Beq_S:
    						case Code.Beq:
    							instruction5.OpCode = OpCodes.Bne_Un;
    							break;
    						case Code.Bge_S:
    						case Code.Bge:
    							instruction5.OpCode = OpCodes.Blt;
    							break;
    						case Code.Bgt_S:
    						case Code.Bgt:
    							instruction5.OpCode = OpCodes.Ble;
    							break;
    						case Code.Ble_S:
    						case Code.Ble:
    							instruction5.OpCode = OpCodes.Bgt;
    							break;
    						case Code.Blt_S:
    						case Code.Blt:
    							instruction5.OpCode = OpCodes.Bge;
    							break;
    						case Code.Bne_Un_S:
    						case Code.Bne_Un:
    							instruction5.OpCode = OpCodes.Beq;
    							break;
    						case Code.Bge_Un_S:
    						case Code.Bge_Un:
    							instruction5.OpCode = OpCodes.Blt_Un;
    							break;
    						case Code.Bgt_Un_S:
    						case Code.Bgt_Un:
    							instruction5.OpCode = OpCodes.Ble;
    							break;
    						case Code.Ble_Un_S:
    						case Code.Ble_Un:
    							instruction5.OpCode = OpCodes.Bgt_Un;
    							break;
    						case Code.Blt_Un_S:
    						case Code.Blt_Un:
    							instruction5.OpCode = OpCodes.Bge_Un;
    							break;
    						}
    					}
    				}
    			}
    			finally
    			{
    				if (obj5 != null)
    				{
    					((IDisposable)obj5).Dispose();
    				}
    			}
    			object obj6 = null;
    			try
    			{
    				int l = default;
    				bool flag = default;
    				for (; l < arrayList_0.Count; l++)
    				{
    					if (((Instruction)arrayList_0[l]).OpCode.Code != Code.Ldc_I4_0)
    					{
    						continue;
    					}
    					int num4 = 0;
    					int m = l + 1;
    					Instruction instruction6 = (Instruction)arrayList_0[l];
    					Instruction instruction7 = null;
    					Instruction instruction8 = null;
    					Instruction instruction9 = null;
    					for (; m < arrayList_0.Count; m++)
    					{
    						Instruction instruction10 = (Instruction)arrayList_0[m];
    						switch (instruction10.OpCode.Code)
    						{
    						case Code.Ldc_I4_1:
    							if (num4 == 1)
    							{
    								num4++;
    								instruction7 = instruction10;
    								continue;
    							}
    							break;
    						case Code.Br_S:
    						case Code.Br:
    							if (instruction10.Operand == arrayList_0[m + 1])
    							{
    								continue;
    							}
    							if (num4 == 0)
    							{
    								num4++;
    								continue;
    							}
    							break;
    						case Code.Ldloc_0:
    						case Code.Ldloc_1:
    						case Code.Ldloc_2:
    						case Code.Ldloc_3:
    						case Code.Stloc_0:
    						case Code.Stloc_1:
    						case Code.Stloc_2:
    						case Code.Stloc_3:
    						case Code.Ldloc_S:
    						case Code.Stloc_S:
    						case Code.Ldloc:
    						case Code.Stloc:
    							continue;
    						case Code.Ret:
    							if (num4 == 2)
    							{
    								num4++;
    								flag = true;
    							}
    							break;
    						case Code.Brfalse_S:
    						case Code.Brfalse:
    							if (num4 == 2)
    							{
    								num4++;
    								instruction9 = (Instruction)arrayList_0[m + 1];
    								instruction8 = (Instruction)instruction10.Operand;
    							}
    							break;
    						}
    						break;
    					}
    					if (num4 != 3)
    					{
    						continue;
    					}
    					int num5 = m;
    					int num6 = l;
    					for (int n = num5; n >= num6; n += -1)
    					{
    						arrayList_0.RemoveAt(n);
    					}
    					if (flag)
    					{
    						arrayList_0.Insert(l, methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Ldc_I4_0));
    						arrayList_0.Insert(l + 1, methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Ret));
    						arrayList_0.Insert(l + 2, methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Ldc_I4_0));
    						arrayList_0.Insert(l + 3, methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Ret));
    						instruction8 = (Instruction)arrayList_0[l];
    						instruction9 = (Instruction)arrayList_0[l + 2];
    					}
    					foreach (Instruction item4 in list)
    					{
    						_ = item4;
    						foreach (Instruction item5 in list)
    						{
    							if (item5.Operand == instruction6)
    							{
    								item5.Operand = instruction8;
    							}
    							if (item5.Operand != instruction7)
    							{
    								continue;
    							}
    							item5.Operand = instruction9;
    							int num7 = arrayList_0.IndexOf(item5);
    							if (arrayList_0[num7 + 1] == instruction9)
    							{
    								item5.Operand = instruction6;
    								switch (item5.OpCode.Code)
    								{
    								case Code.Brfalse_S:
    								case Code.Brfalse:
    									item5.OpCode = OpCodes.Brtrue;
    									break;
    								case Code.Brtrue_S:
    								case Code.Brtrue:
    									item5.OpCode = OpCodes.Brfalse;
    									break;
    								case Code.Beq_S:
    								case Code.Beq:
    									item5.OpCode = OpCodes.Bne_Un;
    									break;
    								case Code.Bge_S:
    								case Code.Bge:
    									item5.OpCode = OpCodes.Blt;
    									break;
    								case Code.Bgt_S:
    								case Code.Bgt:
    									item5.OpCode = OpCodes.Ble;
    									break;
    								case Code.Ble_S:
    								case Code.Ble:
    									item5.OpCode = OpCodes.Bgt;
    									break;
    								case Code.Blt_S:
    								case Code.Blt:
    									item5.OpCode = OpCodes.Bge;
    									break;
    								case Code.Bne_Un_S:
    								case Code.Bne_Un:
    									item5.OpCode = OpCodes.Beq;
    									break;
    								case Code.Bge_Un_S:
    								case Code.Bge_Un:
    									item5.OpCode = OpCodes.Blt_Un;
    									break;
    								case Code.Bgt_Un_S:
    								case Code.Bgt_Un:
    									item5.OpCode = OpCodes.Ble;
    									break;
    								case Code.Ble_Un_S:
    								case Code.Ble_Un:
    									item5.OpCode = OpCodes.Bgt_Un;
    									break;
    								case Code.Blt_Un_S:
    								case Code.Blt_Un:
    									item5.OpCode = OpCodes.Bge_Un;
    									break;
    								}
    							}
    						}
    						foreach (Instruction item6 in list2)
    						{
    							Instruction[] array2 = (Instruction[])item6.Operand;
    							int num8 = array2.Length - 1;
    							for (int num9 = 0; num9 <= num8; num9++)
    							{
    								if (array2[num9] == instruction6)
    								{
    									array2[num9] = instruction8;
    								}
    								if (array2[num9] == instruction7)
    								{
    									array2[num9] = instruction9;
    								}
    							}
    						}
    					}
    				}
    			}
    			finally
    			{
    				if (obj6 != null)
    				{
    					((IDisposable)obj6).Dispose();
    				}
    			}
    		}
    	}

    	private static void smethod_6(MethodDefinition methodDefinition_0, ArrayList arrayList_0, List<ExceptionHandler> list_1)
    	{
    		checked
    		{
    			foreach (ExceptionHandler exceptionHandler in methodDefinition_0.Body.ExceptionHandlers)
    			{
    				int num = smethod_8(arrayList_0, exceptionHandler.HandlerEnd);
    				int num2 = num;
    				int num3 = 1;
    				do
    				{
    					if (!(arrayList_0[num2 - num3] is string))
    					{
    						Instruction instruction = (Instruction)arrayList_0[num2 - num3];
    						if (instruction.OpCode.Code == Code.Call && Operators.CompareString(((MethodReference)instruction.Operand).Name, "MoveNext", TextCompare: false) == 0)
    						{
    							num2 = num - num3 - 1;
    						}
    					}
    					num3++;
    				}
    				while (num3 <= 10);
    				if (num2 == num)
    				{
    					continue;
    				}
    				Instruction instruction2 = (Instruction)arrayList_0[num2];
    				if (!((instruction2.OpCode.Code == Code.Ldloca) | (instruction2.OpCode.Code == Code.Ldloca_S)) || !((VariableDefinition)instruction2.Operand).VariableType.FullName.Contains("Enumerator"))
    				{
    					continue;
    				}
    				int num4 = smethod_8(arrayList_0, exceptionHandler.TryStart);
    				int num5 = num4;
    				while (num5 < num2)
    				{
    					num5++;
    					Instruction instruction3 = (Instruction)arrayList_0[num5];
    					if (instruction3.OpCode.Code == Code.Callvirt && Operators.CompareString(((MethodReference)instruction3.Operand).Name, "GetEnumerator", TextCompare: false) == 0 && (num5 + 3 >= num2 || NewLateBinding.LateGet(arrayList_0[num5 + 3], null, "Operand", new object[0], null, null, null) != instruction2))
    					{
    						break;
    					}
    				}
    				if (num5 == num2)
    				{
    					continue;
    				}
    				ILProcessor iLProcessor = methodDefinition_0.Body.GetILProcessor();
    				Instruction instruction4 = iLProcessor.Create(OpCodes.Br, instruction2);
    				instruction4.Offset = instruction2.Offset;
    				foreach (object item in arrayList_0)
    				{
    					object objectValue = RuntimeHelpers.GetObjectValue(item);
    					if (objectValue is Instruction)
    					{
    						Instruction instruction5 = (Instruction)objectValue;
    						if (instruction5.Operand == instruction2)
    						{
    							instruction5.Operand = instruction4;
    						}
    					}
    				}
    				instruction4.Operand = instruction4;
    				int num6 = num2;
    				int num7 = num - 1;
    				for (int i = num6; i <= num7; i++)
    				{
    					arrayList_0.RemoveAt(num2);
    				}
    				arrayList_0.Insert(num2, instruction4);
    				arrayList_0.Insert(num2, "<FOREACHEND>");
    				VariableDefinition variableDefinition = null;
    				switch (((Instruction)arrayList_0[num5 + 6]).OpCode.Code)
    				{
    				case Code.Stloc_0:
    					variableDefinition = methodDefinition_0.Body.Variables[0];
    					break;
    				case Code.Stloc_1:
    					variableDefinition = methodDefinition_0.Body.Variables[1];
    					break;
    				case Code.Stloc_2:
    					variableDefinition = methodDefinition_0.Body.Variables[2];
    					break;
    				case Code.Stloc_3:
    					variableDefinition = methodDefinition_0.Body.Variables[3];
    					break;
    				case Code.Stloc_S:
    				case Code.Stloc:
    					variableDefinition = (VariableDefinition)((Instruction)arrayList_0[num5 + 6]).Operand;
    					break;
    				}
    				int num8 = 1;
    				do
    				{
    					arrayList_0.RemoveAt(num5);
    					num8++;
    				}
    				while (num8 <= 7);
    				if (variableDefinition.Name.StartsWith("#"))
    				{
    					variableDefinition.Name = variableDefinition.Name.Substring(1);
    				}
    				arrayList_0.Insert(num5, $"<FOREACHSTART={smethod_29(variableDefinition.VariableType)}{variableDefinition.Name}<END>");
    				list_1.Add(exceptionHandler);
    			}
    		}
    	}

    	private static void smethod_7(MethodDefinition methodDefinition_0, ArrayList arrayList_0, List<ExceptionHandler> list_1)
    	{
    		int num = 1;
    		StringCollection stringCollection = new StringCollection();
    		checked
    		{
    			foreach (ExceptionHandler exceptionHandler in methodDefinition_0.Body.ExceptionHandlers)
    			{
    				if (list_1.Contains(exceptionHandler))
    				{
    					continue;
    				}
    				switch (exceptionHandler.HandlerType)
    				{
    				case ExceptionHandlerType.Catch:
    				{
    					if (!stringCollection.Contains(string.Format("{0}-{1}", exceptionHandler.TryStart.Offset, exceptionHandler.TryEnd.Offset)) && !stringCollection.Contains(string.Format("{0}-{1}", exceptionHandler.TryStart.Offset, exceptionHandler.HandlerEnd.Offset)))
    					{
    						arrayList_0.Insert(smethod_8(arrayList_0, exceptionHandler.TryStart), new string(Conversions.ToCharArrayRankOne("<TRYSTART>")));
    						arrayList_0.Insert(smethod_8(arrayList_0, exceptionHandler.TryEnd), "<TRYEND>");
    						stringCollection.Add(string.Format("{0}-{1}", exceptionHandler.TryStart.Offset, exceptionHandler.TryEnd.Offset));
    						stringCollection.Add(string.Format("{0}-{1}", exceptionHandler.TryStart.Offset, exceptionHandler.HandlerEnd.Offset));
    					}
    					int num2 = smethod_8(arrayList_0, exceptionHandler.HandlerStart);
    					string text = "";
    					bool flag = true;
    					if (!(arrayList_0[num2] is string) && ((Instruction)arrayList_0[num2]).OpCode.Code == Code.Dup && !(arrayList_0[num2 + 1] is string) && ((Instruction)arrayList_0[num2 + 1]).OpCode.Code == Code.Call && Operators.CompareString(((MethodReference)((Instruction)arrayList_0[num2 + 1]).Operand).Name, "SetProjectError", TextCompare: false) == 0)
    					{
    						VariableDefinition variableDefinition = null;
    						switch (((Instruction)arrayList_0[num2 + 2]).OpCode.Code)
    						{
    						case Code.Stloc_0:
    							variableDefinition = methodDefinition_0.Body.Variables[0];
    							break;
    						case Code.Stloc_1:
    							variableDefinition = methodDefinition_0.Body.Variables[1];
    							break;
    						case Code.Stloc_2:
    							variableDefinition = methodDefinition_0.Body.Variables[2];
    							break;
    						case Code.Stloc_3:
    							variableDefinition = methodDefinition_0.Body.Variables[3];
    							break;
    						case Code.Stloc_S:
    						case Code.Stloc:
    							variableDefinition = (VariableDefinition)NewLateBinding.LateGet(arrayList_0[num2 + 2], null, "Operand", new object[0], null, null, null);
    							break;
    						}
    						if (variableDefinition != null)
    						{
    							if (variableDefinition.Name.StartsWith("#"))
    							{
    								variableDefinition.Name = variableDefinition.Name.Substring(1);
    							}
    							text = variableDefinition.Name;
    							arrayList_0.RemoveAt(num2);
    							arrayList_0.RemoveAt(num2);
    							arrayList_0.RemoveAt(num2);
    							flag = false;
    						}
    					}
    					if (string.IsNullOrEmpty(text))
    					{
    						text = "exception";
    						text += Conversions.ToString(num);
    						num++;
    					}
    					arrayList_0.Insert(num2, "<CATCHSTART=" + text + "<SEP>" + smethod_29(exceptionHandler.CatchType) + "<END>");
    					int num3 = smethod_8(arrayList_0, exceptionHandler.HandlerEnd);
    					if (flag && num2 + 1 < num3)
    					{
    						arrayList_0.Insert(num2 + 1, text);
    						num3++;
    					}
    					arrayList_0.Insert(num3, "<CATCHEND>");
    					break;
    				}
    				case ExceptionHandlerType.Finally:
    					if (!stringCollection.Contains($"{exceptionHandler.TryStart.Offset}-{exceptionHandler.TryEnd.Offset}") && !stringCollection.Contains($"{exceptionHandler.TryStart.Offset}-{exceptionHandler.HandlerEnd.Offset}"))
    					{
    						arrayList_0.Insert(smethod_8(arrayList_0, exceptionHandler.TryStart), "<TRYSTART>");
    						arrayList_0.Insert(smethod_8(arrayList_0, exceptionHandler.TryEnd), "<TRYEND>");
    						stringCollection.Add($"{exceptionHandler.TryStart.Offset}-{exceptionHandler.TryEnd.Offset}");
    						stringCollection.Add($"{exceptionHandler.TryStart.Offset}-{exceptionHandler.HandlerEnd.Offset}");
    					}
    					arrayList_0.Insert(smethod_8(arrayList_0, exceptionHandler.HandlerStart), "<FINALLYSTART>");
    					arrayList_0.Insert(smethod_8(arrayList_0, exceptionHandler.HandlerEnd), "<FINALLYEND>");
    					break;
    				}
    			}
    		}
    	}

    	private static int smethod_8(ArrayList arrayList_0, Instruction instruction_0)
    	{
    		int result = 0;
    		checked
    		{
    			int num = arrayList_0.Count - 1;
    			int num2 = 0;
    			while (true)
    			{
    				if (num2 <= num)
    				{
    					if (arrayList_0[num2] is Instruction)
    					{
    						if (((Instruction)arrayList_0[num2]).Offset == instruction_0.Offset)
    						{
    							return num2;
    						}
    						if (((Instruction)arrayList_0[num2]).Offset > instruction_0.Offset)
    						{
    							break;
    						}
    						result = num2;
    					}
    					num2++;
    					continue;
    				}
    				return -1;
    			}
    			return result;
    		}
    	}

    	private static void smethod_9(MethodDefinition methodDefinition_0, ArrayList arrayList_0)
    	{
    		object obj = null;
    		checked
    		{
    			try
    			{
    				for (int i = 0; i < arrayList_0.Count; i++)
    				{
    					if (!(arrayList_0[i] is Instruction))
    					{
    						continue;
    					}
    					Instruction instruction = (Instruction)arrayList_0[i];
    					bool flag = false;
    					bool flag2 = false;
    					Code code = instruction.OpCode.Code;
    					if (code == Code.Brfalse || code == Code.Brfalse_S || code == Code.Brtrue || code == Code.Brtrue_S || code == Code.Not)
    					{
    						for (int j = i - 1; j >= 0; j += -1)
    						{
    							if (!(arrayList_0[j] is Instruction) || !smethod_26((Instruction)arrayList_0[j]))
    							{
    								continue;
    							}
    							Instruction instruction2 = (Instruction)arrayList_0[j];
    							if (instruction2.Operand is MethodReference)
    							{
    								if (((MethodReference)instruction2.Operand).ReturnType.IsValueType)
    								{
    									flag2 = Operators.CompareString(((MethodReference)instruction2.Operand).ReturnType.Name, "Boolean", TextCompare: false) != 0;
    								}
    								else
    								{
    									flag = true;
    								}
    								break;
    							}
    							if (instruction2.Operand is FieldReference)
    							{
    								if (((FieldReference)instruction2.Operand).FieldType.IsValueType)
    								{
    									flag2 = Operators.CompareString(((FieldReference)instruction2.Operand).FieldType.Name, "Boolean", TextCompare: false) != 0;
    								}
    								else
    								{
    									flag = true;
    								}
    								break;
    							}
    							switch (instruction2.OpCode.Code)
    							{
    							case Code.Ldarg_0:
    								if (methodDefinition_0.HasThis)
    								{
    									flag = true;
    									break;
    								}
    								flag = !methodDefinition_0.Parameters[0].ParameterType.IsValueType;
    								flag2 = ((methodDefinition_0.Parameters[0].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[0].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							case Code.Ldarg_1:
    								if (methodDefinition_0.HasThis)
    								{
    									flag = !methodDefinition_0.Parameters[0].ParameterType.IsValueType;
    									flag2 = ((methodDefinition_0.Parameters[0].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[0].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								}
    								else
    								{
    									flag = !methodDefinition_0.Parameters[1].ParameterType.IsValueType;
    									flag2 = ((methodDefinition_0.Parameters[1].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[1].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								}
    								break;
    							case Code.Ldarg_2:
    								if (methodDefinition_0.HasThis)
    								{
    									flag = !methodDefinition_0.Parameters[1].ParameterType.IsValueType;
    									flag2 = ((methodDefinition_0.Parameters[1].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[1].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								}
    								else
    								{
    									flag = !methodDefinition_0.Parameters[2].ParameterType.IsValueType;
    									flag2 = ((methodDefinition_0.Parameters[2].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[2].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								}
    								break;
    							case Code.Ldarg_3:
    								if (methodDefinition_0.HasThis)
    								{
    									flag = !methodDefinition_0.Parameters[2].ParameterType.IsValueType;
    									flag2 = ((methodDefinition_0.Parameters[2].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[2].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								}
    								else
    								{
    									flag = !methodDefinition_0.Parameters[3].ParameterType.IsValueType;
    									flag2 = ((methodDefinition_0.Parameters[3].ParameterType.IsValueType && Operators.CompareString(methodDefinition_0.Parameters[3].ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								}
    								break;
    							case Code.Ldloc_0:
    								flag = !methodDefinition_0.Body.Variables[0].VariableType.IsValueType;
    								flag2 = ((methodDefinition_0.Body.Variables[0].VariableType.IsValueType && Operators.CompareString(methodDefinition_0.Body.Variables[0].VariableType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							case Code.Ldloc_1:
    								flag = !methodDefinition_0.Body.Variables[1].VariableType.IsValueType;
    								flag2 = ((methodDefinition_0.Body.Variables[1].VariableType.IsValueType && Operators.CompareString(methodDefinition_0.Body.Variables[1].VariableType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							case Code.Ldloc_2:
    								flag = !methodDefinition_0.Body.Variables[2].VariableType.IsValueType;
    								flag2 = ((methodDefinition_0.Body.Variables[2].VariableType.IsValueType && Operators.CompareString(methodDefinition_0.Body.Variables[2].VariableType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							case Code.Ldloc_3:
    								flag = !methodDefinition_0.Body.Variables[3].VariableType.IsValueType;
    								flag2 = ((methodDefinition_0.Body.Variables[3].VariableType.IsValueType && Operators.CompareString(methodDefinition_0.Body.Variables[3].VariableType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							case Code.Ldlen:
    								flag2 = true;
    								break;
    							case Code.Conv_I1:
    							case Code.Conv_I2:
    							case Code.Conv_I4:
    							case Code.Conv_I8:
    							case Code.Conv_R4:
    							case Code.Conv_R8:
    							case Code.Conv_U4:
    							case Code.Conv_U8:
    							case Code.Conv_R_Un:
    							case Code.Conv_Ovf_I1_Un:
    							case Code.Conv_Ovf_I2_Un:
    							case Code.Conv_Ovf_I4_Un:
    							case Code.Conv_Ovf_I8_Un:
    							case Code.Conv_Ovf_U1_Un:
    							case Code.Conv_Ovf_U2_Un:
    							case Code.Conv_Ovf_U4_Un:
    							case Code.Conv_Ovf_U8_Un:
    							case Code.Conv_Ovf_I_Un:
    							case Code.Conv_Ovf_U_Un:
    							case Code.Conv_Ovf_U1:
    							case Code.Conv_Ovf_I2:
    							case Code.Conv_Ovf_U2:
    							case Code.Conv_Ovf_I4:
    							case Code.Conv_Ovf_U4:
    							case Code.Conv_Ovf_I8:
    							case Code.Conv_Ovf_U8:
    							case Code.Conv_U2:
    							case Code.Conv_U1:
    							case Code.Conv_I:
    							case Code.Conv_Ovf_I:
    							case Code.Conv_Ovf_U:
    							case Code.Conv_U:
    								flag2 = true;
    								break;
    							case Code.Ldarg_S:
    							case Code.Ldarg:
    								flag = !((ParameterDefinition)instruction2.Operand).ParameterType.IsValueType;
    								flag2 = ((((ParameterDefinition)instruction2.Operand).ParameterType.IsValueType && Operators.CompareString(((ParameterDefinition)instruction2.Operand).ParameterType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							case Code.Ldloc_S:
    							case Code.Ldloca_S:
    							case Code.Ldloc:
    							case Code.Ldloca:
    								flag = !((VariableDefinition)instruction2.Operand).VariableType.IsValueType;
    								flag2 = ((((VariableDefinition)instruction2.Operand).VariableType.IsValueType && Operators.CompareString(((VariableDefinition)instruction2.Operand).VariableType.Name, "Boolean", TextCompare: false) != 0) ? true : false);
    								break;
    							}
    							break;
    						}
    					}
    					if (flag)
    					{
    						arrayList_0.Insert(i, "<<MOVEUP>> <ISNOTEQUALS> <NOTHING><NOTEND>");
    						i++;
    					}
    					else if (flag2)
    					{
    						arrayList_0.Insert(i, "0");
    						smethod_15(arrayList_0, i, smethod_13(arrayList_0, i - 1, methodDefinition_0));
    						arrayList_0[i] = "<<MOVEUP>><YES> <NOTEQUALS> " + Conversions.ToString(arrayList_0[i]) + "<YESEND>";
    						i++;
    					}
    				}
    			}
    			finally
    			{
    				if (obj != null)
    				{
    					((IDisposable)obj).Dispose();
    				}
    			}
    		}
    	}

    	private static void smethod_10(ArrayList arrayList_0, MethodDefinition methodDefinition_0)
    	{
    		object obj = null;
    		checked
    		{
    			try
    			{
    				for (int i = 0; i < arrayList_0.Count; i++)
    				{
    					Instruction instruction = (Instruction)arrayList_0[i];
    					if (!(instruction.Operand is Instruction))
    					{
    						continue;
    					}
    					int num = arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction.Operand));
    					Instruction instruction2 = (Instruction)arrayList_0[num];
    					if (num >= i || instruction2.Operand == instruction2)
    					{
    						continue;
    					}
    					ILProcessor iLProcessor = methodDefinition_0.Body.GetILProcessor();
    					Instruction instruction3 = iLProcessor.Create(OpCodes.Br, instruction);
    					foreach (Instruction item in arrayList_0)
    					{
    						if (item.Operand == instruction2)
    						{
    							item.Operand = instruction3;
    						}
    					}
    					instruction3.Operand = instruction3;
    					arrayList_0.Insert(num, instruction3);
    					i++;
    				}
    			}
    			finally
    			{
    				if (obj != null)
    				{
    					((IDisposable)obj).Dispose();
    				}
    			}
    		}
    	}

    	private static void smethod_11(MethodDefinition methodDefinition_0, ArrayList arrayList_0)
    	{
    		object obj = null;
    		checked
    		{
    			try
    			{
    				int num = -1;
    				while (num < arrayList_0.Count - 5)
    				{
    					num++;
    					if (arrayList_0[num] is Instruction && arrayList_0[num + 1] is Instruction && arrayList_0[num + 2] is Instruction && arrayList_0[num + 3] is Instruction && ((Instruction)arrayList_0[num]).OpCode.Code == Code.Ldc_I4_0 && ((Instruction)arrayList_0[num + 1]).OpCode.Code == Code.Call && Operators.CompareString(((MethodReference)((Instruction)arrayList_0[num + 1]).Operand).Name, "CompareString", TextCompare: false) == 0 && ((Instruction)arrayList_0[num + 2]).OpCode.Code == Code.Ldc_I4_0)
    					{
    						switch (((Instruction)arrayList_0[num + 3]).OpCode.Code)
    						{
    						case Code.Beq_S:
    						case Code.Beq:
    							((Instruction)arrayList_0[num + 3]).OpCode = OpCodes.Brfalse;
    							break;
    						case Code.Bne_Un_S:
    						case Code.Bne_Un:
    							((Instruction)arrayList_0[num + 3]).OpCode = OpCodes.Brtrue;
    							break;
    						default:
    							continue;
    						}
    						arrayList_0[num] = methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Ceq);
    						arrayList_0[num + 1] = methodDefinition_0.Body.GetILProcessor().Create(OpCodes.Not);
    						arrayList_0.RemoveAt(num + 2);
    					}
    				}
    			}
    			finally
    			{
    				if (obj != null)
    				{
    					((IDisposable)obj).Dispose();
    				}
    			}
    		}
    	}

    	private static void smethod_12(MethodDefinition methodDefinition_0, ArrayList arrayList_0)
    	{
    		object obj = null;
    		checked
    		{
    			try
    			{
    				int num = -1;
    				while (num < arrayList_0.Count - 7)
    				{
    					num++;
    					if (arrayList_0[num] is Instruction && arrayList_0[num + 1] is Instruction && arrayList_0[num + 2] is Instruction && arrayList_0[num + 3] is Instruction && arrayList_0[num + 4] is Instruction && arrayList_0[num + 5] is Instruction && arrayList_0[num + 6] is Instruction && ((((Instruction)arrayList_0[num]).OpCode.Code == Code.Beq) | (((Instruction)arrayList_0[num]).OpCode.Code == Code.Beq_S)) && ((Instruction)arrayList_0[num]).Operand == (Instruction)arrayList_0[num + 3] && ((Instruction)arrayList_0[num + 1]).OpCode.Code == Code.Ldc_I4_0 && ((((Instruction)arrayList_0[num + 2]).OpCode.Code == Code.Br) | (((Instruction)arrayList_0[num + 2]).OpCode.Code == Code.Br_S)) && ((Instruction)arrayList_0[num + 2]).Operand == (Instruction)arrayList_0[num + 4] && ((Instruction)arrayList_0[num + 3]).OpCode.Code == Code.Ldc_I4_1 && ((Instruction)arrayList_0[num + 4]).OpCode.Name.StartsWith("stloc") && ((Instruction)arrayList_0[num + 5]).OpCode.Name.StartsWith("ldloc"))
    					{
    						switch (((Instruction)arrayList_0[num + 6]).OpCode.Code)
    						{
    						case Code.Brfalse_S:
    						case Code.Brfalse:
    							((Instruction)arrayList_0[num + 6]).OpCode = OpCodes.Brtrue;
    							break;
    						case Code.Brtrue_S:
    						case Code.Brtrue:
    							((Instruction)arrayList_0[num + 6]).OpCode = OpCodes.Brfalse;
    							break;
    						default:
    							continue;
    						}
    						arrayList_0.RemoveAt(num + 5);
    						arrayList_0.RemoveAt(num + 4);
    						arrayList_0.RemoveAt(num + 3);
    						arrayList_0.RemoveAt(num + 2);
    						arrayList_0.RemoveAt(num + 1);
    						arrayList_0.RemoveAt(num);
    					}
    				}
    			}
    			finally
    			{
    				if (obj != null)
    				{
    					((IDisposable)obj).Dispose();
    				}
    			}
    		}
    	}

    	public static TypeReference smethod_13(ArrayList arrayList_0, int int_0, MethodDefinition methodDefinition_0)
    	{
    		if (int_0 < 0)
    		{
    			return null;
    		}
    		if (arrayList_0[int_0] is string)
    		{
    			return null;
    		}
    		Instruction instruction = (Instruction)arrayList_0[int_0];
    		if (instruction.Operand is TypeReference)
    		{
    			return (TypeReference)instruction.Operand;
    		}
    		if (instruction.Operand is MethodReference)
    		{
    			return ((MethodReference)instruction.Operand).ReturnType;
    		}
    		if (instruction.Operand is FieldReference)
    		{
    			return ((FieldReference)instruction.Operand).FieldType;
    		}
    		if (instruction.Operand is VariableReference)
    		{
    			return ((VariableReference)instruction.Operand).VariableType;
    		}
    		if (instruction.Operand is ParameterDefinition)
    		{
    			return ((ParameterDefinition)instruction.Operand).ParameterType;
    		}
    		switch (instruction.OpCode.Code)
    		{
    		case Code.Ldarg_0:
    			if (methodDefinition_0.HasThis)
    			{
    				return null;
    			}
    			return methodDefinition_0.Parameters[0].ParameterType;
    		case Code.Ldarg_1:
    			if (methodDefinition_0.HasThis)
    			{
    				return methodDefinition_0.Parameters[0].ParameterType;
    			}
    			return methodDefinition_0.Parameters[1].ParameterType;
    		case Code.Ldarg_2:
    			if (methodDefinition_0.HasThis)
    			{
    				return methodDefinition_0.Parameters[1].ParameterType;
    			}
    			return methodDefinition_0.Parameters[2].ParameterType;
    		case Code.Ldarg_3:
    			if (methodDefinition_0.HasThis)
    			{
    				return methodDefinition_0.Parameters[2].ParameterType;
    			}
    			return methodDefinition_0.Parameters[3].ParameterType;
    		case Code.Ldloc_0:
    			return methodDefinition_0.Body.Variables[0].VariableType;
    		case Code.Ldloc_1:
    			return methodDefinition_0.Body.Variables[1].VariableType;
    		case Code.Ldloc_2:
    			return methodDefinition_0.Body.Variables[2].VariableType;
    		case Code.Ldloc_3:
    			return methodDefinition_0.Body.Variables[3].VariableType;
    		default:
    			return null;
    		case Code.Ldc_I4_0:
    		case Code.Ldc_I4_1:
    			return typeDefinition_0;
    		}
    	}

    	public static void smethod_14(ArrayList arrayList_0, int int_0)
    	{
    		smethod_15(arrayList_0, int_0, typeDefinition_0);
    	}

    	public static void smethod_15(ArrayList arrayList_0, int int_0, TypeReference typeReference_0)
    	{
    		checked
    		{
    			while (!(arrayList_0[int_0] is string) && int_0 > 0)
    			{
    				int_0--;
    			}
    			if (Conversions.ToString(arrayList_0[int_0]).Contains("<LINEEND>") && int_0 > 0)
    			{
    				string value = Conversions.ToString(arrayList_0[int_0]);
    				arrayList_0.RemoveAt(int_0);
    				arrayList_0.Insert(int_0 - 1, value);
    			}
    			if (typeReference_0 == null)
    			{
    				return;
    			}
    			if (typeReference_0 != typeDefinition_0 && Operators.CompareString(typeReference_0.Name, "Boolean", TextCompare: false) != 0)
    			{
    				TypeDefinition typeDefinition = typeReference_0.Resolve();
    				if (typeDefinition == null || !typeDefinition.IsEnum || !int.TryParse(Conversions.ToString(arrayList_0[int_0]), out var result))
    				{
    					return;
    				}
    				using Collection<FieldDefinition>.Enumerator enumerator = typeDefinition.Fields.GetEnumerator();
    				FieldDefinition current;
    				do
    				{
    					if (enumerator.MoveNext())
    					{
    						current = enumerator.Current;
    						continue;
    					}
    					return;
    				}
    				while (current.Constant == null || !Operators.ConditionalCompareObjectEqual(result, current.Constant, TextCompare: false));
    				arrayList_0[int_0] = smethod_29(typeReference_0) + "." + LanguageWriter.GetReferenceKey(current) + current.Name + "<<>>";
    				return;
    			}
    			if (Operators.CompareString(Conversions.ToString(arrayList_0[int_0]), "0", TextCompare: false) == 0)
    			{
    				arrayList_0[int_0] = "<FALSE>";
    			}
    			if (Operators.CompareString(Conversions.ToString(arrayList_0[int_0]), "1", TextCompare: false) == 0)
    			{
    				arrayList_0[int_0] = "<TRUE>";
    			}
    			if (Operators.CompareString(Conversions.ToString(arrayList_0[int_0]), "<IFCLOSE>", TextCompare: false) != 0 && (!Conversions.ToString(arrayList_0[int_0]).StartsWith("label") || Operators.CompareString(Conversions.ToString(arrayList_0[int_0 - 1]), "<IFCLOSE>", TextCompare: false) != 0))
    			{
    				return;
    			}
    			int num = int_0 - 1;
    			string text = null;
    			string text2 = null;
    			while (num > 0 && (!(arrayList_0[num] is string) || !Conversions.ToString(arrayList_0[num]).StartsWith("<IF>")))
    			{
    				if (arrayList_0[num] is string && !Conversions.ToString(arrayList_0[num]).StartsWith("<GOTO>") && !Conversions.ToString(arrayList_0[num]).StartsWith("<IFCLOSE>") && !Conversions.ToString(arrayList_0[num]).StartsWith("<ELSE>"))
    				{
    					if (text2 == null)
    					{
    						text2 = Conversions.ToString(arrayList_0[num]);
    					}
    					text = Conversions.ToString(arrayList_0[num]);
    				}
    				num--;
    			}
    			string text3 = Conversions.ToString(arrayList_0[num]);
    			text3 = text3.Substring(5, text3.Length - 14);
    			if ((Operators.CompareString(text2, "1", TextCompare: false) == 0) | (Operators.CompareString(text, "0", TextCompare: false) == 0))
    			{
    				text3 = smethod_22(text3);
    				if (Operators.CompareString(text, "0", TextCompare: false) != 0)
    				{
    					text3 = $"({text3} <ORELSE> {text})";
    				}
    				if (Operators.CompareString(text2, "1", TextCompare: false) != 0)
    				{
    					text3 = $"({text3} <ANDALSO> {text2})";
    				}
    			}
    			arrayList_0[int_0] = text3;
    			int num2 = int_0 - 1;
    			int num3 = num;
    			for (int i = num2; i >= num3; i += -1)
    			{
    				arrayList_0[i] = "";
    			}
    		}
    	}

    	private static void smethod_16(ArrayList arrayList_0, int int_0, int int_1)
    	{
    		if (int_1 >= 0)
    		{
    			if (Operators.CompareString(Conversions.ToString(arrayList_0[int_1]), "0", TextCompare: false) == 0)
    			{
    				arrayList_0[int_1] = "<FALSE>";
    			}
    			if (Operators.CompareString(Conversions.ToString(arrayList_0[int_1]), "1", TextCompare: false) == 0)
    			{
    				arrayList_0[int_1] = "<TRUE>";
    			}
    		}
    		if (Operators.CompareString(Conversions.ToString(arrayList_0[int_0]), "0", TextCompare: false) == 0)
    		{
    			arrayList_0[int_0] = "<FALSE>";
    		}
    		if (Operators.CompareString(Conversions.ToString(arrayList_0[int_0]), "1", TextCompare: false) == 0)
    		{
    			arrayList_0[int_0] = "<TRUE>";
    		}
    	}

    	private static bool smethod_17(TypeReference typeReference_0, TypeReference typeReference_1)
    	{
    		if (Operators.CompareString(typeReference_0.FullName, typeReference_1.FullName, TextCompare: false) == 0)
    		{
    			return true;
    		}
    		TypeDefinition typeDefinition = typeReference_0.Resolve();
    		if (typeDefinition == null)
    		{
    			return false;
    		}
    		if (typeDefinition.BaseType != null)
    		{
    			return smethod_17(typeDefinition.BaseType, typeReference_1);
    		}
    		return false;
    	}

    	private static string smethod_18(Collection<ParameterDefinition> collection_0, ArrayList arrayList_0, ref int int_0, int int_1)
    	{
    		string text = "";
    		checked
    		{
    			for (int i = collection_0.Count - 1; i >= 0; i += -1)
    			{
    				smethod_15(arrayList_0, int_0 - 1 - int_1, collection_0[i].ParameterType);
    				if (collection_0[i].IsOut)
    				{
    					arrayList_0[int_0 - 1 - int_1] = Operators.AddObject("<OUT>", arrayList_0[int_0 - 1 - int_1]);
    				}
    				else if (collection_0[i].ParameterType.IsByReference)
    				{
    					arrayList_0[int_0 - 1 - int_1] = Operators.AddObject("<REF>", arrayList_0[int_0 - 1 - int_1]);
    				}
    				text = Conversions.ToString(Operators.ConcatenateObject(arrayList_0[int_0 - 1 - int_1], ", " + text));
    				arrayList_0.RemoveAt(int_0 - 1 - int_1);
    				int_0--;
    			}
    			return text.TrimEnd(' ', ',');
    		}
    	}

    	private static string smethod_19(MethodDefinition methodDefinition_0, int int_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		checked
    		{
    			try
    			{
    				int num = 0;
    				int num2 = 0;
    				MethodBody body = methodDefinition_0.Body;
    				if (methodDefinition_0.Body == null)
    				{
    					return "";
    				}
    				ArrayList arrayList_ = new ArrayList();
    				ILProcessor iLProcessor = methodDefinition_0.Body.GetILProcessor();
    				foreach (Instruction instruction8 in body.Instructions)
    				{
    					Instruction instruction;
    					if (instruction8.Operand == null)
    					{
    						instruction = iLProcessor.Create(instruction8.OpCode);
    					}
    					else
    					{
    						object[] array = new object[2];
    						Instruction instruction2 = instruction8;
    						array[0] = instruction2.OpCode;
    						Instruction instruction3 = instruction8;
    						array[1] = RuntimeHelpers.GetObjectValue(instruction3.Operand);
    						object[] array2 = array;
    						bool[] array3 = new bool[2] { true, true };
    						object obj = NewLateBinding.LateGet(iLProcessor, null, "Create", array2, null, null, array3);
    						if (array3[0])
    						{
    							instruction2.OpCode = (OpCode)Conversions.ChangeType(RuntimeHelpers.GetObjectValue(array2[0]), typeof(OpCode));
    						}
    						if (array3[1])
    						{
    							instruction3.Operand = RuntimeHelpers.GetObjectValue(array2[1]);
    						}
    						instruction = (Instruction)obj;
    					}
    					instruction.Offset = instruction8.Offset;
    					arrayList_.Add(instruction);
    				}
    				foreach (Instruction item in arrayList_)
    				{
    					if (item.Operand is Instruction)
    					{
    						item.Operand = RuntimeHelpers.GetObjectValue(arrayList_[methodDefinition_0.Body.Instructions.IndexOf((Instruction)item.Operand)]);
    					}
    					if (item.Operand is Instruction[])
    					{
    						Instruction[] array4 = (Instruction[])item.Operand;
    						Instruction[] array5 = new Instruction[array4.Length - 1 + 1];
    						int num3 = array4.Length - 1;
    						for (int i = 0; i <= num3; i++)
    						{
    							array5[i] = (Instruction)arrayList_[methodDefinition_0.Body.Instructions.IndexOf(array4[i])];
    						}
    						item.Operand = array5;
    					}
    				}
    				List<string> list = new List<string>();
    				foreach (VariableDefinition variable in body.Variables)
    				{
    					variable.Name = variable.Name.Replace("&", "");
    					if (!string.IsNullOrEmpty(variable.Name))
    					{
    						list.Add(variable.Name);
    					}
    				}
    				foreach (VariableDefinition variable2 in body.Variables)
    				{
    					if (string.IsNullOrEmpty(variable2.Name))
    					{
    						variable2.Name = smethod_28(variable2.VariableType, list);
    					}
    					if (!variable2.Name.StartsWith("#"))
    					{
    						variable2.Name = "#" + variable2.Name;
    					}
    				}
    				smethod_10(arrayList_, methodDefinition_0);
    				smethod_5(arrayList_, methodDefinition_0);
    				List<ExceptionHandler> list_ = new List<ExceptionHandler>();
    				smethod_6(methodDefinition_0, arrayList_, list_);
    				smethod_7(methodDefinition_0, arrayList_, list_);
    				smethod_4(methodDefinition_0, arrayList_);
    				smethod_9(methodDefinition_0, arrayList_);
    				smethod_11(methodDefinition_0, arrayList_);
    				object obj2 = null;
    				try
    				{
    					for (int j = 0; j < arrayList_.Count; j++)
    					{
    						object objectValue = RuntimeHelpers.GetObjectValue(arrayList_[j]);
    						if (objectValue is Instruction)
    						{
    							Instruction instruction5 = (Instruction)objectValue;
    							switch (instruction5.OpCode.Code)
    							{
    							case Code.Ldarg_0:
    								if (!methodDefinition_0.HasThis && methodDefinition_0.Parameters.Count != 0)
    								{
    									arrayList_[j] = RuntimeHelpers.GetObjectValue(Interaction.IIf(Operators.CompareString(methodDefinition_0.Parameters[0].Name, "WithEventsValue", TextCompare: false) == 0, "<VALUE>", methodDefinition_0.Parameters[0].Name));
    								}
    								else
    								{
    									arrayList_[j] = "<ME>";
    								}
    								continue;
    							case Code.Ldarg_1:
    								if (methodDefinition_0.HasThis)
    								{
    									arrayList_[j] = RuntimeHelpers.GetObjectValue(Interaction.IIf(Operators.CompareString(methodDefinition_0.Parameters[0].Name, "WithEventsValue", TextCompare: false) == 0, "<VALUE>", methodDefinition_0.Parameters[0].Name));
    								}
    								else
    								{
    									arrayList_[j] = methodDefinition_0.Parameters[1].Name;
    								}
    								continue;
    							case Code.Ldarg_2:
    								if (methodDefinition_0.HasThis)
    								{
    									arrayList_[j] = methodDefinition_0.Parameters[1].Name;
    								}
    								else
    								{
    									arrayList_[j] = methodDefinition_0.Parameters[2].Name;
    								}
    								continue;
    							case Code.Ldarg_3:
    								if (methodDefinition_0.HasThis)
    								{
    									arrayList_[j] = methodDefinition_0.Parameters[2].Name;
    								}
    								else
    								{
    									arrayList_[j] = methodDefinition_0.Parameters[3].Name;
    								}
    								continue;
    							case Code.Ldloc_0:
    								if (!smethod_25(ref arrayList_, ref j, ref methodDefinition_0))
    								{
    									arrayList_[j] = methodDefinition_0.Body.Variables[0].Name;
    								}
    								continue;
    							case Code.Ldloc_1:
    								if (!smethod_25(ref arrayList_, ref j, ref methodDefinition_0))
    								{
    									arrayList_[j] = methodDefinition_0.Body.Variables[1].Name;
    								}
    								continue;
    							case Code.Ldloc_2:
    								if (!smethod_25(ref arrayList_, ref j, ref methodDefinition_0))
    								{
    									arrayList_[j] = methodDefinition_0.Body.Variables[2].Name;
    								}
    								continue;
    							case Code.Ldloc_3:
    								if (!smethod_25(ref arrayList_, ref j, ref methodDefinition_0))
    								{
    									arrayList_[j] = methodDefinition_0.Body.Variables[3].Name;
    								}
    								continue;
    							case Code.Stloc_0:
    								smethod_15(arrayList_, j - 1, methodDefinition_0.Body.Variables[0].VariableType);
    								if (methodDefinition_0.Body.Variables[0].Name.StartsWith("#"))
    								{
    									methodDefinition_0.Body.Variables[0].Name = methodDefinition_0.Body.Variables[0].Name.Substring(1);
    									smethod_20(arrayList_, ref j, "<DECLARE={value}<END> = {value1}<LINEEND>", Conversions.ToString(methodDefinition_0.Body.Variables[0].Index), 0, methodDefinition_0);
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "{value} = {value1}<LINEEND>", methodDefinition_0.Body.Variables[0].Name, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Stloc_1:
    								smethod_15(arrayList_, j - 1, methodDefinition_0.Body.Variables[1].VariableType);
    								if (methodDefinition_0.Body.Variables[1].Name.StartsWith("#"))
    								{
    									methodDefinition_0.Body.Variables[1].Name = methodDefinition_0.Body.Variables[1].Name.Substring(1);
    									smethod_20(arrayList_, ref j, "<DECLARE={value}<END> = {value1}<LINEEND>", Conversions.ToString(methodDefinition_0.Body.Variables[1].Index), 0, methodDefinition_0);
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "{value} = {value1}<LINEEND>", methodDefinition_0.Body.Variables[1].Name, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Stloc_2:
    								smethod_15(arrayList_, j - 1, methodDefinition_0.Body.Variables[2].VariableType);
    								if (methodDefinition_0.Body.Variables[2].Name.StartsWith("#"))
    								{
    									methodDefinition_0.Body.Variables[2].Name = methodDefinition_0.Body.Variables[2].Name.Substring(1);
    									smethod_20(arrayList_, ref j, "<DECLARE={value}<END> = {value1}<LINEEND>", Conversions.ToString(methodDefinition_0.Body.Variables[2].Index), 0, methodDefinition_0);
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "{value} = {value1}<LINEEND>", methodDefinition_0.Body.Variables[2].Name, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Stloc_3:
    								smethod_15(arrayList_, j - 1, methodDefinition_0.Body.Variables[3].VariableType);
    								if (methodDefinition_0.Body.Variables[3].Name.StartsWith("#"))
    								{
    									methodDefinition_0.Body.Variables[3].Name = methodDefinition_0.Body.Variables[3].Name.Substring(1);
    									smethod_20(arrayList_, ref j, "<DECLARE={value}<END> = {value1}<LINEEND>", Conversions.ToString(methodDefinition_0.Body.Variables[3].Index), 0, methodDefinition_0);
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "{value} = {value1}<LINEEND>", methodDefinition_0.Body.Variables[3].Name, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Ldnull:
    								arrayList_[j] = "<NOTHING>";
    								continue;
    							case Code.Ldc_I4_M1:
    								arrayList_[j] = "-1";
    								continue;
    							case Code.Ldc_I4_0:
    								arrayList_[j] = "0";
    								continue;
    							case Code.Ldc_I4_1:
    								arrayList_[j] = "1";
    								continue;
    							case Code.Ldc_I4_2:
    								arrayList_[j] = "2";
    								continue;
    							case Code.Ldc_I4_3:
    								arrayList_[j] = "3";
    								continue;
    							case Code.Ldc_I4_4:
    								arrayList_[j] = "4";
    								continue;
    							case Code.Ldc_I4_5:
    								arrayList_[j] = "5";
    								continue;
    							case Code.Ldc_I4_6:
    								arrayList_[j] = "6";
    								continue;
    							case Code.Ldc_I4_7:
    								arrayList_[j] = "7";
    								continue;
    							case Code.Ldc_I4_8:
    								arrayList_[j] = "8";
    								continue;
    							case Code.Ldc_I4_S:
    								arrayList_[j] = instruction5.Operand.ToString();
    								continue;
    							case Code.Ldc_I4:
    								arrayList_[j] = instruction5.Operand.ToString();
    								continue;
    							case Code.Ldc_I8:
    								arrayList_[j] = instruction5.Operand.ToString();
    								continue;
    							case Code.Ldc_R4:
    								arrayList_[j] = instruction5.Operand.ToString();
    								continue;
    							case Code.Ldc_R8:
    								arrayList_[j] = instruction5.Operand.ToString();
    								continue;
    							case Code.Dup:
    								arrayList_[j] = RuntimeHelpers.GetObjectValue(arrayList_[j - 1]);
    								continue;
    							case Code.Pop:
    								arrayList_.RemoveAt(j);
    								j--;
    								if (j >= 1 && arrayList_[j - 1] is string && Conversions.ToString(arrayList_[j - 1]).Contains("<CATCHSTART="))
    								{
    									arrayList_.RemoveAt(j);
    									j--;
    								}
    								continue;
    							case Code.Jmp:
    							case Code.Call:
    							case Code.Calli:
    							{
    								MethodReference methodReference5 = (MethodReference)instruction5.Operand;
    								PropertyDefinition propertyDefinition2 = null;
    								EventDefinition eventDefinition2 = null;
    								if (methodReference5.Name.Contains("_"))
    								{
    									TypeDefinition typeDefinition3 = methodReference5.DeclaringType.Resolve();
    									if (typeDefinition3 != null)
    									{
    										foreach (PropertyDefinition property in typeDefinition3.Properties)
    										{
    											if (property.SetMethod != null && Operators.CompareString(property.SetMethod.Name, methodReference5.Name, TextCompare: false) == 0)
    											{
    												propertyDefinition2 = property;
    											}
    											if (property.GetMethod != null && Operators.CompareString(property.GetMethod.Name, methodReference5.Name, TextCompare: false) == 0)
    											{
    												propertyDefinition2 = property;
    											}
    										}
    										foreach (EventDefinition @event in typeDefinition3.Events)
    										{
    											if (@event.AddMethod != null && Operators.CompareString(@event.AddMethod.FullName, methodReference5.FullName, TextCompare: false) == 0)
    											{
    												eventDefinition2 = @event;
    											}
    											if (@event.RemoveMethod != null && Operators.CompareString(@event.RemoveMethod.FullName, methodReference5.FullName, TextCompare: false) == 0)
    											{
    												eventDefinition2 = @event;
    											}
    										}
    									}
    								}
    								if (propertyDefinition2 != null)
    								{
    									string name = propertyDefinition2.Name;
    									if (methodReference5.HasThis && j - methodReference5.Parameters.Count - 1 >= 0)
    									{
    										if (methodDefinition_0.DeclaringType.BaseType != null && Operators.CompareString(methodReference5.DeclaringType.FullName, methodDefinition_0.DeclaringType.BaseType.FullName, TextCompare: false) == 0 && j > methodReference5.Parameters.Count && Operators.ConditionalCompareObjectEqual(arrayList_[j - methodReference5.Parameters.Count - 1], "<ME>", TextCompare: false))
    										{
    											arrayList_[j - methodReference5.Parameters.Count - 1] = "<MYBASE>";
    										}
    										name = string.Format("{0}.{1}", RuntimeHelpers.GetObjectValue(arrayList_[j - methodReference5.Parameters.Count - 1]), LanguageWriter.GetReferenceKey(methodReference5) + name + "<<>>");
    										arrayList_.RemoveAt(j - methodReference5.Parameters.Count - 1);
    										j--;
    									}
    									else
    									{
    										name = smethod_29(methodReference5.DeclaringType) + "." + LanguageWriter.GetReferenceKey(methodReference5) + name + "<<>>";
    									}
    									arrayList_[j] = name;
    									if (propertyDefinition2.Parameters.Count > 0)
    									{
    										int int_2 = 0;
    										if (propertyDefinition2.SetMethod != null && Operators.CompareString(propertyDefinition2.SetMethod.Name, methodReference5.Name, TextCompare: false) == 0)
    										{
    											int_2 = 1;
    										}
    										string right2 = smethod_18(propertyDefinition2.Parameters, arrayList_, ref j, int_2);
    										arrayList_[j] = Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(arrayList_[j], "("), right2), ")");
    									}
    									if (propertyDefinition2.SetMethod != null && Operators.CompareString(propertyDefinition2.SetMethod.Name, methodReference5.Name, TextCompare: false) == 0)
    									{
    										smethod_15(arrayList_, j - 1, propertyDefinition2.PropertyType);
    										arrayList_[j] = Operators.ConcatenateObject(Operators.ConcatenateObject(arrayList_[j], " = "), arrayList_[j - 1]);
    										arrayList_.RemoveAt(j - 1);
    										j--;
    									}
    									continue;
    								}
    								if (eventDefinition2 != null)
    								{
    									string arg5 = smethod_18(methodReference5.Parameters, arrayList_, ref j, 0);
    									string name2 = eventDefinition2.Name;
    									if (methodReference5.HasThis && j - 1 >= 0)
    									{
    										if (methodDefinition_0.DeclaringType.BaseType != null && Operators.CompareString(methodReference5.DeclaringType.FullName, methodDefinition_0.DeclaringType.BaseType.FullName, TextCompare: false) == 0 && j > methodReference5.Parameters.Count && Operators.ConditionalCompareObjectEqual(arrayList_[j - 1], "<ME>", TextCompare: false))
    										{
    											arrayList_[j - 1] = "<MYBASE>";
    										}
    										name2 = string.Format("{0}.{1}", RuntimeHelpers.GetObjectValue(arrayList_[j - 1]), LanguageWriter.GetReferenceKey(eventDefinition2) + name2 + "<<>>");
    										arrayList_.RemoveAt(j - 1);
    										j--;
    									}
    									else
    									{
    										name2 = smethod_29(methodReference5.DeclaringType) + "." + LanguageWriter.GetReferenceKey(eventDefinition2) + name2 + "<<>>";
    									}
    									if (Conversions.ToBoolean(Operators.AndObject(j > 0, Operators.CompareObjectEqual(arrayList_[j - 1], "<ME>", TextCompare: false))))
    									{
    										arrayList_.RemoveAt(j - 1);
    										j--;
    									}
    									if (Operators.CompareString(methodReference5.FullName, eventDefinition2.AddMethod.FullName, TextCompare: false) == 0)
    									{
    										arrayList_[j] = $"<ADDHANDLER1>{name2}<ADDHANDLER2>{arg5}";
    									}
    									else
    									{
    										arrayList_[j] = $"<REMOVEHANDLER1>{name2}<REMOVEHANDLER2>{arg5}";
    									}
    									continue;
    								}
    								if (Operators.CompareString(methodReference5.Name, "CompareString", TextCompare: false) == 0 && Operators.CompareString(methodReference5.DeclaringType.FullName, "Microsoft.VisualBasic.CompilerServices.Operators", TextCompare: false) == 0)
    								{
    									arrayList_[j] = smethod_22($"{RuntimeHelpers.GetObjectValue(arrayList_[j - 3])} <EQUALS> {RuntimeHelpers.GetObjectValue(arrayList_[j - 2])}");
    									int num6 = 1;
    									do
    									{
    										arrayList_.RemoveAt(j - num6);
    										num6++;
    									}
    									while (num6 <= 3);
    									j -= 3;
    									continue;
    								}
    								if (Operators.CompareString(methodReference5.Name, "op_Equality", TextCompare: false) == 0 && Operators.CompareString(methodReference5.DeclaringType.FullName, "System.String", TextCompare: false) == 0)
    								{
    									smethod_20(arrayList_, ref j, "{value2} <EQUALS> {value1}", "", 0, methodDefinition_0);
    									continue;
    								}
    								if (Operators.CompareString(methodReference5.Name, "Concat", TextCompare: false) == 0 && Operators.CompareString(methodReference5.DeclaringType.FullName, "System.String", TextCompare: false) == 0)
    								{
    									arrayList_[j] = RuntimeHelpers.GetObjectValue(arrayList_[j - methodReference5.Parameters.Count]);
    									for (int m = methodReference5.Parameters.Count - 1; m >= 1; m += -1)
    									{
    										ArrayList arrayList = arrayList_;
    										int index = j;
    										arrayList[index] = Operators.ConcatenateObject(arrayList[index], Operators.ConcatenateObject(" <STRINGCONCAT> ", arrayList_[j - m]));
    									}
    									int count = methodReference5.Parameters.Count;
    									for (int n = 1; n <= count; n++)
    									{
    										arrayList_.RemoveAt(j - n);
    									}
    									j -= methodReference5.Parameters.Count;
    									continue;
    								}
    								if (Operators.CompareString(methodReference5.Name, "InitializeArray", TextCompare: false) == 0 && Operators.CompareString(methodReference5.DeclaringType.FullName, "System.Runtime.CompilerServices.RuntimeHelpers", TextCompare: false) == 0)
    								{
    									arrayList_[j] = RuntimeHelpers.GetObjectValue(arrayList_[j - 2]);
    									arrayList_.RemoveAt(j - 1);
    									arrayList_.RemoveAt(j - 2);
    									j -= 2;
    									int num7 = -1;
    									int num8 = methodDefinition_0.Body.Instructions.Count - 1;
    									for (num7 = 0; num7 <= num8 && methodDefinition_0.Body.Instructions[num7].Offset < instruction5.Offset; num7++)
    									{
    									}
    									TypeReference typeReference = (TypeReference)methodDefinition_0.Body.Instructions[num7 - 2].Operand;
    									if (typeReference == null)
    									{
    										typeReference = (TypeReference)methodDefinition_0.Body.Instructions[num7 - 3].Operand;
    									}
    									FieldDefinition fieldDefinition = (FieldDefinition)methodDefinition_0.Body.Instructions[num7 - 1].Operand;
    									string text5 = "";
    									switch (typeReference.Name)
    									{
    									case "Int32":
    									{
    										int num19 = fieldDefinition.InitialValue.Length - 1;
    										for (int num20 = 0; num20 <= num19; num20 += 4)
    										{
    											text5 = text5 + Conversions.ToString(BitConverter.ToInt32(fieldDefinition.InitialValue, num20)) + ", ";
    										}
    										break;
    									}
    									case "Int16":
    									{
    										int num11 = fieldDefinition.InitialValue.Length - 1;
    										for (int num12 = 0; num12 <= num11; num12 += 2)
    										{
    											text5 = text5 + Conversions.ToString(unchecked((int)BitConverter.ToInt16(fieldDefinition.InitialValue, num12))) + ", ";
    										}
    										break;
    									}
    									case "Int64":
    									{
    										int num15 = fieldDefinition.InitialValue.Length - 1;
    										for (int num16 = 0; num16 <= num15; num16 += 8)
    										{
    											text5 = text5 + Conversions.ToString(BitConverter.ToInt64(fieldDefinition.InitialValue, num16)) + ", ";
    										}
    										break;
    									}
    									case "Boolean":
    									{
    										int num9 = fieldDefinition.InitialValue.Length - 1;
    										for (int num10 = 0; num10 <= num9; num10++)
    										{
    											text5 = text5 + Conversions.ToString(BitConverter.ToBoolean(fieldDefinition.InitialValue, num10)) + ", ";
    										}
    										break;
    									}
    									case "Char":
    									{
    										int num17 = fieldDefinition.InitialValue.Length - 1;
    										for (int num18 = 0; num18 <= num17; num18 += 4)
    										{
    											text5 = text5 + Conversions.ToString(BitConverter.ToChar(fieldDefinition.InitialValue, num18)) + ", ";
    										}
    										break;
    									}
    									case "Double":
    									{
    										int num13 = fieldDefinition.InitialValue.Length - 1;
    										for (int num14 = 0; num14 <= num13; num14 += 8)
    										{
    											text5 = text5 + Conversions.ToString(BitConverter.ToDouble(fieldDefinition.InitialValue, num14)) + ", ";
    										}
    										break;
    									}
    									case "String":
    										text5 = ((!BitConverter.ToString(fieldDefinition.InitialValue, 0).Contains("\\")) ? ("<<r>>\"" + BitConverter.ToString(fieldDefinition.InitialValue, 0) + "\"<<>>") : ("<<r>><PLAINSTRING>\"" + BitConverter.ToString(fieldDefinition.InitialValue, 0) + "\"<<>>"));
    										break;
    									}
    									text5 = text5.TrimEnd(' ', ',');
    									arrayList_[j] = "<NEW> " + smethod_29(typeReference) + "<ARRAYBRACKETOPEN><ARRAYBRACKETCLOSE> {" + text5 + "}";
    									continue;
    								}
    								string text6 = methodReference5.Name;
    								string right3 = smethod_18(methodReference5.Parameters, arrayList_, ref j, 0);
    								if (Operators.CompareString(methodDefinition_0.Name, methodReference5.Name, TextCompare: false) == 0 && ((Operators.CompareString(text6, ".ctor", TextCompare: false) == 0) | (Operators.CompareString(text6, ".cctor", TextCompare: false) == 0)) && smethod_17(methodDefinition_0.DeclaringType, methodReference5.DeclaringType))
    								{
    									text6 = $"<CONSTRUCTOR={LanguageWriter.GetReferenceKey(methodReference5)}{methodReference5.DeclaringType.Name}<<>><END>";
    								}
    								else if ((Operators.CompareString(text6, ".ctor", TextCompare: false) == 0) | (Operators.CompareString(text6, ".cctor", TextCompare: false) == 0))
    								{
    									text6 = "<NEW> " + smethod_29(methodReference5.DeclaringType);
    								}
    								if (!text6.StartsWith("<NEW>"))
    								{
    									if (methodReference5.HasThis && j - 1 >= 0)
    									{
    										if (methodDefinition_0.DeclaringType.BaseType != null && Operators.CompareString(methodReference5.DeclaringType.FullName, methodDefinition_0.DeclaringType.BaseType.FullName, TextCompare: false) == 0 && j > methodReference5.Parameters.Count && Operators.ConditionalCompareObjectEqual(arrayList_[j - 1], "<ME>", TextCompare: false))
    										{
    											arrayList_[j - 1] = "<MYBASE>";
    										}
    										text6 = string.Format("{0}.{1}", RuntimeHelpers.GetObjectValue(arrayList_[j - 1]), LanguageWriter.GetReferenceKey(methodReference5) + text6 + "<<>>");
    										arrayList_.RemoveAt(j - 1);
    										j--;
    									}
    									else
    									{
    										text6 = smethod_29(methodReference5.DeclaringType) + "." + LanguageWriter.GetReferenceKey(methodReference5) + text6 + "<<>>";
    									}
    								}
    								if (Operators.CompareString(methodReference5.Name, "GetTypeFromHandle", TextCompare: false) == 0 && Operators.CompareString(methodReference5.DeclaringType.FullName, "System.Type", TextCompare: false) == 0)
    								{
    									text6 = "<GETTYPE>(";
    								}
    								arrayList_[j] = text6 + "(";
    								arrayList_[j] = Operators.ConcatenateObject(Operators.ConcatenateObject(arrayList_[j], right3), ")");
    								if (!text6.StartsWith("<NEW>") || j <= 0 || !(arrayList_[j - 1] is string) || !Conversions.ToString(arrayList_[j - 1]).StartsWith("<LDLOCA>"))
    								{
    									continue;
    								}
    								if (Conversions.ToString(arrayList_[j - 1]).StartsWith("<LDLOCA>#"))
    								{
    									int num21 = methodDefinition_0.Body.Variables.Count - 1;
    									for (int num22 = 0; num22 <= num21; num22++)
    									{
    										if (Operators.CompareString(methodDefinition_0.Body.Variables[num22].Name, Conversions.ToString(arrayList_[j - 1]).Replace("<LDLOCA>", ""), TextCompare: false) == 0)
    										{
    											methodDefinition_0.Body.Variables[num22].Name = methodDefinition_0.Body.Variables[num22].Name.Trim('#');
    											arrayList_[j - 1] = $"<DECLARE={methodDefinition_0.Body.Variables[num22].Index}<END> = {RuntimeHelpers.GetObjectValue(arrayList_[j])}<LINEEND>";
    											arrayList_.RemoveAt(j);
    											j--;
    											break;
    										}
    									}
    								}
    								else
    								{
    									arrayList_[j - 1] = $"{RuntimeHelpers.GetObjectValue(arrayList_[j - 1])} = {RuntimeHelpers.GetObjectValue(arrayList_[j])}<LINEEND>";
    									arrayList_.RemoveAt(j);
    									j--;
    								}
    								continue;
    							}
    							case Code.Ret:
    								if (methodDefinition_0.ReturnType != null && Operators.CompareString(methodDefinition_0.ReturnType.FullName, "System.Void", TextCompare: false) != 0)
    								{
    									smethod_15(arrayList_, j - 1, methodDefinition_0.ReturnType);
    									smethod_20(arrayList_, ref j, "<RETURN> {value1}", null, 0, methodDefinition_0);
    								}
    								else
    								{
    									arrayList_[j] = "<RETURN>";
    								}
    								continue;
    							case Code.Br_S:
    							case Code.Br:
    							{
    								if (arrayList_[j] is Instruction && ((Instruction)arrayList_[j]).Operand == arrayList_[j])
    								{
    									continue;
    								}
    								if (j + 1 < arrayList_.Count)
    								{
    									object objectValue2 = RuntimeHelpers.GetObjectValue(arrayList_[j + 1]);
    									int num4 = j;
    									int num5 = arrayList_.Count - 1;
    									for (int l = num4; l <= num5; l++)
    									{
    										if (arrayList_[l] is Instruction)
    										{
    											Instruction instruction6 = (Instruction)arrayList_[l];
    											if (instruction6.Operand == objectValue2 && !((instruction6.OpCode.Code == Code.Br) | (instruction6.OpCode.Code == Code.Br_S)))
    											{
    												goto IL_3bca;
    											}
    										}
    									}
    								}
    								num2++;
    								string text4 = $"label{num2}";
    								smethod_20(arrayList_, ref j, "<GOTO> {value}", text4, 0, methodDefinition_0);
    								if (((Instruction)instruction5.Operand).Operand == instruction5.Operand)
    								{
    									arrayList_[arrayList_.IndexOf(RuntimeHelpers.GetObjectValue(instruction5.Operand))] = $"{text4}:";
    								}
    								else
    								{
    									arrayList_.Insert(arrayList_.IndexOf(RuntimeHelpers.GetObjectValue(instruction5.Operand)), $"{text4}:");
    								}
    								continue;
    							}
    							case Code.Brfalse_S:
    							case Code.Brfalse:
    								if (Conversions.ToString(arrayList_[j - 1]).StartsWith("(") && Conversions.ToString(arrayList_[j - 1]).EndsWith(")"))
    								{
    									arrayList_[j - 1] = Conversions.ToString(arrayList_[j - 1]).Substring(1, Conversions.ToString(arrayList_[j - 1]).Length - 2);
    								}
    								arrayList_[j - 1] = smethod_22(smethod_22(Conversions.ToString(arrayList_[j - 1])));
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value1}");
    								continue;
    							case Code.Brtrue_S:
    							case Code.Brtrue:
    								if (Conversions.ToString(arrayList_[j - 1]).StartsWith("(") && Conversions.ToString(arrayList_[j - 1]).EndsWith(")"))
    								{
    									arrayList_[j - 1] = Conversions.ToString(arrayList_[j - 1]).Substring(1, Conversions.ToString(arrayList_[j - 1]).Length - 2);
    								}
    								arrayList_[j - 1] = smethod_22(Conversions.ToString(arrayList_[j - 1]));
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value1}");
    								continue;
    							case Code.Beq_S:
    							case Code.Beq:
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value2} <NOTEQUALS> {value1}");
    								continue;
    							case Code.Bne_Un_S:
    							case Code.Bne_Un:
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value2} <EQUALS> {value1}");
    								continue;
    							case Code.Bge_S:
    							case Code.Bge_Un_S:
    							case Code.Bge:
    							case Code.Bge_Un:
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value2} <LESS> {value1}");
    								continue;
    							case Code.Bgt_S:
    							case Code.Bgt_Un_S:
    							case Code.Bgt:
    							case Code.Bgt_Un:
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value2} <LESSOREQUAL> {value1}");
    								continue;
    							case Code.Ble_S:
    							case Code.Ble_Un_S:
    							case Code.Ble:
    							case Code.Ble_Un:
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value2} <GREATER> {value1}");
    								continue;
    							case Code.Blt_S:
    							case Code.Blt_Un_S:
    							case Code.Blt:
    							case Code.Blt_Un:
    								smethod_24(ref arrayList_, ref j, methodDefinition_0, "{value2} <GREATEROREQUAL> {value1}");
    								continue;
    							case Code.Switch:
    							{
    								smethod_20(arrayList_, ref j, "<SELECTCASESTART> {value1} <SELECTCASESTARTMARKER>", null, 0, methodDefinition_0);
    								int num23 = j;
    								Instruction[] array6 = (Instruction[])instruction5.Operand;
    								Instruction[] array7 = array6;
    								foreach (Instruction value in array7)
    								{
    									int num25 = arrayList_.IndexOf(value);
    									if (arrayList_[num25 - 1] is Instruction && ((((Instruction)arrayList_[num25 - 1]).OpCode.Code == Code.Br) | (((Instruction)arrayList_[num25 - 1]).OpCode.Code == Code.Br_S)))
    									{
    										num23 = Math.Max(arrayList_.IndexOf(RuntimeHelpers.GetObjectValue(((Instruction)arrayList_[num25 - 1]).Operand)), num23);
    									}
    								}
    								object objectValue3 = RuntimeHelpers.GetObjectValue(arrayList_[num23]);
    								if (arrayList_[j + 1] is Instruction && ((((Instruction)arrayList_[j + 1]).OpCode.Code == Code.Br) | (((Instruction)arrayList_[j + 1]).OpCode.Code == Code.Br_S)))
    								{
    									int num26 = arrayList_.IndexOf(RuntimeHelpers.GetObjectValue(((Instruction)arrayList_[j + 1]).Operand));
    									if (num23 > num26)
    									{
    										arrayList_.Insert(num26, "<CASEELSE><CASEMARKER>");
    										arrayList_.RemoveAt(j + 1);
    										if (arrayList_[num26 - 1] is Instruction && ((((Instruction)arrayList_[num26 - 1]).OpCode.Code == Code.Br) | (((Instruction)arrayList_[num26 - 1]).OpCode.Code == Code.Br_S)))
    										{
    											num23 = Math.Max(num23, arrayList_.IndexOf(RuntimeHelpers.GetObjectValue(((Instruction)arrayList_[num26 - 1]).Operand)));
    										}
    									}
    									else
    									{
    										num23 = num26;
    										objectValue3 = RuntimeHelpers.GetObjectValue(arrayList_[num23]);
    										arrayList_.RemoveAt(j + 1);
    										num23--;
    									}
    								}
    								arrayList_.Insert(num23, "<SELECTCASEEND>");
    								int num27 = j + 1;
    								int num28 = num23;
    								for (int num29 = num27; num29 <= num28; num29++)
    								{
    									if (arrayList_[num29] is Instruction && ((((Instruction)arrayList_[num29]).OpCode.Code == Code.Br) | (((Instruction)arrayList_[num29]).OpCode.Code == Code.Br_S)) && ((Instruction)arrayList_[num29]).Operand == objectValue3)
    									{
    										arrayList_[num29] = "<CASEEXIT>";
    									}
    								}
    								Instruction[] array8 = array6;
    								foreach (Instruction value2 in array8)
    								{
    									int num31 = arrayList_.IndexOf(value2);
    									if (num31 >= 0 && num31 < num23)
    									{
    										arrayList_.Insert(num31, "<CASE> " + Conversions.ToString(num) + "<CASEMARKER>");
    										num23++;
    									}
    									num++;
    								}
    								continue;
    							}
    							case Code.Ldind_I1:
    							case Code.Ldind_U1:
    							case Code.Ldind_I2:
    							case Code.Ldind_U2:
    							case Code.Ldind_I4:
    							case Code.Ldind_U4:
    							case Code.Ldind_I8:
    							case Code.Ldind_I:
    							case Code.Ldind_R4:
    							case Code.Ldind_R8:
    							case Code.Ldind_Ref:
    								arrayList_[j - 1] = Operators.ConcatenateObject("<OBJECTREF>", arrayList_[j - 1]);
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							case Code.Div:
    							case Code.Div_Un:
    								smethod_20(arrayList_, ref j, "({value2} / {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Rem:
    							case Code.Rem_Un:
    								smethod_20(arrayList_, ref j, "({value2} <MOD> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.And:
    								smethod_16(arrayList_, j - 1, j - 2);
    								smethod_20(arrayList_, ref j, "({value2} <AND> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Or:
    								smethod_16(arrayList_, j - 1, j - 2);
    								smethod_20(arrayList_, ref j, "({value2} <OR> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Xor:
    								smethod_16(arrayList_, j - 1, j - 2);
    								smethod_20(arrayList_, ref j, "({value2} <XOR> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Shl:
    								smethod_20(arrayList_, ref j, "({value2} << {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Shr:
    							case Code.Shr_Un:
    								smethod_20(arrayList_, ref j, "({value2} >> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Neg:
    								smethod_20(arrayList_, ref j, "({value1} * -1)", null, 0, methodDefinition_0);
    								continue;
    							case Code.Not:
    								smethod_14(arrayList_, j - 1);
    								smethod_20(arrayList_, ref j, "{value1}", null, 0, methodDefinition_0);
    								smethod_20(arrayList_, ref j, "{value}", smethod_22(Conversions.ToString(arrayList_[j])), 0, methodDefinition_0);
    								continue;
    							case Code.Callvirt:
    							{
    								MethodReference methodReference2 = (MethodReference)instruction5.Operand;
    								PropertyDefinition propertyDefinition = null;
    								EventDefinition eventDefinition = null;
    								if (methodReference2.Name.Contains("_"))
    								{
    									TypeDefinition typeDefinition2 = methodReference2.DeclaringType.Resolve();
    									if (typeDefinition2 != null)
    									{
    										foreach (PropertyDefinition property2 in typeDefinition2.Properties)
    										{
    											if (property2.SetMethod != null && Operators.CompareString(property2.SetMethod.Name, methodReference2.Name, TextCompare: false) == 0)
    											{
    												propertyDefinition = property2;
    											}
    											if (property2.GetMethod != null && Operators.CompareString(property2.GetMethod.Name, methodReference2.Name, TextCompare: false) == 0)
    											{
    												propertyDefinition = property2;
    											}
    										}
    										foreach (EventDefinition event2 in typeDefinition2.Events)
    										{
    											if (event2.AddMethod != null && Operators.CompareString(event2.AddMethod.FullName, methodReference2.FullName, TextCompare: false) == 0)
    											{
    												eventDefinition = event2;
    											}
    											if (event2.RemoveMethod != null && Operators.CompareString(event2.RemoveMethod.FullName, methodReference2.FullName, TextCompare: false) == 0)
    											{
    												eventDefinition = event2;
    											}
    										}
    									}
    								}
    								if (propertyDefinition != null)
    								{
    									arrayList_[j] = "." + LanguageWriter.GetReferenceKey(propertyDefinition) + propertyDefinition.Name + "<<>>";
    									if (propertyDefinition.Parameters.Count > 0)
    									{
    										int int_1 = 0;
    										if (propertyDefinition.SetMethod != null && Operators.CompareString(propertyDefinition.SetMethod.Name, methodReference2.Name, TextCompare: false) == 0)
    										{
    											int_1 = 1;
    										}
    										string right = smethod_18(propertyDefinition.Parameters, arrayList_, ref j, int_1);
    										arrayList_[j] = Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(arrayList_[j], "("), right), ")");
    									}
    									if (propertyDefinition.SetMethod != null && Operators.CompareString(propertyDefinition.SetMethod.Name, methodReference2.Name, TextCompare: false) == 0)
    									{
    										smethod_15(arrayList_, j - 1, propertyDefinition.PropertyType);
    										arrayList_[j] = Operators.ConcatenateObject(Operators.ConcatenateObject(arrayList_[j], " = "), arrayList_[j - 1]);
    										arrayList_.RemoveAt(j - 1);
    										j--;
    									}
    								}
    								else if (methodReference2.DeclaringType.IsArray && Operators.CompareString(methodReference2.Name, "Set", TextCompare: false) == 0)
    								{
    									smethod_15(arrayList_, j - 1, methodReference2.Parameters[methodReference2.Parameters.Count - 1].ParameterType);
    									arrayList_.RemoveAt(j);
    									j--;
    									string text = "";
    									for (int k = methodReference2.Parameters.Count - 2; k >= 0; k += -1)
    									{
    										smethod_15(arrayList_, j - 1, methodReference2.Parameters[k].ParameterType);
    										if (methodReference2.Parameters[k].IsOut)
    										{
    											text += "<OUT>";
    										}
    										else if (methodReference2.Parameters[k].ParameterType.IsByReference)
    										{
    											text += "<REF>";
    										}
    										text = Conversions.ToString(Operators.ConcatenateObject(arrayList_[j - 1], ", " + text));
    										arrayList_.RemoveAt(j - 1);
    										j--;
    									}
    									text = text.TrimEnd(' ', ',');
    									arrayList_[j] = Operators.ConcatenateObject(string.Concat("<ARRAYBRACKETOPEN>" + text, "<ARRAYBRACKETCLOSE> = "), arrayList_[j]);
    								}
    								else if (methodReference2.DeclaringType.IsArray && Operators.CompareString(methodReference2.Name, "Get", TextCompare: false) == 0)
    								{
    									string text2 = smethod_18(methodReference2.Parameters, arrayList_, ref j, 0);
    									arrayList_[j] = "(" + text2 + ")";
    								}
    								else if (eventDefinition != null)
    								{
    									string arg3 = smethod_18(methodReference2.Parameters, arrayList_, ref j, 0);
    									string arg4 = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(arrayList_[j - 1], "."), LanguageWriter.GetReferenceKey(eventDefinition)), eventDefinition.Name), "<<>>"));
    									arrayList_[j - 1] = "";
    									if (Operators.CompareString(methodReference2.FullName, eventDefinition.AddMethod.FullName, TextCompare: false) == 0)
    									{
    										arrayList_[j] = $"<ADDHANDLER1>{arg4}<ADDHANDLER2>{arg3}";
    									}
    									else
    									{
    										arrayList_[j] = $"<REMOVEHANDLER1>{arg4}<REMOVEHANDLER2>{arg3}";
    									}
    								}
    								else
    								{
    									string text3 = smethod_18(methodReference2.Parameters, arrayList_, ref j, 0);
    									arrayList_[j] = "." + LanguageWriter.GetReferenceKey(methodReference2) + methodReference2.Name + "<<>>(" + text3 + ")";
    								}
    								arrayList_[j] = Operators.AddObject(arrayList_[j - 1], arrayList_[j]);
    								arrayList_.RemoveAt(j - 1);
    								j--;
    								continue;
    							}
    							case Code.Cpobj:
    								smethod_20(arrayList_, ref j, "<OBJECTREF>{value2} = <OBJECTREF>{value1}", null, 0, methodDefinition_0);
    								continue;
    							case Code.Ldstr:
    								if (Conversions.ToString(instruction5.Operand).Contains("\\"))
    								{
    									arrayList_[j] = $"<<r>><PLAINSTRING>\"{RuntimeHelpers.GetObjectValue(instruction5.Operand)}\"<<>>";
    								}
    								else
    								{
    									arrayList_[j] = $"<<r>>\"{RuntimeHelpers.GetObjectValue(instruction5.Operand)}\"<<>>";
    								}
    								continue;
    							case Code.Newobj:
    							{
    								MethodReference methodReference = (MethodReference)instruction5.Operand;
    								TypeDefinition typeDefinition = methodReference.DeclaringType.Resolve();
    								if (typeDefinition != null && typeDefinition.BaseType != null && typeDefinition.BaseType.FullName.EndsWith("Delegate"))
    								{
    									arrayList_[j] = $"<NEW> {smethod_29(methodReference.DeclaringType)}({RuntimeHelpers.GetObjectValue(arrayList_[j - 1])})";
    									arrayList_.RemoveAt(j - 1);
    									j--;
    								}
    								else if (methodReference.DeclaringType.IsArray)
    								{
    									string arg = smethod_18(methodReference.Parameters, arrayList_, ref j, 0);
    									arrayList_[j] = $"<NEW> {smethod_29(methodReference.DeclaringType.GetElementType())}<ARRAYBRACKETOPEN>{arg}<ARRAYBRACKETCLOSE>";
    								}
    								else
    								{
    									string arg2 = smethod_18(methodReference.Parameters, arrayList_, ref j, 0);
    									arrayList_[j] = $"<NEW> {smethod_29(methodReference.DeclaringType)}({arg2})";
    								}
    								continue;
    							}
    							case Code.Isinst:
    								smethod_20(arrayList_, ref j, "(<TYPEOF>{value1} <IS> {value})", ((TypeReference)instruction5.Operand).FullName, 0, methodDefinition_0);
    								continue;
    							case Code.Throw:
    								smethod_20(arrayList_, ref j, "<THROW> {value1}", null, 0, methodDefinition_0);
    								continue;
    							case Code.Ldfld:
    								smethod_20(arrayList_, ref j, "{value1}.{value}", LanguageWriter.GetReferenceKey(RuntimeHelpers.GetObjectValue(instruction5.Operand)) + ((FieldReference)instruction5.Operand).Name + "<<>>", 0, methodDefinition_0);
    								continue;
    							case Code.Ldflda:
    								smethod_20(arrayList_, ref j, "{value1}.{value}", LanguageWriter.GetReferenceKey(RuntimeHelpers.GetObjectValue(instruction5.Operand)) + ((FieldReference)instruction5.Operand).Name + "<<>>", 0, methodDefinition_0);
    								continue;
    							case Code.Stfld:
    								smethod_15(arrayList_, j - 1, ((FieldReference)instruction5.Operand).FieldType);
    								smethod_20(arrayList_, ref j, "{value2}.{value} = {value1}", ((FieldReference)instruction5.Operand).Name, 0, methodDefinition_0);
    								continue;
    							case Code.Ldsfld:
    							case Code.Ldsflda:
    								if (Operators.CompareString(((FieldReference)instruction5.Operand).DeclaringType.FullName, methodDefinition_0.DeclaringType.FullName, TextCompare: false) == 0)
    								{
    									arrayList_[j] = "<ME>";
    								}
    								else if (methodDefinition_0.DeclaringType.BaseType != null && Operators.CompareString(((FieldReference)instruction5.Operand).DeclaringType.FullName, methodDefinition_0.DeclaringType.BaseType.FullName, TextCompare: false) == 0)
    								{
    									arrayList_[j] = "<MYBASE>";
    								}
    								else
    								{
    									arrayList_[j] = ((FieldReference)instruction5.Operand).DeclaringType.FullName;
    								}
    								arrayList_[j] = string.Format("{0}.{1}", RuntimeHelpers.GetObjectValue(arrayList_[j]), LanguageWriter.GetReferenceKey(RuntimeHelpers.GetObjectValue(instruction5.Operand)) + ((FieldReference)instruction5.Operand).Name + "<<>>");
    								continue;
    							case Code.Stsfld:
    								smethod_15(arrayList_, j - 1, ((FieldReference)instruction5.Operand).FieldType);
    								if (Operators.CompareString(((FieldDefinition)instruction5.Operand).DeclaringType.FullName, methodDefinition_0.DeclaringType.FullName, TextCompare: false) == 0)
    								{
    									arrayList_[j] = "<ME>";
    								}
    								else if (methodDefinition_0.DeclaringType.BaseType != null && Operators.CompareString(((FieldReference)instruction5.Operand).DeclaringType.FullName, methodDefinition_0.DeclaringType.BaseType.FullName, TextCompare: false) == 0)
    								{
    									arrayList_[j] = "<MYBASE>";
    								}
    								else
    								{
    									arrayList_[j] = smethod_29(((FieldDefinition)instruction5.Operand).DeclaringType);
    								}
    								arrayList_[j] = string.Format("{0}.{1}", RuntimeHelpers.GetObjectValue(arrayList_[j]), LanguageWriter.GetReferenceKey((FieldReference)instruction5.Operand) + ((FieldReference)instruction5.Operand).Name + "<<>>");
    								smethod_20(arrayList_, ref j, "{value} = {value1}", Conversions.ToString(arrayList_[j]), 0, methodDefinition_0);
    								continue;
    							case Code.Box:
    								smethod_20(arrayList_, ref j, "<CTYPE={value}<SEP>{value1}<END>", smethod_29((TypeReference)instruction5.Operand), 0, methodDefinition_0);
    								continue;
    							case Code.Newarr:
    							{
    								if (int.TryParse(Conversions.ToString(arrayList_[j - 1]), out var result))
    								{
    									arrayList_[j - 1] = Conversions.ToString(result - 1);
    								}
    								else
    								{
    									arrayList_[j - 1] = Operators.ConcatenateObject(arrayList_[j - 1], " - 1");
    								}
    								smethod_20(arrayList_, ref j, "<NEW> {value}<ARRAYBRACKETOPEN>{value1}<ARRAYBRACKETCLOSE> {}", smethod_29((TypeReference)instruction5.Operand), 0, methodDefinition_0);
    								continue;
    							}
    							case Code.Ldlen:
    								smethod_20(arrayList_, ref j, "{value1}.Length()", null, 0, methodDefinition_0);
    								continue;
    							case Code.Ldelema:
    								smethod_20(arrayList_, ref j, "{value2}<ARRAYBRACKETOPEN>{value1}<ARRAYBRACKETCLOSE>", null, 0, methodDefinition_0);
    								continue;
    							case Code.Ldelem_I1:
    							case Code.Ldelem_U1:
    							case Code.Ldelem_I2:
    							case Code.Ldelem_U2:
    							case Code.Ldelem_I4:
    							case Code.Ldelem_U4:
    							case Code.Ldelem_I8:
    							case Code.Ldelem_I:
    							case Code.Ldelem_R4:
    							case Code.Ldelem_R8:
    							case Code.Ldelem_Ref:
    							case Code.Ldelem_Any:
    								smethod_20(arrayList_, ref j, "{value2}<ARRAYBRACKETOPEN>{value1}<ARRAYBRACKETCLOSE>", null, 0, methodDefinition_0);
    								continue;
    							case Code.Stelem_I:
    							case Code.Stelem_I1:
    							case Code.Stelem_I2:
    							case Code.Stelem_I4:
    							case Code.Stelem_I8:
    							case Code.Stelem_R4:
    							case Code.Stelem_R8:
    							case Code.Stelem_Ref:
    							case Code.Stelem_Any:
    								smethod_20(arrayList_, ref j, "{value3}<ARRAYBRACKETOPEN>{value2}<ARRAYBRACKETCLOSE> = {value1}", null, 0, methodDefinition_0);
    								continue;
    							case Code.Castclass:
    							case Code.Unbox_Any:
    								smethod_20(arrayList_, ref j, "<DIRECTCAST={value}<SEP>{value1}<END>", smethod_29((TypeReference)instruction5.Operand), 0, methodDefinition_0);
    								continue;
    							case Code.Ldobj:
    							case Code.Ckfinite:
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							case Code.Ldtoken:
    								if (instruction5.Operand is TypeReference)
    								{
    									arrayList_[j] = ((TypeReference)instruction5.Operand).FullName;
    								}
    								else if (instruction5.Operand is MethodReference)
    								{
    									arrayList_[j] = ((MethodReference)instruction5.Operand).FullName;
    									arrayList_.RemoveAt(j - 1);
    									j--;
    								}
    								else if (instruction5.Operand is FieldReference)
    								{
    									arrayList_[j] = ((FieldReference)instruction5.Operand).FullName;
    									arrayList_.RemoveAt(j - 1);
    									j--;
    								}
    								continue;
    							case Code.Add:
    							case Code.Add_Ovf:
    							case Code.Add_Ovf_Un:
    								if (j + 1 < arrayList_.Count && arrayList_[j + 1] is Instruction && ((Instruction)arrayList_[j + 1]).OpCode.Code == Code.Switch)
    								{
    									smethod_20(arrayList_, ref j, "{value1}", null, 0, methodDefinition_0);
    									num = int.Parse(Conversions.ToString(arrayList_[j])) * -1;
    									arrayList_.RemoveAt(j);
    									j--;
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "({value2} + {value1})", null, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Mul:
    							case Code.Mul_Ovf:
    							case Code.Mul_Ovf_Un:
    								smethod_20(arrayList_, ref j, "({value2} * {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Sub:
    							case Code.Sub_Ovf:
    							case Code.Sub_Ovf_Un:
    								if (j + 1 < arrayList_.Count && arrayList_[j + 1] is Instruction && ((Instruction)arrayList_[j + 1]).OpCode.Code == Code.Switch)
    								{
    									smethod_20(arrayList_, ref j, "{value1}", null, 0, methodDefinition_0);
    									num = int.Parse(Conversions.ToString(arrayList_[j]));
    									arrayList_.RemoveAt(j);
    									j--;
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "({value2} - {value1})", null, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Leave:
    							case Code.Leave_S:
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							case Code.Stind_Ref:
    							case Code.Stind_I1:
    							case Code.Stind_I2:
    							case Code.Stind_I4:
    							case Code.Stind_I8:
    							case Code.Stind_R4:
    							case Code.Stind_R8:
    							case Code.Stobj:
    							case Code.Stind_I:
    								smethod_20(arrayList_, ref j, "<OBJECTREF>{value2} = {value1}<LINEEND>", null, 0, methodDefinition_0);
    								continue;
    							case Code.Conv_I1:
    							case Code.Conv_I2:
    							case Code.Conv_I4:
    							case Code.Conv_I8:
    							case Code.Conv_R4:
    							case Code.Conv_R8:
    							case Code.Conv_U4:
    							case Code.Conv_U8:
    							case Code.Conv_R_Un:
    							case Code.Conv_Ovf_I1_Un:
    							case Code.Conv_Ovf_I2_Un:
    							case Code.Conv_Ovf_I4_Un:
    							case Code.Conv_Ovf_I8_Un:
    							case Code.Conv_Ovf_U1_Un:
    							case Code.Conv_Ovf_U2_Un:
    							case Code.Conv_Ovf_U4_Un:
    							case Code.Conv_Ovf_U8_Un:
    							case Code.Conv_Ovf_I_Un:
    							case Code.Conv_Ovf_U_Un:
    							case Code.Conv_Ovf_I1:
    							case Code.Conv_Ovf_U1:
    							case Code.Conv_Ovf_I2:
    							case Code.Conv_Ovf_U2:
    							case Code.Conv_Ovf_I4:
    							case Code.Conv_Ovf_U4:
    							case Code.Conv_Ovf_I8:
    							case Code.Conv_Ovf_U8:
    							case Code.Conv_U2:
    							case Code.Conv_U1:
    							case Code.Conv_I:
    							case Code.Conv_Ovf_I:
    							case Code.Conv_Ovf_U:
    							case Code.Conv_U:
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							case Code.Arglist:
    								arrayList_[j] = "__arglist";
    								continue;
    							case Code.Ceq:
    								smethod_16(arrayList_, j - 1, j - 2);
    								if (Operators.ConditionalCompareObjectEqual(arrayList_[j - 1], "<FALSE>", TextCompare: false))
    								{
    									arrayList_[j] = smethod_22(Conversions.ToString(arrayList_[j - 2]));
    									arrayList_.RemoveAt(j - 1);
    									arrayList_.RemoveAt(j - 2);
    									j -= 2;
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "({value2} <EQUALS> {value1})", null, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Cgt:
    							case Code.Cgt_Un:
    								smethod_20(arrayList_, ref j, "({value2} <GREATER> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Clt:
    							case Code.Clt_Un:
    								smethod_20(arrayList_, ref j, "({value2} <LESS> {value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Ldftn:
    							{
    								MethodReference methodReference4 = (MethodReference)instruction5.Operand;
    								arrayList_[j] = string.Format("<ADDRESSOF>{0}.{1}", RuntimeHelpers.GetObjectValue(arrayList_[j - 1]), LanguageWriter.GetReferenceKey(methodReference4) + methodReference4.Name + "<<>>");
    								arrayList_.RemoveAt(j - 1);
    								j--;
    								continue;
    							}
    							case Code.Ldvirtftn:
    							{
    								MethodReference methodReference3 = (MethodReference)instruction5.Operand;
    								arrayList_[j] = "." + LanguageWriter.GetReferenceKey(methodReference3) + methodReference3.Name + "<<>>";
    								arrayList_[j] = Operators.ConcatenateObject("<ADDRESSOF>", Operators.AddObject(arrayList_[j - 1], arrayList_[j]));
    								arrayList_.RemoveAt(j - 1);
    								j--;
    								continue;
    							}
    							case Code.Ldarg_S:
    							case Code.Ldarg:
    								arrayList_[j] = ((ParameterDefinition)instruction5.Operand).Name;
    								continue;
    							case Code.Ldarga_S:
    							case Code.Ldarga:
    								arrayList_[j] = ((ParameterDefinition)instruction5.Operand).Name;
    								continue;
    							case Code.Starg_S:
    							case Code.Starg:
    								smethod_20(arrayList_, ref j, "{value} = {value1}<LINEEND>", ((ParameterDefinition)instruction5.Operand).Name, 0, methodDefinition_0);
    								continue;
    							case Code.Ldloc_S:
    							case Code.Ldloc:
    								if (!smethod_25(ref arrayList_, ref j, ref methodDefinition_0))
    								{
    									arrayList_[j] = ((VariableDefinition)instruction5.Operand).Name;
    								}
    								continue;
    							case Code.Ldloca_S:
    							case Code.Ldloca:
    								if (!smethod_25(ref arrayList_, ref j, ref methodDefinition_0))
    								{
    									arrayList_[j] = "<LDLOCA>" + ((VariableDefinition)instruction5.Operand).Name;
    								}
    								continue;
    							case Code.Stloc_S:
    							case Code.Stloc:
    								smethod_15(arrayList_, j - 1, ((VariableDefinition)instruction5.Operand).VariableType);
    								if (((VariableDefinition)instruction5.Operand).Name.StartsWith("#"))
    								{
    									((VariableDefinition)instruction5.Operand).Name = ((VariableDefinition)instruction5.Operand).Name.Substring(1);
    									smethod_20(arrayList_, ref j, "<DECLARE={value}<END> = {value1}<LINEEND>", Conversions.ToString(((VariableDefinition)instruction5.Operand).Index), 0, methodDefinition_0);
    								}
    								else
    								{
    									smethod_20(arrayList_, ref j, "{value} = {value1}<LINEEND>", ((VariableDefinition)instruction5.Operand).Name, 0, methodDefinition_0);
    								}
    								continue;
    							case Code.Localloc:
    								smethod_20(arrayList_, ref j, "System.Runtime.InteropServices.Marshal.AllocCoTaskMem({value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Endfinally:
    							case Code.Endfilter:
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							case Code.Initobj:
    								smethod_20(arrayList_, ref j, "{value1} = <NEW> {value}()", smethod_29((TypeReference)instruction5.Operand), 0, methodDefinition_0);
    								continue;
    							case Code.Cpblk:
    								smethod_20(arrayList_, ref j, "System.Runtime.InteropServices.Marshal.Copy({value2},0,{value3},{value1})", null, 0, methodDefinition_0);
    								continue;
    							case Code.Initblk:
    								smethod_20(arrayList_, ref j, "{value1}", null, 0, methodDefinition_0);
    								smethod_20(arrayList_, ref j, "{value2} = {value1}", null, 0, methodDefinition_0);
    								continue;
    							case Code.Rethrow:
    								smethod_20(arrayList_, ref j, "<RETHROW>", null, 0, methodDefinition_0);
    								continue;
    							case Code.Sizeof:
    								smethod_20(arrayList_, ref j, "Len(New {value}())", ((TypeReference)instruction5.Operand).FullName, 0, methodDefinition_0);
    								continue;
    							case Code.Refanyval:
    							case Code.Mkrefany:
    							case Code.Refanytype:
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							case Code.Nop:
    							case Code.Break:
    							case Code.Unbox:
    							case Code.Unaligned:
    							case Code.Volatile:
    							case Code.Tail:
    							case Code.Constrained:
    							case Code.Readonly:
    								arrayList_.RemoveAt(j);
    								j--;
    								continue;
    							}
    							string arg6 = "";
    							if (instruction5.Operand == null)
    							{
    								arg6 = "";
    							}
    							else if (instruction5.Operand is string)
    							{
    								arg6 = ((!Conversions.ToString(instruction5.Operand).Contains("\\")) ? Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("<<r>>\"", instruction5.Operand), "\"<<>>")) : Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("<<r>><PLAINSTRING>\"", instruction5.Operand), "\"<<>>")));
    							}
    							else if (instruction5.Operand is Instruction)
    							{
    								arg6 = LanguageWriter.FormatIlLabel(((Instruction)instruction5.Operand).Offset);
    							}
    							else if (instruction5.Operand is MethodDefinition)
    							{
    								arg6 = ((MethodDefinition)instruction5.Operand).DeclaringType.Namespace + "." + ((MethodDefinition)instruction5.Operand).DeclaringType.Name + "::" + ((MethodDefinition)instruction5.Operand).Name;
    							}
    							else if (instruction5.Operand is MethodReference)
    							{
    								arg6 = ((MethodReference)instruction5.Operand).DeclaringType.Namespace + "." + ((MethodReference)instruction5.Operand).DeclaringType.Name + "::" + ((MethodReference)instruction5.Operand).Name;
    							}
    							else if (instruction5.Operand is TypeDefinition)
    							{
    								arg6 = ((TypeDefinition)instruction5.Operand).Namespace + "." + ((TypeDefinition)instruction5.Operand).Name;
    							}
    							else if (instruction5.Operand is TypeReference)
    							{
    								arg6 = ((TypeReference)instruction5.Operand).Namespace + "." + ((TypeReference)instruction5.Operand).Name;
    							}
    							else if (instruction5.Operand is VariableDefinition)
    							{
    								arg6 = ((VariableDefinition)instruction5.Operand).Name;
    							}
    							else if (instruction5.Operand is FieldDefinition)
    							{
    								arg6 = ((FieldDefinition)instruction5.Operand).Name;
    							}
    							else if (instruction5.Operand is FieldReference)
    							{
    								arg6 = ((FieldReference)instruction5.Operand).Name;
    							}
    							else if (!(instruction5.Operand is Instruction[]))
    							{
    								arg6 = ((!(instruction5.Operand is ParameterDefinition)) ? Conversions.ToString(instruction5.Operand) : ((ParameterDefinition)instruction5.Operand).Name);
    							}
    							else
    							{
    								Instruction[] array9 = (Instruction[])instruction5.Operand;
    								foreach (Instruction instruction7 in array9)
    								{
    									stringBuilder.Append(LanguageWriter.FormatIlLabel(instruction7.Offset) + ", ");
    								}
    								stringBuilder.Remove(stringBuilder.Length - 2, 2);
    							}
    							arrayList_[j] = $"{LanguageWriter.FormatIlLabel(instruction5.Offset)}: {instruction5.OpCode} {arg6}";
    						}
    						else if (arrayList_[j] is string)
    						{
    							if (Operators.CompareString(Conversions.ToString(arrayList_[j]), "<<IIFEND>>", TextCompare: false) == 0)
    							{
    								smethod_20(arrayList_, ref j, "{value3}{value2}<IIFPART2>{value1}<IIFCLOSE>", null, 0, methodDefinition_0);
    							}
    							if (Conversions.ToString(arrayList_[j]).StartsWith("<<MOVEUP>><NOT>"))
    							{
    								smethod_20(arrayList_, ref j, "(<NOT>{value1}{value}<NOTEND>)", Conversions.ToString(arrayList_[j]).Replace("<<MOVEUP>><NOT>", ""), 0, methodDefinition_0);
    							}
    							else if (Conversions.ToString(arrayList_[j]).StartsWith("<<MOVEUP>>("))
    							{
    								smethod_20(arrayList_, ref j, "({value1}{value}", Conversions.ToString(arrayList_[j]).Replace("<<MOVEUP>>(", ""), 0, methodDefinition_0);
    							}
    							else if (Conversions.ToString(arrayList_[j]).StartsWith("<<MOVEUP>>"))
    							{
    								smethod_20(arrayList_, ref j, "{value1}{value}", Conversions.ToString(arrayList_[j]).Replace("<<MOVEUP>>", ""), 0, methodDefinition_0);
    							}
    							else
    							{
    								Conversions.ToString(arrayList_[j]).StartsWith("<<FOREACH>>");
    							}
    						}
    						IL_3bca:;
    					}
    				}
    				finally
    				{
    					if (obj2 != null)
    					{
    						((IDisposable)obj2).Dispose();
    					}
    				}
    				if (arrayList_.Count > 0 && arrayList_[arrayList_.Count - 1] is string && Operators.ConditionalCompareObjectEqual(arrayList_[arrayList_.Count - 1], "<RETURN>", TextCompare: false))
    				{
    					arrayList_.RemoveAt(arrayList_.Count - 1);
    				}
    				for (int num33 = arrayList_.Count - 1; num33 >= 0; num33 += -1)
    				{
    					if (!(arrayList_[num33] is string))
    					{
    						if (arrayList_[num33] is Instruction && ((Instruction)arrayList_[num33]).Operand == arrayList_[num33])
    						{
    							arrayList_.RemoveAt(num33);
    						}
    						else
    						{
    							arrayList_[num33] = "?";
    						}
    					}
    					else if (Operators.ConditionalCompareObjectEqual(arrayList_[num33], "", TextCompare: false))
    					{
    						arrayList_.RemoveAt(num33);
    					}
    				}
    				int num34 = arrayList_.Count - 1;
    				for (int num35 = 0; num35 <= num34; num35++)
    				{
    					string text7 = Conversions.ToString(arrayList_[num35]);
    					if (num35 + 1 < arrayList_.Count)
    					{
    						Conversions.ToString(arrayList_[num35 + 1]);
    					}
    					if (text7.Contains("<SELECTCASEEND>") || text7.Contains("<SELECTCASESTARTMARKER>") || text7.Contains("<CASEMARKER>") || text7.Contains("<FINALLYSTART>") || text7.Contains("<FINALLYEND>") || text7.Contains("<TRYSTART>") || text7.Contains("<TRYEND>") || text7.Contains("<CATCHSTART=") || text7.Contains("<CATCHEND>") || text7.Contains("<IFOPEN>") || text7.Contains("<IFCLOSE>") || text7.Contains("<LOOPBRACKETOPEN>") || text7.Contains("<ELSE>") || text7.Contains("<LOOPBRACKETCLOSE>") || text7.Contains("<LINEEND>"))
    					{
    						continue;
    					}
    					if (text7.Contains("<FOREACHSTART="))
    					{
    						if (num35 > 0)
    						{
    							arrayList_[num35 - 1] = Conversions.ToString(arrayList_[num35 - 1]).Replace("<LINEEND>", "");
    						}
    					}
    					else if (!text7.Contains("<FOREACHEND>") && !text7.EndsWith(":"))
    					{
    						arrayList_[num35] = text7 + "<LINEEND>";
    					}
    				}
    				int num36 = int_0;
    				bool flag = false;
    				foreach (object item2 in arrayList_)
    				{
    					object objectValue4 = RuntimeHelpers.GetObjectValue(item2);
    					if (!string.IsNullOrEmpty(Conversions.ToString(objectValue4)) && !string.IsNullOrEmpty(Conversions.ToString(objectValue4).Trim()))
    					{
    						string text8 = Conversions.ToString(objectValue4);
    						if (text8.StartsWith("<ELSE>") | text8.StartsWith("<ELSEIF>"))
    						{
    							num36--;
    						}
    						if (text8.StartsWith("<LOOPBRACKETCLOSE>"))
    						{
    							num36--;
    						}
    						if (text8.StartsWith("<TRYEND>"))
    						{
    							num36--;
    						}
    						if (text8.StartsWith("<FINALLYEND>"))
    						{
    							num36--;
    						}
    						if (text8.StartsWith("<CATCHEND>"))
    						{
    							num36--;
    						}
    						if (text8.StartsWith("<CASEELSE>"))
    						{
    							num36--;
    						}
    						if (text8.StartsWith("<FOREACHEND>"))
    						{
    							num36--;
    						}
    						if (text8.Contains("<SELECTCASEEND>"))
    						{
    							num36--;
    						}
    						if (text8.Contains("<SELECTCASEEND>") & flag)
    						{
    							num36--;
    						}
    						if (text8.Contains("<SELECTCASEEND>"))
    						{
    							flag = false;
    						}
    						if (text8.StartsWith("<CASE>") & flag)
    						{
    							num36--;
    						}
    						if (Operators.CompareString(text8, "<IFCLOSE>", TextCompare: false) == 0)
    						{
    							num36--;
    						}
    						stringBuilder.AppendLine("    ".PadLeft(Math.Max(0, num36) * 4) + text8);
    						if (text8.StartsWith("<ELSE>") | text8.StartsWith("<ELSEIF>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<CASEELSE>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<CASE>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<CASE>"))
    						{
    							flag = true;
    						}
    						if (text8.Contains("<SELECTCASESTART>"))
    						{
    							num36++;
    						}
    						if (text8.EndsWith("<LOOPBRACKETOPEN>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<IF>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<CATCHSTART="))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<FINALLYSTART>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<TRYSTART>"))
    						{
    							num36++;
    						}
    						if (text8.StartsWith("<FOREACHSTART="))
    						{
    							num36++;
    						}
    					}
    				}
    			}
    			catch (Exception projectError)
    			{
    				ProjectData.SetProjectError(projectError);
    				stringBuilder.AppendLine("<<g>> <COMMENT> Obfuscated Code<<>>");
    				ProjectData.ClearProjectError();
    			}
    			return stringBuilder.ToString();
    		}
    	}

    	private object method_0(ref ArrayList arrayList_0, int int_0, int int_1)
    	{
    		checked
    		{
    			while (int_0 - int_1 > 0 && int_1 > 0)
    			{
    				int_0--;
    				if (!(arrayList_0[int_0] is Instruction) || !(((Instruction)arrayList_0[int_0]).OpCode == OpCodes.Br))
    				{
    					int_1--;
    				}
    			}
    			return int_0;
    		}
    	}

    	private static void smethod_20(ArrayList arrayList_0, ref int int_0, string string_0, string string_1, int int_1, MethodDefinition methodDefinition_0)
    	{
    		if (string_0.Contains("{value}"))
    		{
    			string_0 = string_0.Replace("{value}", string_1);
    		}
    		checked
    		{
    			if (string_0.Contains("{value1}"))
    			{
    				int_0--;
    				while (!(arrayList_0[int_0] is string) && int_0 > 0)
    				{
    					arrayList_0.RemoveAt(int_0);
    					int_0--;
    				}
    				string_0 = string_0.Replace("{value1}", Conversions.ToString(arrayList_0[int_0]));
    				arrayList_0.RemoveAt(int_0);
    			}
    			if (string_0.Contains("{value2}"))
    			{
    				int_0--;
    				while (!(arrayList_0[int_0] is string) && int_0 > 0)
    				{
    					arrayList_0.RemoveAt(int_0);
    					int_0--;
    				}
    				string_0 = string_0.Replace("{value2}", Conversions.ToString(arrayList_0[int_0]));
    				arrayList_0.RemoveAt(int_0);
    			}
    			if (string_0.Contains("{value3}"))
    			{
    				int_0--;
    				while (!(arrayList_0[int_0] is string) && int_0 > 0)
    				{
    					arrayList_0.RemoveAt(int_0);
    					int_0--;
    				}
    				string_0 = string_0.Replace("{value3}", Conversions.ToString(arrayList_0[int_0]));
    				arrayList_0.RemoveAt(int_0);
    			}
    			if (string_0.Contains("{params}"))
    			{
    				string text = "";
    				for (int i = int_1; i >= 1; i += -1)
    				{
    					text = Conversions.ToString(Operators.ConcatenateObject(text + ", ", arrayList_0[int_0 - i]));
    					arrayList_0.RemoveAt(int_0 - i);
    					int_0--;
    				}
    				if (text.Length > 0)
    				{
    					text = text.Substring(2);
    				}
    				string_0 = string_0.Replace("{params}", text);
    			}
    			arrayList_0[int_0] = string_0;
    		}
    	}

    	private static bool smethod_21(ref string string_0, string string_1, string string_2)
    	{
    		if (!string_0.Contains(string_1) && !string_0.Contains(string_2))
    		{
    			return false;
    		}
    		string_0 = string_0.Replace(string_1, "<PLOP>");
    		string_0 = string_0.Replace(string_2, string_1);
    		string_0 = string_0.Replace("<PLOP>", string_2);
    		return true;
    	}

    	private static string smethod_22(string string_0)
    	{
    		if (Operators.CompareString(string_0, "0", TextCompare: false) == 0)
    		{
    			string_0 = "<FALSE>";
    		}
    		if (Operators.CompareString(string_0, "1", TextCompare: false) == 0)
    		{
    			string_0 = "<TRUE>";
    		}
    		if ((0u | (smethod_21(ref string_0, "<NOT>", "<YES>") ? 1u : 0u) | (smethod_21(ref string_0, "<NOTEND>", "<YESEND>") ? 1u : 0u) | (smethod_21(ref string_0, "<GREATER>", "<LESSOREQUAL>") ? 1u : 0u) | (smethod_21(ref string_0, "<GREATEROREQUAL>", "<LESS>") ? 1u : 0u) | (smethod_21(ref string_0, "<EQUALS>", "<NOTEQUALS>") ? 1u : 0u) | (smethod_21(ref string_0, "<AND>", "<OR>") ? 1u : 0u) | (smethod_21(ref string_0, "<ANDALSO>", "<ORELSE>") ? 1u : 0u) | (smethod_21(ref string_0, "<ISNOTEQUALS>", "<ISEQUALS>") ? 1u : 0u)) == 0)
    		{
    			return "<NOT>" + string_0 + "<NOTEND>";
    		}
    		return string_0;
    	}

    	private static bool smethod_23(Instruction instruction_0)
    	{
    		return (instruction_0.OpCode.Code == Code.Br) | (instruction_0.OpCode.Code == Code.Br_S) | (instruction_0.OpCode.Code == Code.Leave) | (instruction_0.OpCode.Code == Code.Leave_S);
    	}

    	private static void smethod_24(ref ArrayList arrayList_0, ref int int_0, MethodDefinition methodDefinition_0, string string_0)
    	{
    		Instruction instruction = (Instruction)arrayList_0[int_0];
    		Monitor.Enter(staticLocalInitFlag_0);
    		try
    		{
    			if (staticLocalInitFlag_0.State == 0)
    			{
    				staticLocalInitFlag_0.State = 2;
    				bool_0 = false;
    			}
    			else if (staticLocalInitFlag_0.State == 2)
    			{
    				throw new IncompleteInitialization();
    			}
    		}
    		finally
    		{
    			staticLocalInitFlag_0.State = 1;
    			Monitor.Exit(staticLocalInitFlag_0);
    		}
    		bool flag = false;
    		bool flag2 = false;
    		Monitor.Enter(staticLocalInitFlag_1);
    		try
    		{
    			if (staticLocalInitFlag_1.State == 0)
    			{
    				staticLocalInitFlag_1.State = 2;
    				bool_1 = false;
    			}
    			else if (staticLocalInitFlag_1.State == 2)
    			{
    				throw new IncompleteInitialization();
    			}
    		}
    		finally
    		{
    			staticLocalInitFlag_1.State = 1;
    			Monitor.Exit(staticLocalInitFlag_1);
    		}
    		Monitor.Enter(_003C);
    		try
    		{
    			if (_003C.State == 0)
    			{
    				_003C.State = 2;
    				bool_2 = false;
    			}
    			else if (_003C.State == 2)
    			{
    				throw new IncompleteInitialization();
    			}
    		}
    		finally
    		{
    			_003C.State = 1;
    			Monitor.Exit(_003C);
    		}
    		Monitor.Enter(staticLocalInitFlag_2);
    		try
    		{
    			if (staticLocalInitFlag_2.State == 0)
    			{
    				staticLocalInitFlag_2.State = 2;
    				bool_3 = false;
    			}
    			else if (staticLocalInitFlag_2.State == 2)
    			{
    				throw new IncompleteInitialization();
    			}
    		}
    		finally
    		{
    			staticLocalInitFlag_2.State = 1;
    			Monitor.Exit(staticLocalInitFlag_2);
    		}
    		Monitor.Enter(_0024);
    		try
    		{
    			if (_0024.State == 0)
    			{
    				_0024.State = 2;
    				list_0 = new List<Instruction>();
    			}
    			else if (_0024.State == 2)
    			{
    				throw new IncompleteInitialization();
    			}
    		}
    		finally
    		{
    			_0024.State = 1;
    			Monitor.Exit(_0024);
    		}
    		if (arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_1)) < int_0)
    		{
    			bool_0 = false;
    		}
    		checked
    		{
    			if (!bool_0)
    			{
    				bool_0 = true;
    				int num = int_0 + 1;
    				int num2 = int_0;
    				object obj = null;
    				int num3;
    				try
    				{
    					bool flag3 = true;
    					num3 = arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction.Operand));
    					if (num3 > int_0)
    					{
    						for (int i = int_0 + 1; i < num3; i++)
    						{
    							if (!(arrayList_0[i] is Instruction) || NewLateBinding.LateGet(arrayList_0[i], null, "Operand", new object[0], null, null, null) == arrayList_0[i])
    							{
    								continue;
    							}
    							Instruction instruction2 = (Instruction)arrayList_0[i];
    							if (instruction2.Operand is Instruction && !((instruction2.OpCode.Code == Code.Leave) | (instruction2.OpCode.Code == Code.Leave_S)))
    							{
    								int num4 = arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction2.Operand));
    								if (num4 < int_0)
    								{
    									num3 = num4;
    									break;
    								}
    								if (num4 >= num3 && !smethod_23(instruction2) && arrayList_0[i - 1] is Instruction && !(((Instruction)arrayList_0[i - 1]).Operand is Instruction) && !((((Instruction)arrayList_0[i - 1]).OpCode.Code == Code.Leave) | (((Instruction)arrayList_0[i - 1]).OpCode.Code == Code.Leave_S)))
    								{
    									num2 = i;
    									num3 = num4;
    									if (flag3)
    									{
    										num = i + 1;
    									}
    								}
    							}
    							else if (!smethod_26(instruction2))
    							{
    								break;
    							}
    						}
    					}
    				}
    				finally
    				{
    					if (obj != null)
    					{
    						((IDisposable)obj).Dispose();
    					}
    				}
    				object obj2 = null;
    				int num5;
    				try
    				{
    					if (num3 < int_0)
    					{
    						num = num3;
    						num5 = -1;
    						num3 = -1;
    					}
    					else if (!(arrayList_0[num3 - 1] is Instruction))
    					{
    						num5 = num3;
    						num3 = -1;
    					}
    					else
    					{
    						Instruction instruction3 = (Instruction)arrayList_0[num3 - 1];
    						if (((instruction3.OpCode.Code == Code.Br) | (instruction3.OpCode.Code == Code.Br_S)) && arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction3.Operand)) > num3)
    						{
    							num5 = arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction3.Operand));
    						}
    						else
    						{
    							num5 = num3;
    							num3 = -1;
    						}
    					}
    				}
    				finally
    				{
    					if (obj2 != null)
    					{
    						((IDisposable)obj2).Dispose();
    					}
    				}
    				object obj3 = null;
    				try
    				{
    					if (num < int_0)
    					{
    						object objectValue = RuntimeHelpers.GetObjectValue(arrayList_0[num]);
    						int num6 = arrayList_0.Count - 1;
    						int num7 = int_0;
    						for (int j = num6; j >= num7; j += -1)
    						{
    							if (arrayList_0[j] is Instruction)
    							{
    								Instruction instruction4 = (Instruction)arrayList_0[j];
    								if (instruction4.Operand is Instruction && instruction4.Operand == objectValue)
    								{
    									num2 = j;
    									num5 = j + 1;
    									break;
    								}
    							}
    						}
    					}
    					else
    					{
    						int num8 = int_0;
    						int num9 = num5;
    						for (int k = num8; k <= num9; k++)
    						{
    							if (!(arrayList_0[k] is string))
    							{
    								continue;
    							}
    							string text = Conversions.ToString(arrayList_0[k]);
    							if (text.Contains("<IF") | text.Contains("<ELSE"))
    							{
    								num5 = k;
    								if (num3 >= num5)
    								{
    									num3 = -1;
    								}
    								break;
    							}
    						}
    					}
    				}
    				finally
    				{
    					if (obj3 != null)
    					{
    						((IDisposable)obj3).Dispose();
    					}
    				}
    				list_0.Clear();
    				int num10 = int_0;
    				int num11 = num2;
    				for (int l = num10; l <= num11; l++)
    				{
    					if (arrayList_0[l] is Instruction && ((Instruction)arrayList_0[l]).Operand is Instruction && arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(((Instruction)arrayList_0[l]).Operand)) < num2 && arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(((Instruction)arrayList_0[l]).Operand)) > int_0 && !list_0.Contains((Instruction)((Instruction)arrayList_0[l]).Operand))
    					{
    						list_0.Add((Instruction)((Instruction)arrayList_0[l]).Operand);
    					}
    				}
    				object_1 = RuntimeHelpers.GetObjectValue(arrayList_0[num2]);
    				object_3 = null;
    				if (num3 > 0)
    				{
    					object_3 = RuntimeHelpers.GetObjectValue(arrayList_0[num3]);
    				}
    				object_0 = RuntimeHelpers.GetObjectValue(arrayList_0[num5]);
    				if (arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_1)) <= int_0 + 1)
    				{
    					bool_0 = false;
    				}
    				object_2 = RuntimeHelpers.GetObjectValue(arrayList_0[num]);
    				bool_1 = false;
    				if (string_0.Contains("{value2}"))
    				{
    					if (int_0 >= 3 && arrayList_0[int_0 - 3] is string && Operators.ConditionalCompareObjectEqual(arrayList_0[int_0 - 3], "<ELSE>", TextCompare: false))
    					{
    						bool_1 = true;
    						arrayList_0.RemoveAt(int_0 - 3);
    						num3--;
    						num5--;
    						int_0--;
    					}
    				}
    				else if (int_0 >= 2 && arrayList_0[int_0 - 2] is string && Operators.ConditionalCompareObjectEqual(arrayList_0[int_0 - 2], "<ELSE>", TextCompare: false))
    				{
    					bool_1 = true;
    					arrayList_0.RemoveAt(int_0 - 2);
    					num3--;
    					num5--;
    					int_0--;
    				}
    				bool_2 = false;
    				if (arrayList_0[num5 - 1] is Instruction)
    				{
    					Instruction instruction5 = (Instruction)arrayList_0[num5 - 1];
    					if (instruction5.Operand is Instruction && arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction5.Operand)) < int_0)
    					{
    						bool_2 = true;
    					}
    				}
    				if (num > int_0 && !bool_2)
    				{
    					int num12 = num5;
    					while (num12 > 0 && arrayList_0[num12 - 1] is string)
    					{
    						num12--;
    					}
    					if (bool_3)
    					{
    						arrayList_0.Insert(num12, "<IIFEND>");
    					}
    					else if (!bool_1)
    					{
    						arrayList_0.Insert(num12, "<IFCLOSE>");
    					}
    					if (num3 > 0)
    					{
    						arrayList_0.Insert(num3, "<ELSE>");
    						((Instruction)arrayList_0[num3 - 1]).Operand = RuntimeHelpers.GetObjectValue(arrayList_0[num3 - 1]);
    						num5++;
    					}
    				}
    				if (object_2 is string)
    				{
    					ILProcessor iLProcessor = methodDefinition_0.Body.GetILProcessor();
    					Instruction instruction6 = iLProcessor.Create(OpCodes.Br, instruction);
    					instruction6.Operand = instruction6;
    					arrayList_0.Insert(num, instruction6);
    					object_2 = RuntimeHelpers.GetObjectValue(arrayList_0[num]);
    				}
    			}
    			if (arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_1)) <= int_0 + 1)
    			{
    				bool_0 = false;
    			}
    			if (bool_0)
    			{
    				if (instruction.Operand == object_2)
    				{
    					flag = true;
    					flag2 = true;
    				}
    				else if (arrayList_0[int_0 + 1] is Instruction && arrayList_0[int_0 + 1] != object_2 && ((Instruction)arrayList_0[int_0 + 1]).Operand is Instruction)
    				{
    					arrayList_0.RemoveAt(int_0 + 1);
    					flag = true;
    				}
    				else if (arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction.Operand)) <= arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_1)))
    				{
    					if (!(arrayList_0[arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction.Operand)) - 1] is Instruction) || ((Instruction)arrayList_0[arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction.Operand)) - 1]).Operand != object_2)
    					{
    						flag = true;
    					}
    					int index = arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(instruction.Operand)) - 1;
    					if (arrayList_0[index] is Instruction && ((object_3 == null && ((Instruction)arrayList_0[index]).Operand == object_0) || (object_3 != null && ((Instruction)arrayList_0[index]).Operand == object_3)))
    					{
    						flag2 = true;
    					}
    				}
    			}
    			else if (arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_2)) < int_0)
    			{
    				flag2 = !flag2;
    			}
    			switch (instruction.OpCode.Code)
    			{
    			case Code.Brfalse_S:
    			case Code.Brtrue_S:
    			case Code.Brfalse:
    			case Code.Brtrue:
    				smethod_14(arrayList_0, int_0 - 1);
    				break;
    			}
    			smethod_20(arrayList_0, ref int_0, "{value}", string_0, 0, methodDefinition_0);
    			arrayList_0[int_0] = smethod_22(smethod_22(Conversions.ToString(arrayList_0[int_0])));
    			if (flag2)
    			{
    				arrayList_0[int_0] = smethod_22(Conversions.ToString(arrayList_0[int_0]));
    			}
    			if (list_0.Contains((Instruction)instruction.Operand))
    			{
    				arrayList_0[int_0] = "<IFGROUP>" + Conversions.ToString(arrayList_0[int_0]);
    				list_0.Remove((Instruction)instruction.Operand);
    			}
    			if (int_0 > 0 && arrayList_0[int_0 - 1] is Instruction && ((Instruction)arrayList_0[int_0 - 1]).Operand == arrayList_0[int_0 - 1] && arrayList_0[int_0 - 2] is string && Conversions.ToString(arrayList_0[int_0 - 2]).Contains("<IFGROUP>"))
    			{
    				arrayList_0.RemoveAt(int_0 - 1);
    				int_0--;
    			}
    			if (int_0 > 0 && arrayList_0[int_0 - 1] is string && (Conversions.ToString(arrayList_0[int_0 - 1]).Replace("<IFGROUP>", "").EndsWith("<ORELSE>") | Conversions.ToString(arrayList_0[int_0 - 1]).Replace("<IFGROUP>", "").EndsWith("<ANDALSO>")))
    			{
    				if (Conversions.ToString(arrayList_0[int_0]).Contains("<IFGROUP>") && Conversions.ToString(arrayList_0[int_0 - 1]).Contains("<IFGROUP>"))
    				{
    					if (Conversions.ToString(arrayList_0[int_0 - 1]).EndsWith("<ORELSE>"))
    					{
    						arrayList_0[int_0 - 1] = Conversions.ToString(arrayList_0[int_0 - 1]).Replace("<IFGROUP>", "(");
    						arrayList_0[int_0 - 1] = Conversions.ToString(arrayList_0[int_0 - 1]).Substring(0, Conversions.ToString(arrayList_0[int_0 - 1]).Length - 9) + ") <ORELSE>";
    					}
    					else if (Conversions.ToString(arrayList_0[int_0 - 1]).EndsWith("<ANDALSO>"))
    					{
    						arrayList_0[int_0 - 1] = Conversions.ToString(arrayList_0[int_0 - 1]).Replace("<IFGROUP>", "(");
    						arrayList_0[int_0 - 1] = Conversions.ToString(arrayList_0[int_0 - 1]).Substring(0, Conversions.ToString(arrayList_0[int_0 - 1]).Length - 10) + ") <ANDALSO>";
    					}
    					else
    					{
    						arrayList_0[int_0 - 1] = Conversions.ToString(arrayList_0[int_0 - 1]).Replace("<IFGROUP>", "(") + ")";
    					}
    				}
    				smethod_20(arrayList_0, ref int_0, "{value1} {value}", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    			}
    			if (!bool_0 && Conversions.ToString(arrayList_0[int_0]).Contains("<IFGROUP>"))
    			{
    				arrayList_0[int_0] = Conversions.ToString(arrayList_0[int_0]).Replace("<IFGROUP>", "(") + ")";
    			}
    			if (bool_0)
    			{
    				if (flag)
    				{
    					smethod_20(arrayList_0, ref int_0, "{value} <ORELSE>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    				}
    				else
    				{
    					smethod_20(arrayList_0, ref int_0, "{value} <ANDALSO>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    				}
    			}
    			else if (arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_2)) < int_0)
    			{
    				int num13 = arrayList_0.IndexOf(RuntimeHelpers.GetObjectValue(object_2));
    				arrayList_0[num13] = "<LOOPWHILEOPEN> <LOOPBRACKETOPEN>";
    				smethod_20(arrayList_0, ref int_0, "<LOOPWHILECLOSE> ({value})", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    				arrayList_0.Insert(int_0, "<LOOPBRACKETCLOSE>");
    				int_0++;
    				if (num13 - 1 >= 0 && arrayList_0[num13 - 1] is Instruction)
    				{
    					arrayList_0[num13] = "<DOWHILEOPEN>" + Conversions.ToString(arrayList_0[int_0]).Replace("<LOOPWHILECLOSE>", "") + " <LOOPBRACKETOPEN>";
    					arrayList_0[int_0] = "<DOWHILECLOSE>";
    					arrayList_0.RemoveAt(num13 - 1);
    					int_0--;
    				}
    			}
    			else if (bool_2)
    			{
    				arrayList_0[int_0] = smethod_22(Conversions.ToString(arrayList_0[int_0]));
    				if (bool_1)
    				{
    					smethod_20(arrayList_0, ref int_0, "<ELSEIF> ({value}) <IFOPEN>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    				}
    				else
    				{
    					smethod_20(arrayList_0, ref int_0, "<IF> ({value}) <IFOPEN>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    				}
    				arrayList_0.Insert(int_0 + 1, "<EXITLOOP>");
    				arrayList_0.Insert(int_0 + 2, "<IFCLOSE>");
    			}
    			else if (bool_3)
    			{
    				smethod_20(arrayList_0, ref int_0, "<IIFOPEN>({value})<IIFPART1>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    			}
    			else if (bool_1)
    			{
    				smethod_20(arrayList_0, ref int_0, "<ELSEIF> ({value}) <IFOPEN>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    			}
    			else
    			{
    				smethod_20(arrayList_0, ref int_0, "<IF> ({value}) <IFOPEN>", Conversions.ToString(arrayList_0[int_0]), 0, methodDefinition_0);
    			}
    		}
    	}

    	private static bool smethod_25(ref ArrayList arrayList_0, ref int int_0, ref MethodDefinition methodDefinition_0)
    	{
    		VariableDefinition variableDefinition = null;
    		bool flag = false;
    		Instruction instruction = (Instruction)arrayList_0[int_0];
    		switch (instruction.OpCode.Code)
    		{
    		case Code.Ldloc_0:
    			variableDefinition = methodDefinition_0.Body.Variables[0];
    			break;
    		case Code.Ldloc_1:
    			variableDefinition = methodDefinition_0.Body.Variables[1];
    			break;
    		case Code.Ldloc_2:
    			variableDefinition = methodDefinition_0.Body.Variables[2];
    			break;
    		case Code.Ldloc_3:
    			variableDefinition = methodDefinition_0.Body.Variables[3];
    			break;
    		case Code.Ldloca_S:
    		case Code.Ldloca:
    			variableDefinition = (VariableDefinition)instruction.Operand;
    			flag = true;
    			break;
    		case Code.Ldloc_S:
    		case Code.Ldloc:
    			variableDefinition = (VariableDefinition)instruction.Operand;
    			break;
    		}
    		if (variableDefinition.Name.StartsWith("#") & !flag)
    		{
    			arrayList_0[int_0] = "<NOTHING>";
    			if (variableDefinition.VariableType.FullName.StartsWith("System.Int"))
    			{
    				arrayList_0[int_0] = "0";
    			}
    			if (variableDefinition.VariableType.FullName.StartsWith("System.UInt"))
    			{
    				arrayList_0[int_0] = "0";
    			}
    			if (variableDefinition.VariableType.FullName.StartsWith("System.Single"))
    			{
    				arrayList_0[int_0] = "0";
    			}
    			if (variableDefinition.VariableType.FullName.StartsWith("System.Double"))
    			{
    				arrayList_0[int_0] = "0";
    			}
    			if (variableDefinition.VariableType.FullName.StartsWith("System.Byte"))
    			{
    				arrayList_0[int_0] = "0";
    			}
    			if (variableDefinition.VariableType.FullName.StartsWith("System.SByte"))
    			{
    				arrayList_0[int_0] = "0";
    			}
    			if (variableDefinition.VariableType.FullName.StartsWith("System.Boolean"))
    			{
    				arrayList_0[int_0] = "<FALSE>";
    			}
    			return true;
    		}
    		object obj = null;
    		checked
    		{
    			try
    			{
    				if (variableDefinition.VariableType.IsArray)
    				{
    					int num = int_0 - 1;
    					int num2 = -1;
    					int num3 = -1;
    					bool flag2 = false;
    					while (num >= 0 && arrayList_0[num] is string)
    					{
    						if (Conversions.ToString(arrayList_0[num]).StartsWith(variableDefinition.Name + "<ARRAYBRACKETOPEN>"))
    						{
    							num2 = int.Parse(Conversions.ToString(arrayList_0[num]).Substring((variableDefinition.Name + "<ARRAYBRACKETOPEN>").Length, Conversions.ToString(arrayList_0[num]).IndexOf("<ARRAYBRACKETCLOSE>") - (variableDefinition.Name + "<ARRAYBRACKETOPEN>").Length));
    							num3 = Math.Max(num3, num2);
    							num--;
    							continue;
    						}
    						if (Conversions.ToString(arrayList_0[num]).StartsWith("<DECLARE=" + Conversions.ToString(variableDefinition.Index) + "<END>"))
    						{
    							flag2 = num3 == int.Parse(Conversions.ToString(arrayList_0[num]).Substring(Conversions.ToString(arrayList_0[num]).IndexOf("<ARRAYBRACKETOPEN>") + "<ARRAYBRACKETOPEN>".Length, Conversions.ToString(arrayList_0[num]).IndexOf("<ARRAYBRACKETCLOSE>") - (Conversions.ToString(arrayList_0[num]).IndexOf("<ARRAYBRACKETOPEN>") + "<ARRAYBRACKETOPEN>".Length)));
    						}
    						break;
    					}
    					if (flag2)
    					{
    						string text = Conversions.ToString(arrayList_0[int_0 - num3 - 2]);
    						text = text.Replace("<LINEEND>", "");
    						text = text.Replace($"<ARRAYBRACKETOPEN>{num3}<ARRAYBRACKETCLOSE>", "<ARRAYBRACKETOPEN><ARRAYBRACKETCLOSE>");
    						text = text.Substring(0, text.Length - 1);
    						for (num = num3 + 1; num >= 1; num += -1)
    						{
    							text = text + Conversions.ToString(arrayList_0[int_0 - num]).Substring(Conversions.ToString(arrayList_0[int_0 - num]).IndexOf("=") + 2) + ", ";
    							arrayList_0.RemoveAt(int_0 - num);
    							int_0--;
    						}
    						if (text.EndsWith(", "))
    						{
    							text = text.Substring(0, text.Length - 2);
    						}
    						text += "}";
    						arrayList_0[int_0 - 1] = text;
    					}
    				}
    			}
    			catch (Exception projectError)
    			{
    				ProjectData.SetProjectError(projectError);
    				ProjectData.ClearProjectError();
    			}
    			finally
    			{
    				if (obj != null)
    				{
    					((IDisposable)obj).Dispose();
    				}
    			}
    			object obj2 = null;
    			bool result = default;
    			try
    			{
    				int num4 = int_0 - 1;
    				if (num4 > 0 && !(arrayList_0[num4] is string))
    				{
    					num4--;
    				}
    				if (int_0 > 0 && arrayList_0[num4] is string && Conversions.ToString(arrayList_0[num4]).StartsWith("<DECLARE=" + Conversions.ToString(variableDefinition.Index) + "<END>"))
    				{
    					bool flag3 = true;
    					int num5 = int_0 + 1;
    					int num6 = arrayList_0.Count - 1;
    					for (int i = num5; i <= num6; i++)
    					{
    						if (!(arrayList_0[i] is string))
    						{
    							VariableDefinition variableDefinition2 = null;
    							VariableDefinition variableDefinition3 = null;
    							object left = NewLateBinding.LateGet(NewLateBinding.LateGet(arrayList_0[i], null, "OpCode", new object[0], null, null, null), null, "Code", new object[0], null, null, null);
    							if (Operators.ConditionalCompareObjectEqual(left, Code.Ldloc_0, TextCompare: false))
    							{
    								variableDefinition2 = methodDefinition_0.Body.Variables[0];
    							}
    							else if (Operators.ConditionalCompareObjectEqual(left, Code.Ldloc_1, TextCompare: false))
    							{
    								variableDefinition2 = methodDefinition_0.Body.Variables[1];
    							}
    							else if (Operators.ConditionalCompareObjectEqual(left, Code.Ldloc_2, TextCompare: false))
    							{
    								variableDefinition2 = methodDefinition_0.Body.Variables[2];
    							}
    							else if (Operators.ConditionalCompareObjectEqual(left, Code.Ldloc_3, TextCompare: false))
    							{
    								variableDefinition2 = methodDefinition_0.Body.Variables[3];
    							}
    							else if (Conversions.ToBoolean((Conversions.ToBoolean(Operators.CompareObjectEqual(left, Code.Ldloc_S, TextCompare: false)) || Conversions.ToBoolean(Operators.CompareObjectEqual(left, Code.Ldloc, TextCompare: false))) ? ((object)true) : ((object)false)))
    							{
    								variableDefinition2 = (VariableDefinition)NewLateBinding.LateGet(arrayList_0[i], null, "Operand", new object[0], null, null, null);
    							}
    							object left2 = NewLateBinding.LateGet(NewLateBinding.LateGet(arrayList_0[i], null, "OpCode", new object[0], null, null, null), null, "Code", new object[0], null, null, null);
    							if (Operators.ConditionalCompareObjectEqual(left2, Code.Stloc_0, TextCompare: false))
    							{
    								variableDefinition3 = methodDefinition_0.Body.Variables[0];
    							}
    							else if (Operators.ConditionalCompareObjectEqual(left2, Code.Stloc_1, TextCompare: false))
    							{
    								variableDefinition3 = methodDefinition_0.Body.Variables[1];
    							}
    							else if (Operators.ConditionalCompareObjectEqual(left2, Code.Stloc_2, TextCompare: false))
    							{
    								variableDefinition3 = methodDefinition_0.Body.Variables[2];
    							}
    							else if (Operators.ConditionalCompareObjectEqual(left2, Code.Stloc_3, TextCompare: false))
    							{
    								variableDefinition3 = methodDefinition_0.Body.Variables[3];
    							}
    							else if (Conversions.ToBoolean((Conversions.ToBoolean(Operators.CompareObjectEqual(left2, Code.Stloc_S, TextCompare: false)) || Conversions.ToBoolean(Operators.CompareObjectEqual(left2, Code.Stloc, TextCompare: false))) ? ((object)true) : ((object)false)))
    							{
    								variableDefinition3 = (VariableDefinition)NewLateBinding.LateGet(arrayList_0[i], null, "Operand", new object[0], null, null, null);
    							}
    							else if (Conversions.ToBoolean((Conversions.ToBoolean(Operators.CompareObjectEqual(left2, Code.Ldloca, TextCompare: false)) || Conversions.ToBoolean(Operators.CompareObjectEqual(left2, Code.Ldloca_S, TextCompare: false))) ? ((object)true) : ((object)false)))
    							{
    								variableDefinition3 = (VariableDefinition)NewLateBinding.LateGet(arrayList_0[i], null, "Operand", new object[0], null, null, null);
    							}
    							if (variableDefinition3 == variableDefinition)
    							{
    								break;
    							}
    							if (variableDefinition2 == variableDefinition)
    							{
    								flag3 = false;
    								break;
    							}
    						}
    					}
    					if (flag3)
    					{
    						arrayList_0[int_0] = Conversions.ToString(arrayList_0[num4]).Substring(Conversions.ToString(arrayList_0[num4]).IndexOf("= ") + 2).Replace("<LINEEND>", "");
    						if (!variableDefinition.Name.StartsWith("#"))
    						{
    							variableDefinition.Name = "#" + variableDefinition.Name;
    						}
    						arrayList_0.RemoveAt(num4);
    						int_0--;
    						result = true;
    						return result;
    					}
    				}
    			}
    			finally
    			{
    				if (obj2 != null)
    				{
    					((IDisposable)obj2).Dispose();
    				}
    			}
    			return result;
    		}
    	}

    	private static bool smethod_26(Instruction instruction_0)
    	{
    		if (instruction_0.OpCode.StackBehaviourPush == StackBehaviour.Varpop)
    		{
    			return false;
    		}
    		if (instruction_0.OpCode.StackBehaviourPush >= StackBehaviour.Push1)
    		{
    			return true;
    		}
    		return false;
    	}

    	private static string smethod_27(TypeReference typeReference_0)
    	{
    		string text = typeReference_0.GetElementType().Name.ToLower();
    		if (typeReference_0.GetElementType().IsDefinition & text.StartsWith("i"))
    		{
    			text = text.Substring(1);
    		}
    		if (typeReference_0.IsArray)
    		{
    			text = "array";
    		}
    		string text2 = text;
    		switch (text2)
    		{
    		case "string":
    			text = "str";
    			break;
    		default:
    			if (Operators.CompareString(text2, "int32", TextCompare: false) == 0 || Operators.CompareString(text2, "int64", TextCompare: false) == 0 || Operators.CompareString(text2, "uint16", TextCompare: false) == 0 || Operators.CompareString(text2, "uint32", TextCompare: false) == 0 || Operators.CompareString(text2, "uint64", TextCompare: false) == 0)
    			{
    				goto case "long";
    			}
    			if (Operators.CompareString(text2, "boolean", TextCompare: false) == 0)
    			{
    				text = "flag";
    			}
    			break;
    		case "long":
    		case "double":
    		case "int16":
    			text = "num";
    			break;
    		}
    		return text;
    	}

    	private static string smethod_28(TypeReference typeReference_0, List<string> list_1)
    	{
    		string text = smethod_27(typeReference_0);
    		int i;
    		for (i = 1; list_1.Contains(text + Conversions.ToString(i)); i = checked(i + 1))
    		{
    		}
    		list_1.Add(text + Conversions.ToString(i));
    		return text + Conversions.ToString(i);
    	}

    	private static string smethod_29(TypeReference typeReference_0)
    	{
    		string text = "";
    		checked
    		{
    			if (typeReference_0.IsArray)
    			{
    				text += "<ARRAYBRACKETOPEN>";
    				int count = ((ArrayType)typeReference_0).Dimensions.Count;
    				for (int i = 2; i <= count; i++)
    				{
    					text += ",";
    				}
    				text += "<ARRAYBRACKETCLOSE>";
    			}
    			if (typeReference_0.IsPointer | typeReference_0.Name.EndsWith("&"))
    			{
    				text += "<OBJECTREF>";
    			}
    			if (typeReference_0 is GenericInstanceType)
    			{
    				GenericInstanceType genericInstanceType = (GenericInstanceType)typeReference_0;
    				typeReference_0 = typeReference_0.GetElementType();
    				string text2 = typeReference_0.Name;
    				if (typeReference_0.Name.Length > 2 && Operators.CompareString(typeReference_0.Name.Substring(typeReference_0.Name.Length - 2, 1), "`", TextCompare: false) == 0)
    				{
    					text2 = typeReference_0.Name.Substring(0, typeReference_0.Name.Length - 2);
    				}
    				text2 += "<GENERICOPEN>";
    				foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
    				{
    					text2 = text2 + smethod_29(genericArgument) + ", ";
    				}
    				return text2.TrimEnd(' ', ',') + "<GENERICCLOSE>";
    			}
    			typeReference_0 = typeReference_0.GetElementType();
    			switch (typeReference_0.Name)
    			{
    			case "Void":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEVOID>" + text + "<<>>";
    			case "String":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPESTRING>" + text + "<<>>";
    			case "Char":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPECHAR>" + text + "<<>>";
    			case "Boolean":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEBOOLEAN>" + text + "<<>>";
    			case "Object":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEOBJECT>" + text + "<<>>";
    			case "Int16":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEINT16>" + text + "<<>>";
    			case "Int32":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEINT32>" + text + "<<>>";
    			case "Int64":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEINT64>" + text + "<<>>";
    			case "UInt16":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEUINT16>" + text + "<<>>";
    			case "UInt32":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEUINT32>" + text + "<<>>";
    			case "UInt64":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEUINT64>" + text + "<<>>";
    			case "Single":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPESINGLE>" + text + "<<>>";
    			case "Double":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEDOUBLE>" + text + "<<>>";
    			case "Byte":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPEBYTE>" + text + "<<>>";
    			case "SByte":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPESBYTE>" + text + "<<>>";
    			case "Type":
    				return LanguageWriter.GetReferenceKey(typeReference_0) + "<TYPETYPE>" + text + "<<>>";
    			default:
    				switch (typeReference_0.Namespace)
    				{
    				case null:
    				case "":
    					return LanguageWriter.GetReferenceKey(typeReference_0) + typeReference_0.Name + text + "<<>>";
    				default:
    					return LanguageWriter.GetReferenceKey(typeReference_0) + typeReference_0.Namespace + "." + typeReference_0.Name + text + "<<>>";
    				case "System":
    				case "System.Reflection":
    				case "System.Runtime.InteropServices":
    				case "System.Runtime.CompilerServices":
    				case "System.Diagnostics":
    				case "System.ComponentModel":
    				case "System.CodeDom.Compiler":
    				case "Microsoft.VisualBasic":
    				case "Microsoft.VisualBasic.CompilerServices":
    					return LanguageWriter.GetReferenceKey(typeReference_0) + typeReference_0.Name + text + "<<>>";
    				}
    			}
    		}
    	}
    }

}
