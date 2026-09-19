# Embedded Netcode package

`com.unity.netcode.gameobjects/` contains Unity's official Netcode for GameObjects 2.13.2 package, embedded to avoid a stalled Package Manager download on this machine. Runtime and Editor source files are unmodified. The `Documentation~` directory (large documentation images) is omitted; samples, tests, metadata, and the original licence are retained.

- Source: https://download.packages.unity.com/com.unity.netcode.gameobjects/-/com.unity.netcode.gameobjects-2.13.2.tgz
- Registry: https://packages.unity.com/com.unity.netcode.gameobjects
- Download SHA-1, checked against registry metadata: `cfd429cc91ecf7b73d4ed10b91727e2fff0baffd`
- Package licence: `com.unity.netcode.gameobjects/LICENSE.md`
- Online documentation: https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/index.html
- Unity's embedded-package workflow: https://docs.unity3d.com/Manual/upm-embed.html

The embedded package takes precedence over the version listed in `manifest.json`. To return to registry downloads later, close Unity and move this specific embedded package directory outside the project; keep the manifest dependency pinned to 2.13.2. Do not discard the local package until a registry import has succeeded.
