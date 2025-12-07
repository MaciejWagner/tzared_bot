namespace TzarBot.NeuralNetwork.Models;

/// <summary>
/// Activation function types for neural network layers.
/// </summary>
public enum ActivationType
{
    /// <summary>Rectified Linear Unit: max(0, x)</summary>
    ReLU = 0,

    /// <summary>Hyperbolic tangent: tanh(x), output range [-1, 1]</summary>
    Tanh = 1,

    /// <summary>Leaky ReLU: x if x > 0 else alpha * x (default alpha = 0.01)</summary>
    LeakyReLU = 2,

    /// <summary>Sigmoid: 1 / (1 + exp(-x)), output range [0, 1]</summary>
    Sigmoid = 3,

    /// <summary>Softmax: exp(x_i) / sum(exp(x_j)), output sums to 1</summary>
    Softmax = 4,

    /// <summary>Linear (identity): f(x) = x, no activation</summary>
    Linear = 5
}
