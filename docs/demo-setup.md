# Demo Setup

To get this app ready for a fresh demo, do the folowing steps

## Clear DB and start cloudflare tunnel

The cloudflare tunnel relies on a file in the root, .cloudflared-token, which is not in source control. If you want to try this yourself, set your own tunnel up.

```
TBC - Db step
cloudflared tunnel run --token-file .cloudflared-token
```

