using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace GrassRenderDistance
{
    // [BepInPlugin] is how BepInEx discovers the mod: a unique GUID, a display name, and a version.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class GrassRenderDistancePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "mode-smith.valheim.grassrenderdistance";
        public const string PluginName = "Grass Render Distance";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<float> GrassDistance;

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

            // Fires when the value is changed while the game is running (e.g. via the ConfigurationManager F1 menu).
            GrassDistance.SettingChanged += (_, _) => Apply(ClutterSystem.instance);

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

            // Destroys all existing grass patches and flags a rebuild, so grass regenerates at the new distance.
            clutter.ClearAll();

            Log.LogInfo($"Grass distance set to {clutter.m_distance} m");
        }
    }

    // A Harmony "postfix" runs right after the original method. ClutterSystem.Awake runs whenever
    // the game creates the clutter system (each time you load into a world), so this applies our value then.
    [HarmonyPatch(typeof(ClutterSystem), "Awake")]
    internal static class ClutterSystemAwakePatch
    {
        private static void Postfix(ClutterSystem __instance)
        {
            GrassRenderDistancePlugin.Log.LogInfo($"ClutterSystem created with vanilla distance {__instance.m_distance} m");
            GrassRenderDistancePlugin.Apply(__instance);
        }
    }
}
