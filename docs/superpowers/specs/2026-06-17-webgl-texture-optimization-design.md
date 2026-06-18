# WebGL Texture Optimization Design

## Goal

Reduce the uncompressed texture payload in the source GLB assets enough for the user's subsequent Unity WebGL build and Brotli compression to target a `.data.br` file below GitHub's 100 MiB per-file limit, while preserving the current Unity scenes, model references, materials, and gameplay.

## Scope

Optimize the embedded textures in these six GLB assets:

- `Assets/Models/locker_rough_texture.glb`
- `Assets/Models/Door/card_security_reader.glb`
- `Assets/Models/Door/double_door.glb`
- `Assets/Models/russian_first_aid_kit_v3.glb`
- `Assets/Models/Cell/abandoned_wall__peeling_paint__pbr_game_asset.glb`
- `Assets/Models/Cell/Techo.glb`

No scripts, scenes, prefabs, project settings, or unrelated user changes will be modified.

## Approach

1. Inspect each GLB and inventory its embedded images, dimensions, formats, materials, and texture references.
2. Create a backup of each original GLB outside `Assets` so Unity does not import duplicate assets.
3. Resize embedded textures whose width or height exceeds 2048 pixels, preserving aspect ratio and texture semantics.
4. Re-encode color textures with a visually appropriate lossy format and preserve alpha where required. Keep normal, metallic, roughness, occlusion, and other data textures in a format and color space that avoids corrupting their channels.
5. Rebuild each GLB at its original path so existing Unity GUIDs and references remain valid.
6. Reimport the optimized assets in Unity. Do not generate or recompress the WebGL build; the user will perform that final build step.

## Verification

- Confirm all six GLBs remain structurally valid.
- Confirm meshes, materials, texture assignments, and node structure are retained.
- Open the Unity project and check for importer errors without producing a WebGL build.
- Compare the source GLB sizes and imported texture payload before and after optimization.
- Leave the project ready for the user to run the existing WebGL build and Brotli compression.
- The final `.data.br` size cannot be confirmed until the user performs that build; if it remains above 100 MiB, its measured gap will determine whether additional assets need optimization.

## Rollback and Risk Control

Original GLBs will be retained in a timestamped backup directory outside `Assets`. Optimization will preserve filenames and Unity `.meta` files. The principal trade-off is reduced close-up texture detail; 2048 pixels is selected because this is a short-lived browser-delivered university project where download size is the binding constraint.
