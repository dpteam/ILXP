using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace ILXP.Deobfuscation
{
    /// <summary>Result of running a set of IL passes.</summary>
    internal sealed class DeobfuscationReport
    {
        public DeobfuscationReport()
        {
            Results = new List<KeyValuePair<string, int>>();
        }

        public List<KeyValuePair<string, int>> Results { get; private set; }

        public int TotalChanges
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<string, int> pair in Results)
                {
                    total += pair.Value;
                }
                return total;
            }
        }
    }

    /// <summary>
    /// Runs the available IL cleanup passes in a fixed, conservative order.
    /// </summary>
    internal static class DeobfuscationPipeline
    {
        public static IList<IIlPass> CreateDefault()
        {
            return new IIlPass[]
            {
                new SizeOfReplacePass(),
                new EmptyTypesPass(),
                new MathReplacePass(),
                new NopRemoverPass()
            };
        }

        public static DeobfuscationReport Run(AssemblyDefinition assembly, IEnumerable<IIlPass> passes)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            DeobfuscationReport report = new DeobfuscationReport();
            foreach (IIlPass pass in passes)
            {
                int changes;
                try
                {
                    changes = pass.Run(assembly);
                }
                catch (Exception ex)
                {
                    Log.Error("Deobfuscation pass '" + pass.Name + "' failed.", ex);
                    changes = 0;
                }

                report.Results.Add(new KeyValuePair<string, int>(pass.Name, changes));
                Log.Info("Deobfuscation pass '" + pass.Name + "': " + changes + " change(s)");
            }
            return report;
        }
    }
}
