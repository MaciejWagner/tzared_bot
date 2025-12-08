using Google.Protobuf;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.NeuralNetwork.Onnx;

/// <summary>
/// Low-level builder for constructing ONNX graphs using protobuf.
///
/// ONNX format reference: https://github.com/onnx/onnx/blob/main/docs/IR.md
///
/// This class builds the ONNX model directly using Google.Protobuf,
/// avoiding dependency on the full ONNX Python package.
/// </summary>
public sealed class OnnxGraphBuilder
{
    private readonly string _modelName;
    private readonly List<OnnxNode> _nodes = new();
    private readonly List<OnnxTensorInfo> _inputs = new();
    private readonly List<OnnxTensorInfo> _outputs = new();
    private readonly List<OnnxInitializer> _initializers = new();
    private int _nodeCounter;

    public OnnxGraphBuilder(string modelName)
    {
        _modelName = modelName;
    }

    /// <summary>
    /// Adds an input tensor to the graph.
    /// </summary>
    public void AddInput(string name, long[] shape)
    {
        _inputs.Add(new OnnxTensorInfo(name, shape, OnnxDataType.Float));
    }

    /// <summary>
    /// Adds an output tensor to the graph.
    /// </summary>
    public void AddOutput(string name, string sourceTensor, long[] shape)
    {
        _outputs.Add(new OnnxTensorInfo(name, shape, OnnxDataType.Float, sourceTensor));
    }

    /// <summary>
    /// Adds a Conv2D layer with optional activation.
    /// </summary>
    public string AddConv2D(
        string input,
        string name,
        int inChannels,
        int outChannels,
        int kernelSize,
        int stride,
        int padding,
        float[] weights,
        float[] biases,
        ActivationType activation)
    {
        string weightName = $"{name}_weight";
        string biasName = $"{name}_bias";
        string convOutput = $"{name}_conv_out";

        // Add weight initializer [out_channels, in_channels, kernel_h, kernel_w]
        _initializers.Add(new OnnxInitializer(
            weightName,
            new long[] { outChannels, inChannels, kernelSize, kernelSize },
            weights));

        // Add bias initializer [out_channels]
        _initializers.Add(new OnnxInitializer(biasName, new long[] { outChannels }, biases));

        // Conv node
        _nodes.Add(new OnnxNode
        {
            OpType = "Conv",
            Name = $"{name}_conv",
            Inputs = new[] { input, weightName, biasName },
            Outputs = new[] { convOutput },
            Attributes = new Dictionary<string, object>
            {
                { "kernel_shape", new long[] { kernelSize, kernelSize } },
                { "strides", new long[] { stride, stride } },
                { "pads", new long[] { padding, padding, padding, padding } }
            }
        });

        // Add activation
        return AddActivation(convOutput, name, activation);
    }

    /// <summary>
    /// Adds a dense (fully connected) layer with optional activation.
    /// Dense layer in ONNX: MatMul + Add
    /// </summary>
    public string AddDenseLayer(
        string input,
        string name,
        int inFeatures,
        int outFeatures,
        float[] weights,
        float[] biases,
        ActivationType activation)
    {
        string weightName = $"{name}_weight";
        string biasName = $"{name}_bias";
        string matmulOutput = $"{name}_matmul_out";
        string addOutput = $"{name}_add_out";

        // Weight shape for MatMul: [in_features, out_features]
        // Note: ONNX MatMul performs input @ weights, so weights are [in, out]
        _initializers.Add(new OnnxInitializer(
            weightName,
            new long[] { inFeatures, outFeatures },
            weights));

        // Bias shape: [out_features]
        _initializers.Add(new OnnxInitializer(biasName, new long[] { outFeatures }, biases));

        // MatMul node
        _nodes.Add(new OnnxNode
        {
            OpType = "MatMul",
            Name = $"{name}_matmul",
            Inputs = new[] { input, weightName },
            Outputs = new[] { matmulOutput }
        });

        // Add bias
        _nodes.Add(new OnnxNode
        {
            OpType = "Add",
            Name = $"{name}_add",
            Inputs = new[] { matmulOutput, biasName },
            Outputs = new[] { addOutput }
        });

        // Add activation
        return AddActivation(addOutput, name, activation);
    }

