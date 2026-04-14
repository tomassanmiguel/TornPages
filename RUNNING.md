# Running Torn Pages Locally

## Prerequisites

All of these should already be installed. Verify with the commands shown.

```
dotnet --version   # expect 10.x
node --version     # expect 22+ or 24+
cloudflared --version  # after installing below
```

Install cloudflared if not present:
```
winget install Cloudflare.cloudflared
```

---

## Starting the API

From the repo root:
```
dotnet run --project TornPages.Api
```

The API starts on `http://localhost:5000` by default. You can verify it's up at:
```
http://localhost:5000/state
```

To change the port, set the `ASPNETCORE_URLS` environment variable:
```
$env:ASPNETCORE_URLS="http://localhost:7000"; dotnet run --project TornPages.Api
```

---

## Exposing the API publicly (Cloudflare Tunnel)

You have two options. **Option B is recommended** — it gives a permanent URL that never changes, so Vercel only needs to be configured once.

---

### Option A — Quick Tunnel (no setup, URL changes on every restart)

No account or domain required. Run this in a second terminal after the API is up:

```
cloudflared tunnel --url http://localhost:5000
```

cloudflared prints a line like:
```
Your quick Tunnel has been created! Visit it at:
https://some-random-words.trycloudflare.com
```

That URL is your public API endpoint for this session. Update `VITE_API_URL` in Vercel each time it changes.

---

### Option B — Named Tunnel with Custom Domain (one-time setup, permanent URL)

This is a one-time setup. After completing it, starting the tunnel is a single command.

#### One-time setup

**Step 1 — Get a domain in Cloudflare DNS**

You need a domain whose DNS is managed by Cloudflare. If you don't have one:
- Register one at `cloudflare.com/products/registrar` (at-cost pricing, no markup)
- Or transfer an existing domain: go to your domain's current registrar and point the nameservers to Cloudflare's (Cloudflare will tell you which ones)

A `.dev` or `.app` domain typically costs $10–12/year.

**Step 2 — Install and authenticate cloudflared**

```
winget install Cloudflare.cloudflared
cloudflared tunnel login
```

A browser window opens. Log in with the Cloudflare account that owns your domain. A credentials file is saved to `C:\Users\tsanm\.cloudflared\`.

**Step 3 — Create the tunnel**

```
cloudflared tunnel create torn-pages
```

This prints a Tunnel ID (a UUID) and saves a credentials JSON to `C:\Users\tsanm\.cloudflared\`. Note the ID — you'll need it in the next step.

**Step 4 — Create the config file**

Create `C:\Users\tsanm\.cloudflared\config.yml` with this content, replacing the placeholders:

```yaml
tunnel: 47cb0892-aa21-4b7f-bfe2-f3eb44881b4e
credentials-file: C:\Users\tsanm\.cloudflared\47cb0892-aa21-4b7f-bfe2-f3eb44881b4e.json

ingress:
  - hostname: api.torn-pages.com
    service: http://localhost:5000
  - service: http_status:404
```

The config file is already written at `C:\Users\tsanm\.cloudflared\config.yml` with the correct tunnel ID and credentials path.

**Step 5 — Create the DNS record**

```
cloudflared tunnel route dns torn-pages api.yourdomain.com
```

This creates a CNAME record in your Cloudflare DNS dashboard automatically.

**Step 6 — Set VITE_API_URL in Vercel**

In your Vercel project settings → Environment Variables, set:
```
VITE_API_URL = https://api.torn-pages.com
```

Redeploy once. You never need to change this again.

---

#### Starting the named tunnel (after setup)

Every time you want the app to be publicly accessible, run both of these (in separate terminals):

**Terminal 1 — API:**
```
dotnet run --project TornPages.Api
```

**Terminal 2 — Tunnel:**
```
cloudflared tunnel run torn-pages
```

The tunnel connects and your API is live at `https://api.yourdomain.com`. Stop either terminal to take it offline.

---

## Local development (no tunnel needed)

If you're just developing locally and don't need public access:

**Terminal 1 — API:**
```
dotnet run --project TornPages.Api
```

**Terminal 2 — React client:**
```
cd client
npm run dev
```

The React dev server runs at `http://localhost:5173` and proxies API calls to `http://localhost:5000`. No tunnel needed — everything stays on your machine.

Create `client/.env.local` with:
```
VITE_API_URL=http://localhost:5000
```

---

## Vercel deployment (React client)

The React client is hosted on Vercel. Pushing to `main` on GitHub triggers an automatic redeploy.

To manually trigger a redeploy (e.g. after changing an env var):
```
# Install Vercel CLI once:
npm install -g vercel

# Trigger redeploy:
vercel --prod
```

---

## Quick reference

| What | Command |
|------|---------|
| Start API | `dotnet run --project TornPages.Api` |
| Start tunnel | `cloudflared tunnel run torn-pages` |
| Start React dev server | `cd client && npm run dev` |
| Run all tests | `dotnet test` |
| Build React for prod | `cd client && npm run build` |
| Check API is up | `curl http://localhost:5000/state` |
