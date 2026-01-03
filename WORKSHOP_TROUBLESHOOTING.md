# Workshop Mod Troubleshooting Guide

## Common Issue: Mod Works Locally But Not on Workshop

### Problem
The mod works when manually installed but fails when downloaded from Steam Workshop.

### Root Causes

#### 1. **Workshop Folder Structure** (Most Common)
When uploading to Workshop, the folder structure must be:
```
WorkshopUpload/
├── oomtm450PuckMod_Stats.dll
└── mod.json
```

**Critical:** Both files must be in the **root** of the upload folder, not in subfolders.

#### 2. **Missing DLL Dependencies**
The mod references DLLs with `<Private>false</Private>`, meaning they're not copied with the mod. These DLLs must be available in the game's installation directory at:
```
Puck_Data/Managed/
```

**DLLs that might be missing:**
- `0Harmony.dll` - Harmony library for patching
- `MonoMod.*.dll` - MonoMod libraries
- Other custom DLLs from `libs/` folder

### Solutions

#### Solution 1: Verify Workshop Upload Structure
1. Create a clean folder for Workshop upload:
   ```
   WorkshopUpload/
   ├── oomtm450PuckMod_Stats.dll  (from Stats\bin\Release\)
   └── mod.json                    (from Stats\)
   ```

2. **Do NOT** include:
   - `libs/` folder
   - `bin/` or `obj/` folders
   - Source code files
   - Subfolders

3. Upload the **entire folder** to Workshop (Steam will extract it)

#### Solution 2: Check Game Logs for Missing DLLs
1. Check the game's log file for errors like:
   - `FileNotFoundException`
   - `Could not load file or assembly`
   - `Dependency resolution failed`

2. Common log locations:
   - Windows: `%APPDATA%\..\LocalLow\Puck\Puck\Player.log`
   - Or check Unity console output

#### Solution 3: Verify DLLs Are in Game Installation
Check if these DLLs exist in `Puck_Data\Managed\`:
- `0Harmony.dll`
- `MonoMod.RuntimeDetour.dll`
- `MonoMod.Utils.dll`
- `Newtonsoft.Json.dll`

If they're missing, they need to be included in the Workshop upload OR the mod needs to embed them.

#### Solution 4: Embed Critical DLLs (If Needed)
If certain DLLs aren't available in the game installation, you can embed them:

1. Modify `Stats.csproj` to set `<Private>true</Private>` for critical DLLs:
   ```xml
   <Reference Include="0Harmony">
     <HintPath>libs\0Harmony.dll</HintPath>
     <Private>true</Private>  <!-- Changed from false -->
   </Reference>
   ```

2. Include these DLLs in the Workshop upload alongside the mod DLL.

**Note:** Only do this for DLLs that are NOT part of the game installation.

### Verification Steps

1. **Test Workshop Download Locally:**
   - Unsubscribe from the mod
   - Delete local mod files
   - Subscribe to the mod via Workshop
   - Check if files are downloaded correctly
   - Verify `mod.json` is in the correct location

2. **Check File Structure After Workshop Download:**
   Workshop mods are typically downloaded to:
   ```
   Steam\steamapps\workshop\content\[GAME_ID]\[WORKSHOP_ID]\
   ```
   
   Verify the structure matches what you uploaded.

3. **Check Server/Client Logs:**
   - Server logs: `journalctl -u PuckServer.service -f`
   - Client logs: Check Unity console or log files
   - Look for mod loading errors

### Quick Fix Checklist

- [ ] `mod.json` is in the root of Workshop upload folder
- [ ] `oomtm450PuckMod_Stats.dll` is in the root of Workshop upload folder
- [ ] No subfolders in Workshop upload
- [ ] `mod.json` has correct `workshopId: 3608968085`
- [ ] `mod.json` has `clientRequired: true`
- [ ] DLL is built in Release configuration
- [ ] Check game logs for missing DLL errors
- [ ] Verify Workshop download location structure

### If Still Not Working

1. **Check Workshop Item Status:**
   - Ensure mod is published (not just saved as draft)
   - Check visibility settings (should be Public)
   - Verify Workshop ID matches in both `mod.json` and `server_configuration.json`

2. **Test with Minimal Mod:**
   - Create a test mod with just basic functionality
   - Verify it works on Workshop
   - Gradually add features to identify the breaking point

3. **Contact Game Developers:**
   - Check if there are specific Workshop requirements
   - Verify if certain DLLs need to be included
   - Check for known Workshop issues











































