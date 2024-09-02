using System.Collections;
using System.IO;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Ara3D.Speckle.Data
{
    public static class JsonSerializer
    {
        public static JsonWriter WriteProperty(this JsonWriter jw, object o, string fieldName)
        {
            jw.WritePropertyName(fieldName);
            var pi = o.GetType().GetProperty(fieldName);
            jw.SafeWriteValue(pi.GetValue(o));
            return jw;
        }

        public static JsonWriter SafeWriteValue(this JsonWriter jw, object o)
        {
            if (o == null)
                jw.WriteNull();
            else if (o is string s)
                jw.WriteValue(s);
            else if (o is double d)
                jw.WriteValue(d);
            else if (o is float f)
                jw.WriteValue((double)f);
            else if (o is int i)
                jw.WriteValue(i);
            else if (o is uint ui)
                jw.WriteValue(ui);
            else if (o is long l)
                jw.WriteValue(l);
            else if (o is ulong ul)
                jw.WriteValue(ul);
            else if (o is bool b)
                jw.WriteValue(b);
            else if (o is IDictionary dict)
            {
                jw.WriteStartObject();
                foreach (var k in dict.Keys)
                {
                    jw.WritePropertyName((string)k);
                    var v = dict[k];
                    jw.SafeWriteValue(v);
                }
                jw.WriteEndObject();
            }
            else if (o is IEnumerable es)
            {
                jw.WriteStartArray();
                foreach (var e in es)
                    jw.SafeWriteValue(e);
                jw.WriteEndArray();
            }
            else if (o is Base speckleBase)
            {
                jw.WriteValue($"Speckle${speckleBase.id}");
            }
            else
                jw.WriteValue(o.ToString());
            return jw;
        }

        public static string ToJson(this NativeObject nativeObject)
        {
            var sw = new StringWriter();
            var jw = new JsonTextWriter(sw);
            jw.Formatting = Formatting.Indented;
            jw.Indentation = 2;
            nativeObject.Write(jw);
            return sw.ToString();
        }

        public static JsonWriter Write(this NativeObject nativeObject, JsonWriter jw)
        {
            jw.WriteStartObject();
            jw.WriteProperty(nativeObject, nameof(nativeObject.Id));
            jw.WriteProperty(nativeObject, nameof(nativeObject.Name));
            jw.WriteProperty(nativeObject, nameof(nativeObject.CollectionType));
            jw.WriteProperty(nativeObject, nameof(nativeObject.SpeckleType));
            jw.WriteProperty(nativeObject, nameof(nativeObject.DotNetType));
            jw.WriteProperty(nativeObject, nameof(nativeObject.BasePoint));
            jw.WriteProperty(nativeObject, nameof(nativeObject.IsInstanced));
            jw.WriteProperty(nativeObject, nameof(nativeObject.InstanceDefinitionId));
            jw.WritePropertyName("children");
            jw.WriteStartArray();
            foreach (var c in nativeObject.Children)
                jw = Write(c, jw);
            jw.WriteEndArray();

            jw.WritePropertyName("members");
            jw.WriteStartObject();

            if (nativeObject.SpeckleType == "Objects.Geometry.Mesh")
            {
                // NOTE: we skip the members, because they are VERY long. 
                // Maybe, we could just skip 'detachable' members. 
                // Note: some detachable members, don't have the '@' symbol in front of them which is inconsistent. 
            }
            else
            {
                foreach (var member in nativeObject.Members)
                {
                    jw.WritePropertyName(member.Key);
                    jw.SafeWriteValue(member.Value);
                }
            }

            jw.WriteEndObject();

            jw.WriteEndObject();
            return jw;
        }
    }
}