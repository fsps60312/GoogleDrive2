using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace GoogleDrive2.Libraries
{
    static class MySerializer
    {
        static List<string> GetInfoAsStringList(object o,bool includeFields,bool includeProperties,ref HashSet<object>used)
        {
            if (used.Contains(o)) return new List<string> { "(Repeated)" };
            used.Add(o);
            {
                if (o is System.Collections.IEnumerable && !(o is string))
                {
                    var ans = new List<string>();
                    int idx = 0;
                    foreach (var nxto in (System.Collections.IEnumerable)o)
                    {
                        ans.Add($"{($"[{idx++}]:").PadRight(15)} {nxto}");
                        if (nxto != null)
                        {
                            foreach (var s in GetInfoAsStringList(nxto, includeFields, includeProperties, ref used))
                            {
                                ans.Add($"    {s}");
                            }
                        }
                    }
                    return ans;
                }
            }
            {
                List<string> ans = new List<string>();
                if (includeFields)
                {
                    var fs = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    foreach (var f in fs)
                    {
                        object nxto;
                        try
                        {
                            nxto = f.GetValue(o);
                        }
                        catch (Exception error)
                        {
                            nxto = error.ToString().Replace("\r", "\\r").Replace("\n", "\\n");
                        }
                        ans.Add($"{(f.Name + ":").PadRight(15)} {nxto}");
                        if (nxto != null)
                        {
                            foreach (var s in GetInfoAsStringList(nxto, includeFields, includeProperties, ref used))
                            {
                                ans.Add($"    {s}");
                            }
                        }
                    }
                }
                if(includeProperties)
                {
                    var fs = o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    foreach (var f in fs)
                    {
                        object nxto;
                        try
                        {
                            nxto = f.GetValue(o);
                        }
                        catch (Exception error)
                        {
                            nxto = error.ToString().Replace("\r","\\r").Replace("\n","\\n");
                        }
                        ans.Add($"{(f.Name + ":").PadRight(15)} {nxto}");
                        if (nxto != null)
                        {
                            foreach (var s in GetInfoAsStringList(nxto, includeFields, includeProperties, ref used))
                            {
                                ans.Add($"    {s}");
                            }
                        }
                    }
                }
                return ans;
            }
        }
        public static string SerializeProperties(object o)
        {
            var used = new HashSet<object>();
            return string.Join("\r\n", GetInfoAsStringList(o, false, true, ref used));
        }
        public static string SerializeFields(object o)
        {
            var used = new HashSet<object>();
            return string.Join("\r\n", GetInfoAsStringList(o, true, false, ref used));
        }
    }
}
