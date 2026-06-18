# WebGL Texture Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce embedded texture dimensions in six source GLBs to at most 2048 pixels without changing Unity asset paths or generating a WebGL build.

**Architecture:** A focused Node.js utility will parse GLB containers, extract embedded images, call local ImageMagick for resizing, and rebuild buffer views with corrected offsets and lengths. A separate inspection mode will provide before/after manifests and structural validation; timestamped originals will live outside `Assets`.

**Tech Stack:** Node.js, ImageMagick, Unity 6000.4.3f1, glTF 2.0/GLB.

---

## File Structure

- Create `tools/glb-texture-optimizer.mjs`: GLB parser, image inventory, resize orchestration, binary repacking, and validation CLI.
- Create `tools/glb-texture-optimizer.test.mjs`: synthetic GLB regression tests for unchanged structure, 4-byte alignment, buffer-view updates, and 2048-pixel bounds.
- Modify the six GLBs listed in the approved design, preserving their paths and `.meta` files.
- Create `.optimization-backups/2026-06-17/`: originals and generated before/after manifests; this directory remains outside Unity's `Assets` tree.

### Task 1: Build and test the GLB optimizer

**Files:**
- Create: `tools/glb-texture-optimizer.mjs`
- Create: `tools/glb-texture-optimizer.test.mjs`

- [ ] **Step 1: Write synthetic-container tests**

Create tests using `node:test` that build a minimal GLB with an embedded 4096×1024 PNG, invoke the optimizer, and assert: GLB magic/version remain valid; JSON and BIN chunks are 4-byte aligned; non-image bytes are unchanged; image dimensions become 2048×512; image buffer-view offsets and lengths point to valid bytes.

- [ ] **Step 2: Verify the tests fail before implementation**

Run: `node --test tools/glb-texture-optimizer.test.mjs`

Expected: FAIL because `glb-texture-optimizer.mjs` does not exist.

- [ ] **Step 3: Implement the minimal CLI**

Implement these exported boundaries:

```js
export function parseGlb(bytes) {}
export function inventoryImages(document, binaryChunk) {}
export async function optimizeGlb(inputPath, outputPath, options = { maxDimension: 2048 }) {}
export function validateGlb(bytes, maxDimension = 2048) {}
```

The CLI must support:

```bash
node tools/glb-texture-optimizer.mjs inspect input.glb
node tools/glb-texture-optimizer.mjs optimize input.glb output.glb --max-dimension 2048
node tools/glb-texture-optimizer.mjs validate output.glb --max-dimension 2048
```

Use `magick <source> -resize '2048x2048>' -strip <destination>` for oversized images. Preserve original bytes for images already within the limit. Repack every BIN-backed buffer view sequentially with 4-byte padding and update `byteOffset`, `byteLength`, `buffers[0].byteLength`, and GLB chunk lengths. Reject unsupported external images, malformed ranges, and multi-BIN GLBs with explicit errors.

- [ ] **Step 4: Run tests and static checks**

Run: `node --check tools/glb-texture-optimizer.mjs && node --test tools/glb-texture-optimizer.test.mjs`

Expected: syntax check succeeds and all tests PASS.

### Task 2: Inventory and back up the six production GLBs

**Files:**
- Create: `.optimization-backups/2026-06-17/before.json`
- Copy originals under: `.optimization-backups/2026-06-17/originals/`

- [ ] **Step 1: Generate the before manifest**

Run inspection for all six approved paths and record file SHA-256, byte size, image MIME type, dimensions, byte size, and referenced texture roles in `before.json`.

- [ ] **Step 2: Copy originals without touching `.meta` files**

Copy each GLB to the mirrored backup path and verify every backup SHA-256 equals the manifest.

- [ ] **Step 3: Confirm the backup is outside `Assets`**

Run: `find Assets -path '*optimization-backups*' -print`

Expected: no output.

### Task 3: Optimize production GLBs in place

**Files:**
- Modify: `Assets/Models/locker_rough_texture.glb`
- Modify: `Assets/Models/Door/card_security_reader.glb`
- Modify: `Assets/Models/Door/double_door.glb`
- Modify: `Assets/Models/russian_first_aid_kit_v3.glb`
- Modify: `Assets/Models/Cell/abandoned_wall__peeling_paint__pbr_game_asset.glb`
- Modify: `Assets/Models/Cell/Techo.glb`
- Create: `.optimization-backups/2026-06-17/after.json`

- [ ] **Step 1: Optimize each GLB to a temporary sibling file**

Run the optimizer with `--max-dimension 2048`, validate the temporary result, then atomically replace only the corresponding `.glb` file.

- [ ] **Step 2: Generate the after manifest**

Record the same fields as `before.json` and assert every embedded image dimension is at most 2048 pixels.

- [ ] **Step 3: Compare structural invariants**

Assert before/after counts and indices for scenes, nodes, meshes, primitives, materials, textures, samplers, accessors, and animations are identical. Confirm each original `.meta` SHA-256 is unchanged.

### Task 4: Reimport and final verification

**Files:**
- Verify: the six optimized GLBs and Unity Editor log

- [ ] **Step 1: Trigger a Unity asset reimport without a player build**

Open the project with Unity 6000.4.3f1 or use its batch-mode asset import. Do not invoke `BuildPipeline` or modify the existing WebGL output.

- [ ] **Step 2: Check import results**

Inspect the Editor log for errors related to the six GLBs. Expected: no parse, image, material, or buffer errors.

- [ ] **Step 3: Verify scope and report reduction**

Run `git status --short` and confirm no scripts, scenes, prefabs, project settings, package files, or existing user modifications were changed by this work. Report total GLB bytes before/after and remind the user that the final `.data.br` threshold requires their WebGL build.
