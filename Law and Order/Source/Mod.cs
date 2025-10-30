using Verse;
using HarmonyLib;
using HugsLib;
using HugsLib.Utils;

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

            // Example: Log when mod is loaded
            Logger.Message("Law and Order mod loaded successfully!");
        }
    }
}
