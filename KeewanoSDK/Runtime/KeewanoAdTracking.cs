namespace Keewano
{
    /**
    @brief Represents the type of advertisement.
    */
    public enum AdType : byte
    {
        /// Rewarded video ads - user chooses to watch for in-game rewards
        Rewarded = 1,
        /// Full-screen ads at transition points
        Interstitial = 2,
        /// Small persistent ads displayed on screen
        Banner = 3,
        /// Interactive mini-game ads
        Playable = 4,
        /// List of tasks/offers for rewards
        Offerwall = 5,
    }
}
