using System.Reflection;
using SimLab.Configuration;

namespace SimLab.Simulator;

internal class Simulation {
    public WorldCfg? WorldConfiguration { get; set; } = null;
    public MethodInfo? InitializationMethod { get; set; } = null;
    public MethodInfo? UpdateMethod { get; set; } = null;
    public MethodInfo? EvaluationMethod { get; set; } = null;
    public MethodInfo? ReproductionMethod { get; set; } = null;
    public MethodInfo? SelectionMethod { get; set; } = null;
}
