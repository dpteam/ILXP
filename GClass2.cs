using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Mono.Cecil;

[SuppressIldasm]
public abstract class GClass2
{
	[DebuggerNonUserCode]
	public GClass2()
	{
	}

	public abstract string vmethod_0(GClass0 gclass0_0);

	public abstract string vmethod_1(GClass6 gclass6_0);

	public abstract string vmethod_2(TypeDefinition typeDefinition_0);

	public abstract string vmethod_3(ModuleDefinition moduleDefinition_0);

	public abstract string vmethod_4(MethodDefinition methodDefinition_0);

	public abstract string vmethod_5(PropertyDefinition propertyDefinition_0);

	public abstract string vmethod_6(FieldDefinition fieldDefinition_0);

	public abstract string vmethod_7(EventDefinition eventDefinition_0);

	public static string smethod_0(object object_0)
	{
		if (object_0 is GClass0)
		{
			return "<<#" + ((GClass0)object_0).method_1().Name.FullName + ">>";
		}
		if (object_0 is AssemblyNameReference)
		{
			return "<<#" + ((AssemblyNameReference)object_0).FullName + ">>";
		}
		if (object_0 is AssemblyDefinition)
		{
			return "<<#" + ((AssemblyDefinition)object_0).Name.FullName + ">>";
		}
		if (object_0 is GClass6)
		{
			return "<<#" + ((GClass6)object_0).method_1().Assembly.Name.FullName + "//N" + ((GClass6)object_0).method_0() + ">>";
		}
		if (object_0 is TypeReference)
		{
			return "<<#" + smethod_1(((TypeReference)object_0).Scope).FullName + "//N" + ((TypeReference)object_0).Namespace + "//" + ((TypeReference)object_0).GetElementType().Name.Replace("<", "").Replace(">", "") + ">>";
		}
		if (object_0 is MethodReference)
		{
			return "<<#" + smethod_1(((MethodReference)object_0).DeclaringType.Scope).FullName + "//N" + ((MethodReference)object_0).DeclaringType.Namespace + "//" + ((MethodReference)object_0).DeclaringType.Name + "//M" + smethod_4(((MethodReference)object_0).GetElementMethod()) + ">>";
		}
		if (object_0 is PropertyReference)
		{
			return "<<#" + smethod_1(((PropertyReference)object_0).DeclaringType.Scope).FullName + "//N" + ((PropertyReference)object_0).DeclaringType.Namespace + "//" + ((PropertyReference)object_0).DeclaringType.Name + "//P" + ((PropertyReference)object_0).Name + ">>";
		}
		if (object_0 is FieldReference)
		{
			return "<<#" + smethod_1(((FieldReference)object_0).DeclaringType.Scope).FullName + "//N" + ((FieldReference)object_0).DeclaringType.Namespace + "//" + ((FieldReference)object_0).DeclaringType.Name + "//F" + ((FieldReference)object_0).Name + ">>";
		}
		if (!(object_0 is EventReference))
		{
			throw new Exception("Unknown object type");
		}
		return "<<#" + smethod_1(((EventReference)object_0).DeclaringType.Scope).FullName + "//N" + ((EventReference)object_0).DeclaringType.Namespace + "//" + ((EventReference)object_0).DeclaringType.Name + "//E" + ((EventReference)object_0).Name + ">>";
	}

	public static AssemblyNameReference smethod_1(IMetadataScope imetadataScope_0)
	{
		if (imetadataScope_0 is AssemblyNameReference)
		{
			return (AssemblyNameReference)imetadataScope_0;
		}
		if (imetadataScope_0 is ModuleDefinition)
		{
			return ((ModuleDefinition)imetadataScope_0).Assembly.Name;
		}
		if (!(imetadataScope_0 is AssemblyDefinition))
		{
			throw new Exception("Cannot determine scope type");
		}
		return ((AssemblyDefinition)(object)imetadataScope_0).Name;
	}

