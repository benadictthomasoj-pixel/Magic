using System.Reflection;
using Magic.Application.Abstractions;
using Magic.Platform.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Magic.Platform.Runtime;

/// <summary>
/// Resolves runtime deployment metadata for health and diagnostic responses.
/// </summary>
public sealed class RuntimeEnvironmentInfo(
    IHostEnvironment hostEnvironment,
    IOptions<MagicDeploymentOptions> deploymentOptions) : IRuntimeEnvironmentInfo
{
    public string ServiceName => deploymentOptions.Value.ServiceName;
    public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.1.0";
    public string EnvironmentName => hostEnvironment.EnvironmentName;
    public string DeploymentMode => deploymentOptions.Value.Mode;
    public string NetworkPolicy => deploymentOptions.Value.NetworkPolicy;
}

