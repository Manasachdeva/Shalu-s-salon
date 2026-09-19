# Elemental Journey build verification

Validation date: 18 September 2026. Source version: **0.2.0 (2)**.

## Current results

- Unity Editor: 6000.6.0f1.
- Netcode for GameObjects: embedded official 2.13.2.
- Standalone engine-independent LAN checks: **24/24 passed**.
- Unity EditMode: **38/38 passed**. Includes address admission, protocol matching, room capacity, XP thresholds, skill locks and preset weapon identities.
- Unity PlayMode: **4/4 passed**. Covers touch movement and room reopening, network casts and defensive skills, the complete Story quest chain, and guarded Adventure scrolls.
- Desktop development build: succeeded after the menu-label correction.
- Two independent desktop processes: **host and client passed**. Logs and JSON results: `Logs/Lan-20260918-225805`.
- Host observed 5.51 metres of remote movement; client observed 6.38 metres.
- Both peers verified character and damage replication, duel completion and shared co-op XP. The host verified friendly-fire prevention after respawn immunity expired.
- The corrected menu and gameplay previews were visually inspected. See `menu-preview.png` and `game-preview.png`.
- All workspace C# source files matched the tested build copy by SHA-256 before Android compilation.
- Android APK build: **succeeded**, Unity exited with code 0.
- APK signature: **verified**, APK Signature Scheme v2.
- ZIP alignment: **passed** `zipalign -c -P 16 4`.
- Manifest: `com.brothersblock.lan`, version **0.2.0 (2)**, minimum API 26,
  target API 36, ARM64 and ARMv7, INTERNET permission present.

## Delivered APK

`Builds/Android/ElementalJourney.apk`

Size: **48,629,420 bytes** (approximately 46.4 MiB).

SHA-256:

`8951ee35d8682a536b22558e3ea9836293fcc08ed44dcc8dc7d4afd9f60c47fd`

The copied APK's hash matches the verified build output. A checksum file is
stored beside it. Build and package verification logs have the `elemental-`
prefix in `Logs/`.

Unity test reports are `Logs/edit-results.xml` and `Logs/play-results.xml`. The UI label and test-preview appearance were adjusted after those Unity test runs, then compiled and exercised through the passing desktop probe. Gameplay logic was unchanged after the Unity tests.

## Build environment

Builds run from `C:/Users/user/AppData/Local/Temp/BrothersBlock-Build-20260909-230651`, avoiding the file-access failures previously observed in the OneDrive workspace. Source and output are copied back into the project; generated caches and build logs are ignored by Git.

Unity initially stopped with exit code 198 because the licence was inactive. After licence activation, the restricted process could not reach the licensing service. Running Unity outside that restriction restored compilation and testing. No licence bypass was used.

## Device checks still required

No physical-phone gameplay results are claimed. Install the same updated APK on both phones and verify:

- Hosting in either direction, joining, disconnecting and rejoining over Wi-Fi.
- Simultaneous touch movement, camera control, jumping and ability use.
- Story objectives, guarded scroll collection, co-op waves, reviving and duels.
- Saved character choices and XP after restarting the app.
- Landscape layout, camera collision, sustained frame rate and thermal behaviour.

Desktop networking results do not replace these checks. Quest and wave state are not saved, and the host must remain foregrounded. This private LAN prototype trusts player movement and locally saved XP.

## Earlier APK

`Builds/Android/BrothersBlock.apk` is the older **0.1.0 (1)** exploration prototype built on 10 September 2026. It does not include elemental combat or the new modes. Its signature and alignment were verified then; those historical results do not validate the new APK.
