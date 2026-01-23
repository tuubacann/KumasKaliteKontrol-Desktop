using System;
using System.Collections.Generic;
using System.Linq;
using KumasKaliteKontrol.Models;

namespace KumasKaliteKontrol.Services
{
    public class PartyCalculator
    {
        private const int FIRST_MIN = 60;
        private const int FIRST_MAX = 120;

        private const int SECOND_MIN = 20;
        private const int SECOND_MAX = 70;

        public List<Party> CreateParties(int totalMeters, List<Defect> defects, int fabricId)
        {
            if (totalMeters <= 0) return new List<Party>();

            BuildPrefix(totalMeters, defects, out int[] prefix);
            int Points(int a, int b) => prefix[b] - prefix[a];

            var INF = Cost.Inf();
            var best = new Cost[totalMeters + 1];
            var prevPos = new int[totalMeters + 1];
            var prevLen = new int[totalMeters + 1];
            var prevIsSecond = new bool[totalMeters + 1];
            var prevPts = new int[totalMeters + 1];

            for (int i = 0; i <= totalMeters; i++)
            {
                best[i] = INF;
                prevPos[i] = -1;
                prevLen[i] = 0;
                prevIsSecond[i] = false;
                prevPts[i] = 0;
            }

            best[0] = new Cost(0, 0, 0);

            for (int i = 0; i < totalMeters; i++)
            {
                if (best[i].IsInf) continue;

                int remaining = totalMeters - i;
                int maxTry = Math.Min(FIRST_MAX, remaining);

                for (int len = 1; len <= maxTry; len++)
                {
                    int j = i + len;
                    int pts = Points(i, j);
                    bool isEnd = j == totalMeters;

                    bool canFirst = IsFirst(len, pts, isEnd);
                    bool canSecond = IsSecond(len, isEnd);

                    if (!canFirst && !canSecond) continue;

                    bool isSecond = !canFirst;
                    var cand = best[i].Next(len, pts, isSecond);

                    if (cand.BetterThan(best[j]))
                    {
                        best[j] = cand;
                        prevPos[j] = i;
                        prevLen[j] = len;
                        prevIsSecond[j] = isSecond;
                        prevPts[j] = pts;
                    }
                }
            }

            var result = new List<Party>();
            int cur = totalMeters;

            while (cur > 0)
            {
                int p = prevPos[cur];
                int len = prevLen[cur];
                bool isSecond = prevIsSecond[cur];
                int pts = prevPts[cur];

                result.Add(new Party
                {
                    FabricId = fabricId,
                    StartMeter = p,
                    EndMeter = cur,
                    Length = len,
                    TotalPoints = pts,
                    Quality = isSecond ? QualityLevel.SecondQuality : QualityLevel.FirstQuality
                });

                cur = p;
            }

            result.Reverse();

            NormalizeAll(result, prefix, totalMeters);
            return result;
        }

        /* ===========================
           NORMALIZATION PIPELINE
           =========================== */

        private void NormalizeAll(List<Party> parties, int[] prefix, int totalMeters)
        {
            SoftenSecondQualityLinked(parties, prefix, totalMeters);
            MergeAdjacentFirstQuality(parties, prefix, totalMeters);
            RecalculateAll(parties, prefix, totalMeters);
        }

        /* ===========================
           🔥 SECOND SOFTENING (ASIL KURAL)
           =========================== */

        private void SoftenSecondQualityLinked(
            List<Party> parties,
            int[] prefix,
            int totalMeters)
        {
            for (int i = 0; i < parties.Count; i++)
            {
                var sec = parties[i];
                if (sec.Quality != QualityLevel.SecondQuality)
                    continue;

                int allowed = GetMaxAllowedPoints(sec.Length);
                int excess = sec.TotalPoints - allowed;
                if (excess <= 0)
                    continue;

                // SOL
                if (i > 0)
                    excess = AbsorbExcess(parties[i - 1], sec, excess, prefix, totalMeters);

                // SAĞ
                if (excess > 0 && i + 1 < parties.Count)
                    excess = AbsorbExcess(parties[i + 1], sec, excess, prefix, totalMeters);

                // güvenlik
                sec.TotalPoints = Math.Max(allowed, sec.TotalPoints);
            }
        }

        private int AbsorbExcess(
            Party first,
            Party second,
            int excess,
            int[] prefix,
            int totalMeters)
        {
            if (first.Quality != QualityLevel.FirstQuality)
                return excess;

            bool isEnd = first.EndMeter == totalMeters;

            int maxAllowed = GetMaxAllowedPoints(first.Length);
            int capacity = maxAllowed - first.TotalPoints;
            if (capacity <= 0)
                return excess;

            int take = Math.Min(capacity, excess);

            first.TotalPoints += take;
            second.TotalPoints -= take;

            if (!IsFirst(first.Length, first.TotalPoints, isEnd))
            {
                first.TotalPoints -= take;
                second.TotalPoints += take;
                return excess;
            }

            return excess - take;
        }

