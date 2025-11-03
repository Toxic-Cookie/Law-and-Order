using Verse;
using HarmonyLib;
using HugsLib;
using HugsLib.Utils;
using Law_and_Order.Source.Settings;

namespace Law_and_Order.Source
{
    public class Mod : ModBase
    {
        public override string ModIdentifier => "Law_and_Order";

        // Static instance for easy access to logger from anywhere in your mod
        public static Mod Instance { get; private set; }

        // Easy access to logger
        public static ModLogger Log => Instance?.Logger;

        public Mod()
        {
            Instance = this;
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();

            // Initialize settings
            LawAndOrderSettings.Initialize(Settings);

            // Validate settings after initialization
            LawAndOrderSettings.ValidateSettings();

            // Example: Log when mod is loaded
            Logger.Message("Law and Order mod loaded successfully!");
        }

        /// <summary>
        /// Override to show custom settings window
        /// </summary>
        public override void SettingsChanged()
        {
            base.SettingsChanged();

            // Validate settings when user changes them
            LawAndOrderSettings.ValidateSettings();
        }

        /// <summary>
        /// Called when a world/save is loaded
        /// </summary>
        public override void WorldLoaded()
        {
            base.WorldLoaded();

            // Validate settings when a save game is loaded
            LawAndOrderSettings.ValidateSettings();

            // Note: Alerts are automatically discovered by RimWorld
            // The Alert_ContrabandHypocrisy class will be instantiated automatically
        }
    }
}
