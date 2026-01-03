# Publishing Guide for Stats Mod

## Overview
To publish this mod as a client-side mod that players can download from Steam Workshop, you need to:

1. Build the mod in Release configuration
2. Prepare the mod files
3. Update mod.json to require clients
4. Upload to Steam Workshop
5. Configure servers to require the mod

## Step 1: Build the Mod

```powershell
dotnet build oomtm450PuckMod_Stats.sln --configuration Release
```

The output will be in: `Stats\bin\Release\oomtm450PuckMod_Stats.dll`

## Step 2: Prepare Mod Files

For Steam Workshop, you need:
- `oomtm450PuckMod_Stats.dll` (the compiled mod)
- `mod.json` (mod configuration)

### Update mod.json

Change `mod.json` to require clients:

```json
{
  "enabled": true,
  "clientRequired": true,
  "workshopId": 3561076649
}
```

**Note**: The `workshopId` should match your Steam Workshop item ID. If you're creating a new mod, leave this out initially and add it after publishing.

## Step 3: Create Workshop Item Structure

Create a folder structure like this:

```
WorkshopUpload/
├── oomtm450PuckMod_Stats.dll
└── mod.json
```

## Step 4: Upload to Steam Workshop

1. **Open Steam Workshop Tools**:
   - In Steam, go to Library → Tools
   - Find "Puck - Mod Uploader" or similar tool
   - Launch it

2. **Create/Update Workshop Item**:
   - If updating existing mod (ID: 3561076649), select it
   - If creating new mod, click "Create New Item"
   - Fill in:
     - **Title**: "Stats" (or your preferred name)
     - **Description**: Describe the mod features
     - **Visibility**: Public
     - **Tags**: Add relevant tags

3. **Upload Files**:
   - Select the folder containing `oomtm450PuckMod_Stats.dll` and `mod.json`
   - Upload the files
   - Steam will process and publish the mod

4. **Get Workshop ID**:
   - After publishing, note the Workshop ID from the URL
   - Update `mod.json` with the `workshopId` if creating new mod

## Step 5: Configure Server to Require Mod

On your server, update `server_configuration.json`:

```json
{
  "mods": [
    {
      "id": 3561076649,
      "enabled": true,
      "clientRequired": true
    }
  ]
}
```

This will:
- Auto-download the mod for players joining
- Require players to have the mod installed
- Kick players who don't have the mod (if `clientRequired: true`)

## Step 6: Test the Mod

1. **Test Locally**:
   - Install the mod from Workshop on a test client
   - Join a server with the mod enabled
   - Verify tooltip and stats work correctly

2. **Test Auto-Download**:
   - Have a player without the mod join the server
   - Verify Steam auto-downloads the mod
   - Verify the mod loads correctly

## Important Notes

### Version Management
- Update `MOD_VERSION` in `Stats.cs` when releasing new versions
- Add old versions to `OLD_MOD_VERSIONS` list for compatibility

### Client-Side Requirements
- The mod has both server-side and client-side code
- Client-side code handles:
  - UI tooltip display
  - Scoreboard modifications
  - Receiving stats from server
- Server-side code handles:
  - Stat tracking
  - Network communication
  - Data processing

### File Structure for Workshop
The Workshop upload should contain:
- **oomtm450PuckMod_Stats.dll** - The compiled mod (required)
- **mod.json** - Mod configuration (required)

Do NOT include:
- Source code (.cs files)
- Build artifacts (bin/, obj/ folders)
- libs/ folder (DLLs are embedded or referenced)

## Troubleshooting

### Mod Not Auto-Downloading
- Check `server_configuration.json` has correct Workshop ID
- Verify `clientRequired: true` in server config
- Check server logs for mod loading errors

### Clients Getting Kicked
- Ensure mod is published and public on Workshop
- Verify Workshop ID matches in both mod.json and server config
- Check client has mod installed (should auto-download)

### Mod Not Loading
- Verify DLL is built in Release configuration
- Check mod.json syntax is correct JSON
- Verify all dependencies are available (should be in Puck game files)

## Updating an Existing Mod

1. Build new version
2. Update `MOD_VERSION` in `Stats.cs`
3. Upload new DLL to Workshop
4. Update Workshop item description with changelog
5. Servers will automatically use the new version (clients auto-update)




