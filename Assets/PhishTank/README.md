# Phish Tank 🐟

A seated, hands-first mixed reality game for Meta Quest.
Suspicious emails swim around your real room as fish. **Grab one, read it, and throw it into SAFE or SCAM.**
Each sort teaches you one real clue for spotting phishing, and your aquarium grows as you get better.

- **Competition:** Meta VR Start Developer Competition 2026, **Gaming** track, **New Experience** division
- **Deadline:** Nov 18, 2026, 12:00 PM PT (2:00 PM Chicago)
- **Engine:** Unity 6 + Meta XR All-in-One SDK (v81+ / v207)

---

## 1. The game

**Core loop (one round lasts about 2–3 minutes):**
1. Fish swim in, either above your real table (passthrough) or in front of you.
2. **Grab** a fish with your hand, and its email card opens above it.
3. **Inspect** it: the sender name vs. the real address, where the link really goes, the attachment type.
4. **Throw** it into the green **SAFE** inbox or the red **SCAM** net.
5. Feedback: "Caught it! x3 combo" or "Oops", plus the clue and a one-sentence lesson.
6. Clear the level, then move to the next one. You get 3 lives.

**Levels:** Tide Pool → Reef → Kelp Forest → Open Sea → The Deep (more fish, faster fish, trickier scams).

**Reason to come back:** total correct sorts unlock aquarium decorations (coral, shells, castle). There's also a daily streak and your best score.

**The scam clues players learn:** urgency, password or 2FA requests, lookalike domains (`parce1point`), link text that doesn't match the real URL, reading domains from the right (`bank.com.secure-login.net`), dangerous attachments (`.docm`, `.pdf.exe`), gift card requests, and prizes that are too good to be true.

**What's in here:** 24 emails (12 scams, 12 safe) across 3 difficulty levels. Every company is **fictional**, because the rules ban real brand names. Add more emails in `Data/emails.json`.

---

## 2. Files

```
Assets/PhishTank/
  Data/emails.json          — all email content (edit freely)
  Scripts/
    EmailData.cs            — email format
    EmailDatabase.cs        — loads JSON, gives each level a shuffled batch
    EmailFish.cs            — swimming, grab/release, card
    EmailCard.cs            — the email popup above a held fish
    SortBin.cs              — SAFE / SCAM trigger zones
    SpawnArea.cs            — where fish swim (box in front of you)
    MrukSpawnArea.cs        — optional: fish swim above your REAL table (MRUK)
    LevelConfig.cs          — level settings (ScriptableObject)
    FishSpawner.cs          — spawns fish for a level
    GameManager.cs          — score, combo, lives, levels, judging
    FeedbackPanel.cs        — "Caught it!" + lesson
    HUD.cs                  — score / hearts / combo
    ProgressStore.cs        — saved progress + daily streak
    AquariumGrowth.cs       — decorations unlock over time
    PauseHandler.cs         — clean pause when the headset comes off
```

---

## 3. Setup in Unity (step by step)

### A. Project
1. Unity Hub → **New project** → Unity 6 → **Universal 3D** template → name it `PhishTank`.
2. **File → Build Profiles** → select **Android** → **Switch Platform**.
3. **Window → Package Manager** → *Unity Registry* or *Asset Store* → install **Meta XR All-in-One SDK**.
   (Optional for the table feature: **Meta MR Utility Kit**.)
4. When Meta's **Project Setup Tool** pops up, click **Fix All** (and **Apply All**). This sets OpenXR and Android settings for Quest.
5. Copy the `Assets/PhishTank` folder from this zip into your project's `Assets` folder.

### B. Scene
1. Make a new scene, `Main`, and delete the default Main Camera.
2. **Meta → Tools → Building Blocks** (the Meta menu at the top of Unity), then drag in:
   - **Camera Rig**
   - **Passthrough** (your real room as the background)
   - **Hand Tracking** (or **Interaction Rig**, which adds hands plus grab and poke)
   - *(optional)* **MR Utility Kit**, for the real-table feature
3. Create these empty objects:
   - `GameManager` → add **GameManager**, **EmailDatabase**, **FishSpawner**, **SpawnArea**, **PauseHandler**
     - EmailDatabase → drag `Data/emails.json` into **Emails Json**
   - Add an **AudioSource** to GameManager for sound effects (find free "pop", "ding" and "buzz" sounds).

### C. The fish prefab
1. Make an object `Fish` with a simple fish shape. To start, use a stretched **Sphere** (scale 0.08, 0.05, 0.12) plus a small cube tail. You can swap in a nicer model later.
2. Add a **Rigidbody** (no gravity; the script handles it) and a **Collider**.
3. Add **EmailFish**.
4. Add grabbing: the quickest way is the **Grabbable Item** Building Block. You can also right-click the object → **Interaction SDK → Add Grab Interaction**, and keep **throwing enabled**.
5. Add an **Interactable Unity Event Wrapper** (from the Interaction SDK) and set its *Interactable View* to the grab interactable, then:
   - **When Select** → `EmailFish.OnGrabbed`
   - **When Unselect** → `EmailFish.OnReleased`
