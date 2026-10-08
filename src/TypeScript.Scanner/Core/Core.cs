// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/core/core.go, scanner-only spelling suggestion slice.
namespace TypeScript.Scanner;
internal static partial class Core
{
    public static T IfElse<T>(bool condition, T a, T b) => condition ? a : b;
    public static TextRange NewTextRange(int pos, int end) => new(pos, end);
    public static string GetSpellingSuggestionForStrings(string name, IEnumerable<string> candidates)
    {
        int[] runeName = GoString.Runes(name);
        int maximumLengthDifference = Math.Max(2, (int)(runeName.Length * 0.34));
        double bestDistance = Math.Floor(runeName.Length * 0.4) + 0.9;
        string bestCandidate = "";
        bool hasBest = false;
        foreach (string candidateName in candidates)
        {
            int maxLen = Math.Max(candidateName.Length, runeName.Length);
            int minLen = Math.Min(candidateName.Length, runeName.Length);
            if (candidateName != "" && maxLen - minLen <= maximumLengthDifference)
            {
                if (candidateName == name)
                    continue;
                if (candidateName.Length < 3 && !UnicodeCase.EqualFold(candidateName, name))
                    continue;
                double distance = levenshteinWithMax(runeName, GoString.Runes(candidateName), bestDistance);
                if (distance < 0)
                    continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidateName;
                    hasBest = true;
                }
                else if (!hasBest || StringComparer.Ordinal.Compare(candidateName, bestCandidate) < 0)
                {
                    bestCandidate = candidateName;
                    hasBest = true;
                }
            }
        }

        return bestCandidate;
    }

    private static double levenshteinWithMax(int[] s1, int[] s2, double maxValue)
    {
        double[] previous = new double[s2.Length + 1], current = new double[s2.Length + 1];
        double big = maxValue + 0.01;
        for (int i = 0; i < previous.Length; i++)
            previous[i] = i;
        for (int i = 1; i <= s1.Length; i++)
        {
            int c1 = s1[i - 1];
            int minJ = Math.Max((int)Math.Ceiling(i - maxValue), 1);
            int maxJ = Math.Min((int)Math.Floor(maxValue + i), s2.Length);
            double colMin = i;
            current[0] = colMin;
            for (int j = 1; j < minJ; j++)
                current[j] = big;
            for (int j = minJ; j <= maxJ; j++)
            {
                double substitutionDistance = UnicodeCase.ToLower(s1[i - 1]) == UnicodeCase.ToLower(s2[j - 1]) ? previous[j - 1] + 0.1 : previous[j - 1] + 2;
                double dist = c1 == s2[j - 1] ? previous[j - 1] : Math.Min(previous[j] + 1, Math.Min(current[j - 1] + 1, substitutionDistance));
                current[j] = dist;
                colMin = Math.Min(colMin, dist);
            }

            for (int j = maxJ + 1; j <= s2.Length; j++)
                current[j] = big;
            if (colMin > maxValue)
                return -1;
            (previous, current) = (current, previous);
        }

        double res = previous[s2.Length];
        return res > maxValue ? -1 : res;
    }
}
