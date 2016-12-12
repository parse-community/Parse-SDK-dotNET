using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal static class DictionaryEngine
    {
        internal static IDictionary<string, object> Merge(this IDictionary<string, object> dataLeft, IDictionary<string, object> dataRight)
        {
            if (dataRight == null)
                return dataLeft;
            foreach (var kv in dataRight)
            {
                if (dataLeft.ContainsKey(kv.Key))
                {
                    dataLeft[kv.Key] = kv.Value;
                }
                else
                {
                    dataLeft.Add(kv);
                }
            }
            return dataLeft;
        }

        internal static object Grab(this IDictionary<string, object> data, string path)
        {
            var keys = path.Split('.').ToList<string>();
            if (keys.Count == 1) return data[keys[0]];

            var deep = data[keys[0]] as IDictionary<string, object>;

            keys.RemoveAt(0);
            string deepPath = string.Join(".", keys);

            return Grab(deep, deepPath);
        }
    }
}
