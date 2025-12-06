# Phase 2: Neural Network Architecture - Detailed Plan

## Overview

The Neural Network module processes game screenshots and outputs actions. It uses ONNX Runtime for inference and a custom genome representation for genetic algorithm evolution.

## Task Dependency Diagram

```
F2.T1 (NetworkGenome)
   │
   ├──────────────┐
   │              │
   ▼              ▼
F2.T2          F2.T3
(Preprocessor) (ONNX Builder)
   │              │
   └──────┬───────┘
          │
          ▼
       F2.T4
    (Inference)
          │
          ▼
       F2.T5
   (Integration)
```

## Definition of Done - Phase 2

- [ ] All 5 tasks completed with passing tests
- [ ] NetworkGenome can be serialized/deserialized
- [ ] Image preprocessing produces correct tensor format
- [ ] ONNX model can be built from genome
- [ ] Inference produces action output < 50ms
- [ ] Demo: genome → model → inference → action
- [ ] Git tag: `phase-2-complete`

---

## Task Definitions

### F2.T1: NetworkGenome & Serialization

```yaml
task_id: "F2.T1"
name: "NetworkGenome & Serialization"
description: |
  Define the genome structure that represents a neural network.
  Implement serialization for persistence and transfer.

inputs:
  - "src/TzarBot.Common/TzarBot.Common.csproj"
  - "plans/1general_plan.md (section 2.3)"

outputs:
  - "src/TzarBot.NeuralNetwork/TzarBot.NeuralNetwork.csproj"
  - "src/TzarBot.NeuralNetwork/Genome/NetworkGenome.cs"
  - "src/TzarBot.NeuralNetwork/Genome/LayerConfig.cs"
  - "src/TzarBot.NeuralNetwork/Genome/ActivationType.cs"
  - "src/TzarBot.NeuralNetwork/Genome/GenomeSerializer.cs"
  - "tests/TzarBot.Tests/Phase2/GenomeTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase2.Genome\""

test_criteria: |
  - Genome can be created with valid configuration
  - Serialization/deserialization round-trip works
  - Weight count calculation is correct
  - Invalid configurations are rejected
  - Serialized size is reasonable (< 1MB for typical genome)

dependencies: ["F1.T1"]
estimated_complexity: "M"

claude_prompt: |
  Create the NetworkGenome class and serialization for neural network representation.

  ## Context
  Create new project `src/TzarBot.NeuralNetwork/`. Use MessagePack for serialization.

  ## Requirements

  1. Create project with dependencies:
     - MessagePack
     - System.Numerics.Tensors

  2. Create `ActivationType` enum:
     ```csharp
     public enum ActivationType
     {
         ReLU,
         LeakyReLU,
         Tanh,
         Sigmoid,
         Linear
     }
     ```

  3. Create `LayerConfig` classes:
     ```csharp
     [MessagePackObject]
     public abstract class LayerConfig
     {
         [Key(0)] public abstract LayerType Type { get; }
     }

     [MessagePackObject]
     public class ConvLayerConfig : LayerConfig
     {
         [Key(1)] public int Filters { get; set; }
         [Key(2)] public int KernelSize { get; set; }
         [Key(3)] public int Stride { get; set; }
         [Key(4)] public ActivationType Activation { get; set; }
     }

     [MessagePackObject]
     public class DenseLayerConfig : LayerConfig
     {
         [Key(1)] public int NeuronCount { get; set; }
         [Key(2)] public ActivationType Activation { get; set; }
         [Key(3)] public float DropoutRate { get; set; }
     }
     ```

  4. Create `NetworkGenome`:
     ```csharp
     [MessagePackObject]
     public class NetworkGenome
     {
         [Key(0)] public Guid Id { get; set; }
         [Key(1)] public int Generation { get; set; }
         [Key(2)] public float Fitness { get; set; }
         [Key(3)] public float? EloRating { get; set; }

         // Fixed architecture (not evolved)
         [Key(4)] public List<ConvLayerConfig> ConvLayers { get; set; }

         // Evolved architecture
         [Key(5)] public List<DenseLayerConfig> HiddenLayers { get; set; }

         // Weights (flattened)
         [Key(6)] public float[] Weights { get; set; }

         // Metadata
         [Key(7)] public Guid? ParentId1 { get; set; }
         [Key(8)] public Guid? ParentId2 { get; set; }
         [Key(9)] public DateTime CreatedAt { get; set; }

         // Input/Output dimensions
         [Key(10)] public int InputWidth { get; set; }
         [Key(11)] public int InputHeight { get; set; }
         [Key(12)] public int InputChannels { get; set; }
         [Key(13)] public int OutputActionCount { get; set; }

         public int CalculateTotalWeights();
         public void InitializeRandomWeights(Random rng);
         public NetworkGenome Clone();
     }
     ```

  5. Create `GenomeSerializer`:
     ```csharp
     public static class GenomeSerializer
     {
         public static byte[] Serialize(NetworkGenome genome);
         public static NetworkGenome Deserialize(byte[] data);
         public static async Task SaveAsync(NetworkGenome genome, string path);
         public static async Task<NetworkGenome> LoadAsync(string path);
     }
     ```

  6. Create factory for default genome:
     ```csharp
     public static class GenomeFactory
     {
         public static NetworkGenome CreateDefault(int inputWidth, int inputHeight);
         public static NetworkGenome CreateRandom(Random rng, int inputWidth, int inputHeight);
     }
     ```

  7. Create tests:
     - Test_Genome_Serialization_RoundTrip
     - Test_WeightCount_MatchesArchitecture
     - Test_Clone_CreatesDeepCopy
     - Test_RandomInitialization_HasVariance
     - Test_InvalidConfig_ThrowsException

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase2.Genome"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify serialized file size is reasonable"

on_failure: |
  If serialization fails:
  1. Check MessagePack attributes are correct
  2. Verify all types are supported by MessagePack
  3. Check for circular references
  4. Use MessagePack.Resolvers.StandardResolver
```

