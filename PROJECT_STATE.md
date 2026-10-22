# Project state (for continuing work)

This file is the hand-off note for future sessions and for context compaction. Keep it up to
date whenever a milestone is reached.

## Ground rules (from PROMPT.md)

1. Everything targets **.NET Framework 4.0**, x86 and x64, Windows XP compatible.
2. Prefer dynamic fault tolerance and defensive handling over hard failures.
3. Logging goes through `Trace` (file + console when a console is attached) without duplicate
   listeners.
4. Support any MSIL input, including damaged assemblies and memory dumps.
5. Commit regularly; the user pushes manually.
6. Documentation files (this one, `ROADMAP.md`, `IDEAS.md`, `ARCHITECTURE.md`,
   `REVERSE_ENGINEERING.md`, `README.md`, `PROMPT.md`) must be updated as the project evolves.

## Current build status

```
dotnet build ILXP.slnx                              # 0 errors
dotnet build src/ILXP/ILXP.csproj -p:Platform=x86
dotnet build src/ILXP/ILXP.csproj -p:Platform=x64
powershell -File tools/Smoke-Test.ps1               # 11/11 checks pass
```

Verified manually:

* GUI starts and stays alive; log file is written to `%APPDATA%\ILXP\logs\ILXP.log`.
* About box: water logo ripples on click/drag, music starts on open and stops on close.
* `--list`, `--tree`, `--decompile` (il/cs/vb, with `--dialect` and `--bodies`), `--resources`.
* `--ildasm`/`--ilasm` use the **managed** implementations; round trip of `Mono.Cecil.dll`
  (245 types) produces a readable assembly with no external tools.
* `--disasm-x86` uses the managed x86/x64 decoder (no BeaEngine needed, works in AnyCPU).
* `--clean` removes NOPs and folds `sizeof`, `Type.EmptyTypes.Length` and constant
  `System.Math` calls (verified with `tools/Test-MathPass.ps1`).
* `--music` converts `.mid` <-> text and renders `.wav` with the chiptune synth
  (verified with `tools/Test-Wav.ps1`).
* Damaged-input baseline: a PE image is carved out of a larger blob
  (`tools/Test-Carving.ps1`).

## Known issues / next fixes

1. `StatementDecompiler` still has `smethod_N` names for internal helpers. Public entry points
   are `WriteVisualBasicBody` / `WriteCSharpBody`.
2. The C#/VB writers still have rough edges: attributes on properties/events, generic
   constraints, string escaping, and some `checked`/`Conversions` noise from the VB origin.
   `--decompile --bodies` appends method bodies after the type outline (it is an inspection
   dump, not a compilable project).
3. `ManagedIlAssembler` is a pragmatic subset: it handles the IL that
   `ManagedIlDisassembler` emits, downgrading anything it cannot resolve to `nop` (each case is
   reported in `LastWarnings`). Full ILAsm compatibility is a roadmap item.
4. `ManagedDisassembler` resolves lengths and common mnemonics; rare opcodes are shown as
   `opXX`/`sse`/`mmx` with decoded operands.
5. `--clean` does not preserve strong-name signatures and writes without symbols.
6. `native/` is still copied next to the executable but is only used with `--native`.
7. `AssemblyResolver` probes the GAC by directory layout; it does not use the Fusion API.
8. `GetTypeName` in `VbWriter` prints .NET type names (`Int32`, `String`), which is valid VB but
   not idiomatic; the C# writer maps to keywords.
9. The x86 build reports many warnings (mostly obsolete/CLS from the embedded Cecil sources).
10. Output dialects currently affect the header and expose capability flags; the full
    version-specific syntax matrix is tracked in `IDEAS.md`.

## Useful commands

