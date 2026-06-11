using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace BookingPlatform.Tests.Architecture;

public class ArchitectureTests
{
    private static readonly string ModulesNamespace = "BookingPlatform.Server.Modules";
    private static readonly string FeaturesNamespacePattern = "BookingPlatform\\.Server\\.Modules\\.[A-Za-z]+\\.Features";

    private static readonly ArchUnitNET.Domain.Architecture SystemUnderTest = new ArchLoader()
        .LoadAssemblies(typeof(BookingPlatform.Server.Modules.Bookings.Features.CreateBooking.CreateBookingEndpoint).Assembly)
        .Build();

    [Fact]
    public void Should_Load_Architecture()
    {
        Assert.NotNull(SystemUnderTest);
    }

    // ------------------------------------------------------------------
    // 1. No circular dependencies between Modules or Features
    // ------------------------------------------------------------------

    [Fact]
    public void Modules_Should_Not_Have_Circular_Dependencies()
    {
        var rule = SliceRuleDefinition.Slices()
            .Matching($"{ModulesNamespace}.(*).**")
            .Should().BeFreeOfCycles();

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    [Fact]
    public void Features_Should_Not_Have_Circular_Dependencies()
    {
        var rule = SliceRuleDefinition.Slices()
            .Matching($"{ModulesNamespace}.(*).Features.(*).**")
            .Should().BeFreeOfCycles();

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    // ------------------------------------------------------------------
    // 2. Features only depend downward — no cross-feature coupling
    // ------------------------------------------------------------------

    [Fact]
    public void Features_Should_Not_Depend_On_Other_Features()
    {
        var featureTypes = SystemUnderTest.Types
            .Where(t => !string.IsNullOrEmpty(t.Namespace?.FullName)
                        && Regex.IsMatch(t.Namespace.FullName, "^" + FeaturesNamespacePattern + "\\.[A-Za-z]+"))
            .ToList();

        var featureNamespaces = featureTypes
            .Select(t => ExtractFeatureNamespace(t.Namespace.FullName))
            .Where(ns => !string.IsNullOrEmpty(ns))
            .Distinct()
            .ToList();

        foreach (var featureNs in featureNamespaces)
        {
            var otherFeatures = featureNamespaces
                .Where(ns => ns != featureNs)
                .ToList();

            foreach (var otherNs in otherFeatures)
            {
                var rule = Types()
                    .That().ResideInNamespaceMatching(featureNs)
                    .Should().NotDependOnAny(
                        Types().That().ResideInNamespaceMatching(otherNs));

                Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
            }
        }
    }

    // ------------------------------------------------------------------
    // 3. No infrastructure in endpoint code
    // ------------------------------------------------------------------

    [Fact]
    public void Endpoints_Should_Not_Depend_On_Marten()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching($"{FeaturesNamespacePattern}\\..*")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Marten"));

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    [Fact]
    public void Endpoints_Should_Not_Depend_On_Npgsql()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching($"{FeaturesNamespacePattern}\\..*")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Npgsql"));

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    [Fact]
    public void Endpoints_Should_Not_Depend_On_JasperFx_Events()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching($"{FeaturesNamespacePattern}\\..*")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("JasperFx.Events"));

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    [Fact]
    public void Endpoints_Should_Not_Depend_On_Wolverine_Marten()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching($"{FeaturesNamespacePattern}\\..*")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Wolverine.Marten"));

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    // ------------------------------------------------------------------
    // 4. Naming conventions
    // ------------------------------------------------------------------

    [Fact]
    public void All_Server_Types_Should_Reside_In_Server_Namespace()
    {
        var rule = Types()
            .That().ResideInAssembly(typeof(BookingPlatform.Server.Modules.Bookings.Features.CreateBooking.CreateBookingEndpoint).Assembly)
            .And().DoNotResideInNamespace("")
            .And().DoNotResideInNamespace("Microsoft.Extensions.Hosting")
            .Should().ResideInNamespaceMatching("^BookingPlatform\\.Server($|\\.).*");

        Assert.True(rule.HasNoViolations(SystemUnderTest), rule.Evaluate(SystemUnderTest).FirstOrDefault(r => !r.Passed)?.ToString());
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string? ExtractFeatureNamespace(string? fullNamespace)
    {
        if (string.IsNullOrEmpty(fullNamespace)) return null;

        var match = Regex.Match(fullNamespace, "^" + FeaturesNamespacePattern + "\\.[A-Za-z]+");
        return match.Success ? match.Value : null;
    }
}
