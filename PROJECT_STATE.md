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
dotnet build ILXP.slnx                              # 0 errors, single-file output
dotnet build src/ILXP/ILXP.csproj -p:Platform=x86
dotnet build src/ILXP/ILXP.csproj -p:Platform=x64
tests\ILXP.Tests\bin\Debug\net40\ILXP.Tests.exe run # 60/60 checks pass
powershell -File tools/Smoke-Test.ps1               # 11/11 checks pass
```

Build output is now **one executable** (`ILXP.exe` + pdb); Cecil is compiled in and `native/`
is no longer copied.

Measured decompiler accuracy on `ILXP.exe` itself (6 700+ metadata members):

| writer | structural completeness |
| --- | --- |
| IL | 100.0 % |
| C# | 99.9 % |
| VB | 99.6 % |

Verified manually:

* GUI starts and stays alive; log file is written to `%APPDATA%\ILXP\logs\ILXP.log`.
* About box: large visible water ripples on click/drag plus ambient drops; music starts on
  open and stops on close.
* Tree: safe depth-limited expansion (budget 25 000 nodes), `Ctrl+E` depth prompt, and a
  category-based settings dialog (General / Tree / Output / Music / Advanced).
* Music: full General MIDI program map (128 instruments) + percussion map, stereo, ADSR,
  resonant filter, vibrato/detune/PWM, sustain pedal, WAV cache.
* `--list`, `--tree`, `--accuracy`, `--verify`, `--headless`, `--decompile`
  (il/cs/vb with `--dialect`/`--bodies`), `--resources`.
* `--ildasm`/`--ilasm` use the **managed** implementations; ILXP.exe round trip works.
* `--disasm-x86` uses the managed x86/x64 decoder (works in AnyCPU and x64).
* `--clean` removes NOPs and folds `sizeof`, `Type.EmptyTypes.Length`, constant `Math` calls.
* IL ⇄ source navigation: `IlLineMap` + `Ctrl+G` + status-bar offset display.
* IL search: `Ctrl+Shift+F` regex over every method's IL, results with `IL_xxxx` offsets,
  double-click navigation; CLI `--ilsearch`.
* Edit → Copy as… (name, signature, IL, C#, VB, references, target framework, nav key).
* Platform builds are verified (`tools/Test-Platform.ps1`): x64 → machine 0x8664,
  x86 → 32BitRequired, AnyCPU → neither.
* Damaged-input baseline: PE carving from larger blobs; random garbage fails cleanly.

## Known issues / next fixes

1. `StatementDecompiler` still has `smethod_N` names for internal helpers. Public entry points
   are `WriteVisualBasicBody` / `WriteCSharpBody`.
2. The C#/VB writers are outline-oriented: attributes on properties/events, generic
   constraints and string escaping are still rough. `--bodies` appends bodies after the
   outline (inspection dump, not a compilable project).
3. `ManagedIlAssembler` handles the IL that `ManagedIlDisassembler` emits, downgrading
   anything unresolvable to `nop` (reported in `LastWarnings`). Exception handlers and
   `switch` are not reconstructed yet.
4. `ManagedDisassembler` resolves lengths and common mnemonics; rare opcodes appear as
   `opXX`/`sse`/`mmx` with decoded operands.
5. `--clean` does not preserve strong-name signatures and writes without symbols.
6. `AssemblyResolver` probes the GAC by directory layout; it does not use the Fusion API.
7. `VbWriter.GetTypeName` prints .NET type names (`Int32`, `String`) — valid VB, not idiomatic.
8. Output dialects currently affect the header and expose capability flags; the full
   version-specific syntax matrix is tracked in `IDEAS.md`.
9. Music rendering takes ~15 s for a 2-minute tune on first play (cached afterwards). A
   streaming/real-time synth would remove the wait.

## Useful commands

```powershell
# build
dotnet build ILXP.slnx

# run GUI
src\ILXP\bin\Debug\net40\ILXP.exe

# full test suite / smoke test
tests\ILXP.Tests\bin\Debug\net40\ILXP.Tests.exe run
powershell -File tools\Smoke-Test.ps1

# quick open search from the command line
src\ILXP\bin\Debug\net40\ILXP.exe --search src\ILXP\bin\Debug\net40\ILXP.exe waterlogo

