using System.Numerics;

namespace VideoContactSheet;

public static class FormatNumbers
{
    private const double KiFactor = 1024.0;
    private static readonly Unit[] Suffixes = [
        new Unit(1.0, "B", true),
        new Unit(KiFactor, "KiB", false),
        new Unit(KiFactor * KiFactor, "MiB", false),
        new Unit(KiFactor * KiFactor * KiFactor, "GiB", false),
        new Unit(KiFactor * KiFactor * KiFactor * KiFactor, "TiB", false)];

    extension<T>(INumber<T> number)
        where T : INumber<T>
    {
        public string FormatBytes()
            => FormatDoubleBytes(Convert.ToDouble(number));
    }

    private static string FormatDoubleBytes(double bytes)
        => Suffixes
            .Where(IsSmallerThanNextSuffix(bytes))
            .Select(FormatBytes(bytes))
            .First();

    private static Func<Unit, bool> IsSmallerThanNextSuffix(double bytes)
        => suffix
            => bytes < suffix.Magnitude * KiFactor;

    private static Func<Unit, string> FormatBytes(double bytes)
        => suffix
            => suffix.Integral
                ? $"{(long)(bytes / suffix.Magnitude)} {suffix.Suffix}"
                : $"{bytes / suffix.Magnitude:F1} {suffix.Suffix}";

    private record Unit(double Magnitude, string Suffix, bool Integral);
}
