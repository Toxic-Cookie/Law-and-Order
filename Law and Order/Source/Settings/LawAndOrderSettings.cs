using HugsLib.Settings;
using Verse;

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
    }
}
