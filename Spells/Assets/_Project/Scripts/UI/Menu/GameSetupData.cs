/// <summary>
/// Static data bridge between the MainMenu scene and the combat scene.
/// Menu screens populate these fields; BoxArenaBuilder reads them on Start().
/// </summary>
public static class GameSetupData
{
    public static int PlayerCount = 2;
    public static ClassData[] PlayerClasses;

    // Settings overrides (null = use BoxArenaBuilder defaults)
    public static int? RoundsToWin;
    public static float? MaxRoundTime;
    public static int? CardOptionsPerPick;
    public static bool? AllowDuplicateClasses;

    public static bool HasSetup => PlayerClasses != null && PlayerClasses.Length > 0;

    public static void Clear()
    {
        PlayerCount = 2;
        PlayerClasses = null;
        RoundsToWin = null;
        MaxRoundTime = null;
        CardOptionsPerPick = null;
        AllowDuplicateClasses = null;
    }
}