---

### F2.T2: Image Preprocessor

```yaml
task_id: "F2.T2"
name: "Image Preprocessor"
description: |
  Implement image preprocessing pipeline that converts raw screenshots
  into tensors suitable for neural network input.

inputs:
  - "src/TzarBot.NeuralNetwork/TzarBot.NeuralNetwork.csproj"
  - "src/TzarBot.Common/Models/ScreenFrame.cs"
  - "plans/1general_plan.md (section 2.1)"

outputs:
  - "src/TzarBot.NeuralNetwork/Preprocessing/IImagePreprocessor.cs"
  - "src/TzarBot.NeuralNetwork/Preprocessing/ImagePreprocessor.cs"
  - "src/TzarBot.NeuralNetwork/Preprocessing/FrameBuffer.cs"
  - "tests/TzarBot.Tests/Phase2/PreprocessorTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase2.Preprocessor\""

test_criteria: |
  - Image resizes correctly (1920x1080 → 240x135)
  - Grayscale conversion works
  - Normalization produces [0,1] range
  - Frame stacking maintains temporal order
  - Output tensor has correct shape
  - Processing time < 10ms per frame

dependencies: ["F2.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement image preprocessing for neural network input.

  ## Context
  Project: `src/TzarBot.NeuralNetwork/`. Use OpenCvSharp4 for image processing.

  ## Requirements

  1. Create interface:
     ```csharp
     public interface IImagePreprocessor
     {
         float[] ProcessFrame(ScreenFrame frame);
         float[] ProcessFrameStack(ScreenFrame[] frames);
         int OutputWidth { get; }
         int OutputHeight { get; }
         int OutputChannels { get; }
         int StackSize { get; }
     }
     ```

  2. Create preprocessing config:
     ```csharp
     public class PreprocessingConfig
     {
         public int TargetWidth { get; set; } = 240;
         public int TargetHeight { get; set; } = 135;
         public bool ConvertToGrayscale { get; set; } = true;
         public int FrameStackSize { get; set; } = 4;
         public Rectangle? CropRegion { get; set; } = null;
     }
     ```

  3. Implement `ImagePreprocessor`:
     ```csharp
     public class ImagePreprocessor : IImagePreprocessor
     {
         // Pipeline steps:
         // 1. Crop (if CropRegion specified)
         // 2. Resize (bilinear interpolation)
         // 3. Convert to grayscale (optional)
         // 4. Normalize to [0, 1]
         // 5. Flatten to float array

         public float[] ProcessFrame(ScreenFrame frame)
         {
             using var mat = CreateMatFromFrame(frame);
             using var cropped = Crop(mat);
             using var resized = Resize(cropped);
             using var grayscale = ToGrayscale(resized);
             return Normalize(grayscale);
         }
     }
     ```

  4. Create `FrameBuffer` for temporal stacking:
     ```csharp
     public class FrameBuffer
     {
         private readonly Queue<float[]> _frames;
         private readonly int _maxSize;

         public void AddFrame(float[] processedFrame);
         public float[] GetStackedFrames(); // Concatenates all frames
         public bool IsFull { get; }
         public void Clear();
     }
     ```

  5. Create tests:
     - Test_Resize_OutputDimensions
     - Test_Grayscale_SingleChannel
     - Test_Normalize_RangeZeroToOne
     - Test_FrameBuffer_Stacking
     - Test_ProcessingSpeed_Under10ms

  ## Technical Details
  - Input: BGRA32 (4 channels, 8-bit)
  - After grayscale: 1 channel
  - After normalization: float [0, 1]
  - Stacked output: (StackSize * OutputHeight * OutputWidth) floats
  - Default: 4 * 135 * 240 = 129,600 floats

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase2.Preprocessor"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify output dimensions are correct"
  - "Check processing speed"

on_failure: |
  If processing too slow:
  1. Use SIMD operations where possible
  2. Pre-allocate buffers
  3. Consider GPU preprocessing with OpenCL
  4. Profile to find bottleneck
```

