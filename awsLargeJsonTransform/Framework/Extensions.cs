using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ca.awsLargeJsonTransform.Framework
{
    internal static class Extensions
    {
        public static string ToJson(this object value) => JsonConvert.SerializeObject(value);

#pragma warning disable CS8603 // Possible null reference return.
        public static T FromJson<T>(this string json) where T : class => JsonConvert.DeserializeObject<T>(json);
#pragma warning restore CS8603 // Possible null reference return.

        public static string GetCsvColumnValue(this string dataValue)
        {
            if (string.IsNullOrEmpty(dataValue)) { return ""; }
            return $"\"{dataValue?.ToString()}\"";
        }
        public static string GetCsvColumnValue(this DateTime? dataValue)
        {
            return dataValue.HasValue ? dataValue.Value.ToString("MM/dd/yyyy hh:mm:ss t") : "";
        }
        public static string GetCsvColumnValue(this bool? dataValue)
        {
            return dataValue.HasValue ? dataValue.Value.ToString() : "";
        }
        public static string GetCsvColumnValue(this int? dataValue)
        {
            return dataValue.HasValue ? dataValue.Value.ToString() : "";
        }
        public static string GetCsvColumnValue(this long? dataValue)
        {
            return dataValue.HasValue ? dataValue.Value.ToString() : "";
        }
        public static string GetCsvColumnValue(this float? dataValue)
        {
            return dataValue.HasValue ? dataValue.Value.ToString() : "";
        }

        public static string GetCsvColumnValue(this double? dataValue)
        {
            return dataValue.HasValue ? dataValue.Value.ToString() : "";
        }

        public static void SetPropertyValue<T>(this PropertyInfo property, T obj, object value)
        {
            if (value == DBNull.Value || value == null) { return; }
            if (property.PropertyType.Equals(typeof(bool)))
            {
                property.SetValue(obj, Convert.ToBoolean(value), null);
            }
            else if (property.PropertyType.Equals(typeof(int)))
            {
                property.SetValue(obj, Convert.ToInt32(value), null);
            }
            else if (property.PropertyType.Equals(typeof(long)))
            {
                property.SetValue(obj, Convert.ToInt64(value), null);
            }
            else if (property.PropertyType.Equals(typeof(DateTime)))
            {
                property.SetValue(obj, DateTime.Parse(value.ToString()), null);
            }
            else
            {
                property.SetValue(obj, value.ToString(), null);
            }
        }

    }
}
