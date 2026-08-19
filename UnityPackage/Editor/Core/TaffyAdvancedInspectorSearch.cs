using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TaffyUGUI.Editor
{
    internal static class TaffyAdvancedInspectorSearch
    {
        private static readonly Dictionary<string, string> Queries = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> SectionKeywords = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Group.Formatting", "formatting display box sizing writing direction overflow clip scrollbar padding border text align center" },
            { "Group.Flex", "flex direction row column wrap gap spacing justify align alignment center main cross axis" },
            { "Group.Grid", "grid row rows column columns track tracks flow named line area gap placement" },
            { "Group.Responsive", "responsive profile breakpoint safe area scroll pixel rounding rebuild" },
            { "Item.Display", "display box sizing writing direction overflow clip scrollbar" },
            { "Item.PositionSize", "position inset size width height min max aspect ratio percent fixed auto" },
            { "Item.BoxModel", "box model margin padding border spacing inset" },
            { "Item.Flex", "flex basis grow shrink align alignment center" },
            { "Item.Grid", "grid row column placement span justify align alignment center" },
            { "Item.Block", "block float clear text align alignment center" },
            { "Item.Measurement", "measurement intrinsic content replaced table auto fit" },
        };

        internal static string Draw(string inspectorKey)
        {
            string current = Get(inspectorKey);
            EditorGUILayout.Space(2f);
            string next = EditorGUILayout.TextField("Search Advanced", current);
            if (!string.Equals(next, current, StringComparison.Ordinal))
                Queries[inspectorKey] = next ?? string.Empty;
            return next ?? string.Empty;
        }

        internal static string Get(string inspectorKey)
        {
            return Queries.TryGetValue(inspectorKey ?? string.Empty, out string value) ? value : string.Empty;
        }

        internal static void Set(string inspectorKey, string query)
        {
            Queries[inspectorKey ?? string.Empty] = query ?? string.Empty;
        }

        internal static bool Matches(string inspectorKey, string sectionKey, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string key = (inspectorKey ?? string.Empty) + "." + (sectionKey ?? string.Empty);
            if (!SectionKeywords.TryGetValue(key, out string keywords))
                keywords = key;

            string[] tokens = ExpandAliases(query);
            string haystack = keywords.ToLowerInvariant();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (haystack.IndexOf(tokens[i], StringComparison.Ordinal) < 0)
                    return false;
            }
            return true;
        }

        internal static string[] ExpandAliases(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<string>();

            string[] raw = query.ToLowerInvariant().Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                switch (raw[i])
                {
                    case "clip": result.Add("overflow"); break;
                    case "center": result.Add("align"); break;
                    case "spacing": result.Add("spacing"); break;
                    case "size": result.Add("size"); break;
                    default: result.Add(raw[i]); break;
                }
            }
            return result.ToArray();
        }
    }
}
