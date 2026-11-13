# Privacy policy

Turbo Deck is a local-first desktop application. The project maintainers do not operate an application service that
collects Turbo Deck usage data, account data, deck content, uploaded media, playback history, or local-network traffic.

## Local data

Profiles, pages, cells, preferences, media metadata, Twitch reward bindings, and application logs are stored on the
computer running Turbo Deck. Uploaded media and generated thumbnails remain in the configured local application-data
directory. Turbo Deck does not upload this local data to the project maintainers.

The transient playback queue is held in memory. Local-network discovery advertises connection metadata, not API keys,
OAuth tokens, uploaded media, or other credentials.

## User-requested network access

Turbo Deck communicates with other systems only when required by a feature enabled or requested by the user:

- **Twitch:** Connecting a channel opens Twitch's public Device Code authorization flow. Turbo Deck exchanges OAuth and
  EventSub requests directly with Twitch and stores OAuth tokens encrypted on the local computer. Twitch processes this
  traffic under its own privacy terms.
- **GitHub:** Checking for or downloading an application update contacts the Turbo Deck GitHub repository and GitHub
  Releases. Opening documentation, the maintainer profile, or the issue form also opens GitHub in the default browser.
- **PayPal:** Selecting **Donate uwu** opens the configured PayPal donation page in the default browser. Donations are
  voluntary and do not unlock features or grant additional software rights.
- **Local network:** When LAN access or discovery is enabled, trusted devices on the selected network can reach the
  configured deck, visualizer, and explicitly exposed API surfaces. Turbo Deck does not relay that traffic through a
  maintainer-operated service.
- **Configured databases and services:** If the user selects an external database, Redis backplane, or another optional
  integration, Turbo Deck communicates with the endpoint supplied by that user.

The destination service may receive ordinary connection information such as the IP address, timestamp, user agent, and
request metadata. Refer to that service's privacy policy for its processing practices.

## Diagnostics

Turbo Deck writes structured diagnostic logs locally. Logs are designed to exclude control tokens, LAN API keys, OAuth
tokens, database passwords, uploaded file contents, and physical media paths. The project does not automatically upload
logs or crash reports. A user may choose to attach diagnostic information to a GitHub issue and should review it first.

## Security and questions

Report security issues using the process in [SECURITY.md](SECURITY.md). General project questions can be opened in the
public repository, but sensitive information and credentials must never be included in a public issue.

This policy applies to the Turbo Deck software distributed from the official repository. Forks and modified builds may
behave differently.
