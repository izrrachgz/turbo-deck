# Releasing Turbo Deck

Turbo Deck has three distinct Windows package types. They must not be confused or published interchangeably.

## Local unsigned test package

```powershell
.\scripts\package.ps1 -Runtime win-x64
```

This produces an NSIS installer, unpacked application, updater metadata, and adjacent SHA-256 checksum under
`artifacts\installer`. It does not require a signing certificate. The embedded release marker remains disabled, so the
Update Wizard identifies the installation as an unsigned test build and cannot download or apply updates.

## First public unsigned preview

SignPath Foundation requires an OSS project to have an existing release in the form that will later be signed. Until
SignPath enrollment is complete, a maintainer may run the **Publish unsigned preview** workflow manually from the `main`
branch and enter the exact acknowledgement requested by the workflow.

That workflow:

1. Builds the pinned `CommandQ.*` dependencies from their source revision using a read-only repository token.
2. Builds and tests the .NET backend and Angular/Electron applications.
3. Creates a normal unsigned package with the Update Wizard disabled.
4. Uploads the installer, checksum, and effective builder configuration as workflow evidence.
5. Publishes an explicitly named GitHub prerelease with an unsigned warning.

Only one preview may exist for a package version. Increment `desktop/package.json` and its lock file before publishing a
later preview.

## SignPath release candidate

After SignPath Foundation approves Turbo Deck and assigns its organization, project, signing-policy, and artifact
configuration identifiers, the official workflow will build a candidate with:

```powershell
.\scripts\package.ps1 -Runtime win-x64 -ExternalSigningCandidate
```

This mode is restricted to GitHub Actions. It embeds the official-release marker but intentionally leaves signing to
SignPath. It must never be distributed directly. The workflow must upload it as build evidence, submit it through the
official SignPath GitHub Action, wait for manual approval, download the signed result, and verify it before publication.

The final SignPath workflow is intentionally not committed before enrollment because its organization and policy
identifiers are assigned by SignPath. Use `signpath/github-action-submit-signing-request@v3` and the current official
integration instructions when adding it.

## Optional local-certificate release

The legacy local-certificate path remains available as an emergency migration path:

```powershell
$env:WIN_CSC_LINK = "C:\secure\turbo-deck-signing.pfx"
$env:WIN_CSC_KEY_PASSWORD = "<from-secret-store>"
$env:GH_TOKEN = "<from-secret-store>"
.\scripts\package.ps1 -Runtime win-x64 -RequireSigning -Publish
```

`-RequireSigning` enables electron-builder's fail-closed signing verification. Never commit certificate files,
passwords, API tokens, or GitHub tokens. The SignPath workflow will supersede this direct publication path once active.

## SignPath enrollment checklist

Before applying:

1. Make the `command-q` source used by Turbo Deck publicly verifiable. A private maintainer-supplied component does not
   satisfy SignPath Foundation's open-source and verifiable-build requirements.
2. Publish the unsigned preview and verify that it installs and uninstalls on a clean Windows account.
3. Enable multi-factor authentication for GitHub and SignPath accounts involved in development, review, or approval.
4. Keep [the code-signing policy](../CODE_SIGNING_POLICY.md), [privacy policy](../PRIVACY.md), and
   [security policy](../SECURITY.md) public and accurate.
5. Apply with repository `https://github.com/izrrachgz/turbo-deck`, the preview release URL, and its installer sample.
6. Identify `@izrrachgz` as the current committer/reviewer and signing approver.
7. Confirm the exact Turbo Deck-owned files SignPath will sign and exclude third-party Electron/framework binaries.
8. Install or authorize the SignPath GitHub integration requested during onboarding.
9. Store the assigned API token as the protected `SIGNPATH_API_TOKEN` GitHub Actions secret.
10. Add the assigned non-secret organization, project, policy, and artifact-configuration identifiers to the official
   workflow or protected GitHub environment variables.

## Private CommandQ dependency access

Until `command-q` is public, both GitHub workflows require a fine-grained personal access token stored as the Turbo Deck
repository secret `COMMAND_Q_REPOSITORY_TOKEN`. Configure the token with:

- Repository owner: `izrrachgz`
- Repository access: only `command-q`
- Repository permission: **Contents — Read-only**
- No account permissions and no write permissions
- A short, renewable expiration appropriate for CI

The workflow passes this token only to the secondary checkout and disables persisted Git credentials afterward. GitHub
does not provide repository secrets to workflows triggered from forks, so external pull requests cannot build this
private dependency until its source becomes public.

## Official signed-release controls

The final release job must fail closed unless all of these checks pass:

- The workflow runs from the protected release ref on a GitHub-hosted Windows runner.
- Backend and desktop verification succeed before the signing request is submitted.
- SignPath verifies the GitHub origin and a configured approver manually approves the request.
- The returned installer has a valid Authenticode signature from the expected SignPath Foundation publisher.
- The installer version matches the committed `desktop/package.json` version.
- SHA-256 checksums and Electron blockmap/update metadata are regenerated from the final signed bytes.
- The signed installer, blockmap, updater metadata, and checksums originate from one workflow run.
- The GitHub release is created only after verification and never contains the unsigned candidate.

electron-builder's updater metadata created before external signing is not publishable because signing changes the
installer's size and hashes. The SignPath workflow must regenerate or replace it after retrieving the signed installer.

## Other runtimes

The .NET publish and Electron resource selection support `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`,
and `osx-arm64`. Build macOS artifacts on macOS so Developer ID signing and notarization can complete. Build Linux
AppImages on a compatible Linux host. SignPath Foundation Windows signing does not replace Apple notarization or other
platform-specific release requirements.

## Release checklist

1. Increment `desktop/package.json` and `desktop/package-lock.json`, then write release notes.
2. Run backend and desktop verification suites.
3. Build the release candidate from the reviewed GitHub Actions workflow.
4. Review and approve the SignPath signing request.
5. Verify the returned signature and regenerate final update metadata and checksums.
6. Install on a clean test account and confirm tray behavior, backend shutdown, updates, and uninstall.
7. Publish all final artifacts from that same run.
