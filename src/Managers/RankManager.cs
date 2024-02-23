namespace SharpTimer.Managers
{
    public class RankManager
    {
        public string CalculateRankStuff(int totalPlayers, int placement, double percentage, bool getRankImg = false, bool getPlacementOnly = false)
        {
            if (getRankImg)
            {
                if (totalPlayers < 100)
                {
                    if (placement <= 1)
                        return Constants.Icons.god3Icon; // God 3
                    else if (placement <= 2)
                        return Constants.Icons.god2Icon; // God 2
                    else if (placement <= 3)
                        return Constants.Icons.god1Icon; // God 1
                    else if (placement <= 10)
                        return Constants.Icons.royalty3Icon; // Royal 3
                    else if (placement <= 15)
                        return Constants.Icons.royalty2Icon; // Royal 2
                    else if (placement <= 20)
                        return Constants.Icons.royalty1Icon; // Royal 1
                    else if (placement <= 25)
                        return Constants.Icons.legend3Icon; // Legend 3
                    else if (placement <= 30)
                        return Constants.Icons.legend2Icon; // Legend 2
                    else if (placement <= 35)
                        return Constants.Icons.legend1Icon; // Legend 1
                    else if (placement <= 40)
                        return Constants.Icons.master3Icon; // Master 3
                    else if (placement <= 45)
                        return Constants.Icons.master2Icon; // Master 2
                    else if (placement <= 50)
                        return Constants.Icons.master1Icon; // Master 1
                    else if (placement <= 55)
                        return Constants.Icons.diamond3Icon; // Diamond 3
                    else if (placement <= 60)
                        return Constants.Icons.diamond2Icon; // Diamond 2
                    else if (placement <= 65)
                        return Constants.Icons.diamond1Icon; // Diamond 1
                    else if (placement <= 70)
                        return Constants.Icons.platinum3Icon; // Platinum 3
                    else if (placement <= 75)
                        return Constants.Icons.platinum2Icon; // Platinum 2
                    else if (placement <= 80)
                        return Constants.Icons.platinum1Icon; // Platinum 1
                    else if (placement <= 85)
                        return Constants.Icons.gold3Icon; // Gold 3
                    else if (placement <= 90)
                        return Constants.Icons.gold2Icon; // Gold 2
                    else if (placement <= 95)
                        return Constants.Icons.gold1Icon; // Gold 1
                    else
                        return Constants.Icons.silver1Icon; // Silver 1
                }
                else
                {
                    if (placement <= 1)
                        return Constants.Icons.god3Icon; // God 3
                    else if (placement <= 2)
                        return Constants.Icons.god2Icon; // God 2
                    else if (placement <= 3)
                        return Constants.Icons.god1Icon; // God 1
                    else if (percentage <= 1)
                        return Constants.Icons.royalty3Icon; // Royal 3
                    else if (percentage <= 5.0)
                        return Constants.Icons.royalty2Icon; // Royalty 2
                    else if (percentage <= 10.0)
                        return Constants.Icons.royalty1Icon; // Royalty 1
                    else if (percentage <= 15.0)
                        return Constants.Icons.legend3Icon; // Legend 3
                    else if (percentage <= 20.0)
                        return Constants.Icons.legend2Icon; // Legend 2
                    else if (percentage <= 25.0)
                        return Constants.Icons.legend1Icon; // Legend 1
                    else if (percentage <= 30.0)
                        return Constants.Icons.master3Icon; // Master 3
                    else if (percentage <= 35.0)
                        return Constants.Icons.master2Icon; // Master 2
                    else if (percentage <= 40.0)
                        return Constants.Icons.master1Icon; // Master 1
                    else if (percentage <= 45.0)
                        return Constants.Icons.diamond3Icon; // Diamond 3
                    else if (percentage <= 50.0)
                        return Constants.Icons.diamond2Icon; // Diamond 2
                    else if (percentage <= 55.0)
                        return Constants.Icons.diamond1Icon; // Diamond 1
                    else if (percentage <= 60.0)
                        return Constants.Icons.platinum3Icon; // Platinum 3
                    else if (percentage <= 65.0)
                        return Constants.Icons.platinum2Icon; // Platinum 2
                    else if (percentage <= 70.0)
                        return Constants.Icons.platinum1Icon; // Platinum 1
                    else if (percentage <= 75.0)
                        return Constants.Icons.gold3Icon; // Gold 3
                    else if (percentage <= 80.0)
                        return Constants.Icons.gold2Icon; // Gold 2
                    else if (percentage <= 85.0)
                        return Constants.Icons.gold1Icon; // Gold 1
                    else if (percentage <= 90.0)
                        return Constants.Icons.silver3Icon; // Silver 3
                    else if (percentage <= 95.0)
                        return Constants.Icons.silver2Icon; // Silver 2
                    else
                        return Constants.Icons.silver1Icon; // Silver 1
                }
            }
            else
            {
                if (totalPlayers < 100)
                {
                    if (placement <= 1)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God III";
                    else if (placement <= 2)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God II";
                    else if (placement <= 3)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God I";
                    else if (placement <= 10)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty III";
                    else if (placement <= 15)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty II";
                    else if (placement <= 20)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty I";
                    else if (placement <= 25)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend III";
                    else if (placement <= 30)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend II";
                    else if (placement <= 35)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend I";
                    else if (placement <= 40)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master III";
                    else if (placement <= 45)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master II";
                    else if (placement <= 50)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master I";
                    else if (placement <= 55)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond III";
                    else if (placement <= 60)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond II";
                    else if (placement <= 65)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond I";
                    else if (placement <= 70)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum III";
                    else if (placement <= 75)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum II";
                    else if (placement <= 80)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum I";
                    else if (placement <= 85)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold III";
                    else if (placement <= 90)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold II";
                    else if (placement <= 95)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold I";
                    else
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver I";
                }
                else
                {
                    if (placement <= 1)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God III";
                    else if (placement <= 2)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God II";
                    else if (placement <= 3)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"God I";
                    else if (percentage <= 1)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty III";
                    else if (percentage <= 5.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty II";
                    else if (percentage <= 10.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Royalty I";
                    else if (percentage <= 15.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend III";
                    else if (percentage <= 20.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend II";
                    else if (percentage <= 25.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Legend I";
                    else if (percentage <= 30.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master III";
                    else if (percentage <= 35.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master II";
                    else if (percentage <= 40.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Master I";
                    else if (percentage <= 45.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond III";
                    else if (percentage <= 50.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond II";
                    else if (percentage <= 55.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Diamond I";
                    else if (percentage <= 60.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum III";
                    else if (percentage <= 65.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum II";
                    else if (percentage <= 70.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Platinum I";
                    else if (percentage <= 75.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold III";
                    else if (percentage <= 80.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold II";
                    else if (percentage <= 85.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Gold I";
                    else if (percentage <= 90.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver III";
                    else if (percentage <= 95.0)
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver II";
                    else
                        return getPlacementOnly ? $"{placement}/{totalPlayers}" : $"Silver I";
                }
            }
        }
    }
}
