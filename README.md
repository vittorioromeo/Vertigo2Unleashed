# V2U | Vertigo 2 - Unleashed

> **Mod for the excellent [Vertigo 2](https://store.steampowered.com/app/843390/Vertigo_2/), providing more freedom to the player.**

<div align="center">
  <a href="https://www.youtube.com/watch?v=AqFB48q26Yk"><img src="https://img.youtube.com/vi/AqFB48q26Yk/0.jpg" alt="Vertigo 2 - Unleashed | Release Trailer"></a>
  <p><a href="https://www.youtube.com/watch?v=AqFB48q26Yk">Vertigo 2 - Unleashed | Release Trailer</a></p>
</div>

## New Features

* **Universal Melee**
    - ➡️ | Allows physical melee attacks with any weapon, or even with empty hands.
    - 🛠️ | Can be toggled and tweaked in `Vertigo2Unleashed.cfg`.

<p></p>

* **Dual Wielding**
    - ➡️ | Allows each hand to independently equip and operate a weapon.
    - ⚠️ | Requires the following actions to be bound on *both* controllers in the SteamVR bindings menu:
        - `"Weapon Switch"`, `"Reload"`.
    - ⚠️ | It is recommended to rebind `"Toggle Menu"` to be a long press or chord to not interfere with left hand `"Reload"`.
    - ⚠️ | It is also recommended to rebind `"Teleport"` from the left joystick, as that is the most natural bind for weapon selection.
    - 🛈 | Obtaining a weapon makes it usable by both hands. E.g., as soon as you collect the Trident SMG, you'll be able to dual wield two Trident SMGs.
        - If you prefer to not have duplicate weapons, you can set `DualWieldingAllowClonedWeapons` to `false` in `Vertigo2Unleashed.cfg`.

<p></p>

* **Virtual Stock**
    - ➡️ | Interpolates two-handed aiming with the player's approximate shoulder position, giving more stability when aiming down sights.
    - 🛠️ | Strength, shoulder position, and max distance can be tweaked in `Vertigo2Unleashed.cfg` or the non-VR in-game console (F1).

<p></p>

* **Grip-Holster Mode**
    - ➡️ | Automatically holsters weapons when the grip is released, and equips the last holstered weapon when the grip is held.
    - 🛈 | Any hand interaction (e.g. grabbing a prop, using the wrist storage) takes priority over equipping the last holstered weapon.
    - 🛈 | This is the recommended setting when dual wielding, as it simplifies the reloading process for two weapons a lot.
    - 🛈 | Helps to quickly interact with the world (e.g. collect an item, climb a ladder) without having to open the weapon selection menu.
    - 🛠️ | Can be toggled  in `Vertigo2Unleashed.cfg`.

<p></p>

* **Difficulty Tweaks**
    - ➡️ | Allows fine-tuning of damage and movement speed.
    - 🛈 | Both inflicted and taken damage can be tuned, allowing a more deadly and intense (i.e. no bullet sponges) yet fair experience.
    - 🛈 | Player walking and swimming speed can also be tuned.
    - 🛠️ | All values can be tweaked in `Vertigo2Unleashed.cfg` or the non-VR in-game console (F1).
 
<p></p>

* **Miscellaneous**
    - ➡️ | Holding the revolver by its foregrip now uses two-handed aiming (including virtual stock support).
 
## Installation

1. Obtain and install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases).
    - Basically, just extract the `.zip` in your Vertigo 2 folder (usually `C:\Program Files (x86)\Steam\steamapps\common\Vertigo 2\`).
    - Your Vertigo 2 folder should now contain a `BepInEx` subfolder and a `winhttp.dll`, among many other files.

<p></p>

2. Obtain the latest release of V2U [from the "Releases" page](https://github.com/vittorioromeo/Vertigo2Unleashed/releases).

<p></p>

3. Extract all the files on top of your existing Vertigo 2 installation.
    - Again, usually `C:\Program Files (x86)\Steam\steamapps\common\Vertigo 2\`.
    - The `BepInEx/plugins` subfolder should now contain `Vertigo2Unleashed.dll`.

<p></p>

4. Run Vertigo 2 as usual from Steam.

<p></p>

5. ❗ **Make sure to change your SteamVR bindings to support the new features**, and to tweak any settings. ❗
    - You can find a *"Vertigo 2 - Unleashed | Oculus Touch"* binding shared with the SteamVR community.
    - Alternatively, the `steam.app.843390_oculus_touch.json` file in this repository can be imported.

## Removal

1. Simply delete `Vertigo2Unleashed.dll`.

## Known Issues

- The Trident SMG fire mode switch is shared with the SMG duplicate weapon when dual-wielding. This needs to be fixed by Zulubo :)

## Support The Project

- Buy me a coffee:
    - https://ko-fi.com/vittorioromeo
    - https://paypal.me/romeovittorio
    - https://github.com/sponsors/vittorioromeo
    - https://patreon.com/vittorioromeo

## Other Projects

- [HL2VRU | Half-Life 2: VR Mod - Unleashed](https://github.com/vittorioromeo/HL2VRU)
    - *"Unofficial fork of the excellent HL2VR mod, implementing unique VR-only interactions and providing more freedom to the player."*

<p></p>

- [Quake VR](https://vittorioromeo.com/quakevr)
    - *"The timeless classic from 1996, reimagined for virtual reality."*

<p></p>

- [Open Hexagon](https://store.steampowered.com/app/1358090/Open_Hexagon/)
    - *"Four buttons, one goal: survive. Are you ready for a real challenge?"*