    /// <summary>
    /// Adds a Flatten operation.
    /// </summary>
    public string AddFlatten(string input, string outputName)
    {
        _nodes.Add(new OnnxNode
        {
            OpType = "Flatten",
            Name = $"flatten_{_nodeCounter++}",
            Inputs = new[] { input },
            Outputs = new[] { outputName },
            Attributes = new Dictionary<string, object>
            {
                { "axis", 1L } // Flatten from axis 1, keeping batch dimension
            }
        });
        return outputName;
    }

    /// <summary>
    /// Adds an Identity operation (useful for renaming tensors).
    /// </summary>
    public string AddIdentity(string input, string outputName)
    {
        _nodes.Add(new OnnxNode
        {
            OpType = "Identity",
            Name = $"identity_{_nodeCounter++}",
            Inputs = new[] { input },
            Outputs = new[] { outputName }
        });
        return outputName;
    }

    /// <summary>
    /// Adds an activation function node.
    /// </summary>
    private string AddActivation(string input, string namePrefix, ActivationType activation)
    {
        string output = $"{namePrefix}_act_out";

        switch (activation)
        {
            case ActivationType.ReLU:
                _nodes.Add(new OnnxNode
                {
                    OpType = "Relu",
                    Name = $"{namePrefix}_relu",
                    Inputs = new[] { input },
                    Outputs = new[] { output }
                });
                return output;

            case ActivationType.Tanh:
                _nodes.Add(new OnnxNode
                {
                    OpType = "Tanh",
                    Name = $"{namePrefix}_tanh",
                    Inputs = new[] { input },
                    Outputs = new[] { output }
                });
                return output;

            case ActivationType.LeakyReLU:
                _nodes.Add(new OnnxNode
                {
                    OpType = "LeakyRelu",
                    Name = $"{namePrefix}_leakyrelu",
                    Inputs = new[] { input },
                    Outputs = new[] { output },
                    Attributes = new Dictionary<string, object>
                    {
                        { "alpha", 0.01f }
                    }
                });
                return output;

            case ActivationType.Sigmoid:
                _nodes.Add(new OnnxNode
                {
                    OpType = "Sigmoid",
                    Name = $"{namePrefix}_sigmoid",
                    Inputs = new[] { input },
                    Outputs = new[] { output }
                });
                return output;

            case ActivationType.Softmax:
                _nodes.Add(new OnnxNode
                {
                    OpType = "Softmax",
                    Name = $"{namePrefix}_softmax",
                    Inputs = new[] { input },
                    Outputs = new[] { output },
                    Attributes = new Dictionary<string, object>
                    {
                        { "axis", -1L } // Softmax over last dimension
                    }
                });
                return output;

            case ActivationType.Linear:
                // No activation - return input as-is
                return input;

            default:
                throw new ArgumentException($"Unsupported activation type: {activation}");
        }
    }

    /// <summary>
    /// Builds the final ONNX model and returns it as a byte array.
    /// </summary>
    public byte[] Build(int opsetVersion)
    {
        using var stream = new MemoryStream();

        var graph = new OnnxGraph
        {
            Name = _modelName,
            Nodes = _nodes,
            Inputs = _inputs,
            Outputs = _outputs,
            Initializers = _initializers
        };

        var model = new OnnxModelWriter(8, "TzarBot.NeuralNetwork", "1.0.0", opsetVersion, graph);
        model.WriteTo(stream);

        return stream.ToArray();
    }

    #region Internal data structures

