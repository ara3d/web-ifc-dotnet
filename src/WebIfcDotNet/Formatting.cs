using System;
using System.Collections.Generic;
using System.Text;
using WebIfcClrWrapper;

namespace WebIfcDotNet
{
    public static class Formatting
    {
        public static string IfcValToString(this object obj)
        {
            switch (obj)
            {
                case List<object> list:
                    return IfcValToString(list);
                case LabelValue lv:
                    return $"{lv.Type}{IfcValToString(lv.Arguments)}";
                case EnumValue ev:
                    return $".{ev.Name ?? "??"}.";
                case RefValue rv:
                    return $"#{rv.ExpressId}";
                case string s:
                    return $"\"{s}\"";
                case null:
                    return "$";
                default:
                    return obj.ToString() ?? "";
            }
        }

        public static string IfcValToString(this List<object> args)
        {
            var sb = new StringBuilder();
            var first = true;
            sb.Append('(');
            foreach (var a in args)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append(IfcValToString(a));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public static string IfcValToString(this LineData lineData)
        {
            return $"#{lineData.ExpressId}={lineData.TypeStr()}({IfcValToString(lineData.Arguments)})";
        }

        public static string TypeStr(this LineData lineData)
        {
            return DotNetApi.GetNameFromTypeCode(lineData.TypeCode);
        }
    }
}