---

### F2.T3: ONNX Network Builder

```yaml
task_id: "F2.T3"
name: "ONNX Network Builder"
description: |
  Implement conversion from NetworkGenome to ONNX model format.
  This enables GPU-accelerated inference via ONNX Runtime.

inputs:
  - "src/TzarBot.NeuralNetwork/Genome/NetworkGenome.cs"
  - "plans/1general_plan.md (section 2.2)"

outputs:
  - "src/TzarBot.NeuralNetwork/Builder/INetworkBuilder.cs"
  - "src/TzarBot.NeuralNetwork/Builder/OnnxNetworkBuilder.cs"
  - "src/TzarBot.NeuralNetwork/Builder/WeightInitializer.cs"
  - "tests/TzarBot.Tests/Phase2/NetworkBuilderTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase2.NetworkBuilder\""

test_criteria: |
  - ONNX model is created from genome
  - Model file is valid ONNX format
  - Model can be loaded by ONNX Runtime
  - Weights are correctly placed in model
  - Input/output shapes match genome spec

dependencies: ["F2.T1"]
estimated_complexity: "L"

claude_prompt: |
  Implement ONNX model building from NetworkGenome.

  ## Context
  Project: `src/TzarBot.NeuralNetwork/`. Use Microsoft.ML.OnnxRuntime for model operations.

  ## Requirements

  1. Add NuGet packages:
     - Microsoft.ML.OnnxRuntime
     - Microsoft.ML.OnnxRuntime.Managed
     - Google.Protobuf (for ONNX format)

  2. Create interface:
     ```csharp
     public interface INetworkBuilder
     {
         byte[] BuildModel(NetworkGenome genome);
         void SaveModel(NetworkGenome genome, string path);
         bool ValidateModel(byte[] modelData);
     }
     ```

  3. Implement `OnnxNetworkBuilder`:
     ```csharp
     public class OnnxNetworkBuilder : INetworkBuilder
     {
         // Build ONNX model programmatically
         // Note: ONNX uses protobuf format

         public byte[] BuildModel(NetworkGenome genome)
         {
             var graph = new GraphProto();

             // Add input tensor
             AddInput(graph, genome);

             // Add conv layers (fixed architecture)
             int weightOffset = 0;
             foreach (var conv in genome.ConvLayers)
             {
                 weightOffset = AddConvLayer(graph, conv, genome.Weights, weightOffset);
             }

             // Flatten
             AddFlatten(graph);

             // Add dense layers
             foreach (var dense in genome.HiddenLayers)
             {
                 weightOffset = AddDenseLayer(graph, dense, genome.Weights, weightOffset);
             }

             // Add output heads
             AddOutputHead(graph, genome);

             return SerializeModel(graph);
         }
     }
     ```

  4. Create weight initializer:
     ```csharp
     public static class WeightInitializer
     {
         public static void XavierUniform(Span<float> weights, int fanIn, int fanOut);
         public static void HeNormal(Span<float> weights, int fanIn);
         public static void Zeros(Span<float> weights);
         public static void Ones(Span<float> weights);
     }
     ```

  5. Create tests:
     - Test_BuildModel_CreatesValidOnnx
     - Test_Model_HasCorrectInputShape
     - Test_Model_HasCorrectOutputShape
     - Test_Model_LoadsInOnnxRuntime
     - Test_WeightPlacement_MatchesGenome

  ## Alternative Approach
  If building ONNX programmatically is too complex, consider:
  - Creating a simple feed-forward network in Python
  - Exporting to ONNX
  - Loading and modifying weights in C#

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase2.NetworkBuilder"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify ONNX file can be loaded"

on_failure: |
  If ONNX building fails:
  1. Use simpler architecture (dense-only) first
  2. Validate with netron.app
  3. Check ONNX opset version compatibility
  4. Consider using Python ONNX tools for model creation
```

