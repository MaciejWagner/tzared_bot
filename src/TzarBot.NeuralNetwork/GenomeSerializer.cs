using MessagePack;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.NeuralNetwork;

/// <summary>
/// Serialization utilities for NetworkGenome using MessagePack.
/// Provides efficient binary serialization for genome storage and transfer.
/// </summary>
public static class GenomeSerializer
{
    /// <summary>
    /// MessagePack options with compression for smaller file sizes.
    /// </summary>
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray);

    /// <summary>
    /// Serializes a genome to a byte array.
    /// </summary>
    public static byte[] Serialize(NetworkGenome genome)
    {
        return MessagePackSerializer.Serialize(genome, Options);
    }

    /// <summary>
    /// Deserializes a genome from a byte array.
    /// </summary>
    public static NetworkGenome Deserialize(byte[] data)
    {
        return MessagePackSerializer.Deserialize<NetworkGenome>(data, Options);
    }

    /// <summary>
    /// Saves a genome to a file.
    /// </summary>
    public static async Task SaveAsync(NetworkGenome genome, string filePath, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = Serialize(genome);
        await File.WriteAllBytesAsync(filePath, data, ct);
    }

    /// <summary>
    /// Loads a genome from a file.
    /// </summary>
    public static async Task<NetworkGenome> LoadAsync(string filePath, CancellationToken ct = default)
    {
        var data = await File.ReadAllBytesAsync(filePath, ct);
        return Deserialize(data);
    }

    /// <summary>
    /// Saves a genome synchronously.
    /// </summary>
    public static void Save(NetworkGenome genome, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = Serialize(genome);
        File.WriteAllBytes(filePath, data);
    }

    /// <summary>
    /// Loads a genome synchronously.
    /// </summary>
    public static NetworkGenome Load(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        return Deserialize(data);
    }

    /// <summary>
    /// Serializes a population of genomes to a byte array.
    /// </summary>
    public static byte[] SerializePopulation(IEnumerable<NetworkGenome> population)
    {
        return MessagePackSerializer.Serialize(population.ToList(), Options);
    }

    /// <summary>
    /// Deserializes a population of genomes from a byte array.
    /// </summary>
    public static List<NetworkGenome> DeserializePopulation(byte[] data)
    {
        return MessagePackSerializer.Deserialize<List<NetworkGenome>>(data, Options);
    }

    /// <summary>
    /// Saves a population to a file.
    /// </summary>
    public static async Task SavePopulationAsync(
        IEnumerable<NetworkGenome> population,
        string filePath,
        CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = SerializePopulation(population);
        await File.WriteAllBytesAsync(filePath, data, ct);
    }

    /// <summary>
    /// Loads a population from a file.
    /// </summary>
    public static async Task<List<NetworkGenome>> LoadPopulationAsync(
        string filePath,
        CancellationToken ct = default)
    {
        var data = await File.ReadAllBytesAsync(filePath, ct);
        return DeserializePopulation(data);
    }

    /// <summary>
    /// Validates round-trip serialization of a genome.
    /// Returns true if the genome survives serialization intact.
    /// </summary>
    public static bool ValidateRoundTrip(NetworkGenome genome)
    {
        try
        {
            var serialized = Serialize(genome);
            var deserialized = Deserialize(serialized);

            // Verify key properties
            if (deserialized.Id != genome.Id)
                return false;
            if (deserialized.Generation != genome.Generation)
                return false;
            if (deserialized.HiddenLayers.Count != genome.HiddenLayers.Count)
                return false;
            if (deserialized.Weights.Length != genome.Weights.Length)
                return false;

            // Verify weights match
            for (int i = 0; i < genome.Weights.Length; i++)
            {
                if (Math.Abs(deserialized.Weights[i] - genome.Weights[i]) > 1e-7f)
                    return false;
            }

            // Verify hidden layer configs
            for (int i = 0; i < genome.HiddenLayers.Count; i++)
            {
                var orig = genome.HiddenLayers[i];
                var deser = deserialized.HiddenLayers[i];
                if (orig.NeuronCount != deser.NeuronCount ||
                    orig.Activation != deser.Activation ||
                    Math.Abs(orig.DropoutRate - deser.DropoutRate) > 1e-7f)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
