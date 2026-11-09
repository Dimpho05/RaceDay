namespace RaceDay.Web.Services;

public static class OrdinalFormatter
{
    // 1 -> "1st", 2 -> "2nd", 3 -> "3rd", 11 -> "11th", 47 -> "47th"
    public static string Format(int number)
    {
        var lastTwoDigits = number % 100;
        if (lastTwoDigits is >= 11 and <= 13)
        {
            return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }
}