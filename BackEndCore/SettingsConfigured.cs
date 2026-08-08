namespace BackEndCore;

public enum BetOn12section
{
    BetOn_Both_Sets,
    BetOn_1_Set_At_A_Time,
    BetOn_Both_Sets_1_Set_With_1_Bet
}


public enum RandomBet_12section
{
    BetOn_SameBets_EveryTime,
    BetOn_RandomBets_EveryTime,
    BetOn_RandomBets_DuringWins
}

public enum ChipAmountCalc_12section
{
    SimpleDouble,
    DoublePlusOne,
    DoublePlusIncrementingDollar,
    DoubleThenTriple
}


public class SettingsConfigured(bool isRealPlay, 
                                string email,
                                string password, 
                                bool betRandomBetsEveryTime,
                                bool isBetOnOption_1,
                                bool isChipAmountCalc_Option_1,
                                int resetMarkAmount,

                                BetOn12section betOn12sectionMode,
                                RandomBet_12section randomBet12sectionMode,
                                ChipAmountCalc_12section chipAmountCalc12sectionMode,

                                StopOperatingAfterSettings stopOperatingAfter, 
                                ThenStartOperatingAgainAfterSettings thenStartOperatingAgainAfter)
{
    public bool IsRealPlay { get; } = isRealPlay;

    public string Email { get; } = email;

    public string Password { get; } = password;

    public bool BetRandomBetsEveryTime { get; } = betRandomBetsEveryTime;

    public bool IsBetOnOption_1 { get; } = isBetOnOption_1;

    public bool IsChipAmountCalc_Option_1 { get; } = isChipAmountCalc_Option_1;

    public int ResetMarkAmount { get; } = resetMarkAmount;

    public BetOn12section BetOn_12sectionMode { get; } = betOn12sectionMode;

    public RandomBet_12section RandomBet_12sectionMode { get; } = randomBet12sectionMode;

    public ChipAmountCalc_12section ChipAmountCalc_12sectionMode { get; } = chipAmountCalc12sectionMode;


    public StopOperatingAfterSettings StopOperatingAfter { get; } = stopOperatingAfter;

    public ThenStartOperatingAgainAfterSettings ThenStartOperatingAgainAfter { get; } = thenStartOperatingAgainAfter;    

}

public class StopOperatingAfterSettings(bool isDurationOfTime, int duration, int dollar, bool isInMinutes)
{
    public bool IsDurationOfTime { get; } = isDurationOfTime;

    public int Duration { get; } = duration;

    public int Dollar { get; } = dollar;

    public bool IsInMinutes { get; } = isInMinutes;
}

public class ThenStartOperatingAgainAfterSettings(bool isRandomInterval, int btwn1, int btwn2, int fixedEvery, bool isInMinutes)
{
    public bool IsRandomInterval { get; } = isRandomInterval;

    public int Btwn1 { get; } = btwn1;

    public int Btwn2 { get; } = btwn2;

    public int FixedEvery { get; } = fixedEvery;

    public bool IsInMinutes { get; } = isInMinutes;
}


