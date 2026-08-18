using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TaffyUGUI
{
    public enum TaffyCalcOperation
    {
        Length = 0,
        Percent = 1,
        Add = 2,
        Subtract = 3,
        Scale = 4,
        Min = 5,
        Max = 6,
        Clamp = 7,
    }

    [Serializable]
    public sealed class TaffyCalcExpression
    {
        public TaffyCalcOperation operation = TaffyCalcOperation.Length;
        public float value;
        [SerializeReference]
        public List<TaffyCalcExpression> operands = new List<TaffyCalcExpression>();

        public static TaffyCalcExpression Length(float points)
        {
            return new TaffyCalcExpression { operation = TaffyCalcOperation.Length, value = points };
        }

        public static TaffyCalcExpression Percent(float fraction)
        {
            return new TaffyCalcExpression { operation = TaffyCalcOperation.Percent, value = fraction };
        }

        public static TaffyCalcExpression Add(TaffyCalcExpression left, TaffyCalcExpression right)
        {
            return Composite(TaffyCalcOperation.Add, 0f, left, right);
        }

        public static TaffyCalcExpression Subtract(TaffyCalcExpression left, TaffyCalcExpression right)
        {
            return Composite(TaffyCalcOperation.Subtract, 0f, left, right);
        }

        public static TaffyCalcExpression Scale(TaffyCalcExpression expression, float factor)
        {
            return Composite(TaffyCalcOperation.Scale, factor, expression);
        }

        public static TaffyCalcExpression Min(params TaffyCalcExpression[] values)
        {
            return Composite(TaffyCalcOperation.Min, 0f, values);
        }

        public static TaffyCalcExpression Max(params TaffyCalcExpression[] values)
        {
            return Composite(TaffyCalcOperation.Max, 0f, values);
        }

        public static TaffyCalcExpression Clamp(
            TaffyCalcExpression minimum,
            TaffyCalcExpression preferred,
            TaffyCalcExpression maximum)
        {
            return Composite(TaffyCalcOperation.Clamp, 0f, minimum, preferred, maximum);
        }

        internal bool TryGetCanonicalKey(out string key, out string error)
        {
            var builder = new StringBuilder(128);
            var active = new HashSet<TaffyCalcExpression>();
            if (!TryAppendCanonicalKey(builder, active, out error))
            {
                key = null;
                return false;
            }

            key = builder.ToString();
            return true;
        }

        internal bool TryValidate(out string error)
        {
            return TryGetCanonicalKey(out _, out error);
        }

        private bool TryAppendCanonicalKey(
            StringBuilder builder,
            HashSet<TaffyCalcExpression> active,
            out string error)
        {
            if (!active.Add(this))
            {
                error = "Calc expressions cannot contain cycles.";
                return false;
            }

            try
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    error = $"Calc {operation} contains a non-finite value.";
                    return false;
                }

                int count = operands == null ? 0 : operands.Count;
                switch (operation)
                {
                    case TaffyCalcOperation.Length:
                    case TaffyCalcOperation.Percent:
                        if (count != 0)
                        {
                            error = $"Calc {operation} does not accept operands.";
                            return false;
                        }
                        break;
                    case TaffyCalcOperation.Add:
                    case TaffyCalcOperation.Subtract:
                        if (count != 2)
                        {
                            error = $"Calc {operation} requires exactly 2 operands.";
                            return false;
                        }
                        break;
                    case TaffyCalcOperation.Scale:
                        if (count != 1)
                        {
                            error = "Calc Scale requires exactly 1 operand.";
                            return false;
                        }
                        break;
                    case TaffyCalcOperation.Min:
                    case TaffyCalcOperation.Max:
                        if (count == 0)
                        {
                            error = $"Calc {operation} requires at least 1 operand.";
                            return false;
                        }
                        break;
                    case TaffyCalcOperation.Clamp:
                        if (count != 3)
                        {
                            error = "Calc Clamp requires minimum, preferred, and maximum operands.";
                            return false;
                        }
                        break;
                    default:
                        error = $"Unsupported Calc operation value {(int)operation}.";
                        return false;
                }

                builder.Append((int)operation)
                    .Append(':')
                    .Append(value.ToString("R", CultureInfo.InvariantCulture))
                    .Append('[');

                for (int i = 0; i < count; i++)
                {
                    TaffyCalcExpression operand = operands[i];
                    if (operand == null)
                    {
                        error = $"Calc {operation} operand {i} is null.";
                        return false;
                    }

                    if (i != 0)
                        builder.Append(',');
                    if (!operand.TryAppendCanonicalKey(builder, active, out error))
                        return false;
                }

                builder.Append(']');
                error = null;
                return true;
            }
            finally
            {
                active.Remove(this);
            }
        }

        private static TaffyCalcExpression Composite(
            TaffyCalcOperation operation,
            float value,
            params TaffyCalcExpression[] values)
        {
            var expression = new TaffyCalcExpression
            {
                operation = operation,
                value = value,
                operands = new List<TaffyCalcExpression>(),
            };
            if (values != null)
                expression.operands.AddRange(values);
            return expression;
        }
    }

    internal sealed class TaffyCalcResourceCache
    {
        private sealed class Entry
        {
            internal string key;
            internal ulong handle;
            internal bool used;
        }

        private ulong _context;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private readonly List<Entry> _creationOrder = new List<Entry>();

        internal int ResourceCount => _entries.Count;

        internal void Attach(ulong context)
        {
            if (_context == context)
                return;
            _context = context;
            _entries.Clear();
            _creationOrder.Clear();
        }

        internal void Detach()
        {
            _context = 0;
            _entries.Clear();
            _creationOrder.Clear();
        }

        internal void BeginPass(ulong context)
        {
            Attach(context);
            for (int i = 0; i < _creationOrder.Count; i++)
                _creationOrder[i].used = false;
        }

        internal ulong Resolve(TaffyCalcExpression expression)
        {
            if (_context == 0)
                throw new InvalidOperationException("TaffyUGUI cannot create Calc resources without an active native context.");
            if (expression == null)
                throw new InvalidOperationException("TaffyUGUI Calc authoring requires a non-null expression.");
            if (!expression.TryGetCanonicalKey(out string key, out string error))
                throw new InvalidOperationException($"TaffyUGUI Calc authoring is invalid: {error}");

            return Resolve(expression, key);
        }

        internal void EndPass()
        {
            for (int i = _creationOrder.Count - 1; i >= 0; i--)
            {
                Entry entry = _creationOrder[i];
                if (entry.used)
                    continue;

                TaffyNative.Check(TaffyNative.tu_calc_remove(_context, entry.handle), "release unused Calc resource");
                _entries.Remove(entry.key);
                _creationOrder.RemoveAt(i);
            }
        }

        private ulong Resolve(TaffyCalcExpression expression, string key)
        {
            if (_entries.TryGetValue(key, out Entry existing))
            {
                existing.used = true;
                MarkOperandsUsed(expression);
                return existing.handle;
            }

            int count = expression.operands == null ? 0 : expression.operands.Count;
            var operands = count == 0 ? Array.Empty<ulong>() : new ulong[count];
            for (int i = 0; i < count; i++)
                operands[i] = Resolve(expression.operands[i]);

            using (var scope = new TaffyNativeMarshallingScope())
            {
                var spec = new TaffyNative.CalcSpec
                {
                    op = (int)expression.operation,
                    value = expression.value,
                    operands = scope.PinArray(operands),
                    operandCount = (uint)operands.Length,
                };
                TaffyNative.Check(TaffyNative.tu_calc_create(_context, ref spec, out ulong handle), "create Calc resource");
                if (handle == 0)
                    throw new InvalidOperationException("TaffyUGUI native Calc creation returned a null handle.");

                var entry = new Entry { key = key, handle = handle, used = true };
                _entries.Add(key, entry);
                _creationOrder.Add(entry);
                return handle;
            }
        }

        private void MarkOperandsUsed(TaffyCalcExpression expression)
        {
            if (expression.operands == null)
                return;

            for (int i = 0; i < expression.operands.Count; i++)
            {
                TaffyCalcExpression operand = expression.operands[i];
                if (operand == null || !operand.TryGetCanonicalKey(out string key, out _))
                    continue;
                if (_entries.TryGetValue(key, out Entry entry))
                {
                    entry.used = true;
                    MarkOperandsUsed(operand);
                }
            }
        }
    }
}
