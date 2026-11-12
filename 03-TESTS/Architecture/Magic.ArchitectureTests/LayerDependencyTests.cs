using System.Reflection;
using Magic.Api.Controllers;
using Magic.Application.Diagnostics;
using Magic.Contracts.Api;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Platform.Options;
using NetArchTest.Rules;
using Xunit;

namespace Magic.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly ContractsAssembly = typeof(ApiResponse<>).Assembly;
    private static readonly Assembly DomainAssembly = typeof(Money).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ISystemDiagnosticsService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(MagicDbContext).Assembly;
    private static readonly Assembly PlatformAssembly = typeof(MagicDeploymentOptions).Assembly;
    private static readonly Assembly ApiAssembly = typeof(SystemController).Assembly;

    [Fact]
    public void ContractsMustNotDependOnAnyInternalBackendLayer()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Magic.Domain",
                "Magic.Application",
                "Magic.Infrastructure",
                "Magic.Platform",
                "Magic.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailingTypes(result));
    }

    [Fact]
    public void DomainMustRemainPureWithoutApplicationInfrastructurePlatformOrOrmDependencies()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Magic.Contracts",
                "Magic.Application",
                "Magic.Infrastructure",
                "Magic.Platform",
                "Magic.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailingTypes(result));
    }

    [Fact]
    public void ApplicationMustNotDependOnInfrastructurePlatformApiOrEfCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Magic.Infrastructure",
                "Magic.Platform",
                "Magic.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailingTypes(result));
    }

    [Fact]
    public void BackendAssembliesMustNotReferenceUnapprovedSqliteOrQuartzPackages()
    {
        var assemblies = new[]
        {
            ContractsAssembly,
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
            PlatformAssembly,
            ApiAssembly
        };

        foreach (var assembly in assemblies)
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name => name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(referenced, name => name.Contains("Quartz", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypeNames is null || result.FailingTypeNames.Count == 0)
        {
            return "Unknown architecture rule violation.";
        }

        return $"Failing types: {string.Join(", ", result.FailingTypeNames)}";
    }
}

