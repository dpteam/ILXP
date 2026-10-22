using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace ILXP
{
    /// <summary>Structural completeness of a decompiled assembly.</summary>
    internal sealed class AccuracyReport
    {
        public int TypesTotal, TypesFound;
        public int MethodsTotal, MethodsFound;
        public int FieldsTotal, FieldsFound;
        public int PropertiesTotal, PropertiesFound;
        public int EventsTotal, EventsFound;
        public int Failures;
        public List<string> Missing = new List<string>();
        public string Writer;
        public string Assembly;

        public int Total
        {
            get { return TypesTotal + MethodsTotal + FieldsTotal + PropertiesTotal + EventsTotal; }
        }

        public int Found
        {
            get { return TypesFound + MethodsFound + FieldsFound + PropertiesFound + EventsFound; }
        }

        public double Percent
        {
            get { return Total == 0 ? 100.0 : 100.0 * Found / Total; }
        }

        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("assembly : " + Assembly);
            text.AppendLine("writer   : " + Writer);
            text.AppendLine();
            Append(text, "types     ", TypesFound, TypesTotal);
            Append(text, "methods   ", MethodsFound, MethodsTotal);
            Append(text, "fields    ", FieldsFound, FieldsTotal);
            Append(text, "properties", PropertiesFound, PropertiesTotal);
            Append(text, "events    ", EventsFound, EventsTotal);
            text.AppendLine();
            text.AppendFormat("structural completeness: {0}/{1} = {2:0.0}%", Found, Total, Percent).AppendLine();
            if (Failures > 0)
            {
                text.AppendLine("types that failed to render: " + Failures);
            }
            return text.ToString();
        }

        private static void Append(StringBuilder text, string label, int found, int total)
        {
            double percent = total == 0 ? 100.0 : 100.0 * found / total;
            text.AppendFormat("  {0}: {1,6}/{2,-6} {3,6:0.0}%", label, found, total, percent).AppendLine();
        }
    }

    /// <summary>
    /// Measures how much of an assembly survives decompilation: every metadata member is
    /// looked up in the generated text. This is the honest metric for an outline-style
    /// decompiler and it is used by the CLI, the headless shell and the test runner.
    /// </summary>
    internal static class Accuracy
    {
        public static AccuracyReport Measure(AssemblyDefinition assembly, LanguageWriter writer)
        {
            AccuracyReport report = new AccuracyReport
            {
                Assembly = assembly.Name.FullName,
                Writer = writer + (writer.Dialect == null ? string.Empty : " / " + writer.Dialect.DisplayName)
            };

            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }

                    report.TypesTotal++;
                    string text;
                    try
                    {
                        text = writer.StripMarkup(writer.WriteType(type));
                    }
                    catch (Exception ex)
                    {
                        report.Failures++;
                        Log.Warn("Accuracy: type '" + type.FullName + "' failed to render.", ex);
                        continue;
                    }

                    if (text.IndexOf(type.Name, StringComparison.Ordinal) >= 0)
                    {
                        report.TypesFound++;
                    }
                    else
                    {
                        report.Missing.Add("type " + type.FullName);
                    }

                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (LanguageWriter.IsCompilerGeneratedName(method.Name) || IsAccessor(type, method))
                        {
                            continue;
                        }
                        report.MethodsTotal++;
                        string lookup = method.IsConstructor ? type.Name : method.Name;
                        if (text.IndexOf(lookup, StringComparison.Ordinal) >= 0)
                        {
                            report.MethodsFound++;
                        }
                        else
                        {
                            report.Missing.Add("method " + type.FullName + "::" + method.Name);
                        }
                    }

                    foreach (FieldDefinition field in type.Fields)
                    {
                        if (type.IsEnum && field.Name == "value__")
                        {
                            continue;
                        }
                        report.FieldsTotal++;
                        if (text.IndexOf(field.Name, StringComparison.Ordinal) >= 0)
                        {
                            report.FieldsFound++;
                        }
                        else
                        {
                            report.Missing.Add("field " + type.FullName + "::" + field.Name);
                        }
                    }

                    foreach (PropertyDefinition property in type.Properties)
                    {
                        report.PropertiesTotal++;
                        if (text.IndexOf(property.Name, StringComparison.Ordinal) >= 0)
                        {
                            report.PropertiesFound++;
                        }
                        else
                        {
                            report.Missing.Add("property " + type.FullName + "::" + property.Name);
                        }
                    }

                    foreach (EventDefinition @event in type.Events)
                    {
                        report.EventsTotal++;
                        if (text.IndexOf(@event.Name, StringComparison.Ordinal) >= 0)
                        {
                            report.EventsFound++;
                        }
                        else
                        {
                            report.Missing.Add("event " + type.FullName + "::" + @event.Name);
                        }
                    }
                }
            }

            return report;
        }

        private static bool IsAccessor(TypeDefinition type, MethodDefinition method)
        {
            if (method.IsGetter || method.IsSetter || method.IsAddOn || method.IsRemoveOn || method.IsFire)
            {
                return true;
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                if (property.GetMethod == method || property.SetMethod == method)
                {
                    return true;
                }
            }
            foreach (EventDefinition @event in type.Events)
            {
                if (@event.AddMethod == method || @event.RemoveMethod == method || @event.InvokeMethod == method)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
