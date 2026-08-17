using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TaffyUGUI
{
    public enum TaffyGridAutoFlow { Row = 0, Column = 1, RowDense = 2, ColumnDense = 3 }
    public enum TaffyGridAxis { Row = 0, Column = 1 }
    public enum TaffyGridRepeatMode { Count = 0, AutoFill = 1, AutoFit = 2 }

    public enum TaffyGridTrackKind
    {
        Auto = 0,
        Points = 1,
        Percent = 2,
        Fraction = 3,
        MinMax = 4,
        MinContent = 5,
        MaxContent = 6,
        Calc = 7,
        Repeat = 8,
    }

    public enum TaffyGridTrackBreadthKind
    {
        Auto = 0,
        Points = 1,
        Percent = 2,
        Fraction = 3,
        MinContent = 5,
        MaxContent = 6,
        Calc = 7,
    }

    public enum TaffyGridPlacementKind
    {
        Auto = 0,
        Line = 1,
        Span = 2,
        NamedLine = 3,
        NamedSpan = 4,
    }

    [Serializable]
    public struct TaffyGridTrackBreadth
    {
        public TaffyGridTrackBreadthKind kind;
        public float value;
        public TaffyCalcExpression calc;

        public static TaffyGridTrackBreadth Auto => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.Auto };
        public static TaffyGridTrackBreadth Points(float value) => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.Points, value = value };
        public static TaffyGridTrackBreadth Percent(float value) => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.Percent, value = value };
        public static TaffyGridTrackBreadth Fraction(float value) => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.Fraction, value = value };
        public static TaffyGridTrackBreadth MinContent => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.MinContent };
        public static TaffyGridTrackBreadth MaxContent => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.MaxContent };
        public static TaffyGridTrackBreadth Calc(TaffyCalcExpression expression) => new TaffyGridTrackBreadth { kind = TaffyGridTrackBreadthKind.Calc, calc = expression };
    }

    [Serializable]
    public sealed class TaffyGridTrack
    {
        public TaffyGridTrackKind kind = TaffyGridTrackKind.Auto;
        public float value;
        public TaffyCalcExpression calc;
        public TaffyGridTrackBreadth min = default;
        public TaffyGridTrackBreadth max = default;
        public TaffyGridRepeatMode repeatMode = TaffyGridRepeatMode.Count;
        [Min(1)] public int repeatCount = 1;
        public List<TaffyGridTrack> repeatTracks = new List<TaffyGridTrack>();

        public static TaffyGridTrack Auto() => new TaffyGridTrack { kind = TaffyGridTrackKind.Auto };
        public static TaffyGridTrack Points(float value) => new TaffyGridTrack { kind = TaffyGridTrackKind.Points, value = value };
        public static TaffyGridTrack Percent(float value) => new TaffyGridTrack { kind = TaffyGridTrackKind.Percent, value = value };
        public static TaffyGridTrack Fraction(float value) => new TaffyGridTrack { kind = TaffyGridTrackKind.Fraction, value = value };
        public static TaffyGridTrack MinContent() => new TaffyGridTrack { kind = TaffyGridTrackKind.MinContent };
        public static TaffyGridTrack MaxContent() => new TaffyGridTrack { kind = TaffyGridTrackKind.MaxContent };
        public static TaffyGridTrack Calc(TaffyCalcExpression expression) => new TaffyGridTrack { kind = TaffyGridTrackKind.Calc, calc = expression };

        public static TaffyGridTrack MinMax(TaffyGridTrackBreadth minimum, TaffyGridTrackBreadth maximum)
        {
            return new TaffyGridTrack { kind = TaffyGridTrackKind.MinMax, min = minimum, max = maximum };
        }

        public static TaffyGridTrack Repeat(TaffyGridRepeatMode mode, int count, params TaffyGridTrack[] tracks)
        {
            var result = new TaffyGridTrack
            {
                kind = TaffyGridTrackKind.Repeat,
                repeatMode = mode,
                repeatCount = count,
                repeatTracks = new List<TaffyGridTrack>(),
            };
            if (tracks != null)
                result.repeatTracks.AddRange(tracks);
            return result;
        }
    }

    [Serializable]
    public struct TaffyGridNamedLine
    {
        public TaffyGridAxis axis;
        [Min(0)] public int lineIndex;
        public string name;

        public TaffyGridNamedLine(TaffyGridAxis axis, int lineIndex, string name)
        {
            this.axis = axis;
            this.lineIndex = lineIndex;
            this.name = name;
        }
    }

    [Serializable]
    public struct TaffyGridArea
    {
        public string name;
        [Min(1)] public int rowStart;
        [Min(1)] public int rowEnd;
        [Min(1)] public int columnStart;
        [Min(1)] public int columnEnd;

        public TaffyGridArea(string name, int rowStart, int rowEnd, int columnStart, int columnEnd)
        {
            this.name = name;
            this.rowStart = rowStart;
            this.rowEnd = rowEnd;
            this.columnStart = columnStart;
            this.columnEnd = columnEnd;
        }
    }

    [Serializable]
    public struct TaffyGridPlacement
    {
        public TaffyGridPlacementKind kind;
        public int line;
        [Min(1)] public int span;
        public string name;
        public int occurrence;

        public static TaffyGridPlacement Auto => new TaffyGridPlacement { kind = TaffyGridPlacementKind.Auto };
        public static TaffyGridPlacement Line(int line) => new TaffyGridPlacement { kind = TaffyGridPlacementKind.Line, line = line };
        public static TaffyGridPlacement Span(int span) => new TaffyGridPlacement { kind = TaffyGridPlacementKind.Span, span = span };
        public static TaffyGridPlacement NamedLine(string name, int occurrence = 1) => new TaffyGridPlacement { kind = TaffyGridPlacementKind.NamedLine, name = name, occurrence = occurrence };
        public static TaffyGridPlacement NamedSpan(string name, int span = 1) => new TaffyGridPlacement { kind = TaffyGridPlacementKind.NamedSpan, name = name, span = span };

        internal bool TryValidate(string label, out string error)
        {
            switch (kind)
            {
                case TaffyGridPlacementKind.Auto:
                    error = null;
                    return true;
                case TaffyGridPlacementKind.Line:
                    if (line == 0 || line < short.MinValue || line > short.MaxValue)
                    {
                        error = $"{label} line must be non-zero and fit in a signed 16-bit Grid line index.";
                        return false;
                    }
                    error = null;
                    return true;
                case TaffyGridPlacementKind.Span:
                    if (span <= 0 || span > ushort.MaxValue)
                    {
                        error = $"{label} span must be in the range 1..{ushort.MaxValue}.";
                        return false;
                    }
                    error = null;
                    return true;
                case TaffyGridPlacementKind.NamedLine:
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        error = $"{label} named line requires a non-empty name.";
                        return false;
                    }
                    if (occurrence == 0 || occurrence < short.MinValue || occurrence > short.MaxValue)
                    {
                        error = $"{label} named-line occurrence must be non-zero and fit in a signed 16-bit value.";
                        return false;
                    }
                    error = null;
                    return true;
                case TaffyGridPlacementKind.NamedSpan:
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        error = $"{label} named span requires a non-empty name.";
                        return false;
                    }
                    if (span <= 0 || span > ushort.MaxValue)
                    {
                        error = $"{label} named span must be in the range 1..{ushort.MaxValue}.";
                        return false;
                    }
                    error = null;
                    return true;
                default:
                    error = $"{label} has unsupported placement kind value {(int)kind}.";
                    return false;
            }
        }

        internal TaffyNative.GridPlacement ToNative(TaffyNativeMarshallingScope scope, string label)
        {
            if (!TryValidate(label, out string error))
                throw new InvalidOperationException($"TaffyUGUI Grid authoring is invalid: {error}");

            return new TaffyNative.GridPlacement
            {
                kind = (int)kind,
                line = line,
                span = (uint)Mathf.Max(0, span),
                occurrence = occurrence,
                name = kind == TaffyGridPlacementKind.NamedLine || kind == TaffyGridPlacementKind.NamedSpan
                    ? scope.PinString(name)
                    : default,
            };
        }

        internal string Signature()
        {
            return $"{(int)kind}:{line}:{span}:{occurrence}:{name ?? string.Empty}";
        }
    }

    [Serializable]
    public struct TaffyGridItemInfo
    {
        public uint rowStart;
        public uint rowEnd;
        public uint columnStart;
        public uint columnEnd;
    }

    public sealed class TaffyGridDiagnostics
    {
        public uint negativeImplicitRows { get; internal set; }
        public uint explicitRows { get; internal set; }
        public uint positiveImplicitRows { get; internal set; }
        public uint negativeImplicitColumns { get; internal set; }
        public uint explicitColumns { get; internal set; }
        public uint positiveImplicitColumns { get; internal set; }
        public float[] rowTrackSizes { get; internal set; } = Array.Empty<float>();
        public float[] columnTrackSizes { get; internal set; } = Array.Empty<float>();
        public float[] rowGutters { get; internal set; } = Array.Empty<float>();
        public float[] columnGutters { get; internal set; } = Array.Empty<float>();
        public TaffyGridItemInfo[] items { get; internal set; } = Array.Empty<TaffyGridItemInfo>();
    }

    internal static class TaffyGridCompiler
    {
        internal static bool TryValidate(TaffyLayoutGroup group, out string error)
        {
            if (group == null)
            {
                error = "Grid container is null.";
                return false;
            }

            if (!TryBuildTrackList(group.gridRows, true, null, null, null, "gridRows", out _, out error) ||
                !TryBuildTrackList(group.gridColumns, true, null, null, null, "gridColumns", out _, out error) ||
                !TryBuildTrackList(group.gridAutoRows, false, null, null, null, "gridAutoRows", out _, out error) ||
                !TryBuildTrackList(group.gridAutoColumns, false, null, null, null, "gridAutoColumns", out _, out error))
                return false;

            if (!ValidateNamedLines(group.gridNamedLines, out error))
                return false;
            if (!ValidateAreas(group, out _, out _, out error))
                return false;

            for (int i = 0; i < group.transform.childCount; i++)
            {
                Transform child = group.transform.GetChild(i);
                TaffyLayoutItem item = child.GetComponent<TaffyLayoutItem>();
                if (!item)
                    continue;
                if (!item.TryValidateGridPlacement(child.name, out error))
                    return false;
            }

            error = null;
            return true;
        }

        internal static bool TryCompile(
            TaffyLayoutGroup group,
            TaffyCalcResourceCache calcResources,
            TaffyNativeMarshallingScope scope,
            out TaffyNative.GridTemplate template,
            out string signature,
            out string error)
        {
            template = default;
            signature = null;
            var key = new StringBuilder(512);

            if (!TryBuildTrackList(group.gridRows, true, calcResources, scope, key, "rows", out TaffyNative.GridTrack[] rows, out error) ||
                !TryBuildTrackList(group.gridColumns, true, calcResources, scope, key, "columns", out TaffyNative.GridTrack[] columns, out error) ||
                !TryBuildTrackList(group.gridAutoRows, false, calcResources, scope, key, "autoRows", out TaffyNative.GridTrack[] autoRows, out error) ||
                !TryBuildTrackList(group.gridAutoColumns, false, calcResources, scope, key, "autoColumns", out TaffyNative.GridTrack[] autoColumns, out error))
                return false;

            if (!TryBuildNamedLines(group.gridNamedLines, scope, key, out TaffyNative.NamedGridLine[] namedLines, out error))
                return false;
            if (!TryBuildAreas(group, scope, key, out TaffyNative.GridArea[] areas, out uint areaRows, out uint areaColumns, out error))
                return false;

            template = new TaffyNative.GridTemplate
            {
                rows = scope.PinArray(rows),
                rowCount = (uint)rows.Length,
                columns = scope.PinArray(columns),
                columnCount = (uint)columns.Length,
                autoRows = scope.PinArray(autoRows),
                autoRowCount = (uint)autoRows.Length,
                autoColumns = scope.PinArray(autoColumns),
                autoColumnCount = (uint)autoColumns.Length,
                namedLines = scope.PinArray(namedLines),
                namedLineCount = (uint)namedLines.Length,
                areas = scope.PinArray(areas),
                areaCount = (uint)areas.Length,
                areaRows = areaRows,
                areaColumns = areaColumns,
            };
            signature = key.ToString();
            error = null;
            return true;
        }

        private static bool TryBuildTrackList(
            List<TaffyGridTrack> source,
            bool allowRepeat,
            TaffyCalcResourceCache calcResources,
            TaffyNativeMarshallingScope scope,
            StringBuilder signature,
            string label,
            out TaffyNative.GridTrack[] result,
            out string error)
        {
            int count = source == null ? 0 : source.Count;
            result = count == 0 ? Array.Empty<TaffyNative.GridTrack>() : new TaffyNative.GridTrack[count];
            signature?.Append(label).Append('{');
            for (int i = 0; i < count; i++)
            {
                if (!TryBuildTrack(source[i], allowRepeat, calcResources, scope, signature, $"{label}[{i}]", out result[i], out error))
                    return false;
                signature?.Append(';');
            }
            signature?.Append('}');
            error = null;
            return true;
        }

        private static bool TryBuildTrack(
            TaffyGridTrack track,
            bool allowRepeat,
            TaffyCalcResourceCache calcResources,
            TaffyNativeMarshallingScope scope,
            StringBuilder signature,
            string label,
            out TaffyNative.GridTrack result,
            out string error)
        {
            result = default;
            if (track == null)
            {
                error = $"{label} is null.";
                return false;
            }

            signature?.Append((int)track.kind).Append(':');
            switch (track.kind)
            {
                case TaffyGridTrackKind.Auto:
                case TaffyGridTrackKind.MinContent:
                case TaffyGridTrackKind.MaxContent:
                    result.kind = (int)track.kind;
                    break;
                case TaffyGridTrackKind.Points:
                case TaffyGridTrackKind.Percent:
                case TaffyGridTrackKind.Fraction:
                    if (!TryNonNegativeFinite(track.value, label, out error))
                        return false;
                    result.kind = (int)track.kind;
                    result.value = track.value;
                    signature?.Append(track.value.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case TaffyGridTrackKind.Calc:
                    if (!TryCalc(track.calc, label, calcResources, out ulong calc, out string calcKey, out error))
                        return false;
                    result.kind = (int)track.kind;
                    result.resource = calc;
                    signature?.Append(calcKey);
                    break;
                case TaffyGridTrackKind.MinMax:
                    result.kind = (int)track.kind;
                    if (!TryBuildBreadth(track.min, true, calcResources, signature, label + ".min", out result.minKind, out result.minValue, out result.minResource, out error) ||
                        !TryBuildBreadth(track.max, false, calcResources, signature, label + ".max", out result.maxKind, out result.maxValue, out result.maxResource, out error))
                        return false;
                    break;
                case TaffyGridTrackKind.Repeat:
                    if (!allowRepeat)
                    {
                        error = $"{label} cannot use Repeat because implicit/repeated child tracks must be concrete sizing tracks.";
                        return false;
                    }
                    if (track.repeatMode == TaffyGridRepeatMode.Count && (track.repeatCount <= 0 || track.repeatCount > ushort.MaxValue))
                    {
                        error = $"{label} repeat count must be in the range 1..{ushort.MaxValue}.";
                        return false;
                    }
                    if (track.repeatTracks == null || track.repeatTracks.Count == 0)
                    {
                        error = $"{label} Repeat requires at least one nested track.";
                        return false;
                    }
                    result.kind = (int)track.kind;
                    result.repeatMode = (int)track.repeatMode;
                    result.repeatCount = (uint)Mathf.Max(0, track.repeatCount);
                    signature?.Append("repeat=").Append((int)track.repeatMode).Append(':').Append(track.repeatCount).Append('[');
                    if (!TryBuildTrackList(track.repeatTracks, false, calcResources, scope, signature, label + ".repeatTracks", out TaffyNative.GridTrack[] repeated, out error))
                        return false;
                    if (scope != null)
                    {
                        result.repeatTracks = scope.PinArray(repeated);
                        result.repeatTrackCount = (uint)repeated.Length;
                    }
                    signature?.Append(']');
                    break;
                default:
                    error = $"{label} has unsupported track kind value {(int)track.kind}.";
                    return false;
            }

            error = null;
            return true;
        }

        private static bool TryBuildBreadth(
            TaffyGridTrackBreadth breadth,
            bool minimum,
            TaffyCalcResourceCache calcResources,
            StringBuilder signature,
            string label,
            out int kind,
            out float value,
            out ulong resource,
            out string error)
        {
            kind = (int)breadth.kind;
            value = 0f;
            resource = 0;
            signature?.Append(label).Append('=').Append(kind).Append(':');

            if (minimum && breadth.kind == TaffyGridTrackBreadthKind.Fraction)
            {
                error = $"{label} cannot use Fraction as a minmax minimum.";
                return false;
            }

            switch (breadth.kind)
            {
                case TaffyGridTrackBreadthKind.Auto:
                case TaffyGridTrackBreadthKind.MinContent:
                case TaffyGridTrackBreadthKind.MaxContent:
                    break;
                case TaffyGridTrackBreadthKind.Points:
                case TaffyGridTrackBreadthKind.Percent:
                case TaffyGridTrackBreadthKind.Fraction:
                    if (!TryNonNegativeFinite(breadth.value, label, out error))
                        return false;
                    value = breadth.value;
                    signature?.Append(value.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case TaffyGridTrackBreadthKind.Calc:
                    if (!TryCalc(breadth.calc, label, calcResources, out resource, out string calcKey, out error))
                        return false;
                    signature?.Append(calcKey);
                    break;
                default:
                    error = $"{label} has unsupported track breadth kind value {(int)breadth.kind}.";
                    return false;
            }

            error = null;
            return true;
        }

        private static bool TryCalc(
            TaffyCalcExpression expression,
            string label,
            TaffyCalcResourceCache resources,
            out ulong handle,
            out string key,
            out string error)
        {
            handle = 0;
            key = null;
            if (expression == null)
            {
                error = $"{label} requires a Calc expression.";
                return false;
            }
            if (!expression.TryGetCanonicalKey(out key, out error))
            {
                error = $"{label}: {error}";
                return false;
            }
            if (resources != null)
                handle = resources.Resolve(expression);
            error = null;
            return true;
        }

        private static bool ValidateNamedLines(List<TaffyGridNamedLine> source, out string error)
        {
            int count = source == null ? 0 : source.Count;
            for (int i = 0; i < count; i++)
            {
                TaffyGridNamedLine line = source[i];
                if (line.lineIndex < 0 || line.lineIndex > ushort.MaxValue)
                {
                    error = $"gridNamedLines[{i}] lineIndex must be in the range 0..{ushort.MaxValue}.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(line.name))
                {
                    error = $"gridNamedLines[{i}] requires a non-empty name.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static bool TryBuildNamedLines(
            List<TaffyGridNamedLine> source,
            TaffyNativeMarshallingScope scope,
            StringBuilder signature,
            out TaffyNative.NamedGridLine[] result,
            out string error)
        {
            if (!ValidateNamedLines(source, out error))
            {
                result = Array.Empty<TaffyNative.NamedGridLine>();
                return false;
            }

            int count = source == null ? 0 : source.Count;
            result = count == 0 ? Array.Empty<TaffyNative.NamedGridLine>() : new TaffyNative.NamedGridLine[count];
            signature.Append("lines{");
            for (int i = 0; i < count; i++)
            {
                TaffyGridNamedLine line = source[i];
                result[i] = new TaffyNative.NamedGridLine
                {
                    axis = (int)line.axis,
                    lineIndex = (uint)line.lineIndex,
                    name = scope.PinString(line.name),
                };
                signature.Append((int)line.axis).Append(':').Append(line.lineIndex).Append(':').Append(line.name).Append(';');
            }
            signature.Append('}');
            return true;
        }

        private static bool ValidateAreas(
            TaffyLayoutGroup group,
            out uint areaRows,
            out uint areaColumns,
            out string error)
        {
            int inferredRows = group.gridAreaRows > 0 ? group.gridAreaRows : group.gridRows?.Count ?? 0;
            int inferredColumns = group.gridAreaColumns > 0 ? group.gridAreaColumns : group.gridColumns?.Count ?? 0;
            if (inferredRows < 0 || inferredRows > ushort.MaxValue || inferredColumns < 0 || inferredColumns > ushort.MaxValue)
            {
                areaRows = areaColumns = 0;
                error = $"Grid template area dimensions must fit in 0..{ushort.MaxValue}.";
                return false;
            }

            areaRows = (uint)inferredRows;
            areaColumns = (uint)inferredColumns;
            int count = group.gridAreas == null ? 0 : group.gridAreas.Count;
            if (count == 0)
            {
                error = null;
                return true;
            }
            if (areaRows == 0 || areaColumns == 0)
            {
                error = "Grid template areas require positive area row/column dimensions or inferable explicit tracks.";
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                TaffyGridArea area = group.gridAreas[i];
                if (string.IsNullOrWhiteSpace(area.name))
                {
                    error = $"gridAreas[{i}] requires a non-empty name.";
                    return false;
                }
                if (!names.Add(area.name))
                {
                    error = $"gridAreas contains duplicate area name '{area.name}'.";
                    return false;
                }
                if (area.rowStart <= 0 || area.columnStart <= 0 ||
                    area.rowEnd <= area.rowStart || area.columnEnd <= area.columnStart ||
                    area.rowEnd > areaRows + 1 || area.columnEnd > areaColumns + 1)
                {
                    error = $"gridAreas[{i}] '{area.name}' is outside the {areaRows}x{areaColumns} template area bounds or has an empty span.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryBuildAreas(
            TaffyLayoutGroup group,
            TaffyNativeMarshallingScope scope,
            StringBuilder signature,
            out TaffyNative.GridArea[] result,
            out uint areaRows,
            out uint areaColumns,
            out string error)
        {
            if (!ValidateAreas(group, out areaRows, out areaColumns, out error))
            {
                result = Array.Empty<TaffyNative.GridArea>();
                return false;
            }

            int count = group.gridAreas == null ? 0 : group.gridAreas.Count;
            result = count == 0 ? Array.Empty<TaffyNative.GridArea>() : new TaffyNative.GridArea[count];
            signature.Append("areas=").Append(areaRows).Append('x').Append(areaColumns).Append('{');
            for (int i = 0; i < count; i++)
            {
                TaffyGridArea area = group.gridAreas[i];
                result[i] = new TaffyNative.GridArea
                {
                    name = scope.PinString(area.name),
                    rowStart = (uint)area.rowStart,
                    rowEnd = (uint)area.rowEnd,
                    columnStart = (uint)area.columnStart,
                    columnEnd = (uint)area.columnEnd,
                };
                signature.Append(area.name).Append(':')
                    .Append(area.rowStart).Append('-').Append(area.rowEnd).Append(':')
                    .Append(area.columnStart).Append('-').Append(area.columnEnd).Append(';');
            }
            signature.Append('}');
            return true;
        }

        private static bool TryNonNegativeFinite(float value, string label, out string error)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                error = $"{label} requires a finite non-negative value.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