```powershell
# build
dotnet build ILXP.slnx

# run GUI
src\ILXP\bin\Debug\net40\ILXP.exe

# smoke test (run after every change)
powershell -File tools\Smoke-Test.ps1

# decompile an assembly to IL / C# / VB files
src\ILXP\bin\Debug\net40\ILXP.exe --decompile a.dll --language il --output out
src\ILXP\bin\Debug\net40\ILXP.exe --decompile a.dll --language cs --dialect cs2 --bodies --output out

# managed IL round trip (no external tools)
src\ILXP\bin\Debug\net40\ILXP.exe --ildasm a.dll --output a.il
src\ILXP\bin\Debug\net40\ILXP.exe --ilasm a.il --output a.rt.dll --dll

# same, using the shipped native tools
src\ILXP\bin\Debug\net40\ILXP.exe --ildasm a.dll --output a.il --native

# clean NOPs/sizeof/Math
src\ILXP\bin\Debug\net40\ILXP.exe --clean a.dll --output a.clean.dll

# managed x86 disassembly (works in any build)
src\ILXP\bin\Debug\net40\ILXP.exe --disasm-x86 native\BeaEngine.dll --offset 0 --length 64

# music: text -> midi, midi -> wav
src\ILXP\bin\Debug\net40\ILXP.exe --music src\ILXP\Music\Tracks\T2JD.txt --output out.mid
src\ILXP\bin\Debug\net40\ILXP.exe --music out.mid --output out.wav

# automation screenshots
src\ILXP\bin\Debug\net40\ILXP.exe --screenshot about.png --window about
src\ILXP\bin\Debug\net40\ILXP.exe --screenshot main.png --window main
```

## Maintenance scripts (`tools/`)

* `Decrypt-Strings.ps1` – original string-table decryption (already applied; kept for history).
* `Extract-Resx.ps1`, `Extract-IcoFrames.ps1`, `New-CompanyLogo.ps1` – resource recovery.
* `Add-Namespaces.ps1`, `Rename-Types.ps1`, `Rename-Writers.ps1`,
  `Rename-StatementDecompiler.ps1` – the rename passes that were applied.
* `Cleanup-Sources.ps1`, `Fix-*.ps1` – repairs for decompiler artifacts.
* `Inspect-NativeArtifacts.ps1` – PE header/export/version/string inspection for `native/`.
* `List-Usage.ps1`, `List-EventWiring.ps1` – helper reports used while renaming.
* `Smoke-Test.ps1` – end-to-end CLI smoke test (11 checks).
* `Test-Carving.ps1`, `Test-MathPass.ps1`, `Test-Wav.ps1` – focused checks for the
  damaged-input loader, the Math folding pass and the chiptune renderer.

The scripts are not part of the build; they document how the restoration was done and can be
re-run on a fresh decompiled snapshot.

## Suggested next steps (in order)

1. Finish renaming `StatementDecompiler` internals (read each helper, give it a semantic name,
   update the call sites inside the file only).
2. Improve `ManagedIlAssembler` coverage (exception handlers, `switch`, `ldtoken`, generic
   signatures) and add a round-trip test on `ILXP.exe`.
3. Port the remaining Unscrambler passes (`ConvertToIntReplace`, `DoubleParseReplace`, ...).
4. Add dnlib as `src/dnlib` (decompiled), then port `de4dot.blocks` and `de4dot.code.renamer`.
5. Implement the runtime/compiler profile matrix from `IDEAS.md` (target framework and target
   compiler selection, with `csc.exe` syntax validation when available).
6. Improve the C# writer (attributes, generics, events) and re-check the round trip.

## Commit style

Short imperative subjects, e.g.:

* `refactor: decrypt and inline obfuscated string table`
* `build: restructure into solution, embed Mono.Cecil sources, restore resources and settings`
* `refactor: rewrite UI layer with named members, add CLI mode, improve C#/VB writers`
* `feat: integrate ILDasm/ILAsm and BeaEngine, add IL cleanup passes`
* `feat: water logo, music player with MIDI import, managed tool replacements`
