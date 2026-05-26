# Compiler Recipe

`compiler-recipe.json` is an optional source-package file that tells the
compiler which generated content it should create.

Without this file, the compiler only packages explicit source files. It does not
generate formula cards or other Syscalculator-specific content.

## Format

```json
{
  "format": 1,
  "generateFormulaCards": false
}
```

## Formula Cards

Set `generateFormulaCards` to `true` only for a package source that wants the
Syscalculator formula-card generator to create:

```text
formula/index.html
formula/<category>/<card>.html
```

Template source files stay source-only and are not copied into the compiled
`.lngpdk`:

```text
source/templates/formula/index.html
source/templates/formula/card.html
templates/formula-index.html
templates/formula-card.html
```
