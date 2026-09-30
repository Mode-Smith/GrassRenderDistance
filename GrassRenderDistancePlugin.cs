using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace GrassRenderDistance
{
    // [BepInPlugin] is how BepInEx discovers the mod: a unique GUID, a display name, and a version.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class GrassRenderDistancePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "mode-smith.valheim.grassrenderdistance";
        public const string PluginName = "Grass Render Distance";
        public const string PluginVersion = "1.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<float> GrassDistance;
        internal static ConfigEntry<bool> ScaleAllClutter;

        // The distance the game itself set before we changed it (40 in 1.0). Clutter fade distances are scaled
        // by GrassDistance / VanillaDistance.
        internal static float VanillaDistance = 40f;

        private Harmony _harmony;

        // Unity calls Awake once when BepInEx loads the plugin (at game startup, before any world exists).
        private void Awake()
        {
            Log = Logger;

            // Creates/reads BepInEx\config\mode-smith.valheim.grassrenderdistance.cfg
            GrassDistance = Config.Bind(
                "General",
                "GrassDistance",
                80f,
                new ConfigDescription(
                    "How far away (in meters) grass and other clutter is drawn. Vanilla is 40. " +
                    "Higher values cost more CPU/GPU - the number of grass patches grows with the square of this value.",
                    new AcceptableValueRange<float>(20f, 300f)));

            ScaleAllClutter = Config.Bind(
                "General",
                "ScaleAllClutter",
                true,
                "Also stretch the fade-out distance of every clutter type (small plants, saplings, flowers) by the same " +
                "factor as GrassDistance, so they don't pop in closer than the grass. Turn off to only change grass distance.");

            // Fire when a value is changed while the game is running (e.g. via the ConfigurationManager F1 menu).
            GrassDistance.SettingChanged += (_, _) => Apply(ClutterSystem.instance);
            ScaleAllClutter.SettingChanged += (_, _) => Apply(ClutterSystem.instance);

            // Applies every [HarmonyPatch] class in this DLL.
            _harmony = Harmony.CreateAndPatchAll(typeof(GrassRenderDistancePlugin).Assembly, PluginGuid);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static void Apply(ClutterSystem clutter)
        {
            // No clutter system exists in the main menu or on a dedicated server; nothing to do then.
            if (clutter == null)
                return;

            clutter.m_distance = GrassDistance.Value;

            float scale = ScaleAllClutter.Value ? GrassDistance.Value / VanillaDistance : 1f;
            ClutterScaler.Apply(clutter, scale);

            // Destroys all existing grass patches and flags a rebuild, so clutter regenerates with the new settings.
            clutter.ClearAll();

            Log.LogInfo($"Grass distance set to {clutter.m_distance} m (clutter fade scale x{scale:0.##})");
        }
    }

    // Scales the per-type fade distances on the clutter prefabs. Every grass patch is instantiated from these
    // prefabs, so after ClearAll() the regenerated patches pick up the new values.
    internal static class ClutterScaler
    {
        // Vanilla values, recorded the first time we see each prefab, so repeated changes always scale from vanilla
        // instead of compounding (80 -> 150 -> 60 must not multiply three times).
        private class RendererOriginal
        {
            public float Min;
            public float Max;
        }

        private static readonly Dictionary<InstanceRenderer, RendererOriginal> RendererOriginals = new Dictionary<InstanceRenderer, RendererOriginal>();
        private static readonly Dictionary<LODGroup, float[]> LodGroupOriginals = new Dictionary<LODGroup, float[]>();

        public static void Apply(ClutterSystem clutter, float scale)
        {
            // GenerateVegPatch clamps each renderer's max distance to this, so the fade start must not exceed it either.
            float maxAllowed = clutter.m_distance - clutter.m_grassPatchSize / 2f;

            foreach (ClutterSystem.Clutter type in clutter.m_clutter)
            {
                if (type.m_prefab == null)
                    continue;

                if (type.m_instanced)
                    ScaleInstanced(type, scale, maxAllowed);
                else
                    ScaleLodGroups(type, scale);
            }
        }

        // Instanced clutter (grass-like): InstanceRenderer draws all instances up to m_lodMinDistance, thins them
        // out until m_lodMaxDistance, and draws none beyond it.
        private static void ScaleInstanced(ClutterSystem.Clutter type, float scale, float maxAllowed)
        {
            InstanceRenderer renderer = type.m_prefab.GetComponent<InstanceRenderer>();
            if (renderer == null)
                return;

            if (!RendererOriginals.TryGetValue(renderer, out RendererOriginal original))
            {
                original = new RendererOriginal { Min = renderer.m_lodMinDistance, Max = renderer.m_lodMaxDistance };
                RendererOriginals[renderer] = original;
            }

            renderer.m_lodMaxDistance = original.Max * scale;
            renderer.m_lodMinDistance = Mathf.Min(original.Min * scale, Mathf.Min(renderer.m_lodMaxDistance, maxAllowed));

            GrassRenderDistancePlugin.Log.LogInfo(
                $"  [instanced] {type.m_name}: useLod={renderer.m_useLod}, fade {original.Min:0.#}-{original.Max:0.#} m -> " +
                $"{renderer.m_lodMinDistance:0.#}-{Mathf.Min(renderer.m_lodMaxDistance, maxAllowed):0.#} m");
        }

        // Non-instanced clutter is spawned as regular objects. A Unity LODGroup hides them once they shrink below a
        // screen-height threshold; dividing the thresholds by the scale keeps them visible proportionally farther away.
        private static void ScaleLodGroups(ClutterSystem.Clutter type, float scale)
        {
            LODGroup[] groups = type.m_prefab.GetComponentsInChildren<LODGroup>(true);
            if (groups.Length == 0)
            {
                GrassRenderDistancePlugin.Log.LogInfo($"  [object]    {type.m_name}: no LODGroup, visible to full grass distance");
                return;
            }

            foreach (LODGroup group in groups)
            {
                LOD[] lods = group.GetLODs();

                if (!LodGroupOriginals.TryGetValue(group, out float[] original))
                {
                    original = new float[lods.Length];
                    for (int i = 0; i < lods.Length; i++)
                        original[i] = lods[i].screenRelativeTransitionHeight;
                    LodGroupOriginals[group] = original;
                }

                for (int i = 0; i < lods.Length && i < original.Length; i++)
                    lods[i].screenRelativeTransitionHeight = original[i] / scale;
                group.SetLODs(lods);

                GrassRenderDistancePlugin.Log.LogInfo(
                    $"  [object]    {type.m_name}: LODGroup cull height {original[original.Length - 1]:0.###} -> " +
                    $"{lods[lods.Length - 1].screenRelativeTransitionHeight:0.###}");
            }
        }
    }

    // A Harmony "postfix" runs right after the original method. ClutterSystem.Awake runs whenever
    // the game creates the clutter system (each time you load into a world), so this applies our values then.
    [HarmonyPatch(typeof(ClutterSystem), "Awake")]
    internal static class ClutterSystemAwakePatch
    {
        private static void Postfix(ClutterSystem __instance)
        {
            GrassRenderDistancePlugin.VanillaDistance = __instance.m_distance;
            GrassRenderDistancePlugin.Log.LogInfo($"ClutterSystem created with vanilla distance {__instance.m_distance} m");
            GrassRenderDistancePlugin.Apply(__instance);
        }
    }
}
