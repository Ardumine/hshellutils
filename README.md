# HCore.Packages.HShellUtils

Core shell utilities for the HCore shell — hashing, encoding, text processing, and file inspection commands.

## Commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `sha256` | `sha256sum` | SHA-256 hash |
| `sha512` | `sha512sum` | SHA-512 hash |
| `sha1` | `sha1sum` | SHA-1 hash |
| `md5` | `md5sum` | MD5 hash |
| `base64` | — | Base64 encode/decode |
| `hexdump` | `xxd`, `hd` | Hex dump with ASCII gutter |
| `wc` | — | Line, word, byte count |
| `head` | — | First N lines |
| `tail` | — | Last N lines |
| `grep` | — | Search with regex |
| `sort` | — | Sort lines |
| `uniq` | — | Remove adjacent duplicate lines |
| `date` | — | Current date/time (ISO 8601) |
| `env` | — | Environment variables |
| `which` | — | Show command source |
| `clear` | `cls` | Clear terminal |
| `exists` | `test` | Test path existence |

## Build

```bash
dotnet build
```

Deploys to `../hcore/FS/packs/HCore.Packages.HShellUtils/` via PostBuild.

## TODO

- [ ] `sha256sum` file check mode (verify against checksum file)
- [ ] `tar`, `gzip`/`gunzip`, `zip`/`unzip` — archive utilities
- [ ] `find` — recursive file search
- [ ] `cut` — column extraction
- [ ] `tr` — character translation
- [ ] `du` — disk usage
- [ ] `stat` — file metadata
- [ ] `diff` — file comparison
- [ ] `file` — file type detection (magic bytes)
- [ ] `export` / `unset` — environment variable manipulation
