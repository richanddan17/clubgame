# fix-opencode-model-config - Work Plan (v2: 무료 모델 역할 분담)

## TL;DR (For humans)

**What v1 did (DONE, verified):** All agent/category models pointed at `opencode/big-pickle` (the only empirically-proven free model — this session runs on it). Verified 2026-08-03: `oh-my-openagent.json`, `opencode.jsonc`, `oh-my-opencode.jsonc` all match v1 target; JSON valid; no premium model references remain in active files (only `.backup-*`). User restarted opencode and confirmed all subagents spawn on `big-pickle`.

**What v2 does:** Role-split across the 4 **verified/registry-proven free models** (big-pickle, deepseek-v4-flash-free, qwen3.6-plus-free, mimo-v2-omni-free) to (a) spread free-tier rate limits across multiple models and (b) match each role's characteristics (speed-first vs reasoning-first vs image input vs long output). User decision: **conservative split** — only the 4 models already used by v1, no new untested models. Safety net: `big-pickle` is always in every fallback chain.

**What it will NOT do:** Agent names, categories, `variant` values, `$schema` line unchanged. `opencode.jsonc` and `oh-my-opencode.jsonc` are already at final state from v1 — **NO changes to them**. No game code touched.

**Effort:** Trivial — 1 config file, 1 rewrite (only `model` / `fallback_models` values change).
**Risk:** Low — all 4 models already referenced in v1 config; big-pickle proven live. Residual risk: deepseek/qwen/mimo free-tier rate limits (mitigated by fallback chain ending in big-pickle).
**Verification:** User restarts opencode, then spawns one subagent per role that uses a non-big-pickle model; any failure → roll that role back to big-pickle.

## Current state (v1, verified 2026-08-03)

- `C:\Users\midme\.config\opencode\oh-my-openagent.json` — all agents/categories → `opencode/big-pickle` (v1 target, applied 05:17:24)
- `C:\Users\midme\.config\opencode\opencode.jsonc` — provider block removed (applied 05:17:59) — **FINAL, no change**
- `C:\Users\midme\.config\opencode\oh-my-opencode.jsonc` — sisyphus-junior/build → `opencode/big-pickle` (applied 05:18:15) — **FINAL, no change**

## Changes (exact) — v2

### A. `C:\Users\midme\.config\opencode\oh-my-openagent.json` — role-split rewrite

Replace the ENTIRE file with the following (keep `$schema` line, keep every agent/category key, keep every `variant` value; only `model` and `fallback_models` change):

```json
{
  "$schema": "https://raw.githubusercontent.com/code-yeongyu/oh-my-openagent/dev/assets/oh-my-opencode.schema.json",
  "agents": {
    "sisyphus": {
      "model": "opencode/big-pickle",
      "variant": "max",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "hephaestus": {
      "model": "opencode/big-pickle",
      "variant": "medium"
    },
    "oracle": {
      "model": "opencode/big-pickle",
      "variant": "high",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free", "variant": "high" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "explore": {
      "model": "opencode/deepseek-v4-flash-free",
      "fallback_models": [
        { "model": "opencode/big-pickle" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "multimodal-looker": {
      "model": "opencode/qwen3.6-plus-free",
      "variant": "medium",
      "fallback_models": [
        { "model": "opencode/mimo-v2-omni-free" },
        { "model": "opencode/big-pickle" }
      ]
    },
    "prometheus": {
      "model": "opencode/big-pickle",
      "variant": "max",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free", "variant": "high" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "metis": {
      "model": "opencode/big-pickle",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "momus": {
      "model": "opencode/big-pickle",
      "variant": "xhigh",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free", "variant": "high" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "atlas": {
      "model": "opencode/deepseek-v4-flash-free",
      "fallback_models": [
        { "model": "opencode/big-pickle" }
      ]
    },
    "sisyphus-junior": {
      "model": "opencode/big-pickle",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "librarian": {
      "model": "opencode/deepseek-v4-flash-free",
      "fallback_models": [
        { "model": "opencode/big-pickle" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    }
  },
  "categories": {
    "visual-engineering": {
      "model": "opencode/big-pickle",
      "variant": "high",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "ultrabrain": {
      "model": "opencode/big-pickle",
      "variant": "xhigh",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free", "variant": "high" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "deep": {
      "model": "opencode/big-pickle",
      "variant": "medium",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "artistry": {
      "model": "opencode/big-pickle",
      "variant": "high",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "quick": {
      "model": "opencode/deepseek-v4-flash-free",
      "fallback_models": [
        { "model": "opencode/big-pickle" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "unspecified-low": {
      "model": "opencode/deepseek-v4-flash-free",
      "fallback_models": [
        { "model": "opencode/big-pickle" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "unspecified-high": {
      "model": "opencode/big-pickle",
      "fallback_models": [
        { "model": "opencode/deepseek-v4-flash-free" },
        { "model": "opencode/qwen3.6-plus-free" }
      ]
    },
    "writing": {
      "model": "opencode/qwen3.6-plus-free",
      "fallback_models": [
        { "model": "opencode/big-pickle" },
        { "model": "opencode/deepseek-v4-flash-free" }
      ]
    }
  }
}
```

