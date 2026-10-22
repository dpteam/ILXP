param([string]$Path = 'src\ILXP\CommandLine.cs')
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path -Parent $PSScriptRoot } else { (Get-Location).Path }
$full = Join-Path $root $Path
$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $full)

# Replace the accuracy implementation with a call into the shared Accuracy helper.
$start = -1
$end = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'private static int MeasureAccuracy') { $start = $i - 5; break }
}
for ($i = $start; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'private static int DumpTree') { $end = $i - 1; break }
}
if ($start -lt 0 -or $end -lt 0) { throw 'accuracy block not found' }

$replacement = @'
        /// <summary>Prints the structural completeness report for an assembly.</summary>
        private static int MeasureAccuracy(Options options)
        {
            AssemblyDefinition assembly = ReadAssembly(options.Input);

            LanguageWriter writer;
            switch (options.Language)
            {
                case "cs":
                case "csharp":
                    writer = new CSharpWriter();
                    break;
                case "vb":
                    writer = new VbWriter();
                    break;
                default:
                    writer = new IlWriter();
                    break;
            }

            if (!string.IsNullOrEmpty(options.Dialect))
            {
                OutputDialect dialect = OutputDialect.FromId(options.Dialect);
                if (dialect != null)
                {
                    writer.Dialect = dialect;
                }
            }

            AccuracyReport report = Accuracy.Measure(assembly, writer);
            Console.Write(report.ToString());

            if (options.VerboseOutput && report.Missing.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("missing members:");
                foreach (string item in report.Missing)
                {
                    Console.WriteLine("  " + item);
                }
            }

            return report.Percent >= 99.0 ? 0 : 1;
        }

'@
$lines.RemoveRange($start, $end - $start + 1)
$lines.InsertRange($start, ($replacement -split "`r?`n"))
Set-Content -LiteralPath $full -Value ($lines -join "`r`n") -NoNewline
Write-Host 'MeasureAccuracy replaced'