	public string method_0(string string_0)
	{
		string_0 = Regex.Replace(string_0, "\\<\\<[r|g|b]?\\>\\>", "");
		string_0 = Regex.Replace(string_0, "\\<\\<\\#[^\\>|\\<]+\\>\\>", "");
		return string_0;
	}

	public static string smethod_2(TypeReference typeReference_0)
	{
		return typeReference_0.Namespace + "." + smethod_3(typeReference_0);
	}

	public static string smethod_3(TypeReference typeReference_0)
	{
		string text = typeReference_0.GetElementType().Name;
		checked
		{
			if (typeReference_0 is GenericInstanceType)
			{
				if (typeReference_0.Name.Length > 2 && Operators.CompareString(typeReference_0.Name.Substring(typeReference_0.Name.Length - 2, 1), "`", TextCompare: false) == 0)
				{
					text = typeReference_0.Name.Substring(0, typeReference_0.Name.Length - 2);
				}
				text += "<";
				GenericInstanceType genericInstanceType = (GenericInstanceType)typeReference_0;
				foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
				{
					text = Conversions.ToString(Operators.ConcatenateObject(text, Operators.ConcatenateObject(Interaction.IIf(string.IsNullOrEmpty(genericArgument.Name), "?", genericArgument.Name), ", ")));
				}
				text = text.TrimEnd(' ', ',') + ">";
			}
			return text;
		}
	}

	public static string smethod_4(MethodReference methodReference_0)
	{
		string text = "(";
		foreach (ParameterDefinition parameter in methodReference_0.Parameters)
		{
			text = text + parameter.ParameterType.Name + ",";
		}
		text = text.TrimEnd(',') + ")";
		return methodReference_0.Name + text;
	}

	public static string smethod_5(MethodReference methodReference_0)
	{
		string text = methodReference_0.Name + "(";
		foreach (ParameterDefinition parameter in methodReference_0.Parameters)
		{
			text = text + smethod_3(parameter.ParameterType) + ",";
		}
		text = text.TrimEnd(',') + ")";
		if (!text.StartsWith(".") && methodReference_0.ReturnType != null)
		{
			text = $"{text} : {smethod_3(methodReference_0.ReturnType)}";
		}
		return text;
	}

	public static string smethod_6(int int_0)
	{
		return "<<r>>L_" + Convert.ToString(int_0, 16).PadLeft(4, '0') + "<<>>";
	}

	internal string method_1(TypeReference typeReference_0)
	{
		string text = typeReference_0.GetElementType().Name.ToLower();
		if (typeReference_0.IsArray)
		{
			text = "array";
		}
		string left = text;
		if (Operators.CompareString(left, "string", TextCompare: false) == 0)
		{
			text = "str";
		}
		else if (Operators.CompareString(left, "long", TextCompare: false) != 0 && Operators.CompareString(left, "double", TextCompare: false) != 0 && Operators.CompareString(left, "int16", TextCompare: false) != 0 && Operators.CompareString(left, "int32", TextCompare: false) != 0 && Operators.CompareString(left, "int64", TextCompare: false) != 0 && Operators.CompareString(left, "uint16", TextCompare: false) != 0 && Operators.CompareString(left, "uint32", TextCompare: false) != 0 && Operators.CompareString(left, "uint64", TextCompare: false) != 0)
		{
			if (Operators.CompareString(left, "boolean", TextCompare: false) == 0)
			{
				text = "flag";
			}
		}
		else
		{
			text = "num";
		}
		return text;
	}

	internal string method_2(TypeReference typeReference_0, List<string> list_0)
	{
		string text = method_1(typeReference_0);
		int i;
		for (i = 1; list_0.Contains(text + Conversions.ToString(i)); i = checked(i + 1))
		{
		}
		list_0.Add(text + Conversions.ToString(i));
		return text + Conversions.ToString(i);
	}
}
