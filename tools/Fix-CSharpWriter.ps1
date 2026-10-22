$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$path = Join-Path $root 'src\ILXP\CSharpWriter.cs'
$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $path)

# --- 1. constructor must not print a return type ---------------------------
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'stringBuilder\.Append\(GetTypeName\(methodDefinition_0\.ReturnType\) \+ " "\);') {
        $lines[$i] = "    		if (!methodDefinition_0.IsConstructor)`r`n    		{`r`n    			stringBuilder.Append(GetTypeName(methodDefinition_0.ReturnType) + `" `");`r`n    		}"
        break
    }
}

# --- 2. C# event declaration ----------------------------------------------
$start = -1
$end = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'stringBuilder\.Append\("event "\);') { $start = $i; break }
}
if ($start -ge 0) {
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'return stringBuilder\.ToString\(\);') { $end = $i; break }
    }
}
if ($start -ge 0 -and $end -gt $start) {
    $replacement = @(
        '    		stringBuilder.Append("event ");',
        '    		stringBuilder.Append(GetTypeName(eventDefinition_0.EventType) + " ");',
        '    		stringBuilder.Append(LanguageWriter.GetReferenceKey(eventDefinition_0) + eventDefinition_0.Name + "<<>>");',
        '    		stringBuilder.Append(";");',
        '    		return stringBuilder.ToString();'
    )
    $lines.RemoveRange($start, $end - $start + 1)
    $lines.InsertRange($start, [string[]]$replacement)
    Write-Host 'rewrote C# event declaration'
}

# --- 3. type name formatting ----------------------------------------------
$typeStart = -1
$typeEnd = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'private string GetQualifiedTypeName') { $typeStart = $i; break }
}
for ($i = $typeStart; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'public override string ToString\(\)') { $typeEnd = $i; break }
}
if ($typeStart -lt 0 -or $typeEnd -lt 0) { throw 'type name block not found' }

$block = @'
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
    			pointerSuffix += "&";
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

'@
$lines.RemoveRange($typeStart, $typeEnd - $typeStart)
$lines.InsertRange($typeStart, ($block -split "`r?`n"))
Write-Host 'rewrote C# type name formatting'

Set-Content -LiteralPath $path -Value ($lines -join "`r`n") -NoNewline
Write-Host 'CSharpWriter patched'
