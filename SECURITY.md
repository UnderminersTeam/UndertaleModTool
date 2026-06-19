# Security Policy

## Supported Versions

Generally, only the latest major versions of UndertaleModTool (and newer) will receive security maintenance.
Currently, we support UndertaleModTool/Lib version 0.9.0.0, and any newer builds.

UndertaleModTool is intended to not allow any unintentional code execution when loading, saving, or viewing/modifying 
GameMaker data files, as well as not permit any unauthorized file I/O outside of any directories that were selected by the user.

Certain features, by nature of modding, are out of scope for security:
- Non-builtin CSX scripts may do whatever they please, and are not a direct part of this project.
- CSX scripts used as part of a project file (in the project system) additionally may do whatever they please. Exercise caution with these projects.
- Code (or specially-crafted assets) used within (the mods of) games execute arbitrary code at runtime.
  While this should not execute anything within UndertaleModTool itself, the GameMaker runtime is out of our control.

## Reporting a Vulnerability

Please privately report any vulnerabilities, based on the above support, via GitHub's private reporting system.
Maintainers can address the issue(s) from there.