        /* ===========================
           MERGE & RECALC
           =========================== */

        private static void MergeAdjacentFirstQuality(
            List<Party> parties,
            int[] prefix,
            int totalMeters)
        {
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < parties.Count - 1; i++)
                {
                    var a = parties[i];
                    var b = parties[i + 1];

                    if (a.Quality != QualityLevel.FirstQuality ||
                        b.Quality != QualityLevel.FirstQuality)
                        continue;

                    int len = b.EndMeter - a.StartMeter;
                    if (len > FIRST_MAX)
                        continue;

                    int pts = prefix[b.EndMeter] - prefix[a.StartMeter];
                    bool isEnd = b.EndMeter == totalMeters;

                    if (!IsFirst(len, pts, isEnd))
                        continue;

                    a.EndMeter = b.EndMeter;
                    a.Length = len;
                    a.TotalPoints = pts;

                    parties.RemoveAt(i + 1);
                    changed = true;
                    break;
                }
            } while (changed);
        }

        private static void RecalculateAll(
            List<Party> parties,
            int[] prefix,
            int totalMeters)
        {
            foreach (var p in parties)
            {
                p.Length = p.EndMeter - p.StartMeter;
                p.TotalPoints = prefix[p.EndMeter] - prefix[p.StartMeter];

                bool isEnd = p.EndMeter == totalMeters;
                p.Quality = IsFirst(p.Length, p.TotalPoints, isEnd)
                    ? QualityLevel.FirstQuality
                    : QualityLevel.SecondQuality;
            }
        }

        /* ===========================
           QUALITY RULES
           =========================== */

        private static bool IsFirst(int len, int pts, bool isEnd)
        {
            if (len > FIRST_MAX) return false;

            if (len >= FIRST_MIN)
                return pts <= GetMaxAllowedPoints(len);

            if (isEnd && len >= FIRST_MIN)
                return pts <= GetMaxAllowedPoints(len);

            return false;
        }

        private static bool IsSecond(int len, bool isEnd)
        {
            if (len >= SECOND_MIN && len <= SECOND_MAX) return true;
            if (isEnd && len >= 1 && len <= SECOND_MAX) return true;
            return false;
        }

        private static int GetMaxAllowedPoints(int length)
            => (int)Math.Floor(length * 16.0 / 120.0);

        /* ===========================
           PREFIX
           =========================== */

        private static void BuildPrefix(
            int totalMeters,
            List<Defect> defects,
            out int[] prefix)
        {
            var diff = new int[totalMeters + 1];

            void Add(int s, int e, int v)
            {
                s = Math.Max(0, Math.Min(totalMeters, s));
                e = Math.Max(0, Math.Min(totalMeters, e));
                if (e <= s) return;
                diff[s] += v;
                diff[e] -= v;
            }

            foreach (var d in defects)
            {
                if (d.PointType == 4)
                    Add(d.StartMeter, d.EndMeter, 4);
                else
                    Add(d.StartMeter, d.StartMeter + 1, 1);
            }

            int run = 0;
            prefix = new int[totalMeters + 1];
            for (int i = 0; i < totalMeters; i++)
            {
                run += diff[i];
                prefix[i + 1] = prefix[i] + run;
            }
        }

        /* ===========================
           COST (DP)
           =========================== */

        private readonly struct Cost
        {
            public readonly long SecondLen;
            public readonly long SecondPts;
            public readonly long PieceCount;
            public bool IsInf => SecondLen >= long.MaxValue / 8;

            public Cost(long sl, long sp, long pc)
            {
                SecondLen = sl;
                SecondPts = sp;
                PieceCount = pc;
            }

            public static Cost Inf()
                => new Cost(long.MaxValue / 4, long.MaxValue / 4, long.MaxValue / 4);

            public Cost Next(int len, int pts, bool isSecond)
            {
                return new Cost(
                    SecondLen + (isSecond ? len : 0),
                    SecondPts + (isSecond ? pts : 0),
                    PieceCount + 1
                );
            }

            public bool BetterThan(Cost o)
            {
                if (SecondLen != o.SecondLen) return SecondLen < o.SecondLen;
                if (SecondPts != o.SecondPts) return SecondPts < o.SecondPts;
                return PieceCount < o.PieceCount;
            }
        }
    }
}
