# Releasing Turbo Deck

Turbo Deck distinguishes local test packages from signed release packages. Local packages are suitable for manual
testing but deliberately cannot download or apply updates. A release build sets an embedded signed-release marker only
when electron-builder is required to complete platform signing successfully.

## Local Windows test package

```powershell
.\scripts\package.ps1 -Runtime win-x64
```

This produces the NSIS installer, unpacked application, updater metadata, and adjacent SHA-256 checksum under
`artifacts\installer`. It does not require a signing certificate and the Update Wizard identifies it as an unsigned
test build.

## Signed Windows release

Set the signing certificate and GitHub credentials in the release environment; never commit them:

```powershell
$env:WIN_CSC_LINK = "C:\secure\turbo-deck-signing.pfx"
$env:WIN_CSC_KEY_PASSWORD = "<from-secret-store>"
$env:GH_TOKEN = "<from-secret-store>"
.\scripts\package.ps1 -Runtime win-x64 -RequireSigning -Publish
```

`-RequireSigning` enables electron-builder's fail-closed signing verification. `-Publish` is rejected unless signing is
required and `GH_TOKEN` is available. electron-builder publishes the installer, block map, and update metadata together;
never mix update metadata and artifacts from different builds.

The Update Wizard uses electron-updater with automatic download and automatic apply disabled. The user explicitly
initiates Check, Download, and Apply. Apply becomes available only after update metadata, SHA-512 package integrity,
and the Windows publisher signature have passed the updater checks.

## Other runtimes

The .NET publish and Electron resource selection support `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`,
and `osx-arm64`. Build macOS artifacts on macOS so code signing and notarization can complete. Build Linux AppImages on
a compatible Linux host. Platform credentials and notarization remain release-environment responsibilities.

## Release checklist

1. Increment `desktop/package.json` version and write release notes.
2. Run backend and desktop verification suites.
3. Build on the target platform with `-RequireSigning`.
4. Install the artifact on a clean test account and confirm tray, backend shutdown, update channel, and uninstall.
5. Verify platform signatures and compare the published artifact against its checksum.
6. Publish installer, block map, and updater metadata from the same build.
