using System;
namespace Neon.Util
{
    public static class EnvironmentExtensions
    {
        public static bool GetEnvironmentVariableBool(string name, bool defaultValue = false)
        {
            string v = Environment.GetEnvironmentVariable(name);
            if (v == null)
                return defaultValue;

            if (bool.TryParse(v.ToLower(), out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        public static long GetEnvironmentVariableLong(string name, long defaultValue = 0)
        {
            string v = Environment.GetEnvironmentVariable(name);
            if (v == null)
                return defaultValue;

            if (long.TryParse(v, out long result))
            {
                return result;
            }
            return defaultValue;
        }

        public static int GetEnvironmentVariableInt(string name, int defaultValue = 0)
        {
            string v = Environment.GetEnvironmentVariable(name);
            if (v == null)
                return defaultValue;

            if (int.TryParse(v, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        public static int GetEnvironmentVariableIntOrFail(string name)
        {
            string v = Environment.GetEnvironmentVariable(name);
            if (v == null)
                throw new ArgumentException($"Environment variable {name} not set");

            if (int.TryParse(v, out int result))
            {
                return result;
            }

            throw new ArgumentException($"Environment variable {name} not should be int");
        }

        public static string GetEnvironmentVariableStr(string name, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            {
                return defaultValue;
            }
            return Environment.GetEnvironmentVariable(name);
        }

        public static string GetEnvironmentVariableStrOrFail(string name)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                throw new ArgumentException($"Environment variable {name} not set");
            return Environment.GetEnvironmentVariable(name);
        }

        public static bool GetEnvironmentVariableBoolOrFail(string name)
        {
            string v = Environment.GetEnvironmentVariable(name);
            if (v == null)
                throw new ArgumentException($"Environment variable {name} not set");

            if (bool.TryParse(v.ToLower(), out bool result))
            {
                return result;
            }
            throw new ArgumentException($"Environment variable {name} isn't bool");
        }
    }
}
