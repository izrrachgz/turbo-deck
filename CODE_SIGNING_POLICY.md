# Code signing policy

Turbo Deck is developed and released from the public repository at
<https://github.com/izrrachgz/turbo-deck>. The project is distributed under the MIT License.

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Signed artifacts

Official Windows releases are built from this repository on GitHub-hosted GitHub Actions runners. Signing requests are
submitted from the release workflow to SignPath and require manual approval. GitHub releases are published only after
the returned artifact passes Authenticode, version, checksum, and updater-metadata verification.

Local builds and explicitly labelled preview releases may be unsigned. They are not treated as official signed releases,
and Turbo Deck's Update Wizard remains disabled in them.

The SignPath Foundation certificate is used only for Turbo Deck artifacts built from source maintained by this project.
Third-party binaries are not re-signed with the project's certificate.

## Roles

- Committer and reviewer: [@izrrachgz](https://github.com/izrrachgz)
- Signing approver: [@izrrachgz](https://github.com/izrrachgz)

Turbo Deck currently has a single maintainer. Contributions are reviewed before merge, and every release-signing request
is reviewed separately before approval. These roles will be updated if the maintainer team changes.

## Build and release controls

- The release workflow uses only committed build scripts and lock files.
- The shared `CommandQ.*` dependencies are built from the pinned `command-q` source revision recorded in the workflow.
  This dependency source must be made publicly verifiable before applying for SignPath Foundation signing.
- Official release candidates are never published before signing completes successfully.
- Release checksums and Electron updater metadata are generated from the final signed artifact.
- Repository and SignPath access require multi-factor authentication.
- Signing credentials and API tokens are stored only as protected GitHub or SignPath secrets.

See [the release guide](docs/releasing.md) for the operational process and [the privacy policy](PRIVACY.md) for the
application's network behavior.

Signing Turbo Deck does not transfer its copyright, source repository, trademarks, or other project rights to SignPath
or SignPath Foundation. The certificate identifies SignPath Foundation as the publisher that verified the signed build.
