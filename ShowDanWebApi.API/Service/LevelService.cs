namespace ShowDanWebApi.API.Service;

public static class LevelHelper
{
    private static readonly int[] Thresholds =
        [0, 1000, 2100, 3300, 4600, 6000, 7500, 9100, 10800, 12600, 14500, 16500, 18600, 20800, 23100, 25500, 28000, 30600, 33300, 36100];

    public static (int Level, int Percentage) Calculate(int points)
    {
        if (points >= Thresholds[^1]) return (20, 100);
        if (points <= 0) return (1, 1);

        int index = Array.BinarySearch(Thresholds, points);
        int level = index >= 0 ? index + 1 : ~index;

        int currentLevelPoints = Thresholds[level - 1];
        int nextLevelPoints = Thresholds[level];

        int percentage = (int)Math.Round((double)(points - currentLevelPoints) / (nextLevelPoints - currentLevelPoints) * 100);

        return (level, Math.Clamp(percentage, 1, 99));
    }
}