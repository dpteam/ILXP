# ILXP

![ILXP main window](docs/screenshot-main.png)

ILXP is a preserved and restored .NET decompiler / IL analysis tool. It started life as a
decompiled copy of the old **CodeReflect** decompiler (DevExtras) and is being rebuilt into a
maintainable, deobfuscated application with additional functionality merged from other open
tools (ILSpy, dnSpy, de4dot and the Unscrambler feature set).

The About box doubles as a little demo of the built-in music engine and of the interactive
water effect:

![ILXP about box](docs/screenshot-about.png)

The project is deliberately conservative:

* **.NET Framework 4.0**, x86 and x64 (Windows XP compatible).
* No external runtime dependencies at startup; everything needed is either source in this
  repository or shipped in `native/`.
* All third-party libraries are embedded as source and built from this solution.
* Every failure path is logged through `System.Diagnostics.Trace` and degrades gracefully.

## Building

Requirements: .NET SDK (tested with 9.x) and the .NET Framework 4.0 reference assemblies
(installed with Visual Studio or the Windows SDK).

```powershell
dotnet build ILXP.slnx
```

Output:

* `src/ILXP/bin/Debug/net40/ILXP.exe` – AnyCPU application
* `src/ILXP/bin/x86/Debug/net40/ILXP.exe` – x86 build (`-p:Platform=x86`)
* `src/ILXP/bin/x64/Debug/net40/ILXP.exe` – x64 build (`-p:Platform=x64`)

The `native/` folder (ILDasm, ILAsm, BeaEngine, fusion.dll) is copied next to the executable.

## Running

Start the GUI:

```powershell
src\ILXP\bin\Debug\net40\ILXP.exe
```

Command line mode (all commands are logged to `%APPDATA%\ILXP\logs\ILXP.log`):

```text
ILXP --version
ILXP --list <assembly>
ILXP --tree <assembly> [--deep]
ILXP --decompile <assembly> [--language il|cs|vb] [--dialect <id>] [--bodies] [--output <dir>]
ILXP --resources <assembly> --output <dir>
ILXP --ildasm <assembly> [--output <file.il>] [--bytes] [--native]
ILXP --ilasm <file.il> [--output <assembly>] [--dll] [--x86|--x64] [--native]
ILXP --disasm-x86 <file> [--offset <n>] [--length <n>] [--x64]
ILXP --clean <assembly> [--output <assembly>]
ILXP --music <file.mid|file.txt> [--output <out.mid|out.txt|out.wav>]
ILXP --screenshot <out.png> [--window about|main]
```

Output dialects (`--dialect`): `cs1` C# 1.0 / VS 2002, `cs2` C# 2.0 / VS 2005, `cs3` C# 3.0,
`cs4` C# 4.0, `cs5` C# 5.0, `cs6` C# 6.0, `cs7` C# 7.3, `cs8` C# 8.0, `cs9` C# 9.0+,
`sd` SharpDevelop 1.x–5.x, `vb` Visual Basic.

Notes:

* `--decompile` writes one file per type using the selected writer. IL output contains method
  bodies; the C#/VB type view is an outline and `--bodies` appends the method bodies.
* `--ildasm`, `--ilasm` and `--disasm-x86` use the **managed** implementations by default;
  pass `--native` to use `native/ildasm.exe`, `native/ilasm.exe` or `native/BeaEngine.dll`.
* `--clean` applies the IL cleanup passes from `src/ILXP/Deobfuscation` and writes a new
  assembly. Strong-name signatures are not preserved.
* `--music` converts between `.mid` and the text format, and renders `.wav` with the built-in
  chiptune synthesizer.

## Testing

```powershell
# full test suite (40 checks: CLI, accuracy, round trip, obfuscation, music, headless)
dotnet build ILXP.slnx
tests\ILXP.Tests\bin\Debug\net40\ILXP.Tests.exe run

# quick CLI smoke test
powershell -File tools\Smoke-Test.ps1

# structural completeness of the decompiler, measured on ILXP.exe itself
src\ILXP\bin\Debug\net40\ILXP.exe --accuracy src\ILXP\bin\Debug\net40\ILXP.exe --language il
src\ILXP\bin\Debug\net40\ILXP.exe --accuracy src\ILXP\bin\Debug\net40\ILXP.exe --language cs
src\ILXP\bin\Debug\net40\ILXP.exe --accuracy src\ILXP\bin\Debug\net40\ILXP.exe --language vb

# obfuscate an assembly with the built-in test obfuscator, then decompile it
tests\ILXP.Tests\bin\Debug\net40\ILXP.Tests.exe obfuscate ILXP.exe ILXP.obf.exe 1234
src\ILXP\bin\Debug\net40\ILXP.exe --verify ILXP.obf.exe
src\ILXP\bin\Debug\net40\ILXP.exe --clean ILXP.obf.exe --output ILXP.clean.exe

# headless command shell (scriptable, no window)
"open ILXP.exe`naccuracy cs`nquit" | src\ILXP\bin\Debug\net40\ILXP.exe --headless
```

Current measured accuracy on `ILXP.exe` (5 000+ metadata members):

