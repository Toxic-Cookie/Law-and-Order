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
        // Contraband settings (specific to contraband system, not in Crimes Tab)
        public static SettingHandle<float> ContrabandPerDrug;

        // Labor settings
        public static SettingHandle<float> DefaultSilverPerDay;

        // Penalty settings
        public static SettingHandle<float> GlobalPenaltyScale;

        // Debt forgiveness settings
        public static SettingHandle<bool> EnableDebtForgiveness;
        public static SettingHandle<int> DebtForgivenessThreshold;

        // Debug/Logging settings
        public static SettingHandle<LogLevel> LogLevel;

        /// <summary>
        /// Initialize all settings with default values
        /// </summary>
        public static void Initialize(ModSettingsPack settings)
        {
            // Contraband settings
            ContrabandPerDrug = settings.GetHandle(
                "ContrabandPerDrug",
                "LawAndOrder_Setting_ContrabandPerDrug_Title".Translate(),
                "LawAndOrder_Setting_ContrabandPerDrug_Desc".Translate(),
                50f,
                Validators.FloatRangeValidator(0f, 1000f)
            );

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

            // Debt forgiveness settings
            EnableDebtForgiveness = settings.GetHandle(
                "EnableDebtForgiveness",
                "LawAndOrder_Setting_EnableDebtForgiveness_Title".Translate(),
                "LawAndOrder_Setting_EnableDebtForgiveness_Desc".Translate(),
                true
            );

            DebtForgivenessThreshold = settings.GetHandle(
                "DebtForgivenessThreshold",
                "LawAndOrder_Setting_DebtForgivenessThreshold_Title".Translate(),
                "LawAndOrder_Setting_DebtForgivenessThreshold_Desc".Translate(),
                90, // 90% debt paid
                Validators.IntRangeValidator(50, 100)
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
        }

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public static void ResetToDefaults()
        {
            ContrabandPerDrug.Value = 50f;
            DefaultSilverPerDay.Value = 35f;
            GlobalPenaltyScale.Value = 1.0f;
            EnableDebtForgiveness.Value = true;
            DebtForgivenessThreshold.Value = 90;
        }

        /// <summary>
        /// Validate all settings values and clamp them to acceptable ranges.
        /// This prevents issues from corrupted save files or manual XML edits.
        /// </summary>
        public static void ValidateSettings()
        {
            bool hadInvalidValues = false;

            // Contraband settings
            hadInvalidValues |= ClampSetting(ref ContrabandPerDrug, 0f, 1000f, 50f, "ContrabandPerDrug");

            // Labor settings
            hadInvalidValues |= ClampSetting(ref DefaultSilverPerDay, 1f, 500f, 35f, "DefaultSilverPerDay");

            // Penalty settings
            hadInvalidValues |= ClampSetting(ref GlobalPenaltyScale, 0.25f, 3.0f, 1.0f, "GlobalPenaltyScale");

            // Debt forgiveness settings
            hadInvalidValues |= ClampSettingInt(ref DebtForgivenessThreshold, 50, 100, 90, "DebtForgivenessThreshold");

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

        /// <summary>
        /// Clamp an integer setting value to the specified range. Returns true if the value was clamped.
        /// </summary>
        private static bool ClampSettingInt(ref SettingHandle<int> setting, int min, int max, int defaultValue, string settingName)
        {
            if (setting == null)
            {
                ModLog.Error($"Setting '{settingName}' is null during validation");
                return false;
            }

            int originalValue = setting.Value;
            int clampedValue = originalValue;

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
