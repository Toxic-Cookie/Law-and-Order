using System;
using Verse;

namespace LawAndOrder.Investigation
{
    /// <summary>
    /// Represents a progressive piece of information revealed when studying a clue.
    /// Inspired by Anomaly DLC's study unlock system.
    /// </summary>
    public class ClueRevelation : IExposable
    {
        // Study progress needed to unlock this revelation (0.0-1.0)
        public float threshold;

        // Label for this revelation stage
        public string label;

        // Description of what was discovered
        public string description;

        // Accuracy chance (can be wrong, especially for planted evidence)
        public float accuracyChance = 1.0f;

        // Who does this evidence point to?
        public Pawn pointsTo;

        // Has this revelation been shown to the player?
        public bool revealed;

        public ClueRevelation()
        {
        }

        public ClueRevelation(float threshold, string label, string description, float accuracyChance = 1.0f, Pawn pointsTo = null)
        {
            this.threshold = threshold;
            this.label = label;
            this.description = description;
            this.accuracyChance = accuracyChance;
            this.pointsTo = pointsTo;
            this.revealed = false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref threshold, "threshold", 0f);
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref accuracyChance, "accuracyChance", 1.0f);
            Scribe_References.Look(ref pointsTo, "pointsTo");
            Scribe_Values.Look(ref revealed, "revealed", false);
        }

        public bool ShouldRevealAt(float studyProgress)
        {
            return !revealed && studyProgress >= threshold;
        }
    }
}
