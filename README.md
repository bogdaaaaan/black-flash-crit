# Black Flash Crit

This mod adds a **Black Flash effect** from *Jujutsu Kaisen* to **critical hits** in *Hollow Knight: Silksong*.  
Whenever you land a crit, a short Black Flash animation plays for extra impact.

## Features

- Change **critical hit chance**
- Adjust **damage multiplier**
- Toggle **crits for all crests**
- Works with **custom skins** and other visual mods
- Option to replace textures with your own

The mod does **not replace any default textures**, so it works fine with any skin or visual mod that changes hit effects.  
If you want to use your own visuals for the Black Flash, replace the included texture files manually.  

 I'm not an artist — the included Black Flash textures were created using **AI-generated art**.  
 If you prefer a different look, feel free to replace the `black_flash_#.png` files inside the mod folder with your own.

To change settings, you can edit the config file inside `BepInEx/config` or use the **BepInEx Configuration Manager** to adjust everything in-game (`F1` by default).

---

## Installation

1. Install **BepInEx (v5)**.  
2. Run the game once to create the `plugins` folder.  
3. Extract the folder from the archive and move it to:
   \Steam\steamapps\common\Hollow Knight Silksong\BepInEx\plugins\
4. (Optional) Install **BepInEx Configuration Manager** for easy config access in-game.

---

# Black Flash Mod – Config Guide

Location of config file
- BepInEx/config/bodyando.silksong.blackflash.cfg

General
- EnableMod (bool, default: true)
  - Master on/off switch for the entire mod (visual overlay + crit logic changes).
  - Turn OFF to restore fully vanilla behavior.

- EveryCrestCanCrit (bool, default: false)
  - When ON: any crest/tool can roll for a critical hit (subject to silk/luck gates if enabled).
  - When OFF: only the Wanderer crest path can roll crits (vanilla).
  - Interaction:
    - If “CritVanillaSilkGate” is ON, vanilla silk/maggot requirements still apply even with this ON.

Visual
- CritOverlayScale (float, 0.1–2.0, default: 1.0)
  - Scales the size of the crit overlay sprites.
  - Visual only; does not affect damage or chance.

Crit
- CustomCritChance (float, 0.0–1.0, default: 0.15)
  - Base critical hit chance used by the game.
  - Final crit roll in game code uses: EffectiveChance × LuckModifier (Luck comes from vanilla systems like Lucky Dice).
  - Example: 0.20 means 20% before luck.

- CritDamageMultiplier (float, 0–10, default: 3.0)
  - How much damage a critical hit deals relative to a normal hit.
  - Example: 3.0 means criticals deal 3× damage.
  - Visual overlay is unaffected.

- CritVanillaSilkGate (bool, default: false)
  - When ON: restore vanilla gating for crits (requires being free from maggots and having at least 9 silk).
  - Applies even if “EveryCrestCanCrit” is ON (i.e., vanilla silk gate still enforced).
  - When OFF: the mod ignores the silk/maggot gate so your CustomCritChance applies consistently.

Ramping Crit (optional streak mechanic)
- CritRampingEnabled (bool, default: false)
  - When ON: your current crit chance ramps up after each successful hit, and slowly resets after inactivity.

- CritRampingIncreasePercent (float, 0.0–1.0, default: 0.05)
  - Multiplicative increase applied to your current crit chance after each successful hit.
  - Formula after N hits: CurrentChance = BaseChance × (1 + IncreasePercent)^N, clamped to max 1.0.
  - Example: Base 0.20 and Increase 0.10 → 1st hit 0.22, 2nd 0.242, 3rd 0.2662, etc.

- CritRampingDecaySeconds (float, 0.5–60, default: 10.0)
  - Time since your last successful hit after which the ramped chance resets back to BaseChance.
  - A “successful hit” means a hero-to-enemy hit that lands (not self-damage).

Notes and tips
- Lucky Dice: The mod does not change luck. Vanilla “luck” (including Lucky Dice) multiplies your effective chance as usual.
- First-hit behavior: The mod applies your settings on hero load and keeps them synced so crit rolls use the latest values immediately.
- Balance suggestions:
  - If you enable ramping, consider slightly lower BaseChance to avoid always hitting 100% during long combos.
  - Very high CritDamageMultiplier (e.g., >4) can trivialize encounters; adjust to taste.

---

## Troubleshooting

### Mod doesn’t load
- Make sure BepInEx is properly installed and working.  
- Verify that the folder **`BlackFlashCrit`** is inside:
  \BepInEx\plugins\
- Check that the folder contains the `.dll` file and three `black_flash_#.png` files.  
- Look at `BepInEx/LogOutput.log` for any related errors.

### Game crashes on startup
- Ensure you’re using **BepInEx v5.x (x64)**.  
- Conflicts may occur if another mod changes crit chance or damage multiplier behavior.  
  Try disabling other mods to isolate the issue.

### Effect doesn’t work or looks unchanged
- Double-check that the mod is in the correct folder.  
- Some mods that alter combat mechanics may override these values.  
  Disable other mods that affect critical hits and test again.  
- Check if the mod is enabled by pressing **F1** in-game (only works if Configuration Manager is installed).

### Still having issues?
Post your problem in the **Posts/Comments** section of the mod page.  
Include your Silksong version, BepInEx version, and a copy of the `LogOutput.log` file to help diagnose the issue.

---

## Notes

- Tested on **Silksong version v1.0.28650**
- Works with **custom skins**
- May need updates if future patches change crit behavior
- Feedback and suggestions are always welcome

---
