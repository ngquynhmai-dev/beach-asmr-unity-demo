# Beach ASMR — Unity demo

Unity **2022.3.62f2**. Full source project and artwork for the beach scenes and 13-step lounge chair cleaning level.

## Open the project

1. Clone this repository.
2. In Unity Hub, add the repository folder and open with Unity 2022.3.62f2.
3. Wait for Unity to restore packages and import assets.
4. Open `Assets/MAI_ROOM BÃI BIỂN/MAI_CHAIR_CLEANING_13.unity` and press Play.

Beach entry scenes: `MAI_ROOM_BAI_BIEN.unity` and `MAI_ROOM_BAI_BIEN_DEMO14.unity` in the same folder.

## Developer handoff

- Level code, shaders, artwork and detailed notes: `Assets/MAI_ROOM BÃI BIỂN/ChairLevel/`.
- Validation: in Play Mode, use **MAI > Chair Level > Validate 13-step playthrough**.
- Review screenshots and latest playthrough report: `ChairLevel/Review/`.
- The chair level includes marine animal rescue, seaweed removal, sand vacuuming, wet/soap states, persistent brush swirls and accumulating foam, rinsing, drying, and leather repair.
- Foam follows cushion lighting and has a high-resolution smooth edge mask.
- Generated Unity caches, local editor preferences, IDE output, and signing keys are excluded by `.gitignore`.
- All required Unity assets and their `.meta` files are included. No Git LFS setup is required.