# accuracy + IL verification
src\ILXP\bin\Debug\net40\ILXP.exe --accuracy src\ILXP\bin\Debug\net40\ILXP.exe --language cs --verbose
src\ILXP\bin\Debug\net40\ILXP.exe --verify src\ILXP\bin\Debug\net40\ILXP.exe

# headless shell
"open src\ILXP\bin\Debug\net40\ILXP.exe`naccuracy il`nquit" | src\ILXP\bin\Debug\net40\ILXP.exe --headless

# obfuscate with the built-in test obfuscator, then analyse
tests\ILXP.Tests\bin\Debug\net40\ILXP.Tests.exe obfuscate ILXP.exe ILXP.obf.exe 1234
src\ILXP\bin\Debug\net40\ILXP.exe --verify ILXP.obf.exe
src\ILXP\bin\Debug\net40\ILXP.exe --clean ILXP.obf.exe --output ILXP.clean.exe

# decompile / round trip / disassemble
src\ILXP\bin\Debug\net40\ILXP.exe --decompile a.dll --language cs --dialect cs2 --bodies --output out
src\ILXP\bin\Debug\net40\ILXP.exe --ildasm a.dll --output a.il
src\ILXP\bin\Debug\net40\ILXP.exe --ilasm a.il --output a.rt.dll --dll
src\ILXP\bin\Debug\net40\ILXP.exe --disasm-x86 file.bin --offset 0 --length 64

# music
src\ILXP\bin\Debug\net40\ILXP.exe --music src\ILXP\Music\Tracks\T2JD.txt --output out.mid
src\ILXP\bin\Debug\net40\ILXP.exe --music out.mid --output out.wav

# automation screenshot (loads an assembly and selects a type)
src\ILXP\bin\Debug\net40\ILXP.exe --screenshot docs\screenshot-main.png --window main --open src\ILXP\bin\Debug\net40\ILXP.exe --select ILXP.CodeEditor
src\ILXP\bin\Debug\net40\ILXP.exe --render-water docs\water.png
```

## Maintenance scripts (`tools/`)

* `Decrypt-Strings.ps1` – original string-table decryption (already applied; kept for history).
* `Extract-Resx.ps1`, `Extract-IcoFrames.ps1`, `New-CompanyLogo.ps1` – resource recovery.
* `Add-Namespaces.ps1`, `Rename-Types.ps1`, `Rename-Writers.ps1`,
  `Rename-StatementDecompiler.ps1`, `Refactor-Accuracy.ps1` – applied refactorings.
* `Cleanup-Sources.ps1`, `Fix-*.ps1` – repairs for decompiler artifacts.
* `Inspect-NativeArtifacts.ps1` – PE header/export/version/string inspection for `native/`.
* `Inspect-ImageColors.ps1` – quick palette check for rendered screenshots.
* `List-Usage.ps1`, `List-EventWiring.ps1` – helper reports used while renaming.
* `Smoke-Test.ps1` – end-to-end CLI smoke test (11 checks).
* `Test-Carving.ps1`, `Test-MathPass.ps1`, `Test-Wav.ps1`, `Test-AboutMusic.ps1` – focused
  checks.

The scripts are not part of the build; they document how the restoration was done and can be
re-run on a fresh decompiled snapshot.

## Suggested next steps (in order)

1. IL-offset ⇄ source-line navigation (needs a line→offset map from the writers).
2. Finish renaming `StatementDecompiler` internals.
3. Port ILSpy/dnSpy features together with their settings, plus an ILSpy/dnSpy settings
   importer (see `IDEAS.md`, level 2).
4. Improve `ManagedIlAssembler` coverage (exception handlers, `switch`, `ldtoken`).
5. Port the remaining Unscrambler passes and extend `TestObfuscator` with control-flow
   flattening / string encryption so deobfuscation can be measured numerically.
6. Add dnlib as `src/dnlib`, then port `de4dot.blocks` and `de4dot.code.renamer`.

## Commit style

Short imperative subjects, e.g.:

* `refactor: decrypt and inline obfuscated string table`
* `build: restructure into solution, embed Mono.Cecil sources, restore resources and settings`
* `feat: water logo, music player with MIDI import, managed tool replacements`
* `feat: GM instrument engine, single-file build, tests, headless shell, accuracy metrics, CI`
