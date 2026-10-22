using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

[SuppressIldasm]
public sealed class GClass4 : GClass2
{
	[DebuggerNonUserCode]
	public GClass4()
	{
	}

	public override string vmethod_0(GClass0 gclass0_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<<g>>//Name: " + gclass0_0.method_1().Name.Name + "<<>>");
		stringBuilder.AppendLine("<<g>>//Path: " + gclass0_0.method_0() + "<<>>");
		stringBuilder.AppendLine("<<g>>//Version: " + $"{gclass0_0.method_1().Name.Version.Major}:{gclass0_0.method_1().Name.Version.MajorRevision}:{gclass0_0.method_1().Name.Version.Minor}:{gclass0_0.method_1().Name.Version.MinorRevision}" + Class8.smethod_18());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Class8.smethod_81() + GClass2.smethod_0(gclass0_0) + gclass0_0.method_1().Name.Name + Class8.smethod_18());
		stringBuilder.AppendLine("{");
		stringBuilder.AppendLine("    <<b>>.ver<<>> " + $"{gclass0_0.method_1().Name.Version.Major}:{gclass0_0.method_1().Name.Version.MajorRevision}:{gclass0_0.method_1().Name.Version.Minor}:{gclass0_0.method_1().Name.Version.MinorRevision}");
		stringBuilder.AppendLine(Conversions.ToString(Operators.ConcatenateObject("    <<b>>.hash algorithm<<>> 0x" + Convert.ToString(checked((int)gclass0_0.method_1().Name.HashAlgorithm), 16), Interaction.IIf(gclass0_0.method_1().Name.HashAlgorithm == AssemblyHashAlgorithm.SHA1, " <<g>>// SHA1<<>>", ""))));
		method_5(stringBuilder, gclass0_0.method_1().CustomAttributes);
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	public override string vmethod_1(GClass6 gclass6_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrEmpty(gclass6_0.method_0()))
		{
			stringBuilder.AppendLine("<<b>>.namespace<<>> " + GClass2.smethod_0(gclass6_0) + gclass6_0.method_0() + "<<>>");
		}
		stringBuilder.AppendLine("{");
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (TypeDefinition type in gclass6_0.method_1().Types)
		{
			if (Operators.CompareString(type.Namespace, gclass6_0.method_0(), TextCompare: false) == 0)
			{
				stringBuilder2.AppendLine(method_3(type, bool_0: true));
				stringBuilder2.AppendLine();
			}
		}
		stringBuilder2.Replace(Environment.NewLine, Environment.NewLine + Class8.smethod_82());
		stringBuilder.AppendLine(Class8.smethod_82() + stringBuilder2.ToString().Trim());
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	public override string vmethod_2(TypeDefinition typeDefinition_0)
	{
		return method_3(typeDefinition_0, bool_0: false);
	}

