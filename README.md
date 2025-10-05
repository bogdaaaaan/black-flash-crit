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
