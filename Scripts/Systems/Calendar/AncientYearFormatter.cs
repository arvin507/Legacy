using System.Text;

namespace AncientLife.Systems.Calendar;

public static class AncientYearFormatter
{
    private static readonly string[] Digits = ["零", "一", "二", "三", "四", "五", "六", "七", "八", "九"];
    private static readonly int[] Divisors = [1000, 100, 10, 1];
    private static readonly string[] Units = ["千", "百", "十", ""];

    public static string FormatEraYear(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Era year must be positive.");
        }

        return year == 1 ? "元" : FormatNumber(year);
    }

    private static string FormatNumber(int number)
    {
        if (number > 9999)
        {
            return string.Concat(number.ToString().Select(character => Digits[character - '0']));
        }

        var result = new StringBuilder();
        var pendingZero = false;
        for (var index = 0; index < Divisors.Length; index++)
        {
            var divisor = Divisors[index];
            var digit = number / divisor;
            number %= divisor;

            if (digit == 0)
            {
                pendingZero = result.Length > 0 && number > 0;
                continue;
            }

            if (pendingZero)
            {
                result.Append(Digits[0]);
                pendingZero = false;
            }

            if (!(divisor == 10 && digit == 1 && result.Length == 0))
            {
                result.Append(Digits[digit]);
            }

            result.Append(Units[index]);
        }

        return result.ToString();
    }
}
