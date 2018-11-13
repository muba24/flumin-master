using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Flumin {
    static class Extensions {

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action) {
            foreach (T item in enumeration) {
                action(item);
            }
        }

        public static string TryGetAttribute(this XmlNode node, string name, string otherwise) {
            return node?.Attributes?.GetNamedItem(name)?.Value ?? otherwise;
        }

        public static string GetQualifier(this Node node) {
            if (string.IsNullOrEmpty(node.Description)) {
                return node.Name;
            }

            if (string.CompareOrdinal(node.Description, node.Name) == 0) {
                return node.Name;
            }

            return $"{node.Description} ({node.Name})";
        }

    }
}
