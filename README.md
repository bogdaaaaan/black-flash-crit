# Black Flash Mod (v1.5.0)

## Description

This mod adds a **Black Flash effect from Jujutsu Kaisen** to critical hits.  
Whenever you land a crit, a short Black Flash animation **with custom sound** plays for extra impact.

---

## What It Does

- Spawns **custom sprites** on enemies whenever you land a critical hit.  
- Lets you control the game’s crit mechanics:
  - Set a custom crit chance (0–100%).
  - Set a custom crit damage multiplier.
  - Allow every crest to crit (not just Wanderer), and optionally skip vanilla checks.
- Adds an optional **dynamic crit scaling system** — your crit chance can increase or decrease after each critical hit.
- Adds an option to use **canon Black Flash damage** (damage raised to the power of 2.5).
- Supports **custom PNG images** for the effect; scale them or disable visuals entirely.
- Supports **custom sound effects** for critical hits, with options to mute default sounds or randomize pitch.

---

## Compatibility

This mod does **not replace default textures**, so it works fine with custom skins or other visual mods that change hit effects.  
If you prefer your own visuals, you can replace the included texture files manually.

> ⚙️ The included textures were created using AI since I’m not an artist.  
> Feel free to replace the `.png` files in the `images` folder or share your own art in the Posts section.  
>  
> You can also replace the sounds in the `sounds` folder (`.mp3`, `.wav`, `.ogg` supported).

---

## Configuration

You can configure the mod in two ways:
1. **In-game:** Use **BepInEx Configuration Manager** (press **F1** by default).  
2. **Manually:** Edit the config file directly:  
   `BepInEx/config/bodyando.silksong.blackflash.cfg`

---

## Installation

1. Install **BepInEx (v5)**.  
2. Run the game once to generate the `plugins` folder.  
3. Extract the mod folder into:  
   `Steam\steamapps\common\Hollow Knight Silksong\BepInEx\plugins\`
4. *(Optional)* Install **BepInEx Configuration Manager** for in-game settings.

---

## Config Setup Guide

All settings are located in:  
`BepInEx/config/bodyando.silksong.blackflash.cfg`  
Defaults are safe and ready to use.

### General
- **Enable Mod:** Master on/off switch.  
- **Every Crest Can Crit:** If true, any crest can trigger crits; if false, only Wanderer can.  
- **Skip Crit Checks:** If true, bypass vanilla checks; if false, use vanilla rules (e.g., not maggotted and 9+ silk).  

### Crit
- **Custom Crit Chance:** Sets base crit chance (0.0–1.0).  
- **Crit Damage Multiplier:** Multiplies critical hit damage (0.0–10).  
- **Canon Black Flash Damage:** Calculates crit damage as `baseDamage^2.5` (overrides multiplier).  
- **Enable Silk On Crit:** If true, regain silk on critical hits.  
- **Silk Per Crit:** Amount of silk restored per critical hit.  

### Crit Build Up
- **Crit Build Up Enabled:** Enables per-hit crit scaling.  
- **Build Up Increase:** If true, each crit increases next crit chance; if false, decreases it.  
- **Percent Per Hit:** How much crit chance changes per hit (0.0–1.0).  
- **Reset Seconds:** Time (seconds) after last hit before crit resets to base.  

### Visual
- **Display Crit Overlay:** Show or hide the custom Black Flash sprites.  
- **Crit Overlay Scale:** Adjust effect size (0.1–2.0).  

### Audio
- **Enable Crit Sounds:** Play random custom sound on critical hit.  
- **Mute Default Critical Sound:** Disable vanilla crit sound.  
- **Crit Sound Volume:** Volume (0.0–1.0).  
- **Crit Sound Pitch Min / Max:** Random pitch range per crit.  

### Advanced (Hidden)
- **CritBurstMinFrames:** Minimum frames between animation bursts (default 5).  
- **CritBurstMaxFrames:** Maximum frames between bursts (default 10).  
- **Max Simultaneous Voices:** Number of overlapping crit sounds allowed.  

---

## Additional Info
- Tested on **Silksong v1.0.28650**  
- May require updates if future patches change crit or audio behavior  
- Feedback and suggestions are always welcome  
- Work in progress — new features will be added regularly  

---

## Troubleshooting

### The mod doesn’t load at all
- Ensure **BepInEx** is properly installed and functional.  
- Verify the folder `BlackFlashCrit` is in `BepInEx/plugins/`.  
- Check that `BlackFlashCrit` contains the `.dll`, `images` folder, and `sounds` folder.  
- Review `BepInEx/LogOutput.log` for mod-related errors.  

### The game crashes on startup
- Make sure you’re using **BepInEx v5.x (x64)**.  
- Conflicts may occur with other mods that modify crit mechanics — try disabling them.  

### Critical hits feel unchanged
- Double-check mod placement.  
- Some combat mods can override crit logic — disable them temporarily.  
- Use **F1** (Config Manager) to verify if the mod and visual display are enabled.  

### Still having issues?
Post your problem in the **Posts/Comments** section of the mod page.  
Include:
- Silksong version  
- BepInEx version  
- `LogOutput.log` file contents  

---
