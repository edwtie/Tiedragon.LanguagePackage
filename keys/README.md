# Language Package Key Files

This folder contains public trust keys for signed `.lngpdk` packages.

Files ending in `.lngpdk.pubkey.json` are public key files. They can be
committed and shipped with software that verifies language packages.

Private signing keys belong outside git, for example:

```text
private-keys/tiedragon-language-beta-2026.pem
```

Create a new key pair with:

```bash
dotnet run --project src/Tiedragon.LanguagePackage -- create-signing-key private-keys/my-key.pem keys/my-key.lngpdk.pubkey.json my-key
```
