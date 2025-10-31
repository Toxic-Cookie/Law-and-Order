using Verse;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Defines the role a chair can be designated for in a courtroom
    /// </summary>
    public enum CourtroomChairRole
    {
        Unassigned,     // Not assigned to any role
        Judge,          // For the adjudicator/judge
        Jury,           // For jury members
        Defendant,      // For the accused/defendant
        Victim,         // For victims of the crimes
        Spectator       // For general spectators
    }

    /// <summary>
    /// Component that stores chair role designation
    /// </summary>
    public class CompCourtroomChair : ThingComp
    {
        private CourtroomChairRole role = CourtroomChairRole.Unassigned;

        public CourtroomChairRole Role
        {
            get => role;
            set => role = value;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref role, "courtroomRole", CourtroomChairRole.Unassigned);
        }

        public override string CompInspectStringExtra()
        {
            if (role != CourtroomChairRole.Unassigned)
            {
                return $"Courtroom role: {role}";
            }
            return null;
        }
    }

    /// <summary>
    /// CompProperties for the courtroom chair component
    /// </summary>
    public class CompProperties_CourtroomChair : CompProperties
    {
        public CompProperties_CourtroomChair()
        {
            this.compClass = typeof(CompCourtroomChair);
        }
    }
}
