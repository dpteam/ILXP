using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>
    /// Base class for the three output languages supported by the application.
    /// Each writer turns Cecil metadata into text annotated with lightweight markup
    /// (<c>&lt;&lt;b&gt;&gt;</c>, <c>&lt;&lt;r&gt;&gt;</c>, ...) that the editor renders
    /// as colored tokens.
    /// </summary>
    public abstract class LanguageWriter
    {
        /// <summary>Selected output dialect (affects headers and optional syntax).</summary>
        public OutputDialect Dialect { get; set; }

        /// <summary>Runtime the input assembly targets (informational).</summary>
        public RuntimeProfile Runtime { get; set; }

        /// <summary>
        /// When set, <c>WriteType</c> emits method and property bodies instead of signatures
        /// only. Used by the project exporter; the GUI keeps signatures for a compact outline.
        /// </summary>
        public static bool EmitBodies { get; set; }

        /// <summary>Comment prefix used when writing the dialect header.</summary>
        protected virtual string CommentPrefix
        {
            get { return "// "; }
        }

        /// <summary>Header that documents which dialect the output targets.</summary>
        protected string GetDialectHeader()
        {
            if (Dialect == null)
            {
                return string.Empty;
            }

            return CommentPrefix + "Dialect: " + Dialect.DisplayName +
                   "  (" + Dialect.Toolchain + ")" + Environment.NewLine;
        }

        /// <summary>Header that documents the runtime the input was built for.</summary>
        protected string GetRuntimeHeader()
        {
            if (Runtime == null)
            {
                return string.Empty;
            }

            return CommentPrefix + "Target runtime: " + Runtime.FrameworkName +
                   (string.IsNullOrEmpty(Runtime.TargetFramework) ? string.Empty : "  [" + Runtime.TargetFramework + "]") +
                   Environment.NewLine;
        }

        public abstract string WriteAssembly(AssemblyItem assembly);

        public abstract string WriteNamespace(ModuleItem module);

        public abstract string WriteType(TypeDefinition type);

        public abstract string WriteModule(ModuleDefinition module);

        public abstract string WriteMethod(MethodDefinition method);

        public abstract string WriteProperty(PropertyDefinition property);

        public abstract string WriteField(FieldDefinition field);

        public abstract string WriteEvent(EventDefinition @event);

        /// <summary>
        /// Builds the navigation key ("&lt;&lt;#assembly//Nnamespace//Type&gt;&gt;")
        /// that the editor uses to jump to the declaration of a metadata reference.
        /// </summary>
        public static string GetReferenceKey(object value)
        {
            if (value is AssemblyItem)
            {
                return "<<#" + ((AssemblyItem)value).Assembly.Name.FullName + ">>";
            }
            if (value is AssemblyNameReference)
            {
                return "<<#" + ((AssemblyNameReference)value).FullName + ">>";
            }
            if (value is AssemblyDefinition)
            {
                return "<<#" + ((AssemblyDefinition)value).Name.FullName + ">>";
            }
            if (value is ModuleItem)
            {
                ModuleItem module = (ModuleItem)value;
                return "<<#" + module.Module.Assembly.Name.FullName + "//N" + module.Namespace + ">>";
            }
            if (value is TypeReference)
            {
                TypeReference type = (TypeReference)value;
                return "<<#" + GetAssemblyName(type.Scope).FullName + "//N" + type.Namespace + "//" +
                       type.GetElementType().Name.Replace("<", string.Empty).Replace(">", string.Empty) + ">>";
            }
            if (value is MethodReference)
            {
                MethodReference method = (MethodReference)value;
                return "<<#" + GetAssemblyName(method.DeclaringType.Scope).FullName + "//N" +
                       method.DeclaringType.Namespace + "//" + method.DeclaringType.Name + "//M" +
                       GetMethodSignature(method.GetElementMethod()) + ">>";
            }
            if (value is FieldReference)
            {
                FieldReference field = (FieldReference)value;
                return "<<#" + GetAssemblyName(field.DeclaringType.Scope).FullName + "//N" +
                       field.DeclaringType.Namespace + "//" + field.DeclaringType.Name + "//F" + field.Name + ">>";
            }
            if (value is PropertyDefinition)
            {
                PropertyDefinition property = (PropertyDefinition)value;
                return "<<#" + GetAssemblyName(property.DeclaringType.Scope).FullName + "//N" +
                       property.DeclaringType.Namespace + "//" + property.DeclaringType.Name + "//P" + property.Name + ">>";
            }
            if (value is EventDefinition)
            {
                EventDefinition @event = (EventDefinition)value;
                return "<<#" + GetAssemblyName(@event.DeclaringType.Scope).FullName + "//N" +
                       @event.DeclaringType.Namespace + "//" + @event.DeclaringType.Name + "//E" + @event.Name + ">>";
            }
            throw new ArgumentException("Unknown object type: " + (value == null ? "null" : value.GetType().FullName));
        }

        public static AssemblyNameReference GetAssemblyName(IMetadataScope scope)
        {
            AssemblyNameReference reference = scope as AssemblyNameReference;
            if (reference != null)
            {
                return reference;
            }

            ModuleDefinition module = scope as ModuleDefinition;
            if (module != null)
            {
                return module.Assembly.Name;
            }

            object rawScope = scope;
            AssemblyDefinition assembly = rawScope as AssemblyDefinition;
            if (assembly != null)
            {
                return assembly.Name;
            }

            throw new ArgumentException("Cannot determine scope type: " + (scope == null ? "null" : scope.GetType().FullName));
        }

        /// <summary>Removes the markup tags from a generated fragment.</summary>
        public string StripMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // A scanner instead of a regex: navigation keys embed arbitrary metadata names,
            // which may themselves contain angle brackets ("FCS$<>9__Cached..."), and a
            // character class cannot know where the key ends.
            StringBuilder result = new StringBuilder(text.Length);
            int index = 0;
            while (index < text.Length)
            {
                if (text[index] == '<' && index + 1 < text.Length && text[index + 1] == '<')
                {
                    char tag = index + 2 < text.Length ? text[index + 2] : '\0';

                    // A navigation key runs from "<<#" to the next ">>".
                    if (tag == '#')
                    {
                        int close = text.IndexOf(">>", index + 3, StringComparison.Ordinal);
                        index = close < 0 ? index + 2 : close + 2;
                        continue;
                    }

                    // Colour tags are exactly five characters: <<g>>, <<b>>, <<r>>.
                    if ((tag == 'g' || tag == 'b' || tag == 'r') &&
                        index + 4 < text.Length && text[index + 3] == '>' && text[index + 4] == '>')
                    {
                        index += 5;
                        continue;
                    }

                    // "<<>>" closes a coloured token (4 chars).
                    if (tag == '>' && index + 3 < text.Length && text[index + 3] == '>')
                    {
                        index += 4;
                        continue;
                    }

                    // Not a tag: this is a '<' of the generated code itself (a generic argument
                    // list directly followed by a key, "Func<<#...>>"), so emit it and retry
                    // one character later.
                    result.Append(text[index]);
                    index++;
                    continue;
                }

                result.Append(text[index]);
                index++;
            }

            return result.ToString();
        }

        public static string GetFullTypeName(TypeReference type)
        {
            return type.Namespace + "." + GetShortTypeName(type);
        }

        public static string GetShortTypeName(TypeReference type)
        {
            string name = type.GetElementType().Name;
            GenericInstanceType generic = type as GenericInstanceType;
            if (generic != null)
            {
                if (type.Name.Length > 2 && type.Name[type.Name.Length - 2] == '`')
                {
                    name = type.Name.Substring(0, type.Name.Length - 2);
                }

                name += "<";
                foreach (TypeReference argument in generic.GenericArguments)
                {
                    name += (string.IsNullOrEmpty(argument.Name) ? "?" : argument.Name) + ", ";
                }
                name = name.TrimEnd(' ', ',') + ">";
            }
            return name;
        }

        public static string GetMethodSignature(MethodReference method)
        {
            string text = "(";
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                text += parameter.ParameterType.Name + ",";
            }
            return method.Name + text.TrimEnd(',') + ")";
        }

        public static string GetMethodSignatureWithReturn(MethodReference method)
        {
            string text = method.Name + "(";
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                text += GetShortTypeName(parameter.ParameterType) + ",";
            }
            text = text.TrimEnd(',') + ")";
            if (!text.StartsWith(".", StringComparison.Ordinal) && method.ReturnType != null)
            {
                text += " : " + GetShortTypeName(method.ReturnType);
            }
            return text;
        }

        public static string FormatIlLabel(int offset)
        {
            return "<<r>>L_" + offset.ToString("x4") + "<<>>";
        }

        /// <summary>
        /// Compiler-generated members (lambdas, local functions, state machines) have
        /// names starting with '&lt;'. They are hidden from the high-level outline.
        /// </summary>
        internal static bool IsCompilerGeneratedName(string name)
        {
            return !string.IsNullOrEmpty(name) && name[0] == '<';
        }

        /// <summary>Chooses a VB-style prefix for a generated local variable name.</summary>
        internal string GetLocalNamePrefix(TypeReference type)
        {
            string name = type.GetElementType().Name.ToLowerInvariant();
            if (type.IsArray)
            {
                return "array";
            }
            if (name == "string")
            {
                return "str";
            }
            if (name == "boolean")
            {
                return "flag";
            }
            switch (name)
            {
                case "long":
                case "double":
                case "int16":
                case "int32":
                case "int64":
                case "uint16":
                case "uint32":
                case "uint64":
                    return "num";
                default:
                    return name;
            }
        }

        /// <summary>Creates a unique local name such as <c>num1</c>, <c>num2</c>, ...</summary>
        internal string CreateUniqueLocalName(TypeReference type, List<string> usedNames)
        {
            string prefix = GetLocalNamePrefix(type);
            int index = 1;
            while (usedNames.Contains(prefix + index.ToString()))
            {
                index++;
            }
            usedNames.Add(prefix + index.ToString());
            return prefix + index.ToString();
        }
    }
}
