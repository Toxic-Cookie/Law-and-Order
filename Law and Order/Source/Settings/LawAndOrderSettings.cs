using System.Collections.Generic;
using HugsLib.Settings;
using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Settings
{
    /// <summary>
    /// Settings for Law and Order mod - configurable debt values
    /// </summary>
    public static class LawAndOrderSettings
    {
        // Crimes Against Colony
        public static SettingHandle<float> ArmedTrespassing;
        public static SettingHandle<float> ArsonBase;
        public static SettingHandle<float> TheftMultiplier;
        public static SettingHandle<float> ContrabandPerDrug;

        // Crimes Against Persons
        public static SettingHandle<float> Assault;
        public static SettingHandle<float> DownedColonist;
        public static SettingHandle<float> AssaultAnimal;
        public static SettingHandle<float> KillAnimalMultiplier;
        public static SettingHandle<float> KillBondedAnimalMultiplier;
        public static SettingHandle<float> KillBondedAnimalBonus;
        public static SettingHandle<float> Murder;

        // Crimes Against Property
        public static SettingHandle<float> PropertyDestructionMultiplier;

        // Sentencing Modifiers
        public static SettingHandle<float> BannedWeaponModifier;
        public static SettingHandle<float> RepeatOffenderModifier;

        // Labor settings
        public static SettingHandle<float> DefaultSilverPerDay;

        // Debug/Logging settings
        public static SettingHandle<LogLevel> LogLevel;

        /// <summary>
        /// Initialize all settings with default values
        /// </summary>
        public static void Initialize(ModSettingsPack settings)
        {
            // Crimes Against Colony
            ArmedTrespassing = settings.GetHandle(
                "ArmedTrespassing",
                "LawAndOrder_Setting_ArmedTrespassing_Title".Translate(),
                "LawAndOrder_Setting_ArmedTrespassing_Desc".Translate(),
                150f,
                Validators.FloatRangeValidator(0f, 10000f)
            );

            ArsonBase = settings.GetHandle(
                "ArsonBase",
                "LawAndOrder_Setting_ArsonBase_Title".Translate(),
                "LawAndOrder_Setting_ArsonBase_Desc".Translate(),
                250f,
                Validators.FloatRangeValidator(0f, 10000f)
            );

            TheftMultiplier = settings.GetHandle(
                "TheftMultiplier",
                "LawAndOrder_Setting_TheftMultiplier_Title".Translate(),
                "LawAndOrder_Setting_TheftMultiplier_Desc".Translate(),
                1.5f,
                Validators.FloatRangeValidator(1f, 5f)
            );

            ContrabandPerDrug = settings.GetHandle(
                "ContrabandPerDrug",
                "LawAndOrder_Setting_ContrabandPerDrug_Title".Translate(),
                "LawAndOrder_Setting_ContrabandPerDrug_Desc".Translate(),
                50f,
                Validators.FloatRangeValidator(0f, 1000f)
            );

            // Crimes Against Persons
            Assault = settings.GetHandle(
                "Assault",
                "LawAndOrder_Setting_Assault_Title".Translate(),
                "LawAndOrder_Setting_Assault_Desc".Translate(),
                350f,
                Validators.FloatRangeValidator(0f, 10000f)
            );

            DownedColonist = settings.GetHandle(
                "DownedColonist",
                "LawAndOrder_Setting_DownedColonist_Title".Translate(),
                "LawAndOrder_Setting_DownedColonist_Desc".Translate(),
                750f,
                Validators.FloatRangeValidator(0f, 10000f)
            );

            AssaultAnimal = settings.GetHandle(
                "AssaultAnimal",
                "LawAndOrder_Setting_AssaultAnimal_Title".Translate(),
                "LawAndOrder_Setting_AssaultAnimal_Desc".Translate(),
                100f,
                Validators.FloatRangeValidator(0f, 10000f)
            );

            KillAnimalMultiplier = settings.GetHandle(
                "KillAnimalMultiplier",
                "LawAndOrder_Setting_KillAnimalMultiplier_Title".Translate(),
                "LawAndOrder_Setting_KillAnimalMultiplier_Desc".Translate(),
                2f,
                Validators.FloatRangeValidator(1f, 10f)
            );

            KillBondedAnimalMultiplier = settings.GetHandle(
                "KillBondedAnimalMultiplier",
                "LawAndOrder_Setting_KillBondedAnimalMultiplier_Title".Translate(),
                "LawAndOrder_Setting_KillBondedAnimalMultiplier_Desc".Translate(),
                3f,
                Validators.FloatRangeValidator(1f, 10f)
            );

            KillBondedAnimalBonus = settings.GetHandle(
                "KillBondedAnimalBonus",
                "LawAndOrder_Setting_KillBondedAnimalBonus_Title".Translate(),
                "LawAndOrder_Setting_KillBondedAnimalBonus_Desc".Translate(),
                500f,
                Validators.FloatRangeValidator(0f, 10000f)
            );

            Murder = settings.GetHandle(
                "Murder",
                "LawAndOrder_Setting_Murder_Title".Translate(),
                "LawAndOrder_Setting_Murder_Desc".Translate(),
                5000f,
                Validators.FloatRangeValidator(0f, 50000f)
            );

            // Property Destruction
            PropertyDestructionMultiplier = settings.GetHandle(
                "PropertyDestructionMultiplier",
                "LawAndOrder_Setting_PropertyDestructionMultiplier_Title".Translate(),
                "LawAndOrder_Setting_PropertyDestructionMultiplier_Desc".Translate(),
                1.2f,
                Validators.FloatRangeValidator(1f, 5f)
            );

            // Modifiers
            BannedWeaponModifier = settings.GetHandle(
                "BannedWeaponModifier",
                "LawAndOrder_Setting_BannedWeaponModifier_Title".Translate(),
                "LawAndOrder_Setting_BannedWeaponModifier_Desc".Translate(),
                1.25f,
                Validators.FloatRangeValidator(1f, 3f)
            );

            RepeatOffenderModifier = settings.GetHandle(
                "RepeatOffenderModifier",
                "LawAndOrder_Setting_RepeatOffenderModifier_Title".Translate(),
                "LawAndOrder_Setting_RepeatOffenderModifier_Desc".Translate(),
                1.5f,
                Validators.FloatRangeValidator(1f, 5f)
            );

            // Labor settings
            DefaultSilverPerDay = settings.GetHandle(
                "DefaultSilverPerDay",
                "LawAndOrder_Setting_DefaultSilverPerDay_Title".Translate(),
                "LawAndOrder_Setting_DefaultSilverPerDay_Desc".Translate(),
                35f,
                Validators.FloatRangeValidator(1f, 500f)
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
            ArmedTrespassing.Value = 150f;
            ArsonBase.Value = 250f;
            TheftMultiplier.Value = 1.5f;
            ContrabandPerDrug.Value = 50f;
            Assault.Value = 350f;
            DownedColonist.Value = 750f;
            AssaultAnimal.Value = 100f;
            KillAnimalMultiplier.Value = 2f;
            KillBondedAnimalMultiplier.Value = 3f;
            KillBondedAnimalBonus.Value = 500f;
            Murder.Value = 5000f;
            PropertyDestructionMultiplier.Value = 1.2f;
            BannedWeaponModifier.Value = 1.25f;
            RepeatOffenderModifier.Value = 1.5f;
            DefaultSilverPerDay.Value = 35f;
        }

        /// <summary>
        /// Validate all settings values and clamp them to acceptable ranges.
        /// This prevents issues from corrupted save files or manual XML edits.
        /// </summary>
        public static void ValidateSettings()
        {
            bool hadInvalidValues = false;

            // Crimes Against Colony
            hadInvalidValues |= ClampSetting(ref ArmedTrespassing, 0f, 10000f, 150f, "ArmedTrespassing");
            hadInvalidValues |= ClampSetting(ref ArsonBase, 0f, 10000f, 250f, "ArsonBase");
            hadInvalidValues |= ClampSetting(ref TheftMultiplier, 1f, 5f, 1.5f, "TheftMultiplier");
            hadInvalidValues |= ClampSetting(ref ContrabandPerDrug, 0f, 1000f, 50f, "ContrabandPerDrug");

            // Crimes Against Persons
            hadInvalidValues |= ClampSetting(ref Assault, 0f, 10000f, 350f, "Assault");
            hadInvalidValues |= ClampSetting(ref DownedColonist, 0f, 10000f, 750f, "DownedColonist");
            hadInvalidValues |= ClampSetting(ref AssaultAnimal, 0f, 10000f, 100f, "AssaultAnimal");
            hadInvalidValues |= ClampSetting(ref KillAnimalMultiplier, 1f, 10f, 2f, "KillAnimalMultiplier");
            hadInvalidValues |= ClampSetting(ref KillBondedAnimalMultiplier, 1f, 10f, 3f, "KillBondedAnimalMultiplier");
            hadInvalidValues |= ClampSetting(ref KillBondedAnimalBonus, 0f, 10000f, 500f, "KillBondedAnimalBonus");
            hadInvalidValues |= ClampSetting(ref Murder, 0f, 50000f, 5000f, "Murder");

            // Property Destruction
            hadInvalidValues |= ClampSetting(ref PropertyDestructionMultiplier, 1f, 5f, 1.2f, "PropertyDestructionMultiplier");

            // Modifiers
            hadInvalidValues |= ClampSetting(ref BannedWeaponModifier, 1f, 3f, 1.25f, "BannedWeaponModifier");
            hadInvalidValues |= ClampSetting(ref RepeatOffenderModifier, 1f, 5f, 1.5f, "RepeatOffenderModifier");

            // Labor settings
            hadInvalidValues |= ClampSetting(ref DefaultSilverPerDay, 1f, 500f, 35f, "DefaultSilverPerDay");

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
