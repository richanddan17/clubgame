# fix-opencode-model-config — Notepad

## 2026-08-03 — v2 role-split applied & verified

### Learnings
- The opencode Zen gateway free-model registry lives in the plugin bundle at
  `C:\Users\midme\.cache\opencode\packages\oh-my-openagent@latest\node_modules\oh-my-openagent\dist\tui.js` (~line 52593+).
  All free models there are reasoning + toolCall capable; key ones: big-pickle (200K/32K, text-only),
  deepseek-v4-flash-free (200K/128K), qwen3.6-plus-free (262K/65K, image+video), mimo-v2-omni-free (262K/64K, image+audio+pdf).
- v1 (all-big-pickle) was already applied & verified (files rewritten 2026-08-03 05:17-05:18 local).
- v2 role-split (user-approved, conservative): big-pickle keeps the high-trust roles; deepseek-v4-flash-free
  takes explore/librarian/atlas/quick/unspecified-low (speed-first); qwen3.6-plus-free takes writing + multimodal-looker (image input).
- Safety rule: every non-big-pickle primary must keep big-pickle in its fallback chain. Verified.
- Only oh-my-openagent.json changed in v2; opencode.jsonc + oh-my-opencode.jsonc stay byte-identical (sha256 pinned:
  opencode.jsonc = 275bda84...55ebaf, oh-my-opencode.jsonc = 643a3e42...735f9d).

### Gotchas
- Prometheus/orchestrator can only write `.omo/*.md` files directly; JSON state files (boulder.json) must be delegated to a worker.
- JSONC files contain `$schema` URLs with `//` — naive `//`-comment-stripping regex corrupts them; validate without stripping (these files have no real comments).
- Config is read at opencode startup; no hot reload. Live spawn verification requires a user restart.

### Decisions
- Conservative split only (4 verified models); glm-5-free / ling-2.6-flash-free / kimi-k2.5-free etc. are a separate later experiment.
- Rollback rule: any role that fails spawn-test after restart reverts to big-pickle.

## 2026-08-03 — v2.1 correction: stale model IDs removed

### Learnings
- The plugin bundle registry (`dist/tui.js`) was **stale** — `opencode models` (live registry, 2026-08-03) shows
  `qwen3.6-plus-free` and `mimo-v2-omni-free` do **not** exist. Spawn tests confirmed: writing + multimodal-looker
  silently fell back to big-pickle (banner shows `Model: opencode/big-pickle`).
- Live opencode registry (7 models): big-pickle, deepseek-v4-flash-free, laguna-s-2.1-free, ling-3.0-flash-free,
  mimo-v2.5-free (MiMo V2.5 Free, active), nemotron-3-ultra-free, north-mini-code-free.
- Fix applied: every `opencode/qwen3.6-plus-free` and `opencode/mimo-v2-omni-free` reference in oh-my-openagent.json
  → `opencode/mimo-v2.5-free` (replaceAll). Result: only 3 real models referenced (big-pickle / deepseek-v4-flash-free / mimo-v2.5-free).
- Re-test of writing + multimodal-looker immediately after the edit STILL showed big-pickle — confirms config is read
  at startup only (no hot reload). Final runtime confirmation requires a user restart.

### Gotchas
- Trust `opencode models` (live registry) over the plugin's bundled registry cache — the bundle can lag reality.
- `opencode models <provider> --verbose` prints cost/metadata but does NOT expose vision capability flags; assume
  mimo-v2.5-free is image-capable (MiMo-VL family) and verify with a real image spawn after restart.

### Status
- Config file fixed & verified at file level (JSON valid, 0 stale model IDs). Restart + spawn re-test of
  writing/multimodal-looker pending (user action).
- ✅ 2026-08-03 — RESOLVED. After user restart, live spawn verification passed: writing → mimo-v2.5-free,
  multimodal-looker → mimo-v2.5-free (both SPAWN-OK, no big-pickle fallback). Fix confirmed at runtime.