---

### F2.T4: Inference Engine

```yaml
task_id: "F2.T4"
name: "Inference Engine"
description: |
  Implement the inference engine that runs the neural network
  and produces actions from screen frames.

inputs:
  - "src/TzarBot.NeuralNetwork/Builder/OnnxNetworkBuilder.cs"
  - "src/TzarBot.NeuralNetwork/Preprocessing/ImagePreprocessor.cs"
  - "src/TzarBot.Common/Models/GameAction.cs"

outputs:
  - "src/TzarBot.NeuralNetwork/Inference/IInferenceEngine.cs"
  - "src/TzarBot.NeuralNetwork/Inference/OnnxInferenceEngine.cs"
  - "src/TzarBot.NeuralNetwork/Inference/ActionDecoder.cs"
  - "tests/TzarBot.Tests/Phase2/InferenceTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase2.Inference\""

test_criteria: |
  - Inference completes without error
  - Output has correct shape
  - Inference time < 50ms on CPU
  - Inference time < 10ms on GPU (if available)
  - Memory is properly managed (no leaks)
  - Actions are decoded correctly

dependencies: ["F2.T2", "F2.T3"]
estimated_complexity: "M"

claude_prompt: |
  Implement ONNX Runtime inference engine.

  ## Context
  Project: `src/TzarBot.NeuralNetwork/`. Use Microsoft.ML.OnnxRuntime for inference.

  ## Requirements

  1. Create interface:
     ```csharp
     public interface IInferenceEngine : IDisposable
     {
         GameAction Infer(float[] preprocessedInput);
         float[] InferRaw(float[] input); // Returns raw output
         bool IsGpuEnabled { get; }
         TimeSpan LastInferenceTime { get; }
     }
     ```

  2. Implement `OnnxInferenceEngine`:
     ```csharp
     public class OnnxInferenceEngine : IInferenceEngine
     {
         private readonly InferenceSession _session;
         private readonly IActionDecoder _decoder;

         public OnnxInferenceEngine(byte[] modelData, bool useGpu = false)
         {
             var options = new SessionOptions();
             if (useGpu)
             {
                 options.AppendExecutionProvider_CUDA(); // or DirectML
             }
             _session = new InferenceSession(modelData, options);
         }

         public GameAction Infer(float[] input)
         {
             var inputTensor = new DenseTensor<float>(input, GetInputShape());
             var inputs = new List<NamedOnnxValue>
             {
                 NamedOnnxValue.CreateFromTensor("input", inputTensor)
             };

             using var results = _session.Run(inputs);
             var output = results.First().AsTensor<float>().ToArray();

             return _decoder.Decode(output);
         }
     }
     ```

  3. Create `ActionDecoder`:
     ```csharp
     public interface IActionDecoder
     {
         GameAction Decode(float[] networkOutput);
     }

     public class ActionDecoder : IActionDecoder
     {
         // Network output format:
         // [0:1] - Mouse dx, dy (tanh activated, range [-1, 1])
         // [2:N] - Action probabilities (softmax)

         public GameAction Decode(float[] output)
         {
             // Extract mouse movement
             float dx = output[0] * MaxMouseDelta;
             float dy = output[1] * MaxMouseDelta;

             // Extract action (argmax of probabilities)
             var actionProbs = output.Skip(2).ToArray();
             int actionIndex = ArgMax(actionProbs);
             var actionType = (ActionType)actionIndex;

             return new GameAction
             {
                 MouseDeltaX = (int)dx,
                 MouseDeltaY = (int)dy,
                 Type = actionType,
                 Confidence = actionProbs[actionIndex]
             };
         }
     }
     ```

  4. Create `GameAction` in Common:
     ```csharp
     public class GameAction
     {
         public int MouseDeltaX { get; set; }
         public int MouseDeltaY { get; set; }
         public ActionType Type { get; set; }
         public float Confidence { get; set; }
     }

     public enum ActionType
     {
         None,
         LeftClick,
         RightClick,
         DoubleClick,
         DragStart,
         DragEnd,
         Hotkey1, Hotkey2, /* ... */ Hotkey0,
         CtrlHotkey1, /* ... */
         ScrollUp, ScrollDown,
         Escape, Enter
     }
     ```

  5. Create tests:
     - Test_Inference_ReturnsAction
     - Test_InferenceTime_Under50ms
     - Test_ActionDecoder_CorrectParsing
     - Test_MultipleInferences_NoMemoryLeak

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase2.Inference"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify inference timing"

on_failure: |
  If inference slow:
  1. Check if GPU provider is working
  2. Profile to find bottleneck
  3. Consider batching if possible
  4. Use DirectML on Windows for GPU
```