### Role-split rationale

| Role | Model | Why |
|---|---|---|
| sisyphus, hephaestus, oracle, prometheus, metis, momus, sisyphus-junior | `big-pickle` | Highest-trust reasoning roles; proven live |
| ultrabrain, deep, artistry, visual-engineering, unspecified-high | `big-pickle` | Hard / high-stakes categories |
| explore, librarian, atlas | `deepseek-v4-flash-free` | Speed-first lookups / config work; 128K output |
| quick, unspecified-low | `deepseek-v4-flash-free` | Simple tasks, speed over depth |
| writing | `qwen3.6-plus-free` | Creativity + 65K output; image/video input available |
| multimodal-looker | `qwen3.6-plus-free` | Image/video input required (unchanged from v1) |

Safety net: every non-big-pickle role's fallback chain includes `big-pickle`.

## Validation checklist after edit (all must pass)

- [x] Every `model` value starts with `opencode/` and is one of: `big-pickle`, `deepseek-v4-flash-free`, `qwen3.6-plus-free`, `mimo-v2-omni-free`.
- [x] NO occurrences of `gpt-5-nano`, `claude-opus-4-7`, `gpt-5.5`, `gemini-3.1-pro`, `glm-5`, `kimi-k2.5`, `claude-sonnet-4-6`, `gemini-3-flash`, `openai/` remain.
- [x] JSON parses (valid via `node -e "JSON.parse(...)"`).
- [x] All 11 agents (sisyphus, hephaestus, oracle, explore, multimodal-looker, prometheus, metis, momus, atlas, sisyphus-junior, librarian) and all 8 categories present; every `variant` value identical to v1.
- [x] Roles with `deepseek-v4-flash-free` / `qwen3.6-plus-free` as primary have `big-pickle` in their fallback chain.
- [x] `opencode.jsonc` and `oh-my-opencode.jsonc` are byte-identical to before this change (untouched).

## Verification (post-edit, user does the restart)

1. **Restart opencode** (config is read at startup; no hot reload).
2. **Spawn test per non-big-pickle role** — one real subagent task each, in this order:
   - `explore` (deepseek) → e.g. `task(subagent_type="explore", prompt="quick: where is X")`
   - `librarian` (deepseek) → e.g. a short doc lookup task
   - `atlas` (deepseek) → a small config-ish task
   - `quick` (deepseek) → `task(category="quick", ...)`
   - `unspecified-low` (deepseek) → a trivial task
   - `writing` (qwen3.6-plus-free) → a short prose task
   - `multimodal-looker` (qwen3.6-plus-free) → a media analysis task
3. **Pass criteria per role:** subagent completes without `ProviderModelNotFoundError`, without hanging idle, without auth errors. Session banner/model picker shows the assigned model (not silently falling back to big-pickle for the primary attempt).
4. **Rollback rule:** any role that fails → set that role's `model` back to `opencode/big-pickle` (keep its current fallback chain), re-run the failing spawn test until green.
5. Optional later (ONLY after all green): try `opencode/ling-2.6-flash-free` for `explore` for speed — one model at a time, roll back on failure.

## Must NOT do

- Do NOT change agent names, categories, `variant` values, or the `$schema` line in `oh-my-openagent.json`.
- Do NOT touch `opencode.jsonc` / `oh-my-opencode.jsonc` (already at v1 final state).
- Do NOT introduce models outside the 4 verified ones (big-pickle, deepseek-v4-flash-free, qwen3.6-plus-free, mimo-v2-omni-free) in this pass — new models (glm-5-free, ling-2.6-flash-free, kimi-k2.5-free, etc.) are a separate later experiment.
- Do NOT touch any game code, `.omo/plans/skill-system-rework.md`, or the Unity project.
- Do NOT add back `gpt-5-nano` / `claude-opus-4-7` / premium models — user only has free models.
- Do NOT add more than 2 fallback hops.