	public string method_3(TypeDefinition typeDefinition_0, bool bool_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<<b>>");
		stringBuilder.Append(".class ");
		if (typeDefinition_0.IsPublic)
		{
			stringBuilder.Append("public ");
		}
		else if (typeDefinition_0.IsNotPublic)
		{
			stringBuilder.Append("private ");
		}
		else if (typeDefinition_0.IsNestedPrivate)
		{
			stringBuilder.Append("nested private ");
		}
		else if (typeDefinition_0.IsNestedPublic)
		{
			stringBuilder.Append(Class8.smethod_83());
		}
		else if (typeDefinition_0.IsNestedAssembly)
		{
			stringBuilder.Append(Class8.smethod_84());
		}
		else if (typeDefinition_0.IsNestedFamily)
		{
			stringBuilder.Append(Class8.smethod_85());
		}
		else if (typeDefinition_0.IsNestedFamilyAndAssembly)
		{
			stringBuilder.Append(Class8.smethod_86());
		}
		else if (typeDefinition_0.IsNestedFamilyOrAssembly)
		{
			stringBuilder.Append(Class8.smethod_87());
		}
		if (typeDefinition_0.IsSequentialLayout)
		{
			stringBuilder.Append(Class8.smethod_88());
		}
		if (typeDefinition_0.IsExplicitLayout)
		{
			stringBuilder.Append(Class8.smethod_89());
		}
		if (typeDefinition_0.IsAutoLayout)
		{
			stringBuilder.Append(Class8.smethod_90());
		}
		if (typeDefinition_0.IsAnsiClass)
		{
			stringBuilder.Append(Class8.smethod_91());
		}
		if (typeDefinition_0.IsAutoClass)
		{
			stringBuilder.Append(Class8.smethod_90());
		}
		if (typeDefinition_0.IsUnicodeClass)
		{
			stringBuilder.Append(Class8.smethod_92());
		}
		if (typeDefinition_0.IsInterface)
		{
			stringBuilder.Append(Class8.smethod_93());
		}
		if (typeDefinition_0.IsSpecialName)
		{
			stringBuilder.Append(Class8.smethod_94());
		}
		if (typeDefinition_0.IsRuntimeSpecialName)
		{
			stringBuilder.Append(Class8.smethod_95());
		}
		if (typeDefinition_0.IsImport)
		{
			stringBuilder.Append(Class8.smethod_96());
		}
		if (typeDefinition_0.IsAbstract)
		{
			stringBuilder.Append(Class8.smethod_97());
		}
		if (typeDefinition_0.IsSerializable)
		{
			stringBuilder.Append(Class8.smethod_98());
		}
		if (typeDefinition_0.IsBeforeFieldInit)
		{
			stringBuilder.Append(Class8.smethod_99());
		}
		if (typeDefinition_0.IsSealed)
		{
			stringBuilder.Append(Class8.smethod_100());
		}
		stringBuilder.Append(Class8.smethod_18());
		stringBuilder.Append(GClass2.smethod_0(typeDefinition_0));
		stringBuilder.AppendLine(GClass2.smethod_3(typeDefinition_0) + Class8.smethod_18());
		if (typeDefinition_0.BaseType != null)
		{
			stringBuilder.AppendLine(Class8.smethod_101() + method_6(typeDefinition_0.BaseType));
		}
		checked
		{
			if (typeDefinition_0.Interfaces.Count > 0)
			{
				stringBuilder.Append(Class8.smethod_102());
				foreach (TypeReference @interface in typeDefinition_0.Interfaces)
				{
					stringBuilder.Append(method_6(@interface) + ", ");
				}
				stringBuilder.Remove(stringBuilder.Length - 2, 2);
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("{");
			method_5(stringBuilder, typeDefinition_0.CustomAttributes);
			if (bool_0)
			{
				stringBuilder.Append("}");
				return stringBuilder.ToString();
			}
			if (typeDefinition_0.HasLayoutInfo)
			{
				stringBuilder.AppendLine($"    <<b>>.pack<<>> {typeDefinition_0.PackingSize}");
				stringBuilder.AppendLine($"    <<b>>.size<<>> {typeDefinition_0.ClassSize}");
			}
			StringBuilder stringBuilder2 = new StringBuilder();
			if (typeDefinition_0.NestedTypes.Count > 0)
			{
				stringBuilder2.AppendLine("<<g>>// Nested Types<<>>");
			}
			foreach (TypeDefinition nestedType in typeDefinition_0.NestedTypes)
			{
				stringBuilder2.AppendLine(method_3(nestedType, bool_0: true));
				stringBuilder2.AppendLine();
			}
			if (typeDefinition_0.Fields.Count > 0)
			{
				stringBuilder2.AppendLine("<<g>>// Fields<<>>");
			}
			foreach (FieldDefinition field in typeDefinition_0.Fields)
			{
				stringBuilder2.AppendLine(vmethod_6(field));
				stringBuilder2.AppendLine();
			}
			if (typeDefinition_0.Properties.Count > 0)
			{
				stringBuilder2.AppendLine("<<g>>// Properties<<>>");
			}
			foreach (PropertyDefinition property in typeDefinition_0.Properties)
			{
				stringBuilder2.AppendLine(vmethod_5(property));
				stringBuilder2.AppendLine();
			}
			List<string> list = new List<string>();
			foreach (PropertyDefinition property2 in typeDefinition_0.Properties)
			{
				if (property2.SetMethod != null)
				{
					list.Add(property2.SetMethod.Name);
				}
				if (property2.GetMethod != null)
				{
					list.Add(property2.GetMethod.Name);
				}
			}
			foreach (EventDefinition @event in typeDefinition_0.Events)
			{
				if (@event.AddMethod != null)
				{
					list.Add(@event.AddMethod.Name);
				}
				if (@event.RemoveMethod != null)
				{
					list.Add(@event.RemoveMethod.Name);
				}
				foreach (MethodDefinition otherMethod in @event.OtherMethods)
				{
					if (otherMethod != null)
					{
						list.Add(otherMethod.Name);
					}
				}
			}
			if (typeDefinition_0.Methods.Count - list.Count > 0)
			{
				stringBuilder2.AppendLine("<<g>>// Methods<<>>");
			}
			foreach (MethodDefinition method in typeDefinition_0.Methods)
			{
				if (!list.Contains(method.Name))
				{
					stringBuilder2.AppendLine(method_4(method, bool_0: true));
					stringBuilder2.AppendLine();
				}
			}
			if (typeDefinition_0.Events.Count > 0)
			{
				stringBuilder2.AppendLine("<<g>>// Events<<>>");
			}
			foreach (EventDefinition event2 in typeDefinition_0.Events)
			{
				stringBuilder2.AppendLine(vmethod_7(event2));
				stringBuilder2.AppendLine();
			}
			stringBuilder2.Replace(Environment.NewLine, Environment.NewLine + "    ");
			stringBuilder.AppendLine("    " + stringBuilder2.ToString().Trim());
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}
	}

	public override string vmethod_3(ModuleDefinition moduleDefinition_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Class8.smethod_104());
		stringBuilder.AppendLine(moduleDefinition_0.Name);
		stringBuilder.AppendLine(Class8.smethod_105() + moduleDefinition_0.Mvid.ToString() + Class8.smethod_18());
		stringBuilder.Append(Class8.smethod_103() + moduleDefinition_0.Runtime.ToString() + Class8.smethod_18());
		return stringBuilder.ToString();
	}

