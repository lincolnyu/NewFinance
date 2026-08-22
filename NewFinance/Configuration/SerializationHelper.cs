using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewFinance.Configuration
{
    /// <summary>
    /// Helper for JSON serialization and deserialization of <see cref="Configuration"/>.
    /// Uses System.Text.Json with reference preservation to support the object graph
    /// (shared accounts/entities between collections, ownership dictionaries, etc.).
    /// 
    /// Note: Full round-trip of polymorphic Account/Contract hierarchies and types that
    /// lack public parameterless constructors (or use primary constructors / required
    /// members / non-public setters) may require additional attributes or custom
    /// converters on the domain types. Empty or lightly-populated configurations
    /// serialize and deserialize correctly with the current options.
    /// </summary>
    public static class SerializationHelper
    {
        private static readonly JsonSerializerOptions DefaultOptions = CreateOptions();

        /// <summary>
        /// Creates the shared serializer options used by all methods in this helper.
        /// Exposed so callers can further customise if needed (e.g. for tests).
        /// </summary>
        public static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                // Preserve object identity across the graph (Ownership, Family <-> TaxIndividual, etc.)
                ReferenceHandler = ReferenceHandler.Preserve,
                // Prefer leaving unknown members alone for forward compatibility
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // Allow reading numbers as strings and vice-versa where useful for balances
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
            };
        }

        /// <summary>
        /// Serializes a <see cref="Configuration"/> instance to a JSON string.
        /// </summary>
        public static string Serialize(Configuration configuration, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return JsonSerializer.Serialize(configuration, options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserializes a JSON string into a <see cref="Configuration"/> instance.
        /// Returns null if the input is null or empty.
        /// </summary>
        public static Configuration? Deserialize(string? json, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Configuration>(json, options ?? DefaultOptions);
        }

        /// <summary>
        /// Serializes the configuration and writes it to the given file path.
        /// Creates the directory if it does not exist. Overwrites an existing file.
        /// </summary>
        public static void SaveToFile(this Configuration configuration, string filePath, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = Serialize(configuration, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Reads a JSON file and deserializes it into a <see cref="Configuration"/>.
        /// Returns null if the file does not exist or is empty.
        /// </summary>
        public static Configuration? LoadFromFile(string filePath, JsonSerializerOptions? options = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            return Deserialize(json, options);
        }
    }
}
