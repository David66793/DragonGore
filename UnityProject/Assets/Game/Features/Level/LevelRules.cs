namespace Changban
{
    public static class LevelRules
    {
        public static bool CanEscape(bool rescued, bool eliteDefeated, bool insideExit, bool playerAlive)
        {
            return rescued && eliteDefeated && insideExit && playerAlive;
        }
    }
}
