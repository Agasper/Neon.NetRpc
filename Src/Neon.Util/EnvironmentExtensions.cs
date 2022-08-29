using System;
namespace Neon.Util
{
    public static class EnvironmentExtensions
    {
        /// <summary>
        /// Check for valid environment variable and converts it to bool, in case of fail takes the default value
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>bool value</returns>
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

        /// <summary>
        /// Check for valid environment variable and converts it to long, in case of fail takes the default value
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>long value</returns>
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

        /// <summary>
        /// Check for valid environment variable and converts it to int, in case of fail takes the default value
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>int value</returns>
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

        /// <summary>
        /// Check for valid environment variable and converts it to int, in case of fail throw an exception
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <exception cref="ArgumentException">If variable not found or parsing failed</exception>
        /// <returns>int value</returns>
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

        /// <summary>
        /// Check for valid environment variable and converts it to string
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>string value</returns>
        public static string GetEnvironmentVariableStr(string name, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            {
                return defaultValue;
            }
            return Environment.GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Check for valid environment variable and converts it to string, in case of fail throw an exception
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <exception cref="ArgumentException">If variable not found</exception>
        /// <returns>string value</returns>
        public static string GetEnvironmentVariableStrOrFail(string name)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                throw new ArgumentException($"Environment variable {name} not set");
            return Environment.GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Check for valid environment variable and converts it to bool, in case of fail throw an exception
        /// </summary>
        /// <param name="name">Environment variable name</param>
        /// <exception cref="ArgumentException">If variable not found or parsing failed</exception>
        /// <returns>bool value</returns>
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
