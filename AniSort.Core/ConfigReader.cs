// // Copyright © 2022 Lorathas
// //
// // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// // files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// // modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// // Software is furnished to do so, subject to the following conditions:
// //
// // The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// //
// // THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// // OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// // IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AniSort.Core.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AniSort.Core;

public class ConfigReader
{
    private static readonly Dictionary<string, Func<string, Task<Config>>> ConfigLoaderMappings;
    private static readonly XmlSerializer XmlSerializer = new XmlSerializer(typeof(Config));
    private static readonly IDeserializer YamlSerializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    static ConfigReader()
    {
        ConfigLoaderMappings = new Dictionary<string, Func<string, Task<Config>>>
        {
            { ".yaml", ReadYamlConfigAsync },
            { ".yml", ReadYamlConfigAsync },
            { ".xml", ReadXmlConfigAsync },
            { ".json", ReadJsonConfigAsync }
        };
    }

    /// <summary>
    /// Read in a config file from a path
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <returns></returns>
    /// <exception cref="InvalidConfigFileException"></exception>
    public static async Task<Config> ReadConfigAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidConfigFileException($"No file exists at {path}");
        }

        string extension = Path.GetExtension(path);

        if (!ConfigLoaderMappings.TryGetValue(extension, out var configLoader))
        {
            throw new InvalidConfigFileTypeException($"No config loader found for file extension {extension}. File must be of type .xml, .yaml, .yml, or .json.");
        }

        return await configLoader.Invoke(path);
    }

    /// <summary>
    /// Look through all default config file paths and read them if they exist 
    /// </summary>
    /// <returns>Config read from the first valid config path it found</returns>
    /// <exception cref="InvalidConfigFileException">Thrown when no valid config file is found at any of the default paths</exception>
    public static async Task<Config> ReadDefaultConfigFileLocationsAsync()
    {
        foreach (string configPath in AppPaths.DefaultConfigFilePaths)
        {
            if (!File.Exists(configPath))
            {
                continue;
            }
            
            try
            {
                return await ReadConfigAsync(configPath);
            }
            catch (InvalidConfigFileException)
            {
                // Ignore and move onto the next one
            }
        }

        throw new InvalidConfigFileException("No default config file found. Please ensure that a config exists at one of the valid config file paths.");
    }

    /// <summary>
    /// Read XML config file
    /// </summary>
    /// <param name="path">Path to the XML config file</param>
    /// <returns>Config that was loaded from the XML file</returns>
    /// <exception cref="InvalidConfigFileException">Thrown when the config file is not a valid XML config file</exception>
    public static async Task<Config> ReadXmlConfigAsync(string path)
    {
        await using var fs = File.OpenRead(path);

        try
        {
            var config = XmlSerializer.Deserialize(fs) as Config;

            if (config == default)
            {
                throw new InvalidConfigFileException($"No valid config found at provided path: {path}");
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new InvalidConfigFileException($"No valid config found at provided path: {path}", ex);
        }
    }

    /// <summary>
    /// Read YAML config file
    /// </summary>
    /// <param name="path">Path to the YAML config file</param>
    /// <returns>Config that was loaded from the YAML file</returns>
    /// <exception cref="InvalidConfigFileException">Thrown when the config file is not a valid YAML config file</exception>
    public static async Task<Config> ReadYamlConfigAsync(string path)
    {
        await using var fs = File.OpenRead(path);

        try
        {
            using var reader = new StreamReader(fs);
            var config = YamlSerializer.Deserialize<Config>(reader);

            if (config == null)
            {
                throw new InvalidConfigFileException($"No valid config found at provided path: {path}");
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new InvalidConfigFileException($"No valid config found at provided path: {path}", ex);
        }
    }

    /// <summary>
    /// Read JSON config file
    /// </summary>
    /// <param name="path">Path to the JSON config file</param>
    /// <returns>Config that was loaded from the JSON file</returns>
    /// <exception cref="InvalidConfigFileException">Thrown when the config file is not a valid JSON config file</exception>
    public static async Task<Config> ReadJsonConfigAsync(string path)
    {
        await using var fs = File.OpenRead(path);

        try
        {
            var config = JsonSerializer.Deserialize<Config>(fs, JsonSerializerOptions);

            if (config == default)
            {
                throw new InvalidConfigFileException($"No valid config found at provided path: {path}");
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new InvalidConfigFileException($"No valid config found at provided path: {path}", ex);
        }
    }
}
