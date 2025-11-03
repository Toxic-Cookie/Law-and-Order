using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace Law_and_Order.Source.Buildings
{
    /// <summary>
    /// Component that marks a building (usually a table) as a Judge's Bench.
    /// Allows it to be used as a target for court hearing rituals.
    /// </summary>
    public class Comp_JudgesBench : ThingComp
    {
        private bool isDesignatedAsJudgesBench = false;

        public bool IsDesignatedAsJudgesBench
        {
            get => isDesignatedAsJudgesBench;
            set => isDesignatedAsJudgesBench = value;
        }

        public CompProperties_JudgesBench Props => (CompProperties_JudgesBench)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isDesignatedAsJudgesBench, "isDesignatedAsJudgesBench", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            // Only show the gizmo if the player owns this building
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            // Toggle designation button
            Command_Toggle toggle = new Command_Toggle
            {
                defaultLabel = "LawAndOrder_DesignateJudgesBench".Translate(),
                defaultDesc = "LawAndOrder_DesignateJudgesBenchDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Guilty", false) ?? BaseContent.BadTex,
                isActive = () => isDesignatedAsJudgesBench,
                toggleAction = delegate ()
                {
                    isDesignatedAsJudgesBench = !isDesignatedAsJudgesBench;
                }
            };

            yield return toggle;
        }

        public override string CompInspectStringExtra()
        {
            if (isDesignatedAsJudgesBench)
            {
                return "LawAndOrder_JudgesBenchDesignated".Translate();
            }
            return null;
        }
    }
}