| writer | structural completeness |
| --- | --- |
| IL | 100.0 % |
| C# | 99.9 % |
| VB | 99.6 % |

`--accuracy` reports per-member coverage (types, methods, fields, properties, events) and can
list the missing members with `--verbose`. `--verify` checks every instruction operand and
branch target, which is also what the cleanup passes are validated against.

## Repository layout

```text
ILXP.slnx                     solution
ILXP.ico                      application icon (from refs/239.ico)
src/ILXP/                     application source
  AppInfo.cs, AppSettings.cs, Log.cs, Program.cs, OutputDialect.cs
  AssemblyItem.cs, ModuleItem.cs, AssemblyResolver.cs, AssemblyLoader.cs
  LanguageWriter.cs, CSharpWriter.cs, IlWriter.cs, VbWriter.cs
  StatementDecompiler.cs      IL -> C#/VB statement translator
  FileContentInspector.cs     resource classification / hex dump / .resources view
  Accuracy.cs, Decompiler.cs  export helpers and structural completeness metrics
  HeadlessShell.cs            scriptable command shell (--headless)
  MainForm.cs, MainForm.Designer.cs, MainForm.Tree.cs, AboutForm.cs
  CodeEditor.cs, BorderPanel.cs, UiThread.cs, CodeLine.cs, CodeToken.cs ...
  Deobfuscation/              IL cleanup passes (ported from Unscrambler)
  Interop/                    managed ILDasm/ILAsm/disassembler + native fallbacks
  Music/                      MIDI I/O, GM instrument map, synthesizer, player
  UI/                         WaterLogo (interactive water surface)
  Resources/                  embedded images and tree icon payload
src/Cecil/                    Mono.Cecil 0.9.6.4, decompiled and embedded as source
src/Cecil.Pdb/                Mono.Cecil.Pdb, decompiled and embedded as source
tests/ILXP.Tests/             test runner + from-scratch test obfuscator
native/                       original native tools, reference only (not shipped)
tools/                        maintenance and test scripts
docs/                         screenshots used by this README
refs/                         reference material (git-ignored)
```

## Managed replacements for the native tools

The `native/` binaries are no longer required at runtime:

| Native tool | Managed replacement |
| --- | --- |
| `ildasm.exe` | `Interop/ManagedIlDisassembler.cs` (Cecil → ILAsm text) |
| `ilasm.exe` | `Interop/ManagedIlAssembler.cs` (ILAsm text → Cecil → PE) |
| `BeaEngine.dll` | `Interop/ManagedDisassembler.cs` (x86/x64 length decoder + formatter) |
| `fusion.dll` | `AssemblyResolver.cs` (GAC probing in managed code) |

Pass `--native` to use the shipped executables/DLL instead. See
[REVERSE_ENGINEERING.md](REVERSE_ENGINEERING.md) for the analysis behind each one.

## Music player

The About box contains a small player:

* Tunes are stored in the text MIDI format (`Music/Tracks/*.txt`, embedded) and parsed by
  `MidiTextFormat`. `MidiReader`/`MidiWriter` convert real `.mid` files both ways.
* `InstrumentMap` describes the full General MIDI program set (all 128 programs, grouped into
  piano / chromatic / organ / guitar / bass / strings / ensemble / brass / reed / pipe /
  synth lead / synth pad / FX / ethnic / percussive / sound effects) plus the complete
  percussion map (35–81), each with its own oscillator mix, ADSR, filter and modulation.
* `Synthesizer` renders the sequence with pulse/triangle/saw/sine/noise oscillators, a resonant
  state-variable low-pass filter with envelope, vibrato, detune, PWM, per-channel
  volume/expression/pan and sustain-pedal handling. Rendering is streamed to a WAV file and
  cached by content hash, so the second play of a tune is instant.
* `MusicPlayer` renders on a worker thread and loops through `System.Media.SoundPlayer`.
* The player starts when the About box opens, stops when it closes, and "Another music..."
  offers the built-in tunes plus MIDI/TXT import and MIDI export.

The logo is a `WaterLogo` control: an interactive height-field water surface. Clicking or
dragging distorts the image and produces visible blue ripples; ambient drops keep it alive.

## Status

See [PROJECT_STATE.md](PROJECT_STATE.md) for the current state and [ROADMAP.md](ROADMAP.md)
for the plan. The short version: the application builds, starts, loads assemblies, renders
IL/C#/VB, extracts resources, can call ILDasm/ILAsm, and has a working CLI. The remaining
large work items are the full merge of de4dot/dnSpy functionality and a complete rename of
the remaining obfuscated member names inside `StatementDecompiler`.

## Documentation

* [ARCHITECTURE.md](ARCHITECTURE.md) – how the decompiler and UI fit together
* [ROADMAP.md](ROADMAP.md) – planned work
* [IDEAS.md](IDEAS.md) – feature backlog (music, tree, dialects, compatibility)
* [REVERSE_ENGINEERING.md](REVERSE_ENGINEERING.md) – findings for the shipped native tools
* [PROJECT_STATE.md](PROJECT_STATE.md) – state, conventions, next steps
* [PROMPT.md](PROMPT.md) – original task statement and rules
