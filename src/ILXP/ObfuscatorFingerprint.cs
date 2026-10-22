using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILXP
{
    /// <summary>One piece of evidence for (or against) a specific protector.</summary>
    internal sealed class FingerprintEvidence
    {
        public string Kind;
        public string Detail;
        public int Weight;

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2:+#;-#;0})", Kind, Detail, Weight);
        }
    }

    /// <summary>A candidate protector with its score.</summary>
    internal sealed class FingerprintMatch
    {
        public FingerprintMatch(string name)
        {
            Name = name;
            Evidence = new List<FingerprintEvidence>();
        }

        public string Name { get; private set; }

        public List<FingerprintEvidence> Evidence { get; private set; }

        public int Score
        {
            get
            {
                int score = 0;
                foreach (FingerprintEvidence item in Evidence)
                {
                    score += item.Weight;
                }
                return score;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}  (score {1})", Name, Score);
        }
    }

    /// <summary>Generic (protector-agnostic) obfuscation indicators.</summary>
    internal sealed class ObfuscationIndicators
    {
        public int TotalTypes;
        public int RenamedTypes;
        public int TotalMethods;
        public int RenamedMethods;
        public int TotalFields;
        public int RenamedFields;
        public int ResourceCount;
        public int EncryptedStringCandidates;
        public int NopCount;
        public int InstructionCount;
        public int ProxyCallCandidates;
        public double MaxSectionEntropy;
        public bool HasPdb;
        public bool HasStrongName;

        /// <summary>0..100: how likely the assembly has been through a protector.</summary>
        public int ObfuscationScore
        {
            get
            {
                double score = 0;

                if (TotalTypes > 0)
                {
                    score += 30.0 * RenamedTypes / TotalTypes;
                }
                if (TotalMethods > 0)
                {
                    score += 25.0 * RenamedMethods / TotalMethods;
                }
                if (TotalFields > 0)
                {
                    score += 10.0 * RenamedFields / TotalFields;
                }

                // High entropy in a code section means packed or encrypted payloads.
                if (MaxSectionEntropy > 7.2)
                {
                    score += 20;
                }
                else if (MaxSectionEntropy > 6.5)
                {
                    score += 10;
                }

                // NOP padding is a classic control-flow obfuscation artefact.
                if (InstructionCount > 0)
                {
                    double nopRatio = (double)NopCount / InstructionCount;
                    if (nopRatio > 0.25)
                    {
                        score += 15;
                    }
                    else if (nopRatio > 0.08)
                    {
                        score += 7;
                    }
                }

                if (!HasPdb)
                {
                    score += 5;
                }
                if (EncryptedStringCandidates > 20)
                {
                    score += 10;
                }

                if (score > 100)
                {
                    score = 100;
                }
                return (int)Math.Round(score);
            }
        }

        public string Verdict
        {
            get
            {
                int score = ObfuscationScore;
                if (score >= 70) return "heavily obfuscated";
                if (score >= 45) return "obfuscated";
                if (score >= 20) return "lightly obfuscated / stripped";
                return "looks clean";
            }
        }
    }

    /// <summary>Result of a fingerprint run.</summary>
    internal sealed class FingerprintReport
    {
        public FingerprintReport()
        {
            Matches = new List<FingerprintMatch>();
            Indicators = new ObfuscationIndicators();
        }

        public List<FingerprintMatch> Matches { get; private set; }

        public ObfuscationIndicators Indicators { get; private set; }

        /// <summary>Best candidate, or null when nothing matched.</summary>
        public FingerprintMatch Best
        {
            get
            {
                FingerprintMatch best = null;
                foreach (FingerprintMatch match in Matches)
                {
                    if (best == null || match.Score > best.Score)
                    {
                        best = match;
                    }
                }
                return best != null && best.Score > 0 ? best : null;
            }
        }
    }

    /// <summary>
    /// Heuristic protector identification.
    ///
    /// The signatures are the ones used by de4dot and dnSpy, implemented from scratch:
    /// attribute names, marker types, resource names, PE section names and generic
    /// obfuscation artefacts. Everything is scored, so an assembly that only shares one weak
    /// marker is not mislabelled.
    /// </summary>
    internal static class ObfuscatorFingerprint
    {
        private static readonly Regex ObfuscatedName = new Regex(
            @"^[^\w]|^[A-Za-z]{1,2}$|^[\u0001-\u001f]|^\d+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ConfuserHash = new Regex(
            @"^[#\u0001-\u001f]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Base64ish = new Regex(
            @"^[A-Za-z0-9+/=]{24,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static FingerprintReport Analyse(AssemblyDefinition assembly, string path)
        {
            FingerprintReport report = new FingerprintReport();
            ObfuscationIndicators indicators = report.Indicators;

            Dictionary<string, FingerprintMatch> matches =
                new Dictionary<string, FingerprintMatch>(StringComparer.OrdinalIgnoreCase);

            try
            {
                PeInspector.PeInfo pe = PeInspector.Inspect(path);
                indicators.HasStrongName = pe.HasStrongName;
                indicators.HasPdb = System.IO.File.Exists(System.IO.Path.ChangeExtension(path, ".pdb"));
                foreach (PeSection section in pe.Sections)
                {
                    if (section.Entropy > indicators.MaxSectionEntropy)
                    {
                        indicators.MaxSectionEntropy = section.Entropy;
                    }

                    // Packer section names.
                    string name = section.Name.ToLowerInvariant();
                    if (name.StartsWith(".mpress", StringComparison.Ordinal) || name == "mpress1")
                    {
                        Add(matches, "MPRESS", "section", "section " + section.Name, 40);
                    }
                    if (name.StartsWith(".upx", StringComparison.Ordinal))
                    {
                        Add(matches, "UPX", "section", "section " + section.Name, 40);
                    }
                    if (name.Contains("themida") || name.Contains("winlice"))
                    {
                        Add(matches, "Themida/WinLicense", "section", "section " + section.Name, 50);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("PE inspection failed for the fingerprint.", ex);
            }

            try
            {
                ScanAssembly(assembly, indicators, matches);
            }
            catch (Exception ex)
            {
                Log.Warn("Assembly scan failed for the fingerprint.", ex);
            }

            // Aggregated indicators contribute to a generic verdict.
            if (indicators.TotalTypes > 0 && indicators.RenamedTypes * 10 > indicators.TotalTypes * 6)
            {
                Add(matches, "Generic name obfuscation", "names",
                    string.Format("{0}/{1} types have unreadable names", indicators.RenamedTypes, indicators.TotalTypes), 25);
            }
            else if (indicators.TotalMethods > 0 && indicators.RenamedMethods * 10 > indicators.TotalMethods * 6)
            {
                Add(matches, "Generic name obfuscation", "names",
                    string.Format("{0}/{1} methods have unreadable names", indicators.RenamedMethods, indicators.TotalMethods), 20);
            }

            if (indicators.MaxSectionEntropy > 7.2)
            {
                Add(matches, "Packed / encrypted payload", "entropy",
                    string.Format("max section entropy {0:0.00} bits/byte", indicators.MaxSectionEntropy), 30);
            }

            if (indicators.InstructionCount > 0 && indicators.NopCount * 4 > indicators.InstructionCount)
            {
                Add(matches, "Control-flow obfuscation", "nops",
                    string.Format("{0:0.0}% of instructions are NOPs", 100.0 * indicators.NopCount / indicators.InstructionCount), 20);
            }

            if (indicators.EncryptedStringCandidates > 20)
            {
                Add(matches, "String encryption", "strings",
                    indicators.EncryptedStringCandidates + " base64-looking literals", 25);
            }

            if (indicators.ProxyCallCandidates > 50)
            {
                Add(matches, "Proxy calls", "proxy",
                    indicators.ProxyCallCandidates + " one-line static forwarders", 20);
            }

            foreach (FingerprintMatch match in matches.Values)
            {
                report.Matches.Add(match);
            }
            report.Matches.Sort(delegate(FingerprintMatch a, FingerprintMatch b) { return b.Score.CompareTo(a.Score); });
            return report;
        }

        private static void ScanAssembly(AssemblyDefinition assembly, ObfuscationIndicators indicators,
            Dictionary<string, FingerprintMatch> matches)
        {
            foreach (ModuleDefinition module in assembly.Modules)
            {
                // Module-level attributes are a strong, cheap signal.
                foreach (CustomAttribute attribute in module.CustomAttributes)
                {
                    MatchAttribute(attribute, matches, 35);
                }

                foreach (Resource resource in module.Resources)
                {
                    indicators.ResourceCount++;
                    string name = resource.Name ?? string.Empty;
                    if (name.IndexOf("ConfusedBy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Add(matches, "Confuser / ConfuserEx", "resource", "resource " + name, 60);
                    }
                    if (name.IndexOf("ILProtector", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Add(matches, "ILProtector", "resource", "resource " + name, 60);
                    }
                    if (name.IndexOf("SmartAssembly", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Add(matches, "SmartAssembly", "resource", "resource " + name, 50);
                    }
                    if (name.IndexOf("Eazfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Add(matches, "Eazfuscator.NET", "resource", "resource " + name, 50);
                    }
                }

                foreach (TypeDefinition type in module.Types)
                {
                    ScanType(type, indicators, matches);
                }
            }

            foreach (CustomAttribute attribute in assembly.CustomAttributes)
            {
                MatchAttribute(attribute, matches, 35);
            }
        }

        private static void ScanType(TypeDefinition type, ObfuscationIndicators indicators,
            Dictionary<string, FingerprintMatch> matches)
        {
            if (type.Name == "<Module>")
            {
                ScanMembers(type, indicators, matches);
                return;
            }

            indicators.TotalTypes++;
            if (IsObfuscatedName(type.Name, "type"))
            {
                indicators.RenamedTypes++;
            }

            // Marker types.
            string fullName = type.FullName ?? type.Name;
            if (fullName.IndexOf("ConfusedBy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Confuser / ConfuserEx", "type", fullName, 60);
            }
            if (fullName.IndexOf("SmartAssembly", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "SmartAssembly", "type", fullName, 50);
            }
            if (fullName.IndexOf("Eazfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Eazfuscator.NET", "type", fullName, 50);
            }
            if (fullName.IndexOf("Dotfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Dotfuscator", "type", fullName, 50);
            }
            if (fullName.IndexOf("ObfuscatedBy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Agile.NET / SecureTeam", "type", fullName, 45);
            }
            if (fullName.IndexOf("Xenocode", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Xenocode", "type", fullName, 50);
            }
            if (fullName.IndexOf("Babel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Babel Obfuscator", "type", fullName, 40);
            }
            if (fullName.IndexOf("CryptoObfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Crypto Obfuscator", "type", fullName, 50);
            }
            if (fullName.IndexOf("Goliath", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Goliath.NET", "type", fullName, 45);
            }
            if (fullName.IndexOf("Skater", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Skater.NET", "type", fullName, 45);
            }
            if (fullName.IndexOf("CodeWall", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "CodeWall", "type", fullName, 45);
            }
            if (fullName.IndexOf("MaxtoCode", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "MaxtoCode", "type", fullName, 50);
            }
            if (fullName.IndexOf("Spices", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Spices.NET", "type", fullName, 40);
            }
            if (fullName.IndexOf("Reactor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, ".NET Reactor", "type", fullName, 40);
            }

            // ConfuserEx uses types whose name starts with a control character or '#'.
            if (ConfuserHash.IsMatch(type.Name))
            {
                Add(matches, "Confuser / ConfuserEx", "type-name", "type " + type.Name, 15);
            }

            foreach (CustomAttribute attribute in type.CustomAttributes)
            {
                MatchAttribute(attribute, matches, 30);
            }

            ScanMembers(type, indicators, matches);
        }

        private static void ScanMembers(TypeDefinition type, ObfuscationIndicators indicators,
            Dictionary<string, FingerprintMatch> matches)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                indicators.TotalMethods++;
                if (method.IsConstructor || method.IsSpecialName)
                {
                    continue;
                }
                if (IsObfuscatedName(method.Name, "method"))
                {
                    indicators.RenamedMethods++;
                }

                if (!method.HasBody)
                {
                    continue;
                }

                MethodBody body = method.Body;
                bool onlyForwarder = body.Instructions.Count == 2 &&
                                     (body.Instructions[0].OpCode == OpCodes.Ldarg_0 ||
                                      body.Instructions[0].OpCode == OpCodes.Ldarg_1 ||
                                      body.Instructions[0].OpCode == OpCodes.Ldarg_2 ||
                                      body.Instructions[0].OpCode == OpCodes.Ldarg_3 ||
                                      body.Instructions[0].OpCode == OpCodes.Nop) &&
                                     body.Instructions[1].OpCode == OpCodes.Ret;

                if (onlyForwarder)
                {
                    indicators.ProxyCallCandidates++;
                }

                foreach (Instruction instruction in body.Instructions)
                {
                    indicators.InstructionCount++;
                    if (instruction.OpCode == OpCodes.Nop)
                    {
                        indicators.NopCount++;
                    }
                    else if (instruction.OpCode == OpCodes.Ldstr)
                    {
                        string literal = instruction.Operand as string;
                        if (literal != null && Base64ish.IsMatch(literal))
                        {
                            indicators.EncryptedStringCandidates++;
                        }
                    }
                }
            }

            foreach (FieldDefinition field in type.Fields)
            {
                indicators.TotalFields++;
                if (IsObfuscatedName(field.Name, "field"))
                {
                    indicators.RenamedFields++;
                }
            }
        }

        private static void MatchAttribute(CustomAttribute attribute, Dictionary<string, FingerprintMatch> matches, int weight)
        {
            if (attribute.AttributeType == null)
            {
                return;
            }

            string name = attribute.AttributeType.FullName ?? attribute.AttributeType.Name;
            if (name.IndexOf("ConfusedBy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Confuser / ConfuserEx", "attribute", name, weight + 25);
            }
            if (name.IndexOf("SmartAssembly", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("PoweredBy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "SmartAssembly", "attribute", name, weight);
            }
            if (name.IndexOf("Eazfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Eazfuscator.NET", "attribute", name, weight);
            }
            if (name.IndexOf("Dotfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Dotfuscator", "attribute", name, weight);
            }
            if (name.IndexOf("Babel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Babel Obfuscator", "attribute", name, weight);
            }
            if (name.IndexOf("CryptoObfuscator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Crypto Obfuscator", "attribute", name, weight);
            }
            if (name.IndexOf("Xenocode", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Xenocode", "attribute", name, weight);
            }
            if (name.IndexOf("SecureTeam", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("AgileDotNet", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Agile.NET / SecureTeam", "attribute", name, weight);
            }
            if (name.IndexOf("Reactor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, ".NET Reactor", "attribute", name, weight);
            }
            if (name.IndexOf("Obfuscation", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Add(matches, "Generic obfuscation attribute", "attribute", name, 10);
            }
        }

        private static void Add(Dictionary<string, FingerprintMatch> matches, string name,
            string kind, string detail, int weight)
        {
            FingerprintMatch match;
            if (!matches.TryGetValue(name, out match))
            {
                match = new FingerprintMatch(name);
                matches[name] = match;
            }

            // Avoid duplicate evidence for the same detail.
            foreach (FingerprintEvidence existing in match.Evidence)
            {
                if (existing.Detail == detail)
                {
                    return;
                }
            }

            match.Evidence.Add(new FingerprintEvidence { Kind = kind, Detail = detail, Weight = weight });
        }

        internal static bool IsObfuscatedName(string name)
        {
            return IsObfuscatedName(name, "member");
        }

        /// <summary>
        /// Kind-aware name test. Normal .NET code uses PascalCase for types and methods and
        /// camelCase/PascalCase for fields, so a type or method that does not start with an
        /// uppercase letter is a strong obfuscation signal (while a lowercase field name is
        /// perfectly normal).
        /// </summary>
        internal static bool IsObfuscatedName(string name, string kind)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            if (name == "<Module>" || name.StartsWith("get_", StringComparison.Ordinal) ||
                name.StartsWith("set_", StringComparison.Ordinal) || name.StartsWith("add_", StringComparison.Ordinal) ||
                name.StartsWith("remove_", StringComparison.Ordinal) || name.StartsWith("op_", StringComparison.Ordinal))
            {
                return false;
            }

            if (ObfuscatedName.IsMatch(name) || ConfuserHash.IsMatch(name))
            {
                return true;
            }

            char first = name[0];
            if (!char.IsLetter(first) && first != '_')
            {
                return true;
            }

            bool typeLike = kind == "type" || kind == "method";
            if (typeLike && char.IsLower(first))
            {
                return true;
            }

            // Long names without a single vowel are not human words.
            if (name.Length >= 8)
            {
                bool hasVowel = false;
                for (int i = 0; i < name.Length; i++)
                {
                    char c = char.ToLowerInvariant(name[i]);
                    if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' || c == 'y')
                    {
                        hasVowel = true;
                        break;
                    }
                }
                if (!hasVowel)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Formats a report for the CLI.</summary>
        public static string Format(FingerprintReport report)
        {
            StringBuilder text = new StringBuilder();
            ObfuscationIndicators indicators = report.Indicators;

            text.AppendLine("obfuscation score : " + indicators.ObfuscationScore + "/100  (" + indicators.Verdict + ")");
            text.AppendLine();
            text.AppendLine("names             : " +
                indicators.RenamedTypes + "/" + indicators.TotalTypes + " types, " +
                indicators.RenamedMethods + "/" + indicators.TotalMethods + " methods, " +
                indicators.RenamedFields + "/" + indicators.TotalFields + " fields");
            text.AppendLine(string.Format("max section entropy: {0:0.00} bits/byte", indicators.MaxSectionEntropy));
            text.AppendLine(string.Format("NOP ratio         : {0:0.0}%",
                indicators.InstructionCount == 0 ? 0 : 100.0 * indicators.NopCount / indicators.InstructionCount));
            text.AppendLine("proxy candidates  : " + indicators.ProxyCallCandidates);
            text.AppendLine("base64 literals   : " + indicators.EncryptedStringCandidates);
            text.AppendLine("resources         : " + indicators.ResourceCount);
            text.AppendLine("pdb               : " + (indicators.HasPdb ? "present" : "absent"));
            text.AppendLine("strong name       : " + (indicators.HasStrongName ? "present" : "absent"));
            text.AppendLine();

            if (report.Matches.Count == 0)
            {
                text.AppendLine("no protector signatures matched");
                return text.ToString();
            }

            text.AppendLine("candidates:");
            foreach (FingerprintMatch match in report.Matches)
            {
                if (match.Score <= 0)
                {
                    continue;
                }
                text.AppendLine(string.Format("  {0,-28} score {1,4}", match.Name, match.Score));
                foreach (FingerprintEvidence evidence in match.Evidence)
                {
                    text.AppendLine("      " + evidence);
                }
            }
            return text.ToString();
        }
    }
}