6. Add a child **Canvas** (Render Mode: **World Space**, scale 0.0005). Give it a background panel and 5 TextMeshPro texts: From, Subject, Body, Link, Attachment. Add **EmailCard**, connect the texts, and connect the card to EmailFish.
7. Drag `Fish` into the Project window to make it a **prefab**, delete it from the scene, and assign it to **FishSpawner → Fish Prefab**. Assign **SpawnArea** too.

### D. Bins
1. Make two objects about 0.35 m wide, at waist or table height and about 0.5 m out to the left and right:
   - **SAFE**: a green mailbox or inbox shape, then add a **Box Collider (Is Trigger)** and **SortBin** (Is Scam Bin = off)
   - **SCAM**: a red net, then a trigger collider and **SortBin** (Is Scam Bin = **on**)
2. Put big, clear labels on them ("SAFE" / "SCAM").

### E. UI
- **FeedbackPanel**: a World Space canvas about 0.7 m in front of you and slightly above eye level, with a CanvasGroup and 3 texts (title, clues, explanation).
- **HUD**: a small canvas in the lower left showing score, hearts, combo and level.
- **Start menu**: a canvas with the title, plus a **Poke button** (Building Block: **Poke Button**). Its **When Select** → `GameManager.StartGame`.
- **Level complete menu**: a "Next level" poke button → `GameManager.NextLevel`.
- **Game over menu**: shows the score, with a "Play again" button → `GameManager.StartGame` and a "Menu" button → `GameManager.BackToMenu`.
- Connect all of these in the **GameManager** inspector.

### F. Levels
In the Project window, right-click → **Create → PhishTank → Level Config**, and make 5 of them:

| # | Name | Fish | Max difficulty | Scam ratio | Speed |
|---|------|------|----------------|-----------|-------|
| 1 | Tide Pool   | 5 | 1 | 0.6 | 0.8 |
| 2 | Reef        | 6 | 1 | 0.5 | 1.0 |
| 3 | Kelp Forest | 7 | 2 | 0.5 | 1.1 |
| 4 | Open Sea    | 8 | 2 | 0.5 | 1.3 |
| 5 | The Deep    | 8 | 3 | 0.5 | 1.4 |

Drag them, in order, into **GameManager → Levels**.

### G. Optional: fish swim over your real table (MRUK)
1. Install **Meta MR Utility Kit** and add the **MR Utility Kit** Building Block.
2. **Project Settings → Player → Android → Scripting Define Symbols** → add `PHISHTANK_MRUK`.
3. Add **MrukSpawnArea** to GameManager and assign SpawnArea.
   If there's no table in the room scan, it falls back to the box in front of you automatically.

### H. Aquarium (progress)
Make an `Aquarium` object on the table or next to the bins, with disabled child decorations (coral, shell, castle...). Add **AquariumGrowth** and list them with unlock counts, for example 5, 15, 30, 60 and 100.

### I. Test
- **No headset:** use the **Meta XR Simulator** (Meta → Meta XR Simulator → Activate) and press Play.
- **With a Quest:** turn on Developer Mode in the Meta Horizon phone app, connect with USB-C, then **Build Profiles → Run Device → your Quest → Build And Run**.
- Watch the frame rate: it must stay at **60 fps or more**. Keep models simple and the materials unlit or simple lit.

---

## 4. Day-by-day plan

| Day | Date | Goal |
|-----|------|------|
| 1 | Mon Nov 9  | Install Unity and the SDK, set up the project, Camera Rig + Passthrough + Hands working |
| 2 | Tue Nov 10 | Fish prefab: grab, throw, email card shows |
| 3 | Wed Nov 11 | Bins + GameManager: first full playable round 🎉 |
| 4 | Thu Nov 12 | Menus, HUD, feedback panel, 5 levels |
| 5 | Fri Nov 13 | Sounds, pop effects, nicer fish, aquarium growth |
| 6 | Sat Nov 14 | MRUK table placement, test on headset, fix bugs |
| 7 | Sun Nov 15 | Polish the first 5 minutes (clear onboarding), performance at 60 fps or more |
| 8 | Mon Nov 16 | **Feature freeze.** Upload the APK to the "Competition" release channel, record the video |
| 9 | Tue Nov 17 | Edit the video (under 3 min), write the Devpost text, **submit a day early** |
| — | Wed Nov 18 | Deadline at 2:00 PM Chicago, as a backup only |

**Rule for the rest of the build:** if a feature isn't done by Day 7, cut it. A small polished game beats a big buggy one.

---

## 5. Submission checklist
- [ ] Meta VR Start membership approved (check your email)
- [ ] App created in the Meta developer dashboard under team "Aiperi Moldobaeva"
- [ ] APK uploaded to a release channel named exactly **Competition**, plus an **Invite URL** created
- [ ] Demo video **under 3 min** on YouTube (public), showing real gameplay with your best moment first
- [ ] Devpost: name, tagline (140 chars), **Gaming** track, **New Experience** division, description, how hand interactions work, target launch date
- [ ] No real brands or real people anywhere in the game