	public override string vmethod_4(MethodDefinition methodDefinition_0)
	{
		return method_4(methodDefinition_0, bool_0: false);
	}

	public string method_4(MethodDefinition methodDefinition_0, bool bool_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<<b>>");
		stringBuilder.Append(".method ");
		if (methodDefinition_0.IsPrivate)
		{
			stringBuilder.Append("private ");
		}
		else if (methodDefinition_0.IsFamilyAndAssembly)
		{
			stringBuilder.Append("famandassem ");
		}
		else if (methodDefinition_0.IsAssembly)
		{
			stringBuilder.Append("assembly ");
		}
		else if (methodDefinition_0.IsFamily)
		{
			stringBuilder.Append("family ");
		}
		else if (methodDefinition_0.IsFamilyOrAssembly)
		{
			stringBuilder.Append("famorassem ");
		}
		else if (methodDefinition_0.IsPublic)
		{
			stringBuilder.Append("public ");
		}
		if (methodDefinition_0.IsHideBySig)
		{
			stringBuilder.Append("hidebysig ");
		}
		if (methodDefinition_0.IsNewSlot)
		{
			stringBuilder.Append("newslot ");
		}
		if (methodDefinition_0.IsSpecialName)
		{
			stringBuilder.Append("specialname ");
		}
		if (methodDefinition_0.IsConstructor)
		{
			stringBuilder.Append("rtspecialname ");
		}
		if (methodDefinition_0.IsVirtual)
		{
			stringBuilder.Append("virtual ");
		}
		if (methodDefinition_0.IsFinal)
		{
			stringBuilder.Append("final ");
		}
		if (methodDefinition_0.IsAbstract)
		{
			stringBuilder.Append("abstract ");
		}
		if (methodDefinition_0.IsPInvokeImpl)
		{
			stringBuilder.Append("pinvokeimpl<<>>(<<r>>\"" + methodDefinition_0.PInvokeInfo.Module.Name + "\"<<r>>)<<b>> ");
		}
		if (methodDefinition_0.IsUnmanagedExport)
		{
			stringBuilder.Append("export ");
		}
		if ((methodDefinition_0.Attributes & MethodAttributes.RequireSecObject) != 0)
		{
			stringBuilder.Append("requiresecobj ");
		}
		if (methodDefinition_0.IsStatic)
		{
			stringBuilder.Append("static ");
		}
		if (methodDefinition_0.HasThis)
		{
			stringBuilder.Append("instance ");
		}
		if (methodDefinition_0.CallingConvention == MethodCallingConvention.C)
		{
			stringBuilder.Append("unmanaged cdecl ");
		}
		if (methodDefinition_0.CallingConvention == MethodCallingConvention.StdCall)
		{
			stringBuilder.Append("unmanaged stdcall ");
		}
		if (methodDefinition_0.CallingConvention == MethodCallingConvention.ThisCall)
		{
			stringBuilder.Append("unmanaged thiscall ");
		}
		if (methodDefinition_0.CallingConvention == MethodCallingConvention.FastCall)
		{
			stringBuilder.Append("unmanaged fastcall ");
		}
		if (methodDefinition_0.CallingConvention == MethodCallingConvention.VarArg)
		{
			stringBuilder.Append("vararg ");
		}
		if (methodDefinition_0.CallingConvention == MethodCallingConvention.Generic)
		{
			stringBuilder.Append("generic ");
		}
		stringBuilder.Append("<<>>");
		stringBuilder.Append(_003C(methodDefinition_0.ReturnType) + " ");
		stringBuilder.Append(GClass2.smethod_0(methodDefinition_0) + methodDefinition_0.Name + "<<>>");
		stringBuilder.Append("(");
		checked
		{
			if (methodDefinition_0.Parameters.Count > 0)
			{
				foreach (ParameterDefinition parameter in methodDefinition_0.Parameters)
				{
					stringBuilder.Append($"{_003C(parameter.ParameterType)} {parameter.Name}, ");
				}
				stringBuilder.Remove(stringBuilder.Length - 2, 2);
			}
			stringBuilder.Append(") <<b>>");
			if (methodDefinition_0.IsNative)
			{
				stringBuilder.Append("native ");
			}
			if (methodDefinition_0.IsIL)
			{
				stringBuilder.Append("cil ");
			}
			if (methodDefinition_0.IsRuntime)
			{
				stringBuilder.Append("runtime ");
			}
			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.OPTIL) != 0)
			{
				stringBuilder.Append("optil ");
			}
			if (methodDefinition_0.IsManaged)
			{
				stringBuilder.Append("managed");
			}
			else
			{
				stringBuilder.Append("unmanaged");
			}
			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.Synchronized) != 0)
			{
				stringBuilder.Append(" synchronized");
			}
			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.NoInlining) != 0)
			{
				stringBuilder.Append(" noinlining");
			}
			if ((methodDefinition_0.ImplAttributes & MethodImplAttributes.NoOptimization) != 0)
			{
				stringBuilder.Append(" nooptimization");
			}
			stringBuilder.Append("<<>>");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("{");
			if (bool_0 | (methodDefinition_0.Body == null))
			{
				stringBuilder.Append("}");
				return stringBuilder.ToString();
			}
			MethodBody body = methodDefinition_0.Body;
			stringBuilder.AppendLine($"    <<g>>// Method Start RVA 0x{methodDefinition_0.RVA:x4}<<>>");
			stringBuilder.AppendLine(string.Format(Class8.smethod_106(), body.CodeSize));
			method_5(stringBuilder, methodDefinition_0.CustomAttributes);
			if (methodDefinition_0.Module.Assembly.EntryPoint == methodDefinition_0)
			{
				stringBuilder.AppendLine(Class8.smethod_107());
			}
			if (body != null)
			{
				stringBuilder.AppendLine(string.Format(Class8.smethod_108(), body.MaxStackSize));
			}
			if (body.Variables.Count > 0)
			{
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
						variable2.Name = method_2(variable2.VariableType, list);
					}
				}
				if (body.InitLocals)
				{
					stringBuilder.AppendLine("    <<b>>.locals init<<>>");
				}
				else
				{
					stringBuilder.AppendLine("    <<b>>.locals<<>>");
				}
				stringBuilder.AppendLine("    (");
				foreach (VariableDefinition variable3 in body.Variables)
				{
					stringBuilder.Append($"        [{variable3.Index}] {_003C(variable3.VariableType)} ");
					if (variable3.IsPinned)
					{
						stringBuilder.Append("<<b>>pinned<<>> ");
					}
					stringBuilder.Append(variable3.Name);
					if (variable3 != body.Variables[body.Variables.Count - 1])
					{
						stringBuilder.Append(",");
					}
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine("    )");
			}
			foreach (Instruction instruction2 in body.Instructions)
			{
				string text = "";
				if (instruction2.Operand == null)
				{
					text = "";
				}
				else if (instruction2.Operand is string)
				{
					text = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("<<r>>\"", instruction2.Operand), "\"<<>>"));
				}
				else if (instruction2.Operand is Instruction)
				{
					text = GClass2.smethod_6(((Instruction)instruction2.Operand).Offset);
				}
				else if (instruction2.Operand is TypeReference)
				{
					text = method_6((TypeReference)instruction2.Operand);
				}
				else if (instruction2.Operand is MethodReference)
				{
					text = "";
					if (((MethodReference)instruction2.Operand).HasThis)
					{
						text += "<<b>>instance<<>> ";
					}
					if (((MethodReference)instruction2.Operand).ReturnType != null)
					{
						text = text + _003C(((MethodReference)instruction2.Operand).ReturnType) + " ";
					}
					text = text + method_6(((MethodReference)instruction2.Operand).DeclaringType) + "::" + GClass2.smethod_0((MethodReference)instruction2.Operand) + ((MethodReference)instruction2.Operand).Name + "<<>>";
					text += "(";
					foreach (ParameterDefinition parameter2 in ((MethodReference)instruction2.Operand).Parameters)
					{
						text = text + _003C(parameter2.ParameterType) + ",";
					}
					text = text.TrimEnd(',');
					text += ")";
				}
				else if (instruction2.Operand is VariableDefinition)
				{
					text = ((VariableDefinition)instruction2.Operand).Name;
				}
				else if (instruction2.Operand is FieldReference)
				{
					text = method_6(((FieldReference)instruction2.Operand).DeclaringType) + "::" + GClass2.smethod_0((FieldReference)instruction2.Operand) + ((FieldReference)instruction2.Operand).Name + "<<>>";
				}
				else if (instruction2.Operand is EventReference)
				{
					text = method_6(((EventReference)instruction2.Operand).DeclaringType) + "::" + GClass2.smethod_0((EventReference)instruction2.Operand) + ((EventReference)instruction2.Operand).Name + "<<>>";
				}
				else if (instruction2.Operand is Instruction[])
				{
					Instruction[] array = (Instruction[])instruction2.Operand;
					foreach (Instruction instruction in array)
					{
						text = text + GClass2.smethod_6(instruction.Offset) + ", ";
					}
					text.Remove(text.Length - 2, 2);
				}
				else
				{
					text = ((!(instruction2.Operand is ParameterDefinition)) ? Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("<<r>>", instruction2.Operand), "<<>>")) : ("<<r>>" + ((ParameterDefinition)instruction2.Operand).Name + "<<>>"));
				}
				stringBuilder.AppendLine(string.Format("    {0}: <<b>>{1}<<>> {2}", GClass2.smethod_6(instruction2.Offset).Substring(5).Replace("<<>>", ""), instruction2.OpCode, text));
			}
			foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
			{
				stringBuilder.Append("    <<b>>.try<<>> ");
				stringBuilder.Append(GClass2.smethod_6(exceptionHandler.TryStart.Offset));
				stringBuilder.Append(" <<b>>to<<>> ");
				stringBuilder.Append(GClass2.smethod_6(exceptionHandler.TryEnd.Offset));
				switch (exceptionHandler.HandlerType)
				{
				default:
					stringBuilder.Append(" <<b>>catch<<>> ");
					if (exceptionHandler.CatchType != null)
					{
						stringBuilder.Append(method_6(exceptionHandler.CatchType) + " ");
					}
					stringBuilder.Append("<<b>>handler<<>> ");
					break;
				case ExceptionHandlerType.Filter:
					stringBuilder.Append(" <<b>>filter<<>> " + GClass2.smethod_6(exceptionHandler.FilterStart.Offset) + " <<b>>handler<<>> ");
					break;
				case ExceptionHandlerType.Finally:
					stringBuilder.Append(" <<b>>finally handler<<>> ");
					break;
				}
				stringBuilder.Append(GClass2.smethod_6(exceptionHandler.HandlerStart.Offset));
				stringBuilder.Append(" <<b>>to<<>> ");
				stringBuilder.Append(GClass2.smethod_6(exceptionHandler.HandlerEnd.Offset));
				stringBuilder.AppendLine();
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}
	}

	public override string vmethod_5(PropertyDefinition propertyDefinition_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Class8.smethod_25());
		stringBuilder.Append(Class8.smethod_109());
		if (propertyDefinition_0.IsSpecialName)
		{
			stringBuilder.Append(Class8.smethod_94());
		}
		if (propertyDefinition_0.IsRuntimeSpecialName)
		{
			stringBuilder.Append(Class8.smethod_95());
		}
		if (propertyDefinition_0.HasDefault)
		{
			stringBuilder.Append(Class8.smethod_110());
		}
		if (propertyDefinition_0.GetMethod != null && propertyDefinition_0.GetMethod.IsStatic)
		{
			stringBuilder.Append(Class8.smethod_111());
		}
		else if (propertyDefinition_0.SetMethod != null && propertyDefinition_0.SetMethod.IsStatic)
		{
			stringBuilder.Append(Class8.smethod_111());
		}
		else
		{
			stringBuilder.Append(Class8.smethod_112());
		}
		stringBuilder.Append(Class8.smethod_18());
		stringBuilder.Append(_003C(propertyDefinition_0.PropertyType) + " ");
		stringBuilder.AppendLine(GClass2.smethod_0(propertyDefinition_0) + propertyDefinition_0.Name + "<<>>");
		stringBuilder.AppendLine("{");
		checked
		{
			if (propertyDefinition_0.GetMethod != null)
			{
				MethodDefinition getMethod = propertyDefinition_0.GetMethod;
				stringBuilder.Append("    <<b>>.get<<>> ");
				if (!getMethod.IsStatic)
				{
					stringBuilder.Append("<<b>>instance<<>> ");
				}
				stringBuilder.Append(_003C(getMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(getMethod.DeclaringType), GClass2.smethod_0(getMethod) + getMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (getMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter in getMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter.ParameterType)}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			if (propertyDefinition_0.SetMethod != null)
			{
				MethodDefinition setMethod = propertyDefinition_0.SetMethod;
				stringBuilder.Append("    <<b>>.set<<>> ");
				if (!setMethod.IsStatic)
				{
					stringBuilder.Append("<<b>>instance<<>> ");
				}
				stringBuilder.Append(_003C(setMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(setMethod.DeclaringType), GClass2.smethod_0(setMethod) + setMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (setMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter2 in setMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter2.ParameterType)}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			foreach (MethodDefinition otherMethod in propertyDefinition_0.OtherMethods)
			{
				stringBuilder.Append("    <<b>>.method<<>> ");
				if (!otherMethod.IsStatic)
				{
					stringBuilder.Append("<<b>>instance<<>> ");
				}
				stringBuilder.Append(_003C(otherMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(otherMethod.DeclaringType), GClass2.smethod_0(otherMethod) + otherMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (otherMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter3 in otherMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter3.ParameterType)}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			method_5(stringBuilder, propertyDefinition_0.CustomAttributes);
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}
	}

	public override string vmethod_6(FieldDefinition fieldDefinition_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<<b>>");
		stringBuilder.Append(".field ");
		if (fieldDefinition_0.IsPrivate)
		{
			stringBuilder.Append(Class8.smethod_26());
		}
		else if (fieldDefinition_0.IsFamilyAndAssembly)
		{
			stringBuilder.Append(Class8.smethod_113());
		}
		else if (fieldDefinition_0.IsAssembly)
		{
			stringBuilder.Append(Class8.smethod_114());
		}
		else if (fieldDefinition_0.IsFamily)
		{
			stringBuilder.Append(Class8.smethod_115());
		}
		else if (fieldDefinition_0.IsFamilyOrAssembly)
		{
			stringBuilder.Append(Class8.smethod_116());
		}
		else if (fieldDefinition_0.IsPublic)
		{
			stringBuilder.Append(Class8.smethod_29());
		}
		if (fieldDefinition_0.IsStatic)
		{
			stringBuilder.Append(Class8.smethod_30());
		}
		if (fieldDefinition_0.IsLiteral)
		{
			stringBuilder.Append(Class8.smethod_117());
		}
		if (fieldDefinition_0.IsSpecialName)
		{
			stringBuilder.Append(Class8.smethod_94());
		}
		if (fieldDefinition_0.IsRuntimeSpecialName)
		{
			stringBuilder.Append(Class8.smethod_95());
		}
		if (fieldDefinition_0.IsInitOnly)
		{
			stringBuilder.Append(Class8.smethod_118());
		}
		if (fieldDefinition_0.IsNotSerialized)
		{
			stringBuilder.Append(Class8.smethod_119());
		}
		stringBuilder.Append(Class8.smethod_18());
		stringBuilder.Append(_003C(fieldDefinition_0.FieldType) + " ");
		stringBuilder.Append(GClass2.smethod_0(fieldDefinition_0) + fieldDefinition_0.Name + "<<>>");
		if (fieldDefinition_0.Constant != null)
		{
			stringBuilder.Append(" = ");
			if (fieldDefinition_0.Constant is string)
			{
				stringBuilder.Append($"{_003C(fieldDefinition_0.FieldType)}(<<r>>'{RuntimeHelpers.GetObjectValue(fieldDefinition_0.Constant)}'<<>>)");
			}
			else
			{
				stringBuilder.Append($"{_003C(fieldDefinition_0.FieldType)}(<<r>>{RuntimeHelpers.GetObjectValue(fieldDefinition_0.Constant)}<<>>)");
			}
		}
		if (fieldDefinition_0.CustomAttributes.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("{");
			method_5(stringBuilder, fieldDefinition_0.CustomAttributes);
			stringBuilder.Append("}");
		}
		return stringBuilder.ToString();
	}

	public override string vmethod_7(EventDefinition eventDefinition_0)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<<b>>");
		stringBuilder.Append(".event ");
		if (eventDefinition_0.IsSpecialName)
		{
			stringBuilder.Append("specialname ");
		}
		if (eventDefinition_0.IsRuntimeSpecialName)
		{
			stringBuilder.Append("rtspecialname ");
		}
		stringBuilder.Append("<<>>");
		stringBuilder.Append(method_6(eventDefinition_0.EventType) + " ");
		stringBuilder.AppendLine(GClass2.smethod_0(eventDefinition_0) + eventDefinition_0.Name + "<<>>");
		stringBuilder.AppendLine("{");
		checked
		{
			if (eventDefinition_0.AddMethod != null)
			{
				MethodDefinition addMethod = eventDefinition_0.AddMethod;
				stringBuilder.Append(Class8.smethod_120());
				if (!addMethod.IsStatic)
				{
					stringBuilder.Append(Class8.smethod_121());
				}
				stringBuilder.Append(_003C(addMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(addMethod.DeclaringType), GClass2.smethod_0(addMethod) + addMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (addMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter in addMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter.ParameterType)} {parameter.Name}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			if (eventDefinition_0.RemoveMethod != null)
			{
				MethodDefinition removeMethod = eventDefinition_0.RemoveMethod;
				stringBuilder.Append("    <<b>>.removeon<<>> ");
				if (!removeMethod.IsStatic)
				{
					stringBuilder.Append("<<b>>instance<<>> ");
				}
				stringBuilder.Append(_003C(removeMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(removeMethod.DeclaringType), GClass2.smethod_0(removeMethod) + removeMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (removeMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter2 in removeMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter2.ParameterType)} {parameter2.Name}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			if (eventDefinition_0.InvokeMethod != null)
			{
				MethodDefinition invokeMethod = eventDefinition_0.InvokeMethod;
				stringBuilder.Append("    <<b>>.invokeon<<>> ");
				if (!invokeMethod.IsStatic)
				{
					stringBuilder.Append("<<b>>instance<<>> ");
				}
				stringBuilder.Append(_003C(invokeMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(invokeMethod.DeclaringType), GClass2.smethod_0(invokeMethod) + invokeMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (invokeMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter3 in invokeMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter3.ParameterType)} {parameter3.Name}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			foreach (MethodDefinition otherMethod in eventDefinition_0.OtherMethods)
			{
				stringBuilder.Append("    <<b>>.method<<>> ");
				if (!otherMethod.IsStatic)
				{
					stringBuilder.Append("<<b>>instance<<>> ");
				}
				stringBuilder.Append(_003C(otherMethod.ReturnType) + " ");
				stringBuilder.Append(string.Format("{0}::{1}", method_6(otherMethod.DeclaringType), GClass2.smethod_0(otherMethod) + otherMethod.Name + "<<>>"));
				stringBuilder.Append("(");
				if (otherMethod.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter4 in otherMethod.Parameters)
					{
						stringBuilder.Append($"{_003C(parameter4.ParameterType)} {parameter4.Name}, ");
					}
					stringBuilder.Remove(stringBuilder.Length - 2, 2);
				}
				stringBuilder.AppendLine(")");
			}
			method_5(stringBuilder, eventDefinition_0.CustomAttributes);
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}
	}

	private void method_5(StringBuilder stringBuilder_0, Collection<CustomAttribute> collection_0)
	{
		checked
		{
			foreach (CustomAttribute item in collection_0)
			{
				stringBuilder_0.Append(Class8.smethod_122() + method_6(item.AttributeType) + Class8.smethod_123());
				if (item.Constructor.Parameters.Count > 0)
				{
					foreach (ParameterDefinition parameter in item.Constructor.Parameters)
					{
						stringBuilder_0.Append(_003C(parameter.ParameterType) + ", ");
					}
					stringBuilder_0.Remove(stringBuilder_0.Length - 2, 2);
				}
				stringBuilder_0.Append(")");
				if (item.ConstructorArguments.Count > 0)
				{
					stringBuilder_0.Append(" = { ");
					foreach (CustomAttributeArgument constructorArgument in item.ConstructorArguments)
					{
						if (constructorArgument.Value is string)
						{
							stringBuilder_0.Append($"{_003C(constructorArgument.Type)}(<<r>>'{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}'<<>>), ");
						}
						else
						{
							stringBuilder_0.Append($"{_003C(constructorArgument.Type)}(<<r>>{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}<<>>), ");
						}
					}
					if (item.ConstructorArguments.Count > 0)
					{
						stringBuilder_0.Remove(stringBuilder_0.Length - 2, 2);
					}
					stringBuilder_0.Append(" }");
				}
				stringBuilder_0.AppendLine();
			}
		}
	}

	private string _003C(TypeReference typeReference_0)
	{
		if (typeReference_0.IsGenericParameter)
		{
			return typeReference_0.Name;
		}
		string name = typeReference_0.Name;
		string text = "";
		string text2 = "";
		if (typeReference_0.IsArray)
		{
			text = "[]";
		}
		if (typeReference_0.IsPointer)
		{
			text2 = "*";
		}
		if (name.EndsWith("&"))
		{
			text2 = "&";
		}
		typeReference_0 = typeReference_0.GetElementType();
		if (typeReference_0 == null)
		{
			name = "<<b>>void<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Void", TextCompare: false) == 0)
		{
			name = "<<b>>void<<>>";
		}
		else if (typeReference_0.IsArray)
		{
			name = "<<b>>array<<>> " + method_6(typeReference_0);
		}
		else if (Operators.CompareString(typeReference_0.Name, "String", TextCompare: false) == 0)
		{
			name = "<<b>>string<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Char", TextCompare: false) == 0)
		{
			name = "<<b>>char<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Boolean", TextCompare: false) == 0)
		{
			name = "<<b>>bool<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Object", TextCompare: false) == 0)
		{
			name = "<<b>>object<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Int16", TextCompare: false) == 0)
		{
			name = "<<b>>int16<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Int32", TextCompare: false) == 0)
		{
			name = "<<b>>int32<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, "Int64", TextCompare: false) == 0)
		{
			name = "<<b>>int64<<>>";
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_124(), TextCompare: false) == 0)
		{
			name = Class8.smethod_125();
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_39(), TextCompare: false) == 0)
		{
			name = Class8.smethod_126();
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_41(), TextCompare: false) == 0)
		{
			name = Class8.smethod_127();
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_43(), TextCompare: false) == 0)
		{
			name = Class8.smethod_128();
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_45(), TextCompare: false) == 0)
		{
			name = Class8.smethod_129();
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_47(), TextCompare: false) == 0)
		{
			name = Class8.smethod_130();
		}
		else if (Operators.CompareString(typeReference_0.Name, Class8.smethod_49(), TextCompare: false) == 0)
		{
			name = Class8.smethod_131();
		}
		else
		{
			if (Operators.CompareString(typeReference_0.Name, Class8.smethod_51(), TextCompare: false) != 0)
			{
				if (typeReference_0.IsValueType)
				{
					return Class8.smethod_62() + method_6(typeReference_0) + text + text2;
				}
				return Class8.smethod_63() + method_6(typeReference_0) + text + text2;
			}
			name = Class8.smethod_132();
		}
		return GClass2.smethod_0(typeReference_0) + name + Class8.smethod_18() + text + text2;
	}

	private string method_6(TypeReference typeReference_0)
	{
		return GClass2.smethod_0(GClass2.smethod_1(typeReference_0.Scope)) + "[" + GClass2.smethod_1(typeReference_0.Scope).Name + "]<<>>" + GClass2.smethod_0(typeReference_0) + typeReference_0.Namespace + "." + GClass2.smethod_3(typeReference_0) + "<<>>";
	}

	public override string ToString()
	{
		return "IL";
	}
}
