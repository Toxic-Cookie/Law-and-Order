using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Generates believable fake backstories for infiltrators
    /// </summary>
    public static class BackstoryFabricator
    {
        // Cover story templates categorized by pawn type
        private static readonly List<string> refugeeStories = new List<string>
        {
            "Fled {0} after a devastating mechanoid attack destroyed the colony. Seeking safety and a new beginning.",
            "Escaped from {0} when a plague outbreak killed most of the population. Looking for a fresh start.",
            "Survived the collapse of {0} when political infighting tore the settlement apart. Hoping to find stability.",
            "Left {0} after raiders burned the settlement to the ground. Searching for a peaceful colony.",
            "Rescued from the ruins of {0} by a passing trader caravan. Grateful for any chance at a new life."
        };

        private static readonly List<string> traderStories = new List<string>
        {
            "Former merchant from {0} who decided to settle down after years on the road. Has experience with trade and negotiation.",
            "Caravan guard from {0} who grew tired of constant travel. Skilled in protection and security.",
            "Retired trader who made enough silver to retire comfortably. Originally from {0}.",
            "Ex-caravan master from {0} seeking a quieter life. Brings years of organizational experience.",
            "Former trade negotiator from {0} looking to use diplomatic skills in a settled community."
        };

        private static readonly List<string> colonistStories = new List<string>
        {
            "Grew up on {0}, a rimworld colony with harsh conditions. Learned to adapt and survive.",
            "Raised on {0}, a farming settlement where everyone worked together. Values community cooperation.",
            "Born on {0}, a mining colony in the outer rim. Familiar with hard work and danger.",
            "Spent early years on {0}, a scientific research station. Appreciates knowledge and learning.",
            "Childhood on {0}, a frontier outpost at the edge of known space. Comfortable with isolation."
        };

        private static readonly List<string> militaryStories = new List<string>
        {
            "Former soldier from {0} who completed their service and sought civilian life. Disciplined and reliable.",
            "Ex-security officer from {0} with combat training. Looking for a place to protect and defend.",
            "Retired mercenary from {0} who wants to put fighting skills to peaceful use.",
            "Former militia member from {0} trained in defense. Ready to guard a new home.",
            "Discharged veteran from {0} seeking purpose beyond warfare. Loyal and protective."
        };

        private static readonly List<string> technicalStories = new List<string>
        {
            "Former engineer from {0} with expertise in mechanical systems. Enjoys building and repairing.",
            "Ex-researcher from {0} who prefers practical application over theory. Skilled in problem-solving.",
            "Technician from {0} with years of experience maintaining complex equipment.",
            "Former craftsperson from {0} known for attention to detail and quality work.",
            "Mechanic from {0} who can fix almost anything. Practical and resourceful."
        };

        // Colony names for backstory generation
        private static readonly List<string> colonyNames = new List<string>
        {
            "New Haven", "Frontier Station", "Prosperity", "Last Hope",
            "Redemption", "Far Reach", "Sanctuary", "New Dawn",
            "Horizon's End", "Star's Rest", "Refuge Point", "Liberty",
            "Pioneer's Rest", "Waypoint", "Safe Harbor", "Promise",
            "Distant Shore", "New Beginnings", "Haven's Gate", "Respite"
        };

        /// <summary>
        /// Generate a believable cover story for an infiltrator
        /// Matches story type to pawn's visible skills
        /// </summary>
        public static string GenerateCoverStory(Pawn pawn)
        {
            if (pawn == null)
                return "A settler from a distant colony seeking a new life.";

            // Choose story type based on pawn's highest skills
            var backstoryTemplate = ChooseBackstoryTemplate(pawn);

            // Pick a fake colony name
            string colonyName = colonyNames.RandomElement();

            // Format the backstory
            string backstory = string.Format(backstoryTemplate, colonyName);

            if (Prefs.DevMode)
            {
                ModLog.Debug($"Generated cover story for {pawn.LabelShort}: {backstory}");
            }

            return backstory;
        }

        /// <summary>
        /// Choose backstory template that matches pawn's skills
        /// Makes the cover story more believable
        /// </summary>
        private static string ChooseBackstoryTemplate(Pawn pawn)
        {
            if (pawn.skills == null)
                return refugeeStories.RandomElement();

            // Get pawn's top skills
            var topSkills = pawn.skills.skills
                .Where(s => !s.TotallyDisabled)
                .OrderByDescending(s => s.Level)
                .Take(3)
                .ToList();

            if (topSkills.Count == 0)
                return refugeeStories.RandomElement();

            var topSkillDef = topSkills[0].def;

            // Match backstory to skill profile
            // Military skills -> military backstory
            if (topSkillDef == SkillDefOf.Shooting || topSkillDef == SkillDefOf.Melee)
            {
                return militaryStories.RandomElement();
            }

            // Social skills -> trader backstory
            if (topSkillDef == SkillDefOf.Social)
            {
                return traderStories.RandomElement();
            }

            // Technical skills -> technical backstory
            if (topSkillDef == SkillDefOf.Crafting ||
                topSkillDef == SkillDefOf.Construction ||
                topSkillDef == SkillDefOf.Intellectual)
            {
                return technicalStories.RandomElement();
            }

            // Default to colonist or refugee
            return Rand.Bool ? colonistStories.RandomElement() : refugeeStories.RandomElement();
        }

        /// <summary>
        /// Generate a fake name that sounds believable
        /// Uses RimWorld's built-in name generator
        /// </summary>
        public static string GenerateFakeName(Pawn pawn)
        {
            if (pawn == null)
                return "Unknown";

            try
            {
                // Use RimWorld's name generator with the pawn's name style
                NameTriple originalName = pawn.Name as NameTriple;

                // Generate new name using same style
                Name newName = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full);

                // Return as string
                return newName?.ToStringShort ?? "Unknown Settler";
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Failed to generate fake name: {ex.Message}");
                return "Unknown Settler";
            }
        }

        /// <summary>
        /// Generate childhood and adulthood backstory identifiers
        /// Creates a complete fake background
        /// </summary>
        public static void GenerateFakeBackstoryIdentifiers(Pawn pawn, out string childhood, out string adulthood)
        {
            // For now, we'll just use generic backstories
            // In future, could create custom backstory defs
            childhood = "ColonySettler50";
            adulthood = "Farmer72";

            // Try to pick random backstories from the pawn's existing backstories
            // We'll just use generic placeholders since BackstoryDatabase access is complex
            if (pawn?.story != null)
            {
                // Use the pawn's existing backstories as templates
                if (pawn.story.Childhood != null)
                    childhood = pawn.story.Childhood.identifier;

                if (pawn.story.Adulthood != null)
                    adulthood = pawn.story.Adulthood.identifier;
            }

            if (Prefs.DevMode)
            {
                ModLog.Debug($"Generated fake backstories: {childhood}, {adulthood}");
            }
        }
    }
}
