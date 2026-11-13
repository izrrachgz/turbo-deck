# Releasing Turbo Deck

Turbo Deck currently publishes unsigned Windows packages. An unsigned installer can trigger Microsoft Defender
SmartScreen because Windows cannot verify its publisher. Release notes must state this clearly, and every installer must
be accompanied by its SHA-256 checksum.

## Local unsigned test package

```powershell
.\scripts\package.ps1 -Runtime win-x64
```

This produces an NSIS installer, unpacked application, updater metadata, and adjacent SHA-256 checksum under
`artifacts\installer`. It does not require a signing certificate. The embedded release marker remains disabled, so the
Update Wizard identifies the installation as an unsigned test build and cannot download or apply updates.

## Public unsigned preview

A maintainer can run the **Publish unsigned preview** workflow manually from the `main` branch and enter the exact
acknowledgement requested by the workflow. The workflow is never triggered automatically by a version change or push.

The workflow:

1. Builds the pinned public `CommandQ.*` dependencies from source.
2. Builds and tests the .NET backend and Angular/Electron applications.
3. Creates an unsigned package with the Update Wizard disabled.
4. Uploads the installer, checksum, and effective builder configuration as workflow evidence.
5. Publishes a GitHub prerelease whose title and notes identify the installer as unsigned.

Only one preview may exist for a package version. Increment `desktop/package.json` and the root package entry in
`desktop/package-lock.json` before publishing another preview. The workflow creates the tag
`v<version>-unsigned-preview`.

## Optional owner-supplied certificate

The local-certificate path remains available if the project obtains a suitable code-signing certificate later:

```powershell
$env:WIN_CSC_LINK = "C:\secure\turbo-deck-signing.pfx"
$env:WIN_CSC_KEY_PASSWORD = "<from-secret-store>"
$env:GH_TOKEN = "<from-secret-store>"
.\scripts\package.ps1 -Runtime win-x64 -RequireSigning -Publish
```

`-RequireSigning` enables electron-builder's fail-closed signing verification. Never commit certificate files,
passwords, API tokens, or GitHub tokens.

The generic `-ExternalSigningCandidate` mode is reserved for a future reviewed signing workflow. It is restricted to
GitHub Actions, cannot publish directly, and must not be distributed before an external signer returns and the project
verifies the final artifact. Turbo Deck does not currently claim or depend on any external signing provider.

## Release verification

Before publishing an unsigned preview:

1. Confirm `desktop/package.json` and `desktop/package-lock.json` contain the intended version.
2. Run the backend and desktop verification suites.
3. Build from the reviewed commit through the manual GitHub Actions workflow.
4. Verify the installer version and attached SHA-256 checksum.
5. Install and uninstall on a clean Windows account and confirm tray and backend shutdown behavior.
6. Confirm the GitHub release is marked as a prerelease and clearly identifies the artifact as unsigned.

## Other runtimes

The .NET publish and Electron resource selection support `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`,
and `osx-arm64`. Build macOS artifacts on macOS so Developer ID signing and notarization can complete. Build Linux
AppImages on a compatible Linux host. Each platform's signing, notarization, package verification, and distribution
requirements remain independent.
