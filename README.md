# Tiedragon.LanguagePackage

Tiedragon.LanguagePackage is the package engine for Tiedragon language packages.
It builds, validates, inspects and reads `.lngpdk` packages.

The package model is used by Syscalculator and ToolEditor, but this repository
is the independent home for the language package software.

## Projects

```text
src/Tiedragon.LanguagePackage
src/Tiedragon.NodSystem.Core
```

`Tiedragon.NodSystem.Core` is included because the current package compiler can
export formula-card and NOD help content while compiling language packages.

## Build

```bash
dotnet build Tiedragon.LanguagePackage.slnx
```

## Commands

```bash
dotnet run --project src/Tiedragon.LanguagePackage -- validate package.lngpdk
dotnet run --project src/Tiedragon.LanguagePackage -- inspect package.lngpdk
dotnet run --project src/Tiedragon.LanguagePackage -- compile source-folder output.lngpdk
```

## Direction

This repository is the first split from the Syscalculator monorepo. The next
step is to keep the public compiler and reader APIs stable enough for
Syscalculator, ToolEditor and future Tiedragon software to share.
