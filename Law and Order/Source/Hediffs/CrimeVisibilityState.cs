namespace Law_and_Order.Source.Hediffs
{
    /// <summary>
    /// Represents the visibility state of a crime in the justice system.
    /// Based on Dwarf Fortress-inspired crime detection mechanics.
    /// </summary>
    public enum CrimeVisibilityState
    {
        /// <summary>
        /// Crime committed with no witnesses present (using Fog of War detection).
        /// Hidden crimes are not visible in the Justice UI and do not create cases.
        /// They remain on the criminal's record but have no immediate game impact.
        /// </summary>
        Hidden = 0,

        /// <summary>
        /// Crime witnessed by colonists or has sufficient evidence.
        /// Suspected crimes create an active criminal case and appear in the Justice UI.
        /// Player must investigate and choose to convict or dismiss the case.
        /// </summary>
        Suspected = 1,

        /// <summary>
        /// Crime has been formally convicted by the player.
        /// Convicted crimes allow the player to assign punishments.
        /// Once convicted, crimes remain on permanent record.
        /// </summary>
        Convicted = 2
    }

    /// <summary>
    /// Represents the status of a criminal case.
    /// </summary>
    public enum CaseStatus
    {
        /// <summary>
        /// Case is open and awaiting player action (investigation/conviction/dismissal).
        /// </summary>
        Open = 0,

        /// <summary>
        /// Case has been convicted - punishment may be assigned.
        /// </summary>
        Convicted = 1,

        /// <summary>
        /// Case has been dismissed by the player - no punishment.
        /// </summary>
        Dismissed = 2
    }
}
