using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;

namespace Neon.Logging.Formatters
{
    public class LoggingFormatterJson : LoggingFormatterDefault
    {
        /// <summary>
        ///     Converts any incoming object with meta to the log string
        /// </summary>
        /// <param name="severity">Final row severity</param>
        /// <param name="message">Object to format</param>
        /// <param name="meta">Final meta information</param>
        /// <param name="logger">Parent logger</param>
        /// <returns>Log string</returns>
        public override string Format(LogSeverity severity, object message, Exception exception, LoggingMeta meta,
            ILogger logger)
        {
            string messageFormatted = base.Format(severity, message, exception, meta, logger).Replace("\"", "\\\"")
                .Replace("\n", "\\n");
            StringBuilder stringBuilder = GetStringBuilder();
            try
            {
                stringBuilder.AppendFormat("{{\"severity\":\"{0}\",", severity);
                stringBuilder.AppendFormat("\"message\":\"{0}\",",
                    HttpUtility.JavaScriptStringEncode(messageFormatted, false));
                stringBuilder.AppendFormat("\"timestamp\":\"{0}\",",
                    DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff000Z",
                        DateTimeFormatInfo.InvariantInfo));
                stringBuilder.AppendFormat("\"loggerName\":\"{0}\"", logger.Name);

                if (meta.Count > 0)
                {
                    stringBuilder.Append(",\"labels\":{");
                    var first = true;
                    foreach (KeyValuePair<string, object> metaPair in meta)
                    {
                        string value = null;
                        switch (metaPair.Value)
                        {
                            case DateTime dt:
                                value = string.Format("\"{0}\"",
                                    DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff000Z",
                                        DateTimeFormatInfo.InvariantInfo));
                                break;
                            case sbyte _:
                            case byte _:
                            case short _:
                            case ushort _:
                            case int _:
                            case uint _:
                            case long _:
                            case ulong _:
                                value = metaPair.Value.ToString();
                                break;
                            case bool bl:
                                value = bl ? "true" : "false";
                                break;
                            case float f:
                                value = f.ToString(CultureInfo.InvariantCulture);
                                break;
                            case double d:
                                value = d.ToString(CultureInfo.InvariantCulture);
                                break;
                            default:
                                value = HttpUtility.JavaScriptStringEncode(metaPair.Value.ToString(), true);
                                break;
                        }

                        if (!first)
                            stringBuilder.Append(",");

                        stringBuilder.AppendFormat("\"{0}\":{1}",
                            HttpUtility.JavaScriptStringEncode(metaPair.Key, false), value);
                        first = false;
                    }

                    stringBuilder.Append("}");
                }

                stringBuilder.Append("}");

                return stringBuilder.ToString();
            }
            finally
            {
                stringBuilder.Clear();
            }
        }
    }
}