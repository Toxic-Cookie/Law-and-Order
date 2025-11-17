using System.Collections.Generic;
using HugsLib.Settings;
using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Settings
{
    /// <summary>
    /// Settings for Law and Order mod
    /// Crime penalties are now configured in the Crimes Tab (Justice Menu → Crimes)
    /// </summary>
    public static class LawAndOrderSettings
    {
        // Labor settings
        public static SettingHandle<float> DefaultSilverPerDay;

        // Penalty settings
        public static SettingHandle<float> GlobalPenaltyScale;

        // Debug/Logging settings
        public static SettingHandle<LogLevel> LogLevel;
        public static SettingHandle<bool> ShowHiddenCrimes;
        public static SettingHandle<bool> AutoConvictRedHanded;
        public static SettingHandle<int> StatuteOfLimitationsDays;

        // Phase 6: False Accusations & Infiltration settings
        public static SettingHandle<float> FalseAccusationRate;
        public static SettingHandle<float> FalseAccusationChancePerGrudge;
        public static SettingHandle<float> InfiltratorSpawnChance;
        public static SettingHandle<int> MaxAccomplicesPerInfiltrator;
        public static SettingHandle<float> TruthDiscoveryChance;
        public static SettingHandle<int> CluesPerCrime_Min;
        public static SettingHandle<int> CluesPerCrime_Max;

        /// <summary>
        /// Initialize all settings with default values
        /// </summary>
        public static void Initialize(ModSettingsPack settings)
        {
            // Labor settings
            DefaultSilverPerDay = settings.GetHandle(
                "DefaultSilverPerDay",
                "LawAndOrder_Setting_DefaultSilverPerDay_Title".Translate(),
                "LawAndOrder_Setting_DefaultSilverPerDay_Desc".Translate(),
                35f,
                Validators.FloatRangeValidator(1f, 500f)
            );

            // Penalty settings
            GlobalPenaltyScale = settings.GetHandle(
                "GlobalPenaltyScale",
                "LawAndOrder_Setting_GlobalPenaltyScale_Title".Translate(),
                "LawAndOrder_Setting_GlobalPenaltyScale_Desc".Translate(),
                1.0f,
                Validators.FloatRangeValidator(0.25f, 3.0f)
            );

            // Debug/Logging settings
            LogLevel = settings.GetHandle(
                "LogLevel",
                "Log Level",
                "Controls how much logging the mod produces. Info is recommended for normal play, Debug for troubleshooting.",
                Utils.LogLevel.Info
            );

            // Set custom drawer for log level to show friendly names
            LogLevel.CustomDrawer = rect =>
            {
                if (Widgets.ButtonText(rect, ModLog.GetLogLevelName(LogLevel.Value)))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (Utils.LogLevel level in System.Enum.GetValues(typeof(Utils.LogLevel)))
                    {
                        var levelCopy = level; // Capture for lambda
                        options.Add(new FloatMenuOption(
                            ModLog.GetLogLevelName(level),
                            () =>
                            {
                                LogLevel.Value = levelCopy;
                                ModLog.CurrentLogLevel = levelCopy;
                                ModLog.Info($"Log level changed to: {ModLog.GetLogLevelName(levelCopy)}");
                            }
                        ));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                return false;
            };

            // Initialize ModLog with current setting
            ModLog.CurrentLogLevel = LogLevel.Value;

            // Debug settings (god mode only - enforced in UI)
            ShowHiddenCrimes = settings.GetHandle(
                "ShowHiddenCrimes",
                "LawAndOrder_Setting_ShowHiddenCrimes".Translate(),
                "LawAndOrder_Setting_ShowHiddenCrimes_Desc".Translate(),
                false
            );

            AutoConvictRedHanded = settings.GetHandle(
                "AutoConvictRedHanded",
                "LawAndOrder_Setting_AutoConvictRedHanded".Translate(),
                "LawAndOrder_Setting_AutoConvictRedHanded_Desc".Translate(),
                false
            );

            // Sync auto-convict setting with JusticeManager
            AutoConvictRedHanded.ValueChanged += (SettingHandle handle) =>
            {
                Components.WorldComponent_JusticeManager.SetAutoConvictSetting(AutoConvictRedHanded.Value);
            };

            StatuteOfLimitationsDays = settings.GetHandle(
                "StatuteOfLimitationsDays",
                "LawAndOrder_Setting_StatuteOfLimitations".Translate(),
                "LawAndOrder_Setting_StatuteOfLimitations_Desc".Translate(),
                60,
                Validators.IntRangeValidator(0, 120)
            );

            // Phase 6: False Accusations & Infiltration settings
            FalseAccusationRate = settings.GetHandle(
                "FalseAccusationRate",
                "LawAndOrder_Setting_FalseAccusationRate_Title".Translate(),
                "LawAndOrder_Setting_FalseAccusationRate_Desc".Translate(),
                0.12f, // 12% default (moderate drama)
                Validators.FloatRangeValidator(0f, 0.30f)
            );

            FalseAccusationChancePerGrudge = settings.GetHandle(
                "FalseAccusationChancePerGrudge",
                "LawAndOrder_Setting_FalseAccusationChancePerGrudge_Title".Translate(),
                "LawAndOrder_Setting_FalseAccusationChancePerGrudge_Desc".Translate(),
                0.05f, // 5% default
                Validators.FloatRangeValidator(0f, 1.0f)
            );

            InfiltratorSpawnChance = settings.GetHandle(
                "InfiltratorSpawnChance",
                "LawAndOrder_Setting_InfiltratorSpawnChance_Title".Translate(),
                "LawAndOrder_Setting_InfiltratorSpawnChance_Desc".Translate(),
                0.05f, // 5% of visitors
                Validators.FloatRangeValidator(0f, 0.15f)
            );

            MaxAccomplicesPerInfiltrator = settings.GetHandle(
                "MaxAccomplicesPerInfiltrator",
                "LawAndOrder_Setting_MaxAccomplicesPerInfiltrator_Title".Translate(),
                "LawAndOrder_Setting_MaxAccomplicesPerInfiltrator_Desc".Translate(),
                2,
                Validators.IntRangeValidator(0, 5)
            );

            TruthDiscoveryChance = settings.GetHandle(
                "TruthDiscoveryChance",
                "LawAndOrder_Setting_TruthDiscoveryChance_Title".Translate(),
                "LawAndOrder_Setting_TruthDiscoveryChance_Desc".Translate(),
                0.15f, // 15% base chance
                Validators.FloatRangeValidator(0.05f, 0.40f)
            );

            CluesPerCrime_Min = settings.GetHandle(
                "CluesPerCrime_Min",
                "LawAndOrder_Setting_CluesPerCrime_Min_Title".Translate(),
                "LawAndOrder_Setting_CluesPerCrime_Min_Desc".Translate(),
                1,
                Validators.IntRangeValidator(0, 5)
            );

            CluesPerCrime_Max = settings.GetHandle(
                "CluesPerCrime_Max",
                "LawAndOrder_Setting_CluesPerCrime_Max_Title".Translate(),
                "LawAndOrder_Setting_CluesPerCrime_Max_Desc".Translate(),
                3,
                Validators.IntRangeValidator(1, 5)
            );
        }

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public static void ResetToDefaults()
        {
            DefaultSilverPerDay.Value = 35f;
            GlobalPenaltyScale.Value = 1.0f;

            // Debug defaults
            ShowHiddenCrimes.Value = false;
            AutoConvictRedHanded.Value = false;
            StatuteOfLimitationsDays.Value = 60;

            // Phase 6 defaults
            FalseAccusationRate.Value = 0.12f;
            FalseAccusationChancePerGrudge.Value = 0.05f;
            InfiltratorSpawnChance.Value = 0.05f;
            MaxAccomplicesPerInfiltrator.Value = 2;
            TruthDiscoveryChance.Value = 0.15f;
            CluesPerCrime_Min.Value = 1;
            CluesPerCrime_Max.Value = 3;
        }

        /// <summary>
        /// Validate all settings values and clamp them to acceptable ranges.
        /// This prevents issues from corrupted save files or manual XML edits.
        /// </summary>
        public static void ValidateSettings()
        {
            bool hadInvalidValues = false;

            // Labor settings
            hadInvalidValues |= ClampSetting(ref DefaultSilverPerDay, 1f, 500f, 35f, "DefaultSilverPerDay");

            // Penalty settings
            hadInvalidValues |= ClampSetting(ref GlobalPenaltyScale, 0.25f, 3.0f, 1.0f, "GlobalPenaltyScale");

            // Phase 6 settings validation
            hadInvalidValues |= ClampSetting(ref FalseAccusationRate, 0f, 0.30f, 0.12f, "FalseAccusationRate");
            hadInvalidValues |= ClampSetting(ref InfiltratorSpawnChance, 0f, 0.15f, 0.05f, "InfiltratorSpawnChance");
            hadInvalidValues |= ClampSetting(ref TruthDiscoveryChance, 0.05f, 0.40f, 0.15f, "TruthDiscoveryChance");

            if (hadInvalidValues)
            {
                ModLog.Warning("Some Law and Order settings were outside valid ranges and have been corrected. This may indicate a corrupted save file or manual configuration file edit.");
            }
        }

        /// <summary>
        /// Clamp a setting value to the specified range. Returns true if the value was clamped.
        /// </summary>
        private static bool ClampSetting(ref SettingHandle<float> setting, float min, float max, float defaultValue, string settingName)
        {
            if (setting == null)
            {
                ModLog.Error($"Setting '{settingName}' is null during validation");
                return false;
            }

            float originalValue = setting.Value;
            float clampedValue = originalValue;

            // Check for invalid values (NaN, Infinity)
            if (float.IsNaN(originalValue) || float.IsInfinity(originalValue))
            {
                ModLog.Warning($"Setting '{settingName}' had invalid value ({originalValue}), resetting to default ({defaultValue})");
                setting.Value = defaultValue;
                return true;
            }

            // Clamp to valid range
            if (originalValue < min)
            {
                clampedValue = min;
            }
            else if (originalValue > max)
            {
                clampedValue = max;
            }

            if (clampedValue != originalValue)
            {
                ModLog.Warning($"Setting '{settingName}' was out of range ({originalValue}), clamped to {clampedValue} (valid range: {min}-{max})");
                setting.Value = clampedValue;
                return true;
            }

            return false;
        }
    }
}
