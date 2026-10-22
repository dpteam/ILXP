using System;
using System.Collections.Generic;

namespace ILXP
{
    /// <summary>
    /// Output dialect for the generated source. The goal is historical coverage: code
    /// produced by very old compilers (C# 1.0 / VS 2002, SharpDevelop 1.x) and by modern
    /// ones should both be expressible.
    ///
    /// Only the parts that change the emitted text are modelled here; the full
    /// version-specific syntax matrix is tracked in ROADMAP.md.
    /// </summary>
    public sealed class OutputDialect    {
        public static readonly OutputDialect Il = new OutputDialect("il", "IL", "ILAsm", 0);
        public static readonly OutputDialect CSharp1 = new OutputDialect("cs1", "C# 1.0", "Visual Studio .NET 2002", 1);
        public static readonly OutputDialect CSharp2 = new OutputDialect("cs2", "C# 2.0", "Visual Studio 2005", 2);
        public static readonly OutputDialect CSharp3 = new OutputDialect("cs3", "C# 3.0", "Visual Studio 2008", 3);
        public static readonly OutputDialect CSharp4 = new OutputDialect("cs4", "C# 4.0", "Visual Studio 2010", 4);
        public static readonly OutputDialect CSharp5 = new OutputDialect("cs5", "C# 5.0", "Visual Studio 2012", 5);
        public static readonly OutputDialect CSharp6 = new OutputDialect("cs6", "C# 6.0", "Visual Studio 2015", 6);
        public static readonly OutputDialect CSharp7 = new OutputDialect("cs7", "C# 7.3", "Visual Studio 2017", 7);
        public static readonly OutputDialect CSharp8 = new OutputDialect("cs8", "C# 8.0", "Visual Studio 2019", 8);
        public static readonly OutputDialect CSharp9 = new OutputDialect("cs9", "C# 9.0+", "Visual Studio 2022", 9);
        public static readonly OutputDialect VisualBasic = new OutputDialect("vb", "Visual Basic", "Visual Studio", 0);
        public static readonly OutputDialect SharpDevelop = new OutputDialect("sd", "C# (SharpDevelop)", "SharpDevelop 1.x-5.x", 2);

        private OutputDialect(string id, string displayName, string toolchain, int csharpVersion)
        {
            Id = id;
            DisplayName = displayName;
            Toolchain = toolchain;
            CSharpVersion = csharpVersion;
        }

        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        /// <summary>Human readable name of the toolchain that produced/consumed this dialect.</summary>
        public string Toolchain { get; private set; }

        /// <summary>C# language version, 0 when the dialect is not C#.</summary>
        public int CSharpVersion { get; private set; }

        public bool IsCSharp
        {
            get { return CSharpVersion > 0; }
        }

        /// <summary>True when implicit typing (<c>var</c>) is available.</summary>
        public bool SupportsVar
        {
            get { return CSharpVersion >= 3; }
        }

        /// <summary>True when <c>using</c> declarations / expression bodies are available.</summary>
        public bool SupportsModernSyntax
        {
            get { return CSharpVersion >= 7; }
        }

        public override string ToString()
        {
            return DisplayName + "  (" + Toolchain + ")";
        }

        public static IList<OutputDialect> All
        {
            get
            {
                return new[]
                {
                    Il, CSharp1, CSharp2, CSharp3, CSharp4, CSharp5,
                    CSharp6, CSharp7, CSharp8, CSharp9, VisualBasic, SharpDevelop
                };
            }
        }

        public static OutputDialect FromId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (OutputDialect dialect in All)
            {
                if (string.Equals(dialect.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return dialect;
                }
            }
            return null;
        }
    }
}