    private class OnnxNode
    {
        public string OpType { get; set; } = "";
        public string Name { get; set; } = "";
        public string[] Inputs { get; set; } = Array.Empty<string>();
        public string[] Outputs { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    private class OnnxTensorInfo
    {
        public string Name { get; }
        public long[] Shape { get; }
        public OnnxDataType DataType { get; }
        public string? SourceTensor { get; }

        public OnnxTensorInfo(string name, long[] shape, OnnxDataType dataType, string? sourceTensor = null)
        {
            Name = name;
            Shape = shape;
            DataType = dataType;
            SourceTensor = sourceTensor;
        }
    }

    private class OnnxInitializer
    {
        public string Name { get; }
        public long[] Shape { get; }
        public float[] Data { get; }

        public OnnxInitializer(string name, long[] shape, float[] data)
        {
            Name = name;
            Shape = shape;
            Data = data;
        }
    }

    private enum OnnxDataType
    {
        Float = 1,
        Int64 = 7
    }

    private class OnnxGraph
    {
        public string Name { get; set; } = "";
        public List<OnnxNode> Nodes { get; set; } = new();
        public List<OnnxTensorInfo> Inputs { get; set; } = new();
        public List<OnnxTensorInfo> Outputs { get; set; } = new();
        public List<OnnxInitializer> Initializers { get; set; } = new();
    }

    /// <summary>
    /// Writes ONNX ModelProto to a stream using protobuf encoding.
    /// </summary>
    private class OnnxModelWriter
    {
        private readonly long _irVersion;
        private readonly string _producerName;
        private readonly string _producerVersion;
        private readonly int _opsetVersion;
        private readonly OnnxGraph _graph;

        public OnnxModelWriter(long irVersion, string producerName, string producerVersion, int opsetVersion, OnnxGraph graph)
        {
            _irVersion = irVersion;
            _producerName = producerName;
            _producerVersion = producerVersion;
            _opsetVersion = opsetVersion;
            _graph = graph;
        }

        public void WriteTo(Stream stream)
        {
            using var output = new CodedOutputStream(stream, leaveOpen: true);

            // Field 1: ir_version (int64)
            output.WriteTag(1, WireFormat.WireType.Varint);
            output.WriteInt64(_irVersion);

            // Field 3: producer_name (string)
            if (!string.IsNullOrEmpty(_producerName))
            {
                output.WriteTag(3, WireFormat.WireType.LengthDelimited);
                output.WriteString(_producerName);
            }

            // Field 4: producer_version (string)
            if (!string.IsNullOrEmpty(_producerVersion))
            {
                output.WriteTag(4, WireFormat.WireType.LengthDelimited);
                output.WriteString(_producerVersion);
            }

            // Field 6: model_version (int64) - default 1
            output.WriteTag(6, WireFormat.WireType.Varint);
            output.WriteInt64(1);

            // Field 7: graph (GraphProto)
            var graphBytes = SerializeGraph(_graph);
            output.WriteTag(7, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(graphBytes));

            // Field 8: opset_import (repeated OpsetIdProto)
            var opsetBytes = SerializeOpset(_opsetVersion);
            output.WriteTag(8, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(opsetBytes));

            output.Flush();
        }

        private byte[] SerializeOpset(int version)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // OpsetIdProto has:
            // field 1: domain (string) - empty for default ONNX
            // field 2: version (int64)

            output.WriteTag(2, WireFormat.WireType.Varint);
            output.WriteInt64(version);

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeGraph(OnnxGraph graph)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: node (repeated NodeProto)
            foreach (var node in graph.Nodes)
            {
                var nodeBytes = SerializeNode(node);
                output.WriteTag(1, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.CopyFrom(nodeBytes));
            }

            // Field 2: name (string)
            if (!string.IsNullOrEmpty(graph.Name))
            {
                output.WriteTag(2, WireFormat.WireType.LengthDelimited);
                output.WriteString(graph.Name);
            }

            // Field 5: initializer (repeated TensorProto)
            foreach (var init in graph.Initializers)
            {
                var tensorBytes = SerializeTensor(init);
                output.WriteTag(5, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.CopyFrom(tensorBytes));
            }

            // Field 11: input (repeated ValueInfoProto)
            foreach (var input in graph.Inputs)
            {
                var valueBytes = SerializeValueInfo(input.Name, input.Shape, input.DataType);
                output.WriteTag(11, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.CopyFrom(valueBytes));
            }

            // Field 12: output (repeated ValueInfoProto)
            foreach (var outputInfo in graph.Outputs)
            {
                // For outputs, use the source tensor name in the ValueInfo
                var valueBytes = SerializeValueInfo(outputInfo.SourceTensor ?? outputInfo.Name, outputInfo.Shape, outputInfo.DataType);
                output.WriteTag(12, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.CopyFrom(valueBytes));
            }

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeNode(OnnxNode node)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: input (repeated string)
            foreach (var input in node.Inputs)
            {
                output.WriteTag(1, WireFormat.WireType.LengthDelimited);
                output.WriteString(input);
            }

            // Field 2: output (repeated string)
            foreach (var nodeOutput in node.Outputs)
            {
                output.WriteTag(2, WireFormat.WireType.LengthDelimited);
                output.WriteString(nodeOutput);
            }

            // Field 3: name (string)
            if (!string.IsNullOrEmpty(node.Name))
            {
                output.WriteTag(3, WireFormat.WireType.LengthDelimited);
                output.WriteString(node.Name);
            }

            // Field 4: op_type (string)
            output.WriteTag(4, WireFormat.WireType.LengthDelimited);
            output.WriteString(node.OpType);

            // Field 5: attribute (repeated AttributeProto)
            foreach (var attr in node.Attributes)
            {
                var attrBytes = SerializeAttribute(attr.Key, attr.Value);
                output.WriteTag(5, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.CopyFrom(attrBytes));
            }

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeAttribute(string name, object value)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: name (string)
            output.WriteTag(1, WireFormat.WireType.LengthDelimited);
            output.WriteString(name);

            // AttributeProto type field values:
            // FLOAT = 1, INT = 2, STRING = 3, TENSOR = 4, GRAPH = 5
            // FLOATS = 6, INTS = 7, STRINGS = 8

            switch (value)
            {
                case float f:
                    // Field 4: type
                    output.WriteTag(20, WireFormat.WireType.Varint);
                    output.WriteInt32(1); // FLOAT
                    // Field 2: f (float)
                    output.WriteTag(2, WireFormat.WireType.Fixed32);
                    output.WriteFloat(f);
                    break;

                case long l:
                    // Field 4: type
                    output.WriteTag(20, WireFormat.WireType.Varint);
                    output.WriteInt32(2); // INT
                    // Field 3: i (int64)
                    output.WriteTag(3, WireFormat.WireType.Varint);
                    output.WriteInt64(l);
                    break;

                case long[] ints:
                    // Field 4: type
                    output.WriteTag(20, WireFormat.WireType.Varint);
                    output.WriteInt32(7); // INTS
                    // Field 8: ints (repeated int64)
                    foreach (var i in ints)
                    {
                        output.WriteTag(8, WireFormat.WireType.Varint);
                        output.WriteInt64(i);
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported attribute type: {value.GetType()}");
            }

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeTensor(OnnxInitializer init)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: dims (repeated int64)
            foreach (var dim in init.Shape)
            {
                output.WriteTag(1, WireFormat.WireType.Varint);
                output.WriteInt64(dim);
            }

            // Field 2: data_type (int32) - FLOAT = 1
            output.WriteTag(2, WireFormat.WireType.Varint);
            output.WriteInt32(1);

            // Field 8: name (string)
            output.WriteTag(8, WireFormat.WireType.LengthDelimited);
            output.WriteString(init.Name);

            // Field 9: raw_data (bytes) - float data as raw bytes (little-endian)
            var rawData = new byte[init.Data.Length * sizeof(float)];
            Buffer.BlockCopy(init.Data, 0, rawData, 0, rawData.Length);
            output.WriteTag(9, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(rawData));

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeValueInfo(string name, long[] shape, OnnxDataType dataType)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: name (string)
            output.WriteTag(1, WireFormat.WireType.LengthDelimited);
            output.WriteString(name);

            // Field 2: type (TypeProto)
            var typeBytes = SerializeTypeProto(shape, dataType);
            output.WriteTag(2, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(typeBytes));

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeTypeProto(long[] shape, OnnxDataType dataType)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: tensor_type (TypeProto.Tensor)
            var tensorTypeBytes = SerializeTensorType(shape, dataType);
            output.WriteTag(1, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(tensorTypeBytes));

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeTensorType(long[] shape, OnnxDataType dataType)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: elem_type (int32)
            output.WriteTag(1, WireFormat.WireType.Varint);
            output.WriteInt32((int)dataType);

            // Field 2: shape (TensorShapeProto)
            var shapeBytes = SerializeShape(shape);
            output.WriteTag(2, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom(shapeBytes));

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeShape(long[] shape)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: dim (repeated TensorShapeProto.Dimension)
            foreach (var dim in shape)
            {
                var dimBytes = SerializeDimension(dim);
                output.WriteTag(1, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.CopyFrom(dimBytes));
            }

            output.Flush();
            return stream.ToArray();
        }

        private byte[] SerializeDimension(long dim)
        {
            using var stream = new MemoryStream();
            using var output = new CodedOutputStream(stream);

            // Field 1: dim_value (int64)
            output.WriteTag(1, WireFormat.WireType.Varint);
            output.WriteInt64(dim);

            output.Flush();
            return stream.ToArray();
        }
    }

    #endregion
}
