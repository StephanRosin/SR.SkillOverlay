# SR.SkillOverlay

SR.SkillOverlay is a Valheim addon that displays an on-screen overlay showing all of your skills that are above a specified threshold (default: 15). This makes it easy to keep track of your character's strongest abilities at a glance.

## Features

* 🎯 **Skill Overlay**: Shows skills and their levels when they exceed the threshold.
* ⚙️ **Configurable Threshold**: Default threshold is 15; can be changed via config file.
* 🛠️ **Lightweight**: Minimal performance impact and easy to install.

## Requirements

* **Valheim** (latest stable version)
* **BepInEx** (5.4.21 or later)
* **Unity Mod Manager** (optional, if preferred)

## Installation

1. Download and install [BepInEx](https://github.com/BepInEx/BepInEx) for Valheim, if you haven’t already.

2. Download the DLL from [here](https://github.com/StephanRosin/SR.SkillOverlay/releases/tag/release).

3. (Optional) Customize settings by editing `BepInEx/config/SR.SkillOverlay.cfg`.

4. **Install the mod**
   - Place the `SR.SkillOverlay.dll` file into your Valheim `BepInEx/plugins` folder:
     ```
     <Valheim Folder>/BepInEx/plugins/
     ```

4. **Configure hotkeys (optional)**
   - After the first launch, a config file will be generated at:
     ```
     <Valheim Folder>/BepInEx/config/SR.SkillOverlay.cfg
     ```
   You can modify the following settings:

    ```ini
    ## Skill Overlay Settings
    
    ## The minimum skill level threshold to display (default: 15)
    B:ShowThreshold = 15
    
    ## Screen position of the overlay (X and Y offset in pixels)
    I:OverlayOffsetX = 10
    I:OverlayOffsetY = 10
    
    ## Enable or disable the overlay by default
    B:Enabled = true
    ```
    
    After editing the config, save and restart Valheim.

5. Launch Valheim — the skill overlay will appear automatically when skills exceed the threshold.

## Usage

* The overlay displays at the top-left corner of the screen by default.
* It lists each skill name alongside its current level.
* Only skills with levels **greater than** the configured threshold are shown.

