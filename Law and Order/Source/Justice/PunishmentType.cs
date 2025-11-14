namespace Law_and_Order.Source.Justice
{
    /// <summary>
    /// Enum for different types of punishments that can be assigned to convicted criminals.
    /// Phase 4: Conviction & Punishment System
    /// </summary>
    public enum PunishmentType
    {
        /// <summary>
        /// No punishment assigned yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// Imprisonment for a duration.
        /// Uses vanilla prison system with tracked release date.
        /// </summary>
        Imprisonment = 1,

        /// <summary>
        /// Physical beating administered by a colonist.
        /// Non-lethal damage with pain and potential injuries.
        /// </summary>
        Beating = 2,

        /// <summary>
        /// Scheduled execution.
        /// Uses vanilla execution mechanics.
        /// </summary>
        Execution = 3,

        /// <summary>
        /// Silver fine that must be paid.
        /// Tracked via hediff system.
        /// </summary>
        Fine = 4,

        /// <summary>
        /// Permanent banishment from the colony.
        /// Uses vanilla exile mechanics.
        /// </summary>
        Exile = 5
    }
}
