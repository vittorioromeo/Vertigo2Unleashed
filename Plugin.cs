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

        private static ConfigEntry<bool> _configGripHolsterModeEnabled;

        private static ConfigEntry<float> _configDamageMultiplierToEnemy;
        private static ConfigEntry<float> _configDamageMultiplierToPlayer;
        private static ConfigEntry<float> _configSpeedMultiplierPlayerWalk;
        private static ConfigEntry<float> _configSpeedMultiplierPlayerSwim;

        private static ConfigEntry<bool> _configVirtualStockEnabled;
        private static ConfigEntry<float> _configVirtualStockStrength;
        private static ConfigEntry<float> _configVirtualStockShoulderForward;
        private static ConfigEntry<float> _configVirtualStockShoulderRight;
        private static ConfigEntry<float> _configVirtualStockShoulderUp;
        private static ConfigEntry<float> _configVirtualStockForwardDepth;
        private static ConfigEntry<float> _configVirtualStockShoulderMaxDistance;

        private static ConfigEntry<bool> _configRevolver2HGripEnabled;

        private static ConfigEntry<bool> _configDualWieldingEnabled;
        private static ConfigEntry<bool> _configDualWieldingAllowClonedWeapons;

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
                "From the player's head position, sets how many units rightwards the shoulder is" +
                " (for left-handed players, this value should probably be negative)");

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
                0.4f,
                "Maximum distance between the dominant hand and shoulder to enable virtual stock aiming " +
                "(reverts to vanilla 2H aiming if exceeded)");

            _configRevolver2HGripEnabled = Config.Bind("General",
                "Revolver2HGripEnabled",
                true,
                "Enable two-handed aiming for the revolver (including virtual stock)");

            _configDualWieldingEnabled = Config.Bind("General",
                "DualWieldingEnabled",
                true,
                "Enable dual wielding (requires 'weapon switch' action bound to both hands in SteamVR)");

            _configDualWieldingAllowClonedWeapons = Config.Bind("General",
                "DualWieldingAllowClonedWeapons",
                true,
                "Allows dual wielding two clones of the same weapon");

            HarmonyFileLog.Enabled = true;
        }

        //
        //
        // ------------------------------------------------------------------------------------------------------------
        // AWAKE
        // ------------------------------------------------------------------------------------------------------------

#pragma warning disable IDE0051
        private void Awake()