---

### F2.T5: Integration Tests

```yaml
task_id: "F2.T5"
name: "Phase 2 Integration Tests"
description: |
  Create integration tests that verify the full neural network pipeline
  from raw frame to action output.

inputs:
  - "All Phase 2 source files"
  - "Phase 1 screen capture (for real frame testing)"

outputs:
  - "tests/TzarBot.Tests/Phase2/IntegrationTests.cs"
  - "src/TzarBot.NeuralNetwork.Demo/Program.cs"
  - "src/TzarBot.NeuralNetwork.Demo/TzarBot.NeuralNetwork.Demo.csproj"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase2.Integration\""

test_criteria: |
  - Full pipeline works: frame → preprocess → infer → action
  - Random genome produces valid actions
  - Serialization round-trip preserves behavior
  - Demo runs without errors
  - Performance acceptable for real-time use

dependencies: ["F2.T4"]
estimated_complexity: "M"

claude_prompt: |
  Create integration tests and demo for Phase 2.

  ## Context
  All Phase 2 components are implemented. Verify end-to-end functionality.

  ## Requirements

  1. Create demo console app `src/TzarBot.NeuralNetwork.Demo/`:
     ```csharp
     // Demo workflow:
     // 1. Create random genome
     // 2. Build ONNX model
     // 3. Create inference engine
     // 4. Capture screen (or use test image)
     // 5. Preprocess frame
     // 6. Run inference
     // 7. Print action output
     // 8. Measure and report timing
     ```

  2. Create integration tests:
     ```csharp
     public class NeuralNetworkIntegrationTests
     {
         [Fact]
         public void FullPipeline_Frame_To_Action()
         {
             // Create test frame
             var frame = CreateTestFrame(1920, 1080);

             // Create genome
             var genome = GenomeFactory.CreateRandom(new Random(42), 240, 135);

             // Build model
             var builder = new OnnxNetworkBuilder();
             var modelData = builder.BuildModel(genome);

             // Run inference
             var preprocessor = new ImagePreprocessor();
             var engine = new OnnxInferenceEngine(modelData);
             var buffer = new FrameBuffer(4);

             // Fill buffer
             for (int i = 0; i < 4; i++)
             {
                 buffer.AddFrame(preprocessor.ProcessFrame(frame));
             }

             // Infer
             var action = engine.Infer(buffer.GetStackedFrames());

             // Verify
             Assert.NotNull(action);
             Assert.InRange(action.MouseDeltaX, -100, 100);
         }

         [Fact]
         public void Genome_Serialization_PreservesBehavior()
         {
             var genome = GenomeFactory.CreateRandom(new Random(42), 240, 135);

             // Get action with original
             var action1 = RunInference(genome);

             // Serialize and deserialize
             var data = GenomeSerializer.Serialize(genome);
             var restored = GenomeSerializer.Deserialize(data);

             // Get action with restored
             var action2 = RunInference(restored);

             // Should be identical
             Assert.Equal(action1.Type, action2.Type);
             Assert.Equal(action1.MouseDeltaX, action2.MouseDeltaX);
         }

         [Fact]
         public void Performance_EndToEnd_Under100ms()
         {
             // Setup
             var genome = GenomeFactory.CreateRandom(new Random(42), 240, 135);
             var frame = CreateTestFrame(1920, 1080);
             // ... setup engine

             // Measure
             var sw = Stopwatch.StartNew();
             for (int i = 0; i < 100; i++)
             {
                 var processed = preprocessor.ProcessFrame(frame);
                 buffer.AddFrame(processed);
                 if (buffer.IsFull)
                 {
                     engine.Infer(buffer.GetStackedFrames());
                 }
             }
             sw.Stop();

             Assert.True(sw.ElapsedMilliseconds / 100.0 < 100);
         }
     }
     ```

  3. Add performance benchmarks to demo:
     - Preprocessing time
     - Inference time
     - Total pipeline time
     - Memory usage

  After completion:
  1. Run: `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase2.Integration"`
  2. Run: `dotnet run --project src/TzarBot.NeuralNetwork.Demo`

