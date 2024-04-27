using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Vertigo2.Player;
using Vertigo2;
using HarmonyLib.Tools;
using Valve.VR;
using Vertigo2.Weapons;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using System;
using System.Collections;
using Vertigo2.Interaction;

namespace Vertigo2Unleashed
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // GLOBALS
        // ------------------------------------------------------------------------------------------------------------

        private static ManualLogSource _logger;
        private static ConfigFile _configFile;

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // CONFIG ENTRIES
        // ------------------------------------------------------------------------------------------------------------

        //
        // Grip-holster mode
        private static ConfigEntry<bool> _configGripHolsterModeEnabled;
        private static ConfigEntry<bool> _configGripHolsterModeNoTrigger;

        //
        // Difficulty multipliers
        private static ConfigEntry<float> _configDamageMultiplierToEnemy;
        private static ConfigEntry<float> _configDamageMultiplierToPlayer;
        private static ConfigEntry<float> _configSpeedMultiplierPlayerWalk;
        private static ConfigEntry<float> _configSpeedMultiplierPlayerSwim;
        private static ConfigEntry<float> _configPlayerRecoilMultiplier;

        //
        // Virtual stock
        private static ConfigEntry<bool> _configVirtualStockEnabled;
        private static ConfigEntry<float> _configVirtualStockStrength;
        private static ConfigEntry<float> _configVirtualStockShoulderForward;
        private static ConfigEntry<float> _configVirtualStockShoulderRight;
        private static ConfigEntry<float> _configVirtualStockShoulderUp;
        private static ConfigEntry<float> _configVirtualStockForwardDepth;
        private static ConfigEntry<float> _configVirtualStockShoulderMaxDistance;

        //
        // Miscellaneous
        private static ConfigEntry<bool> _configRevolver2HGripEnabled;

        //
        // Dual wielding
        private static ConfigEntry<bool> _configDualWieldingEnabled;
        private static ConfigEntry<bool> _configDualWieldingAllowClonedWeapons;

        //
        // Melee attacks
        private static ConfigEntry<bool> _configMeleeEnabled;
        private static ConfigEntry<float> _configMeleeHandSphereCastRadius;
        private static ConfigEntry<float> _configMeleeMaxDistance;
        private static ConfigEntry<float> _configMeleeMinSpeed;
        private static ConfigEntry<float> _configMeleeMaxSpeed;
        private static ConfigEntry<float> _configMeleeMinDamage;
        private static ConfigEntry<float> _configMeleeMaxDamage;
        private static ConfigEntry<float> _configMeleeHitForceMultiplier;

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // CONSTRUCTOR
        // ------------------------------------------------------------------------------------------------------------

        public Plugin()
        {
            _logger = Logger;
            _configFile = Config;

            _configGripHolsterModeEnabled = Config.Bind("General",
                "GripHolsterModeEnabled",
                true,
                "Enable grip-holster mode (automatically holsters weapons when the grip is released," +
                " and equips the last holstered weapon when the grip is held)");

            _configGripHolsterModeNoTrigger = Config.Bind("General",
                "GripHolsterModeNoTrigger",
                true,
                "Prevents grip-holster mode from activating if the trigger button is held before the grip" +
                " button (useful to allow to curl hands into fists for hand-to-hand melee combat)");

            _configDamageMultiplierToEnemy = Config.Bind("General",
                "DamageMultiplierToEnemy",
                1.0f,
                "Multiplier for damage inflicted to enemies (1.0 is the default)");

            _configDamageMultiplierToPlayer = Config.Bind("General",
                "DamageMultiplierToPlayer",
                1.0f,
                "Multiplier for damage inflicted to the player (1.0 is the default)");

            _configSpeedMultiplierPlayerWalk = Config.Bind("General",
                "SpeedMultiplierPlayerWalk",
                1.0f,
                "Multiplier for player walk speed (m/s, smooth locomotion)");

            _configSpeedMultiplierPlayerSwim = Config.Bind("General",
                "SpeedMultiplierPlayerSwim",
                1.0f,
                "Multiplier for player swim speed (m/s, smooth locomotion)");

            _configPlayerRecoilMultiplier = Config.Bind("General",
                "PlayerRecoilMultiplier",
                1.0f,
                "Multiplier for player weapon recoil");

            _configVirtualStockEnabled = Config.Bind("General",
                "VirtualStockEnabled",
                true,
                "Enable virtual stock for two-handed weapons (interpolates two-handed aiming angle " +
                "with approximate shoulder position)");

            _configVirtualStockStrength = Config.Bind("General",
                "VirtualStockStrength",
                0.5f,
                "How strongly the shoulder position affects two-handed aiming " +
                "(0.0: only the off-hand matters, shoulder is ignored) " +
                "(1.0: only the shoulder matters, off-hand is ignored) " +
                "(in-between values are blended)");

            _configVirtualStockShoulderForward = Config.Bind("General",
                "VirtualStockShoulderForward",
                -0.1f,
                "From the player's head position, sets how many units forward the shoulder is");

            _configVirtualStockShoulderRight = Config.Bind("General",
                "VirtualStockShoulderRight",
                0.25f,
                "From the player's head position, sets how many units rightwards the right shoulder is");

            _configVirtualStockShoulderUp = Config.Bind("General",
                "VirtualStockShoulderUp",
                -0.1f,
                "From the player's head position, sets how many units upwards the shoulder is");

            _configVirtualStockForwardDepth = Config.Bind("General",
                "VirtualStockForwardDepth",
                2.75f,
                "Starting from the shoulder position, sets how many units forward the virtual stock " +
                "interpolation point is");

            _configVirtualStockShoulderMaxDistance = Config.Bind("General",
                "VirtualStockShoulderMaxDistance",
                0.45f,
                "Maximum distance between the dominant hand and shoulder to enable virtual stock aiming " +
                "(reverts to vanilla 2H aiming if exceeded)");

            _configRevolver2HGripEnabled = Config.Bind("General",
                "Revolver2HGripEnabled",
                true,
                "Enable two-handed aiming for the revolver (including virtual stock)");

            _configDualWieldingEnabled = Config.Bind("General",
                "DualWieldingEnabled",
                true,
                "Enable dual wielding (needs 'weapon switch' bound to both hands in SteamVR)");

            _configDualWieldingAllowClonedWeapons = Config.Bind("General",
                "DualWieldingAllowClonedWeapons",
                true,
                "Allows dual wielding two clones of the same weapon");

            _configMeleeEnabled = Config.Bind("General",
                "MeleeEnabled",
                true,
                "Enable universal melee attacks (hand-to-hand, held weapons)");

            _configMeleeHandSphereCastRadius = Config.Bind("General",
                "MeleeHandSphereCastRadius",
                0.15f,
                "Radius around player hand considered for hand-to-hand melee attacks");

            _configMeleeMaxDistance = Config.Bind("General",
                "MeleeMaxDistance",
                0.08f,
                "Maximum distance from the contact point for melee targets to register");

            _configMeleeMinSpeed = Config.Bind("General",
                "MeleeMinSpeed",
                2.0f,
                "Minimum speed required for a melee attack to register, also used for damage calculations");

            _configMeleeMaxSpeed = Config.Bind("General",
                "MeleeMaxSpeed",
                11.0f,
                "Speed upper bound used for melee damage calculations");

            _configMeleeMinDamage = Config.Bind("General",
                "MeleeMinDamage",
                2.5f,
                "Minimum damage dealt for a successful melee attack");

            _configMeleeMaxDamage = Config.Bind("General",
                "MeleeMaxDamage",
                22.5f,
                "Maximum damage dealt for a successful melee attack");

            _configMeleeHitForceMultiplier = Config.Bind("General",
                "MeleeHitForceMultiplier",
                1.5f,
                "Physics force multiplier for melee attacks (e.g. affects ragdolls from melee kills)");

            HarmonyFileLog.Enabled = true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // AWAKE
        // ------------------------------------------------------------------------------------------------------------

#pragma warning disable IDE0051
        // ReSharper disable once UnusedMember.Local
        private void Awake()
#pragma warning restore IDE0051
        {
            _logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // --------------------------------------------------------------------------------------------------------
            // HARMONY PATCHES
            Harmony.CreateAndPatchAll(typeof(Plugin));
            _logger.LogInfo("Injected all Harmony patches");

            // --------------------------------------------------------------------------------------------------------
            // DAMAGE MULTIPLIERS
            Enemy.ModifyDamage += (ref float damage) => { damage *= _configDamageMultiplierToEnemy.Value; };
            VertigoPlayer.ModifyDamage += (ref float damage) => { damage *= _configDamageMultiplierToPlayer.Value; };
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // UTILS
        // ------------------------------------------------------------------------------------------------------------

        private static object GetAndInvokePrivateMethod(object obj, string name, object[] args = null)
        {
            Debug.Assert(obj != null);

            var info = obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(info != null);

            return info.Invoke(obj, args ?? new object[] { });
        }

        private static object GetPrivatePropertyValue(object obj, string name)
        {
            Debug.Assert(obj != null);

            var info = obj.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(info != null);

            return info.GetValue(obj);
        }

        // ReSharper disable once UnusedMember.Local
        private static void SetPrivateFieldValue(object obj, string name, object value)
        {
            Debug.Assert(obj != null);

            var info = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(info != null);

            info.SetValue(obj, value);
        }

        private static object GetPrivateFieldValue(object obj, string name)
        {
            Debug.Assert(obj != null);

            var info = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(info != null);

            return info.GetValue(obj);
        }

        private static void RaisePrivateEvent<TEventArgs>(object obj, string eventName, TEventArgs eventArgs)
            where TEventArgs : EventArgs
        {
            Debug.Assert(obj != null);

            var eventField = (MulticastDelegate)GetPrivateFieldValue(obj, eventName);
            if (eventField == null)
            {
                return;
            }

            foreach (var handler in eventField.GetInvocationList())
            {
                handler.Method.Invoke(handler.Target, new[] { obj, eventArgs });
            }
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // RECOIL PATCHES
        // ------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(HeldEquippablePhysical), "Recoil")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool HeldEquippableRecoilPatchPrefix(HeldEquippablePhysical __instance, float recoilMul)
        {
            var d = (__instance.attachedGrip == null) ? 1f : __instance.attachedGrip.stabilizationMul;

            var normalized = Vector3.Lerp(-__instance.recoilOrigin.forward, UnityEngine.Random.insideUnitSphere,
                __instance.randomizeRecoil).normalized;

            var recoilMagic = (float)GetPrivateFieldValue(__instance, "recoilMagic");

            var recoilVec = normalized * __instance.recoilEnergy * recoilMagic * recoilMul *
                __instance.manager.recoilMultiplier / d;

            var task = (IEnumerator)GetAndInvokePrivateMethod(__instance, "SpreadRecoil",
                new object[] { recoilVec * _configPlayerRecoilMultiplier.Value, 3 });

            __instance.StartCoroutine(task);

            return false;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // DUAL WIELDING PATCHES
        // ------------------------------------------------------------------------------------------------------------

        // Basically, I dynamically the weapon selection wheel "input source" depending on what hand was used to open
        // the weapon selection wheel UI. By overriding `inputSource` and `inputSourceOtherHand` I am able to have a
        // per-hand fully functional weapon wheel.

        private static SteamVR_Input_Sources _weaponSwitcherInputSourceOverride;
        private static SteamVR_Input_Sources _weaponSwitcherInputSourceOtherHandOverride;

        private static SteamVR_Input_Sources VanillaInputSourceDominant =>
            VertigoPlayer.instance.GetHand(GameManager.Hand_Dominant).inputSource;

        private static SteamVR_Input_Sources VanillaInputSourceNonDominant =>
            VertigoPlayer.instance.GetHand(GameManager.Hand_NonDominant).inputSource;

        private static SteamVR_Input_Sources OverridenInputSourceDominant => _configDualWieldingEnabled.Value
            ? _weaponSwitcherInputSourceOverride
            : VanillaInputSourceDominant;

        private static SteamVR_Input_Sources OverridenInputSourceNonDominant => _configDualWieldingEnabled.Value
            ? _weaponSwitcherInputSourceOtherHandOverride
            : VanillaInputSourceNonDominant;

        [HarmonyPatch(typeof(WeaponSwitcher), "inputSource", MethodType.Getter)]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool WeaponSwitcherInputSourceGetterPatchPrefix(ref SteamVR_Input_Sources __result)
        {
            __result = OverridenInputSourceDominant;
            return false;
        }

        [HarmonyPatch(typeof(WeaponSwitcher), "inputSourceOtherHand", MethodType.Getter)]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool WeaponSwitcherInputSourceOtherHandGetterPatchPrefix(ref SteamVR_Input_Sources __result)
        {
            __result = OverridenInputSourceNonDominant;
            return false;
        }

        private static void SetInputSourceOverridesToDominant()
        {
            _weaponSwitcherInputSourceOverride = VanillaInputSourceDominant;
            _weaponSwitcherInputSourceOtherHandOverride = VanillaInputSourceNonDominant;
        }

        private static void SetInputSourceOverridesToNonDominant()
        {
            _weaponSwitcherInputSourceOverride = VanillaInputSourceNonDominant;
            _weaponSwitcherInputSourceOtherHandOverride = VanillaInputSourceDominant;
        }

        // Now I had to patch out the annoying behavior that holsters BOTH hands if the "hands" icon is selected in ANY
        // weapon selection wheel. The code below is mostly copy-pasted from the disassembly, and I just commented the
        // few annoying lines.

        [HarmonyPatch(typeof(WeaponSwitcher), "MenuStart")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool WeaponSwitcherMenuStartPatchPrefix(WeaponSwitcher __instance)
        {
            GetAndInvokePrivateMethod(__instance, "ResetPos");
            __instance.ResetIcons();
            GetAndInvokePrivateMethod(__instance, "CheckSecretWeapons");

            if (__instance.ActiveSlot != -1) // TODO: this is probably why unequipping fails
            {
                var equipInputSource =
                    ((EquippablesManager.EquippableInstance)GetPrivatePropertyValue(__instance, "activeEquippable"))
                    .eqip.inputSource;

                var task = (IEnumerator)GetAndInvokePrivateMethod(__instance, "IconToSpot", new object[]
                {
                    __instance.ActiveSlot,
                    equipInputSource
                });

                __instance.StartCoroutine(task);
                __instance.manager.SwitchToEquippable(null, OverridenInputSourceDominant, false);

                // Reset grip-holster mode history if hands are explicitly selected:
                (_lastOpenedWeaponSwitchMenu == LastOpenedWeaponSwitchMenu.Dominant
                    ? ref _oldEquippableDominant
                    : ref _oldEquippableNonDominant) = null;

                // Removed from original code:
                // __instance.manager.SwitchToEquippable(null, inputSourceOtherHand, true);
            }

            __instance.cursorPos = Vector2.zero;
            __instance.au.PlayOneShot(__instance.au_open);

            return false;
        }

        [HarmonyPatch(typeof(WeaponSwitcher), "SelectedItem")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool WeaponSwitcherSelectedItemPatchPrefix(WeaponSwitcher __instance, string item,
            bool ___openAllowed)
        {
            if (__instance.menuOpen || !___openAllowed)
            {
                return false;
            }

            var inputSource = OverridenInputSourceDominant;
            var inputSourceOtherHand = OverridenInputSourceNonDominant;

            if (item == "Hands")
            {
                __instance.manager.SwitchToEquippable(null, inputSource, false);

                // Reset grip-holster mode history if hands are explicitly selected:
                (_lastOpenedWeaponSwitchMenu == LastOpenedWeaponSwitchMenu.Dominant
                    ? ref _oldEquippableDominant
                    : ref _oldEquippableNonDominant) = null;

                // Removed from original code:
                // __instance.manager.SwitchToEquippable(null, __instance.inputSourceOtherHand, true);

                __instance.au.PlayOneShot(__instance.au_close);
            }
            else
            {
                var num = Array.IndexOf(__instance.slots, Array.Find(__instance.slots, s => s.name == item));

                var resIconToHand = (IEnumerator)GetAndInvokePrivateMethod(__instance, "IconToHand", new object[]
                {
                    num,
                    (num != -1 && __instance.slots[num].equippable.holdInOppositeHand)
                        ? inputSourceOtherHand
                        : inputSource
                });

                __instance.au.PlayOneShot(__instance.au_select);
                __instance.StartCoroutine(resIconToHand);
            }

            __instance.action_haptic.Execute(0f, 0.3f, 10f, 0.8f, inputSource);
            return false;
        }

        [HarmonyPatch(typeof(WeaponSwitcher), "IconToHand")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool WeaponSwitcherIconToHandPatchPrefix(int ID)
        {
            if (!_configDualWieldingEnabled.Value || ID == -1)
            {
                return true;
            }

            // Setting `paused` to true suppresses the weapon switch action performed at the end of the coroutine.
            // We are going to do it manually in the postfix.
            GameManager.paused = true;

            return true;
        }

        [HarmonyPatch(typeof(WeaponSwitcher), "IconToHand")]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void WeaponSwitcherIconToHandPatchPostfix(WeaponSwitcher __instance, int ID,
            SteamVR_Input_Sources hand)
        {
            if (!_configDualWieldingEnabled.Value || ID == -1)
            {
                return;
            }

            // The only difference here is setting `autoSwitchOtherHand` to `false`.

            __instance.manager.SwitchToEquippable(__instance.slots[ID].equippable, hand, false);
            if (__instance.slots[ID].otherHandEquippable != null)
            {
                var forHand = hand == SteamVR_Input_Sources.RightHand
                    ? SteamVR_Input_Sources.LeftHand
                    : SteamVR_Input_Sources.RightHand;
                __instance.manager.SwitchToEquippable(__instance.slots[ID].otherHandEquippable, forHand, false);
            }

            GameManager.paused = false;
        }

        [HarmonyPatch(typeof(WeaponPickup), "Pickup")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool WeaponPickupPickupPatchPrefix(WeaponPickup __instance)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return true;
            }

            // Mostly copied from the original, with a few tweaks.

            if (__instance.pickedUp || EquippablesManager.instance == null)
            {
                return false;
            }

            var interactable = (VertigoInteractable)GetPrivateFieldValue(__instance, "interactable");
            if (interactable == null)
            {
                return true;
            }

            // Tweak: input source depends on the interactable hand.
            var inputSource = interactable.mainHoldingHand.inputSource;

            // Tweak: override input sources before doing stuff with equippables. Actually not sure if this is needed.
            if (inputSource == GameManager.Hand_Dominant)
            {
                SetInputSourceOverridesToDominant();
            }
            else
            {
                SetInputSourceOverridesToNonDominant();
            }

            // Tweak: do not use `Disarm` here to set `autoSwitchOtherHand` to `false`.
            EquippablesManager.instance.SwitchToEquippable(null, inputSource, false);

            if (interactable.isBeingHeld)
            {
                interactable.ForceDrop();
            }

            if (__instance.unlockWeapon)
            {
                EquippablesManager.instance.PickupWeapon(__instance.weapon, inputSource);
            }
            else
            {
                EquippablesManager.instance.SwitchToEquippable(__instance.weapon, inputSource, false);
            }

            if (__instance.pickupSmoothTransform != null)
            {
                EquippablesManager.instance.PickupSmooth(inputSource, __instance.pickupSmoothTransform);
            }

            __instance.pickedUp = true;
            RaisePrivateEvent(__instance, "OnPickup", EventArgs.Empty);

            __instance.PickupUnityEvent.Invoke();

            var eqip = EquippablesManager.instance.GetHand(inputSource).currentEquippable.eqip;

            var weaponTutorialCoroutineResult =
                (IEnumerator)GetAndInvokePrivateMethod(__instance, "DoWeaponTutorial", new object[] { eqip });

            eqip.StartCoroutine(weaponTutorialCoroutineResult);

            if (__instance.saveGameWhenPickedUp)
            {
                var saveCoroutineResult = (IEnumerator)GetAndInvokePrivateMethod(__instance, "SaveCoroutine");
                __instance.StartCoroutine(saveCoroutineResult);
            }

            // Tweak: restore input sources before the end.
            SetInputSourceOverridesToDominant();

            return false;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // DUAL WIELDING CLONED WEAPONS PATCHES
        // ------------------------------------------------------------------------------------------------------------

        // To make it possible to dual wield weapon duplicates, I had to create a separate array to instantiate all the
        // weapon prefabs twice. I then inject this array in `FindInstanceForProfile` only when the weapon selection
        // wheel was opened from the non-dominant hand.

        private static EquippablesManager.EquippableInstance[] _clonedEquippableInstances;

        [HarmonyPatch(typeof(EquippablesManager), "Start")]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void DualWieldingEquippablesManagerPatchPostfix(EquippablesManager __instance,
            Collider ___playerHeadCollider)
        {
            _clonedEquippableInstances = new EquippablesManager.EquippableInstance[__instance.equippableCount];

            for (var i = 0; i < __instance.equippableCount; ++i)
            {
                if (__instance.allEquippables[i].prefab == null)
                {
                    continue;
                }

                _clonedEquippableInstances[i] = new EquippablesManager.EquippableInstance(
                    Instantiate(__instance.allEquippables[i].prefab, VertigoPlayer.instance.playArea),
                    __instance.allEquippables[i]);

                _clonedEquippableInstances[i].gameObject.SetActive(false);

                if (___playerHeadCollider == null)
                {
                    continue;
                }

                var componentsInChildren = _clonedEquippableInstances[i].gameObject
                    .GetComponentsInChildren<Collider>(true);

                foreach (var collider in componentsInChildren)
                {
                    Physics.IgnoreCollision(collider, ___playerHeadCollider);
                }
            }
        }

        [HarmonyPatch(typeof(EquippablesManager), "FindInstanceForProfile")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool DualWieldingEquippablesManagerInstancePatchPrefix(EquippableProfile prof,
            ref EquippablesManager.EquippableInstance __result)
        {
            if (!_configDualWieldingAllowClonedWeapons.Value ||
                _gripHolsterModeState == GripHolsterModeState.Dominant)
            {
                return true;
            }

            if (_gripHolsterModeState == GripHolsterModeState.NonDominant ||
                _weaponSwitcherInputSourceOverride == VanillaInputSourceNonDominant) // TODO: use last opened menu?
            {
                __result = Array.Find(_clonedEquippableInstances, e => e.profile == prof);
                return false;
            }

            return true;
        }

        private static bool _mustUpdateBelt; // When dual wielding, the belt must be updated whenever weapons change.

        [HarmonyPatch(typeof(EquippablesManager), "SwitchToEquippable")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool DualWieldingEquippablesManagerSwitchToEquippablePatchPrefix(EquippablesManager __instance,
            EquippableProfile profile,
            SteamVR_Input_Sources forHand,
            ref bool autoSwitchOtherHand)
        {
            _mustUpdateBelt = true;
            autoSwitchOtherHand = false;

            if (!_configDualWieldingAllowClonedWeapons.Value)
            {
                var oppositeForHand = forHand == GameManager.Hand_Dominant
                    ? GameManager.Hand_NonDominant
                    : GameManager.Hand_Dominant;

                if (__instance.GetHand(oppositeForHand).currentProfile == profile)
                {
                    __instance.SwitchToEquippable(null, oppositeForHand, false);
                }
            }

            return true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // GRIP-HOLSTER MODE & WEAPON SWITCH MENU PATCHES
        // ------------------------------------------------------------------------------------------------------------

        enum GripHolsterModeState
        {
            Dominant,
            NonDominant,
            Done
        }

        private static GripHolsterModeState _gripHolsterModeState = GripHolsterModeState.Done;
        private static EquippableProfile _oldEquippableDominant;
        private static EquippableProfile _oldEquippableNonDominant;

        enum LastOpenedWeaponSwitchMenu
        {
            Dominant,
            NonDominant
        }

        private static LastOpenedWeaponSwitchMenu _lastOpenedWeaponSwitchMenu = LastOpenedWeaponSwitchMenu.Dominant;

        [HarmonyPatch(typeof(GameManager), "LoadLevel")]
        [HarmonyPrefix]
        public static bool GameManagerLoadLevelPatchPrefix()
        {
            _gripHolsterModeState = GripHolsterModeState.Done;
            _oldEquippableDominant = null;
            _oldEquippableNonDominant = null;

            SetInputSourceOverridesToDominant();

            return true;
        }

        [HarmonyPatch(typeof(WeaponSwitcher), "Update")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool GripHolsterPatchPrefix(WeaponSwitcher __instance, bool ___openAllowed)
        {
            if (_configGripHolsterModeEnabled.Value && !__instance.menuOpen && ___openAllowed)
            {
                _gripHolsterModeState = GripHolsterModeState.Dominant;
                doHand(GameManager.Hand_Dominant, ref _oldEquippableDominant);
                _gripHolsterModeState = GripHolsterModeState.NonDominant;
                doHand(GameManager.Hand_NonDominant, ref _oldEquippableNonDominant);
                _gripHolsterModeState = GripHolsterModeState.Done;
            }

            if (_configDualWieldingEnabled.Value)
            {
                Debug.Assert(VanillaInputSourceDominant != VanillaInputSourceNonDominant);

                if (__instance.a_weaponSwitch.GetState(VanillaInputSourceDominant))
                {
                    SetInputSourceOverridesToDominant();
                    _lastOpenedWeaponSwitchMenu = LastOpenedWeaponSwitchMenu.Dominant;
                }
                else if (__instance.a_weaponSwitch.GetState(VanillaInputSourceNonDominant))
                {
                    SetInputSourceOverridesToNonDominant();
                    _lastOpenedWeaponSwitchMenu = LastOpenedWeaponSwitchMenu.NonDominant;
                }
            }
            else
            {
                SetInputSourceOverridesToDominant();
            }

            return true;

            void doHand(SteamVR_Input_Sources handType, ref EquippableProfile oldEquippable)
            {
                var hand = VertigoPlayer.instance.GetHand(handType);
                var triggerActionState = SteamVR_Actions.default_Fire.GetState(hand.inputSource);
                var grabGripActionState = hand.a_grab_grip.GetState(hand.inputSource);
                var grabGripActionLastState = hand.a_grab_grip.GetLastState(hand.inputSource);
                var handEquippable = __instance.manager.GetHand(handType);

                if (!grabGripActionState && handEquippable.currentProfile != null)
                {
                    oldEquippable = handEquippable.currentProfile;
                    __instance.manager.SwitchToEquippable(null, handType, false);
                    __instance.au.PlayOneShot(__instance.au_open);
                }

                if ((!_configGripHolsterModeNoTrigger.Value || !triggerActionState) // trigger not pressed (fist pose)
                    && (!grabGripActionLastState && grabGripActionState) // rising edge
                    && oldEquippable != null
                    && handEquippable.currentProfile == null
                    && hand.hoveringInteractable == null
                    && hand.attachedInteractable == null)
                {
                    __instance.manager.SwitchToEquippable(oldEquippable, handType, false);
                    __instance.au.PlayOneShot(__instance.au_select);
                }
            }
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // DUAL WIELDING AMMO BELT PATCHES
        // ------------------------------------------------------------------------------------------------------------

        private static SteamVR_Input_Sources InputSourceForBelt()
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return GameManager.Hand_Dominant;
            }

            var dominantHand = EquippablesManager.instance.GetHand(GameManager.Hand_Dominant);
            var nonDominantHand = EquippablesManager.instance.GetHand(GameManager.Hand_NonDominant);

            // The only "interesting" case for dual wielding is when the only hand that's holding a weapon is the
            // non-dominant one.

            if (dominantHand.currentProfile == null && nonDominantHand.currentProfile != null)
            {
                return GameManager.Hand_NonDominant;
            }

            return GameManager.Hand_Dominant;
        }

        [HarmonyPatch(typeof(AmmoBelt), "UpdateHandedness")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool AmmoBeltUpdateHandednessPatchPrefix(Transform ___pos_rightHip,
            Transform ___pos_leftHip, Transform ___uiRoot)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return true;
            }

            var hip = InputSourceForBelt() == GameManager.Hand_NonDominant ? ___pos_rightHip : ___pos_leftHip;
            ___uiRoot.transform.position = hip.position;
            ___uiRoot.transform.rotation = hip.rotation;

            return false;
        }

        [HarmonyPatch(typeof(AmmoBelt), "GetAmmoIndexForEquippable")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool AmmoBeltGetAmmoIndexForEquippablePatchPrefix(ref int __result,
            ref EquippableProfile equippable)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return true;
            }

            var dominantHand = EquippablesManager.instance.GetHand(GameManager.Hand_Dominant);
            var nonDominantHand = EquippablesManager.instance.GetHand(GameManager.Hand_NonDominant);

            if (dominantHand.currentProfile != null && nonDominantHand.currentProfile != null)
            {
                // Do not display the belt at all if both hands have a weapon equipped.

                __result = -1;
                return false;
            }

            equippable = EquippablesManager.instance.GetHand(InputSourceForBelt()).currentProfile;
            return true;
        }

        [HarmonyPatch(typeof(AmmoBelt), "Update")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool AmmoBeltUpdatePatchPrefix(AmmoBelt __instance)
        {
            if (_mustUpdateBelt)
            {
                _mustUpdateBelt = false;
                __instance.UpdateBelt();
            }

            return true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // SPEED MULTIPLIER PATCHES
        // ------------------------------------------------------------------------------------------------------------

        // For some reason I cannot patch `VertigoCharacterController.UpdateVelocity` directly, but
        // this function seems to be called close to the beginning of `UpdateVelocity`.
        [HarmonyPatch(typeof(VertigoCharacterController), "UpdateVelocityBuffer")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool SpeedMultiplierPlayerPatchPrefix(VertigoCharacterController __instance, object[] __args)
        {
            _ = __args;

            // `3.0f` seems to be the hardcoded speed
            __instance.walkSpeed = 3.0f * _configSpeedMultiplierPlayerWalk.Value;
            __instance.swimSpeed = 3.0f * _configSpeedMultiplierPlayerSwim.Value;

            return true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // VIRTUAL STOCK PATCHES
        // ------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(HeldEquippablePhysical), "FixedUpdate")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool VirtualStockPatchPrefix(HeldEquippablePhysical __instance,
            Vector3 ___localHeldOrigin, Vector3 ___gunUpVector)
        {
            if (!_configVirtualStockEnabled.Value)
            {
                return true;
            }

            // The code below is mostly copy-pasted from Vertigo 2's disassembled DLL because I couldn't find a way
            // to cleanly inject the virtual stock calculations in the middle of `HeldEquippablePhysical.FixedUpdate`.

            if (Time.timeScale == 0f)
            {
                return false;
            }

            var num = 1f;

            if (EquippablesManager.instance != null)
            {
                num *= EquippablesManager.instance.pickupFactor * EquippablesManager.instance.pickupFactor;
            }

            var a = __instance.controller.TransformPoint(___localHeldOrigin);

            var quaternion = __instance.controller.rotation *
                             Quaternion.Euler(GameManager.options.gameplay_gunAngle * 4f, 0f, 0f);

            var attachedGrip = __instance.attachedGrip;

            if (attachedGrip != null && attachedGrip.isForegrip)
            {
                var transform = attachedGrip.interactable.MainAttachPoint.transform;
                var normalized =
                    __instance.transform.InverseTransformPoint(transform.transform.position).normalized;

                //
                // ----------------------------------------------------------------------------------------------------
                // CUSTOM CODE START

                var grabbingOffHandPos = attachedGrip.handgrabbing.handAnimator.CenterPosition_UnAnimated;
                var grabbingMainHand = attachedGrip.handgrabbing.otherHand;
                var grabbingMainHandPos = grabbingMainHand.transform.position;
                var dirWithoutVirtualStock = (grabbingOffHandPos - __instance.transform.position).normalized;

                var headTransform = VertigoPlayer.instance.head.transform;

                Vector3 dirWithVirtualStock = dirWithoutVirtualStock;

                if (_configVirtualStockEnabled.Value && __instance.equippable is not QuadBow)
                {
                    var interpolationForwardDepth =
                        headTransform.forward.normalized * _configVirtualStockForwardDepth.Value;

                    var rightShoulderOffset =
                        headTransform.forward.normalized * _configVirtualStockShoulderForward.Value +
                        headTransform.right.normalized * _configVirtualStockShoulderRight.Value +
                        headTransform.up.normalized * _configVirtualStockShoulderUp.Value;

                    var rightShoulderPos = headTransform.position + rightShoulderOffset;
                    var rightShoulderInterpolationPos = rightShoulderPos + interpolationForwardDepth;

                    var rightShoulderEligible = grabbingMainHand == VertigoPlayer.instance.RHand &&
                                                (rightShoulderPos - grabbingMainHandPos).magnitude <=
                                                _configVirtualStockShoulderMaxDistance.Value;

                    var leftShoulderOffset =
                        headTransform.forward.normalized * _configVirtualStockShoulderForward.Value +
                        -headTransform.right.normalized * _configVirtualStockShoulderRight.Value +
                        headTransform.up.normalized * _configVirtualStockShoulderUp.Value;

                    var leftShoulderPos = headTransform.position + leftShoulderOffset;
                    var leftShoulderInterpolationPos = leftShoulderPos + interpolationForwardDepth;

                    var leftShoulderEligible = grabbingMainHand == VertigoPlayer.instance.LHand &&
                                               (leftShoulderPos - grabbingMainHandPos).magnitude <=
                                               _configVirtualStockShoulderMaxDistance.Value;

                    if (rightShoulderEligible)
                    {
                        dirWithVirtualStock =
                            (rightShoulderInterpolationPos - __instance.transform.position).normalized;
                    }
                    else if (leftShoulderEligible)
                    {
                        dirWithVirtualStock =
                            (leftShoulderInterpolationPos - __instance.transform.position).normalized;
                    }
                }

                var dirFinal = Vector3.Lerp(dirWithoutVirtualStock, dirWithVirtualStock,
                    _configVirtualStockStrength.Value).normalized;

                var quaternion2 = Math3d.LookRotationQuatExtended(dirFinal,
                    __instance.controller.TransformDirection(___gunUpVector), normalized,
                    ___gunUpVector);

                // CUSTOM CODE END
                // ----------------------------------------------------------------------------------------------------
                //

                var num2 = Mathf.InverseLerp(200f, 0f, Quaternion.Angle(quaternion2,
                    __instance.controller.rotation));

                num2 = Mathf.Clamp01(num2);
                quaternion = Quaternion.Slerp(quaternion, quaternion2, Mathf.Pow(num2, 0.3f));
            }

            var quaternion3 = quaternion * Quaternion.Inverse(__instance.transform.rotation);
            var a2 = Vector3.zero;

            quaternion3.ToAngleAxis(out var num3, out var vector);

            if (num3 > 180f)
            {
                num3 -= 360f;
            }

            if (num3 != 0f && !float.IsNaN(vector.x) && !float.IsInfinity(vector.x))
            {
                a2 = num3 * vector;
            }

            __instance.rigidbody.angularVelocity +=
                a2 * Time.fixedDeltaTime * 35f * num * __instance.attachTorqueMultiplier;

            __instance.rigidbody.angularVelocity -= Vector3.ClampMagnitude(
                __instance.rigidbody.angularVelocity * Time.fixedDeltaTime * 50f * num,
                __instance.rigidbody.angularVelocity.magnitude * 0.9f);

            var a3 = a - __instance.heldOrigin.position;

            __instance.rigidbody.AddForceAtPosition(800f * a3 * num * __instance.attachForceMultiplier,
                __instance.heldOrigin.position, ForceMode.Acceleration);

            var pointVelocity = __instance.rigidbody.GetPointVelocity(__instance.heldOrigin.position);

            __instance.rigidbody.AddForceAtPosition(
                Vector3.ClampMagnitude(-30f * pointVelocity * num,
                    pointVelocity.magnitude / Time.fixedDeltaTime * 0.8f), __instance.heldOrigin.position,
                ForceMode.Acceleration);

            return false;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // 2H AIMING PATCHES
        // ------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(Revolver), "GunUpdate")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool RevolverTwoHandedPatchPrefix(Revolver __instance)
        {
            if (_configRevolver2HGripEnabled.Value)
            {
                ((HeldEquippablePhysical)__instance.heldEquippable).secondHandGrips[1].isForegrip = true;
            }

            return true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // TRIDENT DUAL WIELDING PATCHES
        // ------------------------------------------------------------------------------------------------------------

        private static readonly TridentRifle.RifleModes[] TridentModePerHand =
        {
            TridentRifle.RifleModes.Fast,
            TridentRifle.RifleModes.Fast
        };

        [HarmonyPatch(typeof(TridentRifle), "OnEnable")]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void TridentOnEnableDualWieldingPatchPostfix(TridentRifle __instance)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return;
            }

            var handIndex = GetHandIndex(__instance.inputSource);
            var isModeFast = TridentModePerHand[handIndex] == TridentRifle.RifleModes.Fast;

            __instance.slide.restPos = isModeFast ? 1f : 0f;
            __instance.slide.position = isModeFast ? 1f : 0f;
        }

        [HarmonyPatch(typeof(TridentRifle), "GunUpdate")]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void TridentGunUpdateDualWieldingPatchPostfix(TridentRifle __instance)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return;
            }

            var handIndex = GetHandIndex(__instance.inputSource);

            TridentModePerHand[handIndex] = __instance.slide.position > 0.5f
                ? TridentRifle.RifleModes.Fast
                : TridentRifle.RifleModes.Slow;

            __instance.slide.restPos = TridentModePerHand[handIndex] == TridentRifle.RifleModes.Fast ? 1f : 0f;
        }

        [HarmonyPatch(typeof(TridentRifle), "Firing")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool TridentGunUpdateFiringPatchPrefix(TridentRifle __instance)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return true;
            }

            TridentRifle.mode = TridentModePerHand[GetHandIndex(__instance.inputSource)];
            return true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // CONSOLE PATCHES
        // ------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(ConsoleController), "Init")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool ConsoleInitPatchPrefix(ConsoleController __instance)
        {
            registerSetConfigCommand("v2u_set_dmgmult_to_enemy", _configDamageMultiplierToEnemy);
            registerSetConfigCommand("v2u_set_dmgmult_to_player", _configDamageMultiplierToPlayer);

            registerSetConfigCommand("v2u_set_spdmult_player_walk", _configSpeedMultiplierPlayerWalk);
            registerSetConfigCommand("v2u_set_spdmult_player_swim", _configSpeedMultiplierPlayerSwim);

            registerSetConfigCommand("v2u_set_vstock_strength", _configVirtualStockStrength);
            registerSetConfigCommand("v2u_set_vstock_shoulder_forward", _configVirtualStockShoulderForward);
            registerSetConfigCommand("v2u_set_vstock_shoulder_right", _configVirtualStockShoulderRight);
            registerSetConfigCommand("v2u_set_vstock_shoulder_up", _configVirtualStockShoulderUp);
            registerSetConfigCommand("v2u_set_vstock_forward_depth", _configVirtualStockForwardDepth);
            registerSetConfigCommand("v2u_set_vstock_shoulder_max_distance", _configVirtualStockShoulderMaxDistance);

            registerSetConfigCommand("v2u_set_Melee_spherecast_radius", _configMeleeHandSphereCastRadius);
            registerSetConfigCommand("v2u_set_Melee_max_distance", _configMeleeMaxDistance);
            registerSetConfigCommand("v2u_set_Melee_min_speed", _configMeleeMinSpeed);
            registerSetConfigCommand("v2u_set_Melee_max_speed", _configMeleeMaxSpeed);
            registerSetConfigCommand("v2u_set_Melee_min_damage", _configMeleeMinDamage);
            registerSetConfigCommand("v2u_set_Melee_max_damage", _configMeleeMaxDamage);
            registerSetConfigCommand("v2u_set_Melee_hit_force_multiplier", _configMeleeHitForceMultiplier);

            return true;

            void registerCommand(string command, CommandHandler handler)
            {
                GetAndInvokePrivateMethod(__instance,
                    "registerCommand", new object[] { command, handler, "Vertigo 2 Unleashed command" });
            }

            void registerSetConfigCommand(string command, ConfigEntry<float> configEntry)
            {
                registerCommand(command, args =>
                {
                    configEntry.Value = float.Parse(args[0]);
                    __instance.Log("Set config value to " + args[0]);
                    _configFile.Save();
                });
            }
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // UNIVERSAL MELEE
        // ------------------------------------------------------------------------------------------------------------

        private static readonly float[] MeleeCooldownPerHand = { 0.0f, 0.0f };

        private enum MeleeResult
        {
            Nothing,
            HitSomething,
            HitEnemy
        }

        private static MeleeResult DoMelee(VertigoHand hand, float hitDistance, Collider hitCollider, Vector3 hitPoint,
            Vector3 hitNormal, Vector3 velocity, Vector3 hitDir)
        {
            if (hitDistance > _configMeleeMaxDistance.Value)
            {
                return MeleeResult.Nothing;
            }

            var vertigoHittable = VertigoHittable.GetByCollider(hitCollider);

            if (vertigoHittable == null)
            {
                return MeleeResult.Nothing;
            }

            var vertigoEntity = vertigoHittable.GetLinkedEntity();

            if (vertigoEntity == null ||
                vertigoEntity is VertigoPlayer ||
                vertigoEntity == hand.otherHand ||
                hitCollider.GetComponentInParent<HeldEquippablePhysical>() != null ||
                (hand.attachedInteractable != null &&
                 vertigoEntity.gameObject == hand.attachedInteractable.gameObject) ||
                (hand.otherHand.attachedInteractable != null &&
                 vertigoEntity.gameObject == hand.otherHand.attachedInteractable.gameObject))
            {
                return MeleeResult.Nothing;
            }

            var damageScale = Mathf.Clamp01(Mathf.InverseLerp(_configMeleeMinSpeed.Value,
                _configMeleeMaxSpeed.Value, velocity.magnitude));

            var hitDamage =
                Mathf.Lerp(_configMeleeMinDamage.Value, _configMeleeMaxDamage.Value, damageScale);

            vertigoHittable.Hit(
                new HitInfo(hitDamage, velocity.magnitude * _configMeleeHitForceMultiplier.Value,
                    hitPoint, hitDir, hitNormal,
                    VertigoPlayer.instance, DamageType.Impact));

            if (hitCollider.gameObject.layer == 8 && vertigoEntity is Enemy enemy)
            {
                BulletHitAudioManager.HitSuccess(enemy.GetEnemyType(), hitPoint);
                return MeleeResult.HitEnemy;
            }

            BulletHitAudioManager.HitSurface(hitCollider.sharedMaterial, hitPoint, 1.0f);
            return MeleeResult.HitSomething;
        }

        private static VertigoHand GetHandFromInputSource(SteamVR_Input_Sources inputSource)
        {
            return inputSource == VertigoPlayer.instance.RHand.inputSource
                ? VertigoPlayer.instance.RHand
                : VertigoPlayer.instance.LHand;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static int GetHandIndex(VertigoHand hand)
        {
            return hand == VertigoPlayer.instance.LHand ? 0 : 1;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static int GetHandIndex(SteamVR_Input_Sources hand)
        {
            return GetHandIndex(GetHandFromInputSource(hand));
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class MeleeCollisionComponent : MonoBehaviour
        {
            public HeldEquippablePhysical Parent;

#pragma warning disable IDE0051
            // ReSharper disable once UnusedMember.Local
            private void OnCollisionEnter(Collision other)
#pragma warning restore IDE0051
            {
                var hand = GetHandFromInputSource(Parent.inputSource);
                Debug.Assert(hand != null);

                var gunVelocity = Parent.rigidbody.velocity;

                ref var cooldown = ref MeleeCooldownPerHand[GetHandIndex(hand)];
                if (cooldown > 0.0f || gunVelocity.magnitude < _configMeleeMinSpeed.Value)
                {
                    return;
                }

                Debug.Assert(other != null);
                Debug.Assert(other.contactCount > 0);
                Debug.Assert(other.collider != null);

                var contact = other.contacts[0];

                var meleeResult = DoMelee(hand, contact.separation, other.collider, contact.point, contact.normal,
                    gunVelocity, gunVelocity.normalized);

                if (meleeResult != MeleeResult.Nothing)
                {
                    cooldown = 0.2f;
                }
            }
        }

        [HarmonyPatch(typeof(HeldEquippablePhysical), "FixedUpdate")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool VertigoWeaponMeleePatchPrefix(HeldEquippablePhysical __instance)
        {
            if (!_configMeleeEnabled.Value)
            {
                return true;
            }

            if (__instance.gameObject.GetComponent<MeleeCollisionComponent>() == null)
            {
                __instance.gameObject.AddComponent<MeleeCollisionComponent>().Parent = __instance;
                __instance.rigidbody.detectCollisions = true;
            }

            return true;
        }

        [HarmonyPatch(typeof(VertigoCharacterController), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool VertigoPlayerCharacterControllerMeleeCooldownPatchPrefix()
        {
            if (!_configMeleeEnabled.Value)
            {
                return true;
            }

            foreach (var handIndex in new[] { 0, 1 })
            {
                ref var cooldown = ref MeleeCooldownPerHand[handIndex];
                cooldown -= Time.deltaTime;

                if (cooldown <= 0.0f)
                {
                    cooldown = 0.0f;
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(VertigoHand), "FixedUpdate")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool VertigoHandMeleePatchPrefix(VertigoHand __instance)
        {
            if (!_configMeleeEnabled.Value)
            {
                return true;
            }

            ref var cooldown = ref MeleeCooldownPerHand[GetHandIndex(__instance)];
            if (cooldown > 0.0f || __instance.velocity.magnitude < _configMeleeMinSpeed.Value)
            {
                return true;
            }

            var results = Physics.SphereCastAll(
                __instance.transform.position,
                _configMeleeHandSphereCastRadius.Value,
                __instance.transform.forward,
                _configMeleeMaxDistance.Value,
                Physics.AllLayers);

            foreach (var hit in results)
            {
                var meleeResult = DoMelee(__instance, hit.distance, hit.collider, hit.point, hit.normal,
                    __instance.velocity, -__instance.velocity.normalized);

                if (meleeResult != MeleeResult.Nothing)
                {
                    cooldown = 0.2f;
                }
            }

            return true;
        }
    }
}
