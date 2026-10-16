# macOS performance prep

Settings to apply on the presenter Mac before the talk. All reversible.

## Demo-critical
- Docker Desktop → General: enable "Use Rosetta for x86/amd64 emulation". The SQL Server image is amd64-only; without Rosetta it runs under QEMU.
- Docker Desktop → Resources: memory 8 GB or more (SQL Server, smtp4dev, LGTM), VirtioFS file sharing.
- Run `aspire run` once beforehand so images are pulled and the build is warm.
- Plugged in, Low Power Mode off. On 14"/16" Pro/Max: Settings → Battery → Energy Mode → High Power.
- Projector at native or "default" resolution; scaled modes cost GPU.

## Background services
| Service | Action |
|---|---|
| Spotlight | Add repo, `~/.nuget`, Docker data dir to Settings → Spotlight → Search Privacy. Or `sudo mdutil -a -i off` (revert: `sudo mdutil -a -i on`) |
| Time Machine | `sudo tmutil disable` (revert: `sudo tmutil enable`) |
| iCloud / Dropbox / OneDrive | Pause sync |
| Software Update | Settings → General → Software Update → Automatic Updates: all off |
| Apple Intelligence / Siri | Off |
| Login & background items | Settings → General → Login Items & Extensions: remove unneeded |
| Apps | Quit Slack, Teams, extra browsers |
| Endpoint security / AV | If permitted, exclude repo, `bin/obj`, `~/.nuget` |

Login and background items:
- "Open at Login" removals apply from the next login; quit running apps manually.
- "Allow in Background" toggles usually stop the agent immediately; some helpers survive until logout.
- Check what is still running: Activity Monitor (sort by CPU/Memory) or `sfltool dumpbtm`.

## Animations
Settings → Accessibility → Display: Reduce motion, Reduce transparency (also tones down Liquid Glass).

```sh
defaults write -g NSAutomaticWindowAnimationsEnabled -bool false
defaults write -g NSWindowResizeTime -float 0.001
defaults write com.apple.dock autohide-delay -float 0
defaults write com.apple.dock autohide-time-modifier -float 0
defaults write com.apple.dock expose-animation-duration -float 0.1
defaults write com.apple.dock mineffect -string scale
killall Dock
```

Revert: `defaults delete <domain> <key>`, then `killall Dock`.

## Talk hygiene
- Focus / Do Not Disturb on; Screen Time and Handoff popups off.
- Restart about 30 minutes before the talk; start only what the demo needs.
- Leave SIP and system daemons alone.
