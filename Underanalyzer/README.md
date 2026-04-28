# Underanalyzer 
GameMaker VM analysis, compiler, and decompiler library, for use in larger modding tools. Written in C#, with interfaces that map to existing structures in game data. Licensed under [MPL-2.0](https://mozilla.org/MPL/2.0/).

Why? Existing tools have their own tools for this, but they are often tightly coupled to a large codebase (unnecessarily). This project has a much more narrow and testable scope, with its own lifecycle.

> [!WARNING]
> The decompiler in this project should not be used in ways a game's developer does not wish for it to be used. Please consult developers, if at all possible. Along that line, this project will not aim to deal with any forms of obfuscation.

## Decompiler 
Features a highly accurate GML decompiler, designed around the GML VM compiler's quirks.
- Designed with testability in mind; many tests exist to prevent future regressions. Tested on versions from GM:S 1.4 through modern GameMaker (not including GMRT, of course; that is out of scope of this project).
- Iterative control flow analysis allows for accurate decompilation, and with improved performance. This comes at the slight cost of not being able to deal with obfuscated or malformed VM assembly.
- Supports decompiling unreachable code that is left over by the decompiler. Note that this does *not* include code optimized out by the compiler; that is entirely removed.
- Highly customizable: supply settings to reformat the output, and supply data for each game (e.g. asset names) through a simple interface.
- Support for supplying game-specific cleanup data, including conditional types for simple macros, and names of arguments. Either JSON or direct C# may be used.
- Supports direct AST output, for cases where stringifying/printing the output is not required or desired. Useful for processing the decompiled code programmatically.
- Can detect likely usage of enums, whether known or unknown.
- Performs a cleanup pass on the decompiled AST before completion, making output more readable.

## Compiler
Features a fairly accurate GML compiler, also designed around the GML VM compiler's quirks.
- Also designed with testability in mind; many tests exist to prevent future regressions. Tested on versions from GM:S 1.4 through modern GameMaker, just like the decompiler.
- Supports many of the latest GMLv2 features (as introduced in GM 2.3 and beyond), although as of writing, not *every* feature is supported.
- Supports many odd code generation quirks of the official GML compiler, but only to an extent. Some patterns are not fully replicated, but usually act the same.
- Leaves a lot of freedom to the library consumer on how code should be generated, linked, and so on. No major external-facing interfaces have public setters.

## How can I help?
If you believe you have found incorrect decompilation output, please [make an issue](https://github.com/UnderminersTeam/Underanalyzer/issues/new/choose) describing the problem. Generally, this means supplying the expected decompiled output, and what the decompiler produced. VM assembly is also appreciated (e.g. from UndertaleModTool or in this project's mock assembly format). If you do not know what the expected output should be, that is okay, but the VM assembly is then required.

If you believe you have found incorrect compilation output, either make an issue [here](https://github.com/UnderminersTeam/Underanalyzer/issues/new/choose) if it's obviously an assembly/code generation problem, or make an issue on the tool being used to compile, such as [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool/issues/new/choose).

If you have a feature you would like to suggest (e.g. a new setting or better cleanup), feel free to [make an issue](https://github.com/UnderminersTeam/Underanalyzer/issues/new/choose) describing the feature request. Note that support is very limited on this project, so not many features will be able to be added on top of the main support. Feel free to make pull requests, though!

If you have features or bug fixes of your own you would like to contribute to the project, please submit a pull request and we can review it. These should never result in any test regressions, and ideally new tests should be added to confirm the new behavior works as intended.

## How can I use this in a larger project?
To integrate this library into larger projects, you likely want to implement the following interfaces found in [VMData.cs](Underanalyzer/VMData.cs) and [IGameContext.cs](Underanalyzer/IGameContext.cs):
- `IGMCode`: Code entries in the game data
- `IGMInstruction`: A single instruction within a code entry
- `IGMVariable`: A variable as contained in the game data
- `IGMFunction`: A function as contained in the game data
- `IGMString`: A string as contained in the game data
- `IGlobalFunctions`: Lookup for game-defined functions in global scope
- `IGameContext`: Interface for data belonging to a specific game

There are additional interfaces for the compiler specifically, as well:
- `ICodeBuilder`: Used to create and patch instructions as they are generated by the compiler.
- `IBuiltins`, `IBuiltinFunction`, `IBuiltinVariable`: Used to supply builtin function, variable, and constant information to the compiler.

Default mock implementations of the above interfaces exist, however they are primarily for library testing purposes.

To use the decompiler after you have these set up, you can initialize a `DecompileContext`, which can then be used directly to decompile a code entry through its `DecompileToString` or `DecompileToAST` methods. Additional settings can be specified through `IDecompileSettings`, with a provided/default implementation being `DecompileSettings`.

To use the compiler, you can initialize a `CompileContext`, which can then be parsed (designed to be able to be thread-safe) and compiled. Data is output through the supplied code builder interface, as well as via read-only output lists on the context itself.

> [!IMPORTANT]
> As of writing, the API for the library is not completely solidified. While many concepts will likely remain the same, things may change, so keep this in mind if you depend on the project.

## Future plans
Some potential future plans for the decompiler, at the time of writing, are as follows:
- Further cosmetic settings for code output, e.g. indentation style (as long as this is balanced with performance).
- Option to perform AST cleanup on data structure (`ds_*`) family of functions, to become accessors.
- Option to perform AST cleanup on struct variable functions, to become struct accessors.
- Improved JSON interface for game-specific data, as well as tests.
- Improved handling of slightly corrupt code generated by modding tools.
- Improved detection of `for` loops, in cases where they can be equivalently decompiled as `while` loops.
- Improved detection of `continue`, instead of empty `if` blocks (currently only a problem when nested inside other control flow).
- Option to scope local variable declarations to shorter blocks of code, rather than the entire function they are defined in.
- Better ability to detect and halt infinite loops/recursion in the decompilation process, possibly adding cancellation tokens as well.

Some potential future plans for the compiler, at the time of writing, are as follows:
- Implementing non-array accessors.
- Implementing array owners, when enabled.
- Implementing more compile-time optimizations.
- Implementing more version-specific (and some non-version-specific!) code generation quirks.

Beyond that, support will be needed for future GML/VM updates, as well as for handling any remaining edge cases.

## Thanks & further references
- The Underminers community, for their help, support, and research over the years.
- [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool), for reference of game structures and its own GML decompiler.
- [DogScepter](https://github.com/colinator27/DogScepter), for additional reference of game structures, and several of the algorithms used in this project's GML decompiler.
- Earlier modding, dumping, and decompilation tools: especially [GMdsam](https://github.com/WarlockD/GMdsam) and [Altar.NET](https://github.com/PoroCYon/Altar.NET).

## Contributors
A full list of contributors to this project can be found [here](https://github.com/UnderminersTeam/Underanalyzer/graphs/contributors).
