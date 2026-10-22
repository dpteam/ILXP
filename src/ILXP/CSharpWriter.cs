using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace ILXP
{
    public sealed class CSharpWriter : LanguageWriter
    {
    	[DebuggerNonUserCode]
    	public CSharpWriter()
    	{
    	}

    	public override string WriteAssembly(AssemblyItem gclass0_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("<<g>>" + GetDialectHeader().TrimEnd() + "<<>>");
    		if (Runtime != null)
    		{
    			stringBuilder.AppendLine("<<g>>" + GetRuntimeHeader().TrimEnd() + "<<>>");
    		}
    		stringBuilder.AppendLine("<<g>>//Name: " + gclass0_0.Assembly.Name.Name + "<<>>");
    		stringBuilder.AppendLine("<<g>>//Path: " + gclass0_0.FilePath + "<<>>");
    		stringBuilder.AppendLine("<<g>>//Version: " + $"{gclass0_0.Assembly.Name.Version.Major}:{gclass0_0.Assembly.Name.Version.MajorRevision}:{gclass0_0.Assembly.Name.Version.Minor}:{gclass0_0.Assembly.Name.Version.MinorRevision}" + "<<>>");
    		stringBuilder.AppendLine();
    		foreach (CustomAttribute customAttribute in gclass0_0.Assembly.CustomAttributes)
    		{
    			stringBuilder.Append("[<<b>>Assembly<<>>: ");
    			stringBuilder.Append(GetTypeName(customAttribute.AttributeType) + "(");
    			foreach (CustomAttributeArgument constructorArgument in customAttribute.ConstructorArguments)
    			{
    				if ((constructorArgument.Value is string) | (constructorArgument.Value is char))
    				{
    					stringBuilder.Append($"<<r>>\"{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}\"<<>>, ");
    				}
    				else
    				{
    					stringBuilder.Append($"<<r>>{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}<<>>, ");
    				}
    			}
    			if (customAttribute.ConstructorArguments.Count > 0)
    			{
    				stringBuilder.Remove(checked(stringBuilder.Length - 2), 2);
    			}
    			stringBuilder.AppendLine(")]");
    		}
    		return stringBuilder.ToString();
    	}

    	public override string WriteNamespace(ModuleItem gclass6_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		if (!string.IsNullOrEmpty(gclass6_0.Namespace))
    		{
    			stringBuilder.AppendLine("<<b>>namespace<<>> " + LanguageWriter.GetReferenceKey(gclass6_0) + gclass6_0.Namespace + "<<>>");
    			stringBuilder.AppendLine("{");
    		}
    		StringBuilder stringBuilder2 = new StringBuilder();
    		foreach (TypeDefinition type in gclass6_0.Module.Types)
    		{
    			if (Operators.CompareString(type.Namespace, gclass6_0.Namespace, TextCompare: false) == 0)
    			{
    				stringBuilder2.AppendLine(WriteType(type, bool_0: true));
    				stringBuilder2.AppendLine();
    			}
    		}
    		stringBuilder2.Replace(Environment.NewLine, Environment.NewLine + "    ");
    		stringBuilder.AppendLine("    " + stringBuilder2.ToString().Trim());
    		if (!string.IsNullOrEmpty(gclass6_0.Namespace))
    		{
    			stringBuilder.Append("}");
    		}
    		return stringBuilder.ToString();
    	}

    	public override string WriteModule(ModuleDefinition moduleDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("<<g>>//Module: " + moduleDefinition_0.Name + "<<>>");
    		stringBuilder.Append("<<g>>//Runtime Version: " + moduleDefinition_0.Runtime.ToString() + "<<>>");
    		return stringBuilder.ToString();
    	}

    	public override string WriteField(FieldDefinition fieldDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		WriteCustomAttributes(stringBuilder, fieldDefinition_0.CustomAttributes);
    		if (fieldDefinition_0.DeclaringType.IsEnum)
    		{
    			stringBuilder.Append(LanguageWriter.GetReferenceKey(fieldDefinition_0) + fieldDefinition_0.Name + "<<>>");
    		}
    		else
    		{
    			stringBuilder.Append("<<b>>");
    			if (fieldDefinition_0.IsPublic)
    			{
    				stringBuilder.Append("public ");
    			}
    			else if (fieldDefinition_0.IsPrivate)
    			{
    				stringBuilder.Append("private ");
    			}
    			else if (fieldDefinition_0.IsAssembly)
    			{
    				stringBuilder.Append("internal ");
    			}
    			else if (fieldDefinition_0.IsFamily)
    			{
    				stringBuilder.Append("protected ");
    			}
    			if (fieldDefinition_0.IsLiteral & fieldDefinition_0.IsStatic)
    			{
    				stringBuilder.Append("const ");
    			}
    			if (fieldDefinition_0.IsStatic & !fieldDefinition_0.IsLiteral)
    			{
    				stringBuilder.Append("static ");
    			}
    			if (fieldDefinition_0.IsInitOnly)
    			{
    				stringBuilder.Append("readonly ");
    			}
    			stringBuilder.Append("<<>>");
    			stringBuilder.Append(GetTypeName(fieldDefinition_0.FieldType));
    			stringBuilder.Append(" " + LanguageWriter.GetReferenceKey(fieldDefinition_0) + fieldDefinition_0.Name + "<<>>");
    		}
    		if (fieldDefinition_0.Constant != null)
    		{
    			stringBuilder.Append(" = ");
    			if (fieldDefinition_0.Constant is string)
    			{
    				stringBuilder.Append(string.Format("<<r>>\"{0}\"<<>>", RuntimeHelpers.GetObjectValue(fieldDefinition_0.Constant)));
    			}
    			else
    			{
    				stringBuilder.Append(string.Format("<<r>>{0}<<>>", RuntimeHelpers.GetObjectValue(fieldDefinition_0.Constant)));
    			}
    		}
    		stringBuilder.Append(";");
    		return stringBuilder.ToString();
    	}

    	public override string WriteType(TypeDefinition typeDefinition_0)
    	{
    		return WriteType(typeDefinition_0, bool_0: false);
    	}

    	public string WriteType(TypeDefinition typeDefinition_0, bool bool_0)
    	{
    		string text = LanguageWriter.GetReferenceKey(typeDefinition_0);
    		checked
    		{
    			if (typeDefinition_0.HasGenericParameters)
    			{
    				string text2 = typeDefinition_0.Name;
    				if (typeDefinition_0.Name.Length > 2 && Operators.CompareString(typeDefinition_0.Name.Substring(typeDefinition_0.Name.Length - 2, 1), "`", TextCompare: false) == 0)
    				{
    					text2 = typeDefinition_0.Name.Substring(0, typeDefinition_0.Name.Length - 2);
    				}
    				text = text + text2 + "<<>>";
    				text += "<";
    				text2 = "";
    				foreach (GenericParameter genericParameter in typeDefinition_0.GenericParameters)
    				{
    					text2 = Conversions.ToString(Operators.ConcatenateObject(text2, Operators.ConcatenateObject(Interaction.IIf(string.IsNullOrEmpty(genericParameter.Name), "?", genericParameter.Name), ",")));
    				}
    				text2 = text2.TrimEnd(',') + ">";
    				text += text2;
    			}
    			else
    			{
    				text = text + typeDefinition_0.Name + "<<>>";
    			}
    			StringBuilder stringBuilder = new StringBuilder();
    			WriteCustomAttributes(stringBuilder, typeDefinition_0.CustomAttributes);
    			stringBuilder.Append("<<b>>");
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
    				stringBuilder.Append("private ");
    			}
    			else if (typeDefinition_0.IsNestedPublic)
    			{
    				stringBuilder.Append("public ");
    			}
    			else if (typeDefinition_0.IsNestedFamilyAndAssembly)
    			{
    				stringBuilder.Append("protected internal ");
    			}
    			else if (typeDefinition_0.IsNestedAssembly)
    			{
    				stringBuilder.Append("internal ");
    			}
    			else if (typeDefinition_0.IsNestedFamily)
    			{
    				stringBuilder.Append("protected ");
    			}
    			if (typeDefinition_0.BaseType != null && typeDefinition_0.BaseType.FullName.EndsWith("Delegate"))
    			{
    				foreach (MethodDefinition method in typeDefinition_0.Methods)
    				{
    					if (Operators.CompareString(method.Name, "Invoke", TextCompare: false) != 0)
    					{
    						continue;
    					}
    					stringBuilder.Append("delegate ");
    					stringBuilder.Append(GetTypeName(method.ReturnType) + " ");
    					stringBuilder.Append(text);
    					stringBuilder.Append("(");
    					if (method.Parameters.Count > 0)
    					{
    						foreach (ParameterDefinition parameter in method.Parameters)
    						{
    							if (parameter.IsOptional)
    							{
    								if (parameter.Constant == null)
    								{
    									stringBuilder.Append("[Optional, DefaultParameterValue(<<b>>null<<>>)] ");
    								}
    								else if (parameter.Constant is string)
    								{
    									stringBuilder.Append($"[Optional, DefaultParameterValue(<<r>>\"{RuntimeHelpers.GetObjectValue(parameter.Constant)}\"<<>>)] ");
    								}
    								else
    								{
    									stringBuilder.Append($"[Optional, DefaultParameterValue(<<r>>{RuntimeHelpers.GetObjectValue(parameter.Constant)}<<>>)] ");
    								}
    							}
    							if (parameter.IsOut)
    							{
    								stringBuilder.Append("<<b>out<<>> ");
    							}
    							stringBuilder.Append(string.Format("{1} {0}, ", parameter.Name, GetTypeName(parameter.ParameterType)));
    						}
    						stringBuilder.Remove(stringBuilder.Length - 2, 2);
    					}
    					stringBuilder.Append(")");
    					stringBuilder.Append(";");
    					return stringBuilder.ToString();
    				}
    			}
    			if (typeDefinition_0.IsSealed && !(typeDefinition_0.IsValueType | typeDefinition_0.IsEnum))
    			{
    				stringBuilder.Append("sealed ");
    			}
    			if (typeDefinition_0.IsAbstract)
    			{
    				stringBuilder.Append("abstract ");
    			}
    			if (typeDefinition_0.IsEnum)
    			{
    				stringBuilder.Append("enum ");
    			}
    			else if (typeDefinition_0.IsInterface)
    			{
    				stringBuilder.Append("interface ");
    			}
    			else if (typeDefinition_0.IsValueType)
    			{
    				stringBuilder.Append("struct ");
    			}
    			else if (typeDefinition_0.IsClass)
    			{
    				stringBuilder.Append("class ");
    			}
    			stringBuilder.Append("<<>>");
    			stringBuilder.Append(text);
    			if (!typeDefinition_0.IsValueType && !typeDefinition_0.IsInterface && !typeDefinition_0.IsEnum && typeDefinition_0.BaseType != null && Operators.CompareString(typeDefinition_0.BaseType.FullName, "System.Object", TextCompare: false) != 0)
    			{
    				stringBuilder.Append(":");
    				stringBuilder.Append(GetQualifiedTypeName(typeDefinition_0.BaseType));
    				if (typeDefinition_0.Interfaces.Count > 0)
    				{
    					stringBuilder.Append(", ");
    				}
    			}
    			else if (typeDefinition_0.Interfaces.Count > 0)
    			{
    				stringBuilder.Append(":");
    			}
    			if (typeDefinition_0.Interfaces.Count > 0)
    			{
    				foreach (TypeReference @interface in typeDefinition_0.Interfaces)
    				{
    					stringBuilder.Append(GetQualifiedTypeName(@interface) + ", ");
    				}
    				stringBuilder.Remove(stringBuilder.Length - 2, 2);
    			}
    			stringBuilder.AppendLine("{");
    			if (!bool_0)
    			{
    				StringBuilder stringBuilder2 = new StringBuilder();
    				if (typeDefinition_0.NestedTypes.Count > 0)
    				{
    					stringBuilder2.AppendLine("<<g>>// Nested Types<<>>");
    					foreach (TypeDefinition nestedType in typeDefinition_0.NestedTypes)
    					{
    						if (!LanguageWriter.IsCompilerGeneratedName(nestedType.Name))
    						{
    							stringBuilder2.AppendLine(WriteType(nestedType, bool_0: true));
    						}
    					}
    					stringBuilder2.AppendLine();
    				}
    				if (typeDefinition_0.Fields.Count > 0)
    				{
    					stringBuilder2.AppendLine("<<g>>// Fields<<>>");
    					foreach (FieldDefinition field in typeDefinition_0.Fields)
    					{
    						if (Operators.CompareString(field.Name, "value__", TextCompare: false) != 0 || !typeDefinition_0.IsEnum)
    						{
    							stringBuilder2.AppendLine(WriteField(field));
    						}
    					}
    					stringBuilder2.AppendLine();
    				}
    				if (typeDefinition_0.Properties.Count > 0)
    				{
    					stringBuilder2.AppendLine("<<g>>// Properties<<>>");
    					foreach (PropertyDefinition property in typeDefinition_0.Properties)
    					{
    						stringBuilder2.AppendLine(WriteProperty(property, bool_0: true));
    					}
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
    					foreach (MethodDefinition method2 in typeDefinition_0.Methods)
    					{
    						if (!list.Contains(method2.Name) && !LanguageWriter.IsCompilerGeneratedName(method2.Name))
    						{
    							stringBuilder2.AppendLine(WriteMethod(method2, bool_0: true));
    						}
    					}
    					stringBuilder2.AppendLine();
    				}
    				if (typeDefinition_0.Events.Count > 0)
    				{
    					stringBuilder2.AppendLine("<<g>>// Events<<>>");
    					foreach (EventDefinition event2 in typeDefinition_0.Events)
    					{
    						stringBuilder2.AppendLine(WriteEvent(event2));
    					}
    				}
    				stringBuilder2.Replace(Environment.NewLine, Environment.NewLine + "    ");
    				stringBuilder.AppendLine("    " + stringBuilder2.ToString().Trim());
    			}
    			stringBuilder.Append("}");
    			return stringBuilder.ToString();
    		}
    	}

    	public override string WriteEvent(EventDefinition eventDefinition_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		WriteCustomAttributes(stringBuilder, eventDefinition_0.CustomAttributes);
    		MethodDefinition methodDefinition = eventDefinition_0.AddMethod;
    		stringBuilder.Append("<<b>>");
    		if (!methodDefinition.DeclaringType.IsInterface)
    		{
    			if (methodDefinition.IsPrivate)
    			{
    				stringBuilder.Append("private ");
    			}
    			else if (methodDefinition.IsAssembly)
    			{
    				stringBuilder.Append("internal ");
    			}
    			else if (methodDefinition.IsFamily)
    			{
    				stringBuilder.Append("protected ");
    			}
    			else if (methodDefinition.IsPublic)
    			{
    				stringBuilder.Append("public ");
    			}
    			if (methodDefinition.IsStatic)
    			{
    				stringBuilder.Append("static ");
    			}
    			if (methodDefinition.IsVirtual & methodDefinition.IsNewSlot)
    			{
    				stringBuilder.Append("virtual ");
    			}
    			if (methodDefinition.IsVirtual & methodDefinition.IsReuseSlot)
    			{
    				stringBuilder.Append("override ");
    			}
    			if (!methodDefinition.IsVirtual & methodDefinition.IsNewSlot)
    			{
    				stringBuilder.Append("new ");
    			}
    		}
    		stringBuilder.Append("event ");
    		stringBuilder.Append(GetTypeName(eventDefinition_0.EventType) + " ");
    		stringBuilder.Append(LanguageWriter.GetReferenceKey(eventDefinition_0) + eventDefinition_0.Name + "<<>>");
    		stringBuilder.Append(";");
    		return stringBuilder.ToString();
    	}

    	public override string WriteMethod(MethodDefinition methodDefinition_0)
    	{
    		return WriteMethod(methodDefinition_0, bool_0: false);
    	}

    	public string WriteMethod(MethodDefinition methodDefinition_0, bool bool_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		WriteCustomAttributes(stringBuilder, methodDefinition_0.CustomAttributes);
    		if (methodDefinition_0.IsPInvokeImpl)
    		{
    			stringBuilder.AppendLine("[DllImport(<<r>>\"" + methodDefinition_0.PInvokeInfo.Module.Name + "\"<<>>)]");
    		}
    		stringBuilder.Append("<<b>>");
    		if (!methodDefinition_0.DeclaringType.IsInterface)
    		{
    			if (methodDefinition_0.IsPrivate)
    			{
    				stringBuilder.Append("private ");
    			}
    			else if (methodDefinition_0.IsAssembly)
    			{
    				stringBuilder.Append("internal ");
    			}
    			else if (methodDefinition_0.IsFamily)
    			{
    				stringBuilder.Append("protected ");
    			}
    			else if (methodDefinition_0.IsPublic)
    			{
    				stringBuilder.Append("public ");
    			}
    			if (methodDefinition_0.IsStatic)
    			{
    				stringBuilder.Append("static ");
    			}
    			if (methodDefinition_0.IsVirtual & methodDefinition_0.IsNewSlot)
    			{
    				stringBuilder.Append("virtual ");
    			}
    			if (methodDefinition_0.IsVirtual & methodDefinition_0.IsReuseSlot)
    			{
    				stringBuilder.Append("override ");
    			}
    			if (!methodDefinition_0.IsVirtual & methodDefinition_0.IsNewSlot)
    			{
    				stringBuilder.Append("new ");
    			}
    		}
    		if (!methodDefinition_0.IsConstructor)
    		{
    			stringBuilder.Append(GetTypeName(methodDefinition_0.ReturnType) + " ");
    		}
    		if (methodDefinition_0.IsConstructor)
    		{
    			stringBuilder.Append(LanguageWriter.GetReferenceKey(methodDefinition_0) + methodDefinition_0.DeclaringType.Name + "<<>>");
    		}
    		else
    		{
    			stringBuilder.Append(LanguageWriter.GetReferenceKey(methodDefinition_0) + methodDefinition_0.Name + "<<>>");
    		}
    		stringBuilder.Append("(");
    		if (methodDefinition_0.Parameters.Count > 0)
    		{
    			foreach (ParameterDefinition parameter in methodDefinition_0.Parameters)
    			{
    				if (parameter.IsOptional)
    				{
    					if (parameter.Constant == null)
    					{
    						stringBuilder.Append("[Optional, DefaultParameterValue(<<b>>null<<>>)] ");
    					}
    					else if (parameter.Constant is string)
    					{
    						stringBuilder.Append($"[Optional, DefaultParameterValue(<<r>>\"{RuntimeHelpers.GetObjectValue(parameter.Constant)}\"<<>>)] ");
    					}
    					else
    					{
    						stringBuilder.Append($"[Optional, DefaultParameterValue(<<r>>{RuntimeHelpers.GetObjectValue(parameter.Constant)}<<>>)] ");
    					}
    				}
    				if (parameter.IsOut)
    				{
    					stringBuilder.Append("<<b>>out<<>> ");
    				}
    				if (parameter.ParameterType.IsByReference)
    				{
    					stringBuilder.Append("<<b>>ref<<>> ");
    				}
    				stringBuilder.Append(string.Format("{1} {0}, ", parameter.Name, GetTypeName(parameter.ParameterType)));
    			}
    			stringBuilder.Remove(checked(stringBuilder.Length - 2), 2);
    		}
    		stringBuilder.Append(")");
    		if (bool_0 | methodDefinition_0.DeclaringType.IsInterface)
    		{
    			stringBuilder.Append(";");
    			return stringBuilder.ToString();
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("{");
    		StatementDecompiler.WriteCSharpBody(stringBuilder, methodDefinition_0, 1);
    		stringBuilder.Append("}");
    		return stringBuilder.ToString();
    	}

    	public override string WriteProperty(PropertyDefinition propertyDefinition_0)
    	{
    		return WriteProperty(propertyDefinition_0, bool_0: false);
    	}

    	public string WriteProperty(PropertyDefinition propertyDefinition_0, bool bool_0)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		WriteCustomAttributes(stringBuilder, propertyDefinition_0.CustomAttributes);
    		stringBuilder.Append("<<b>>");
    		if (!propertyDefinition_0.DeclaringType.IsInterface)
    		{
    			if (propertyDefinition_0.GetMethod != null)
    			{
    				if (propertyDefinition_0.GetMethod.IsPrivate)
    				{
    					stringBuilder.Append("private ");
    				}
    				else if (propertyDefinition_0.GetMethod.IsAssembly)
    				{
    					stringBuilder.Append("internal ");
    				}
    				else if (propertyDefinition_0.GetMethod.IsFamily)
    				{
    					stringBuilder.Append("protected ");
    				}
    				else if (propertyDefinition_0.GetMethod.IsPublic)
    				{
    					stringBuilder.Append("public ");
    				}
    			}
    			else if (propertyDefinition_0.SetMethod != null)
    			{
    				if (propertyDefinition_0.SetMethod.IsPrivate)
    				{
    					stringBuilder.Append("private ");
    				}
    				else if (propertyDefinition_0.SetMethod.IsAssembly)
    				{
    					stringBuilder.Append("internal ");
    				}
    				else if (propertyDefinition_0.SetMethod.IsFamily)
    				{
    					stringBuilder.Append("protected ");
    				}
    				else if (propertyDefinition_0.SetMethod.IsPublic)
    				{
    					stringBuilder.Append("public ");
    				}
    			}
    		}
    		stringBuilder.Append("<<>>");
    		stringBuilder.Append(GetTypeName(propertyDefinition_0.PropertyType) + " ");
    		stringBuilder.Append(LanguageWriter.GetReferenceKey(propertyDefinition_0) + propertyDefinition_0.Name + "<<>>");
    		if (propertyDefinition_0.Parameters.Count > 0)
    		{
    			stringBuilder.Append("(");
    			foreach (ParameterDefinition parameter in propertyDefinition_0.Parameters)
    			{
    				if (parameter.IsOptional)
    				{
    					if (parameter.Constant == null)
    					{
    						stringBuilder.Append("[Optional, DefaultParameterValue(<<b>>null<<>>)] ");
    					}
    					else if (parameter.Constant is string)
    					{
    						stringBuilder.Append($"[Optional, DefaultParameterValue(<<r>>\"{RuntimeHelpers.GetObjectValue(parameter.Constant)}\"<<>>)] ");
    					}
    					else
    					{
    						stringBuilder.Append($"[Optional, DefaultParameterValue(<<r>>{RuntimeHelpers.GetObjectValue(parameter.Constant)}<<>>)] ");
    					}
    				}
    				if (parameter.IsOut)
    				{
    					stringBuilder.Append("<<b>out<<>> ");
    				}
    				stringBuilder.Append(string.Format("{1} {0}, ", parameter.Name, GetTypeName(parameter.ParameterType)));
    			}
    			stringBuilder.Remove(checked(stringBuilder.Length - 2), 2);
    			stringBuilder.Append(")");
    		}
    		if (bool_0 | propertyDefinition_0.DeclaringType.IsInterface)
    		{
    			stringBuilder.Append(" {");
    			if (propertyDefinition_0.GetMethod != null)
    			{
    				stringBuilder.Append(" get;");
    			}
    			if (propertyDefinition_0.SetMethod != null)
    			{
    				stringBuilder.Append(" set;");
    			}
    			stringBuilder.Append(" }");
    			return stringBuilder.ToString();
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("{");
    		if (propertyDefinition_0.GetMethod != null)
    		{
    			MethodDefinition getMethod = propertyDefinition_0.GetMethod;
    			stringBuilder.AppendLine("    <<b>>get<<>>");
    			stringBuilder.AppendLine("    {");
    			StatementDecompiler.WriteCSharpBody(stringBuilder, getMethod, 2);
    			stringBuilder.AppendLine("    }");
    		}
    		if (propertyDefinition_0.SetMethod != null)
    		{
    			MethodDefinition setMethod = propertyDefinition_0.SetMethod;
    			stringBuilder.AppendLine("    <<b>>set<<>>");
    			stringBuilder.AppendLine("    {");
    			StatementDecompiler.WriteCSharpBody(stringBuilder, setMethod, 2);
    			stringBuilder.AppendLine("    }");
    		}
    		stringBuilder.Append("}");
    		return stringBuilder.ToString();
    	}

    	private void WriteCustomAttributes(StringBuilder stringBuilder_0, Collection<CustomAttribute> collection_0)
    	{
    		if (collection_0.Count == 0)
    		{
    			return;
    		}
    		stringBuilder_0.Append("[");
    		checked
    		{
    			foreach (CustomAttribute item in collection_0)
    			{
    				string text = GetTypeName(item.AttributeType);
    				if (text.EndsWith("Attribute<<>>"))
    				{
    					text = text.Replace("Attribute<<>>", "<<>>");
    				}
    				stringBuilder_0.Append(text + "(");
    				foreach (CustomAttributeArgument constructorArgument in item.ConstructorArguments)
    				{
    					if ((constructorArgument.Value is string) | (constructorArgument.Value is char))
    					{
    						stringBuilder_0.Append($"<<r>>\"{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}\"<<>>, ");
    					}
    					else
    					{
    						stringBuilder_0.Append($"<<r>>{RuntimeHelpers.GetObjectValue(constructorArgument.Value)}<<>>, ");
    					}
    				}
    				if (item.ConstructorArguments.Count > 0)
    				{
    					stringBuilder_0.Remove(stringBuilder_0.Length - 2, 2);
    				}
    				stringBuilder_0.Append("), ");
    			}
    			stringBuilder_0.Remove(stringBuilder_0.Length - 2, 2);
    			stringBuilder_0.AppendLine("]");
    		}
    	}

    	private string GetQualifiedTypeName(TypeReference type)
    	{
    		return LanguageWriter.GetReferenceKey(type) + type.Namespace + "." + type.Name.Replace("&", string.Empty) + "<<>>";
    	}

    	private string GetTypeName(TypeReference type)
    	{
    		if (type == null)
    		{
    			return "<<b>>void<<b>>";
    		}
    		if (type.IsGenericParameter)
    		{
    			return type.Name;
    		}

    		string arraySuffix = string.Empty;
    		string pointerSuffix = string.Empty;
    		if (type.IsArray)
    		{
    			int rank = type is ArrayType ? ((ArrayType)type).Rank : 1;
    			arraySuffix = "[" + new string(',', rank - 1) + "]";
    			type = type.GetElementType();
    		}
    		if (type.IsPointer)
    		{
    			pointerSuffix += "*";
    			type = type.GetElementType();
    		}
    		if (type.IsByReference)
    		{
    			// "ref" is emitted by the parameter writer, so only unwrap the type here.
    			type = type.GetElementType();
    		}

    		string name;
    		GenericInstanceType generic = type as GenericInstanceType;
    		if (generic != null)
    		{
    			string elementName = generic.ElementType.Name;
    			int tick = elementName.IndexOf('`');
    			if (tick >= 0)
    			{
    				elementName = elementName.Substring(0, tick);
    			}
    			StringBuilder arguments = new StringBuilder();
    			foreach (TypeReference argument in generic.GenericArguments)
    			{
    				if (arguments.Length > 0)
    				{
    					arguments.Append(", ");
    				}
    				arguments.Append(GetTypeName(argument));
    			}
    			name = elementName + "<" + arguments + ">";
    		}
    		else
    		{
    			name = GetKeywordTypeName(type.Name) ?? QualifyTypeName(type);
    		}

    		return LanguageWriter.GetReferenceKey(type) + "<<b>>" + name + "<<>>" + arraySuffix + pointerSuffix;
    	}

    	private static string GetKeywordTypeName(string name)
    	{
    		switch (name)
    		{
    			case "Void": return "void";
    			case "Boolean": return "bool";
    			case "Char": return "char";
    			case "SByte": return "sbyte";
    			case "Byte": return "byte";
    			case "Int16": return "short";
    			case "UInt16": return "ushort";
    			case "Int32": return "int";
    			case "UInt32": return "uint";
    			case "Int64": return "long";
    			case "UInt64": return "ulong";
    			case "Single": return "float";
    			case "Double": return "double";
    			case "Decimal": return "decimal";
    			case "String": return "string";
    			case "Object": return "object";
    			default: return null;
    		}
    	}

    	private static string QualifyTypeName(TypeReference type)
    	{
    		if (string.IsNullOrEmpty(type.Namespace) || type.Namespace == "System")
    		{
    			return type.Name;
    		}
    		switch (type.Namespace)
    		{
    			case "System.Reflection":
    			case "System.Runtime.InteropServices":
    			case "System.Runtime.CompilerServices":
    			case "System.Diagnostics":
    			case "System.ComponentModel":
    			case "System.CodeDom.Compiler":
    			case "Microsoft.VisualBasic":
    			case "Microsoft.VisualBasic.CompilerServices":
    				return type.Name;
    			default:
    				return type.Namespace + "." + type.Name;
    		}
    	}

    	public override string ToString()
    	{
    		return "C#";
    	}
    }

}