#pragma warning restore IDE0051
        {
            _logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // --------------------------------------------------------------------------------------------------------
            // HARMONY PATCHES
            Harmony.CreateAndPatchAll(typeof(Plugin));
            _logger.LogInfo($"Injected all Harmony patches");

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

        private static object GetAndInvokePrivateMethod(object instance, string name, object[] args = null)
        {
            var methodInfo =
                instance.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.Assert(methodInfo != null);
            return methodInfo.Invoke(instance, args ?? new object[] { });
        }

        private static object GetPrivatePropertyValue(object instance, string name)
        {
            var propertyInfo =
                instance.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.Assert(propertyInfo != null);
            return propertyInfo.GetValue(instance);
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

        private static SteamVR_Input_Sources InputSourceDominant =>
            VertigoPlayer.instance.GetHand(GameManager.Hand_Dominant).inputSource;

        private static SteamVR_Input_Sources InputSourceNonDominant =>
            VertigoPlayer.instance.GetHand(GameManager.Hand_NonDominant).inputSource;

        private static SteamVR_Input_Sources OverridenInputSourceDominant => _configDualWieldingEnabled.Value
            ? _weaponSwitcherInputSourceOverride
            : InputSourceDominant;

        private static SteamVR_Input_Sources OverridenInputSourceNonDominant => _configDualWieldingEnabled.Value
            ? _weaponSwitcherInputSourceOtherHandOverride
            : InputSourceNonDominant;

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
            _weaponSwitcherInputSourceOverride = InputSourceDominant;
            _weaponSwitcherInputSourceOtherHandOverride = InputSourceNonDominant;
        }

        private static void SetInputSourceOverridesToNonDominant()
        {
            _weaponSwitcherInputSourceOverride = InputSourceNonDominant;
            _weaponSwitcherInputSourceOtherHandOverride = InputSourceDominant;
        }

        [HarmonyPatch(typeof(WeaponSwitcher), "Update")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool DualWieldingWeaponSwitchPatchPrefix(WeaponSwitcher __instance)
        {
            if (!_configDualWieldingEnabled.Value)
            {
                return true;
            }

            if (__instance.a_weaponSwitch.GetState(InputSourceDominant))
            {
                SetInputSourceOverridesToDominant();
            }

            if (__instance.a_weaponSwitch.GetState(InputSourceNonDominant))
            {
                SetInputSourceOverridesToNonDominant();
            }

            return true;
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

            var inputSource = OverridenInputSourceDominant;

            if (__instance.ActiveSlot != -1)
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
                __instance.manager.SwitchToEquippable(null, inputSource, false);

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

        // Would also be nice to patch `WeaponPickup.Pickup` to allow both hands to pick up weapons, but it's not that
        // important.

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
                _weaponSwitcherInputSourceOverride != InputSourceNonDominant)
            {
                return true;
            }

            __result = Array.Find(_clonedEquippableInstances, e => e.profile == prof);
            return false;
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
        // GRIP-HOLSTER MODE PATCHES
        // ------------------------------------------------------------------------------------------------------------

        private static EquippableProfile _oldEquippableDominant;
        private static EquippableProfile _oldEquippableNonDominant;

        [HarmonyPatch(typeof(WeaponSwitcher), "Update")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool GripHolsterPatchPrefix(WeaponSwitcher __instance, bool ___openAllowed)
        {
            if (!_configGripHolsterModeEnabled.Value || __instance.menuOpen || !___openAllowed)
            {
                return true;
            }

            SetInputSourceOverridesToDominant();
            doHand(GameManager.Hand_Dominant, ref _oldEquippableDominant);

            SetInputSourceOverridesToNonDominant();
            doHand(GameManager.Hand_NonDominant, ref _oldEquippableNonDominant);

            return true;

            void doHand(SteamVR_Input_Sources handType, ref EquippableProfile oldEquippable)
            {
                var hand = VertigoPlayer.instance.GetHand(handType);
                var grabGripActionState = hand.a_grab_grip.GetState(hand.inputSource);
                var grabGripActionLastState = hand.a_grab_grip.GetLastState(hand.inputSource);
                var handEquippable = __instance.manager.GetHand(handType);

                if (!grabGripActionState && handEquippable.currentProfile != null)
                {
                    oldEquippable = handEquippable.currentProfile;
                    __instance.manager.SwitchToEquippable(null, handType, false);
                    __instance.au.PlayOneShot(__instance.au_open);
                }

                if ((!grabGripActionLastState && grabGripActionState) // rising edge
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

            var equippablesManager = VertigoPlayer.instance.equippablesManager;
            var dominantHand = equippablesManager.GetHand(GameManager.Hand_Dominant);
            var nonDominantHand = equippablesManager.GetHand(GameManager.Hand_NonDominant);

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

            var hip = (InputSourceForBelt() == GameManager.Hand_NonDominant) ? ___pos_rightHip : ___pos_leftHip;
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

            var equippablesManager = VertigoPlayer.instance.equippablesManager;
            var dominantHand = equippablesManager.GetHand(GameManager.Hand_Dominant);
            var nonDominantHand = equippablesManager.GetHand(GameManager.Hand_NonDominant);

            if (dominantHand.currentProfile != null && nonDominantHand.currentProfile != null)
            {
                // Do not display the belt at all if both hands have a weapon equipped.

                __result = -1;
                return false;
            }

            equippable = equippablesManager.GetHand(InputSourceForBelt()).currentProfile;
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

                var dirWithoutVirtualStock = (attachedGrip.handgrabbing.handAnimator.CenterPosition_UnAnimated -
                                              __instance.transform.position).normalized;

                var headTransform = VertigoPlayer.instance.head.transform;

                var shoulderPos = headTransform.position +
                                  headTransform.forward.normalized * _configVirtualStockShoulderForward.Value +
                                  headTransform.right.normalized * _configVirtualStockShoulderRight.Value +
                                  headTransform.up.normalized * _configVirtualStockShoulderUp.Value;

                var shoulderInterpolationPos =
                    shoulderPos + headTransform.forward.normalized * _configVirtualStockForwardDepth.Value;

                var dirWithVirtualStock = (shoulderInterpolationPos - __instance.transform.position).normalized;

                var dominantHand = VertigoPlayer.instance.GetHand(GameManager.Hand_Dominant);

                if ((shoulderPos - dominantHand.transform.position).magnitude >
                    _configVirtualStockShoulderMaxDistance.Value)
                {
                    // Disable virtual stock
                    dirWithVirtualStock = dirWithoutVirtualStock;
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
                    __instance.Log("Set config value to " + args[0], false);
                    _configFile.Save();
                });
            }
        }
    }
}