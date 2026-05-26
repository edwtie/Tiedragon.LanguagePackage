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
dotnet run --project src/Tiedragon.LanguagePackage -- create-signing-key private-keys/beta.pem keys/beta.lngpdk.pubkey.json tiedragon-language-beta-2026
```

## Compiler Recipe

Package sources can include `compiler-recipe.json` to tell the compiler which
generated content to create. Without a recipe, the compiler only packages the
explicit files in the source package.

See [`docs/COMPILER_RECIPE.md`](docs/COMPILER_RECIPE.md).

## Key Files

Public trusted keys are stored as `.lngpdk.pubkey.json` files. These files may
be committed and distributed with software that needs to verify signed language
packages.

Private signing keys must never be committed. The `.gitignore` blocks common
private key file names and the `private-keys/` folder.

The verifier also keeps compatibility with the older
`language-package-trusted-keys.json` key store.

## Direction

This repository is the first split from the Syscalculator monorepo. The next
step is to keep the public compiler and reader APIs stable enough for
Syscalculator, ToolEditor and future Tiedragon software to share.
