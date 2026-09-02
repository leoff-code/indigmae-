# Island / harvesting / music

The existing scene is `Assets/Scenes/CrystalSprint.unity`. The installer refuses to overwrite an already integrated island. Imported package files are not modified.

## Coast

- The existing forest, cabin and pond terrain vertices within 55 m are preserved. The outer 135 m grid is reshaped into a beach, continued with a matching 1.5 m grid to a 240 m seabed.
- Coast radius varies around 73 m. The sea is 2400 m across, at Y = -2.4. Its mesh has no surface beneath the central pond.
- Sand: **Yughues Free Sand Materials**, texture set **01** (albedo, normal and specular), used in `CrystalSprint/Island Shore URP`. Existing grass material stays on the centre. World-space sand, irregular blending, normal relief, darker/wetter shore and lower shore grass density.
- Ocean: **Houidisoft / One Click Add Water – Stylized Water Shader**, original `water shader ocean.shadergraph`, with a separate tuned material. URP depth/opaque textures and depth prepass are enabled. No imported shader is edited.
- 28 small existing rocks/branches decorate the coast. They do not block walking. Deep ocean returns the player to the last dry spot; swimming is intentionally not introduced.
- The pond material, water animation, fish timing/models and splash/ripple assets remain unchanged. `ContainsWater` now rejects positions outside its existing surface bounds, so the new low sea terrain cannot accidentally trigger pond-height effects.

## Trees / axe / inventory

- All 240 existing trees keep their models/materials/LODs while standing. Added bark-only collision meshes use the actual imported trunk triangles. Existing movement capsules remain, but never count as axe contacts.
- `AxeChopping` sweeps a 6.5 cm contact volume at the actual blade during the strike phase. One contact per swing. Air swings, stumps and unequipped attacks do not damage a tree. Contact stops the follow-through and initiates recovery.
- Exactly 3 hits fell the tree. A 0.22 s delay precedes gravity-accelerated rotation; the crown settles onto terrain as the trunk reaches 88 degrees. Nearby building/obstacle clearance influences fall direction.
- `TreeMeshCut` splits the imported geometry, retaining materials/UVs and adding a cut cap. Ten shared readable mesh copies support runtime cuts without changing import settings. Only struck trees allocate split meshes; meshes are cleaned on reload.
- The matching base remains as a stump. Fallen bark stays hittable, while its temporary capsule is disabled once settled. Grass is locally cleared around the stump/trunk.
- Exactly 3 more actual axe hits process the fallen tree over 0.85 s with visible log pieces/chips/dust. Low strikes include a controlled crouching head dip; arm lengths/grip remain unchanged.
- `LumberjackEquipment` still owns all four slots. Slot 1 remains the axe; wood occupies a free slot 2–4. `InventoryHud` shows the model-rendered bundle icon/name. If full, an E-usable bundle stays in the world with a clear message. There is no second inventory system.

## Music / pause

- `MusicMenu` owns one looping, non-spatial AudioSource, a 1.8 s unscaled fade-in, volume and pause state. M opens/closes the top-right slider. Opening pauses time and gameplay input; closing resumes and captures the cursor. Esc closes without capturing, preserving the existing click-to-capture behaviour.
- Slider clicks cannot recapture the mouse or attack. Look input accumulated while closing the menu is discarded. Volume is persisted as `CrystalSprint.MusicVolume` in PlayerPrefs.
- **Pending asset:** no Jungle audio clip was present in the project at integration time. Assign the user's intended clip to `Music and Pause / AudioSource`. No substitute song or unlicensed audio is included.

## Verification

PlayMode tests in `IslandGameplayPlayModeTests` cover exact real blade hits (standing and fallen), ten imported variants, processing/rewards/full inventory, coastline continuity/protected terrain, UI pointer volume control, M/Esc/cursor/pause, shader validity and safe deep water. Existing cabin/door/curtain/FPS/fish tests remain enabled. Test results and render previews are in `Logs/` (generated, not game assets).

Final run: **45 passed / 0 failed**, Unity exit code 0, 2026-09-02 15:56:22–15:58:56 UTC. Report: `Logs/island-final-tests.xml`. Render review completed without shader errors.

The Input System package logs an existing settings-cleanup assertion after the test results are saved and Play Mode exits. The same assertion appears in the earlier `cabin-final-tests.log` and `first-person-tests.log`; it is not a game-time/compiler failure. Imported package code was not patched to suppress it. Actual music playback remains untested until the intended audio file is provided.