validation_steps:
  - "All integration tests pass"
  - "Demo runs successfully"
  - "Performance is acceptable"
  - "Memory usage is stable"

on_failure: |
  If integration fails:
  1. Test individual components
  2. Check tensor shapes match
  3. Verify weight initialization
  4. Add logging for debugging
```

---

## Rollback Plan

If Phase 2 implementation fails:

1. **Simpler Architecture**: Start with Dense-only network (no conv layers)
   - Faster to implement
   - Easier to debug
   - Can add conv layers later

2. **Alternative Inference**: Use ML.NET instead of ONNX Runtime
   - Native .NET
   - Simpler API
   - May be slower

3. **Pre-built Models**: Create network in Python, export to ONNX
   - Use PyTorch or TensorFlow
   - Export with weights
   - Only load and run in C#

---

## API Documentation

### NetworkGenome API

```csharp
// Create random genome
var genome = GenomeFactory.CreateRandom(rng, width: 240, height: 135);

// Customize architecture
genome.HiddenLayers.Add(new DenseLayerConfig
{
    NeuronCount = 512,
    Activation = ActivationType.ReLU,
    DropoutRate = 0.1f
});

// Serialize
byte[] data = GenomeSerializer.Serialize(genome);
await GenomeSerializer.SaveAsync(genome, "genome.bin");

// Deserialize
var loaded = GenomeSerializer.Deserialize(data);
```

### ImagePreprocessor API

```csharp
var config = new PreprocessingConfig
{
    TargetWidth = 240,
    TargetHeight = 135,
    ConvertToGrayscale = true,
    FrameStackSize = 4
};

var preprocessor = new ImagePreprocessor(config);

// Process single frame
float[] processed = preprocessor.ProcessFrame(screenFrame);

// Use with frame buffer
var buffer = new FrameBuffer(4);
buffer.AddFrame(processed);
if (buffer.IsFull)
{
    float[] stacked = buffer.GetStackedFrames();
}
```

### Inference API

```csharp
// Build model
var builder = new OnnxNetworkBuilder();
byte[] modelData = builder.BuildModel(genome);

// Create engine
using var engine = new OnnxInferenceEngine(modelData, useGpu: true);

// Run inference
float[] input = buffer.GetStackedFrames();
GameAction action = engine.Infer(input);

// Execute action
Console.WriteLine($"Action: {action.Type}, Mouse: ({action.MouseDeltaX}, {action.MouseDeltaY})");
```

---

*Phase 2 Detailed Plan - Version 1.0*
*See prompts/phase_2/ for individual task prompts*
