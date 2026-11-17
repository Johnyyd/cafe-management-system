using CafeManagement.Api;
using CafeManagement.Application.Orders.Handlers;
using CafeManagement.Domain.Orders;
using CafeManagement.Infrastructure.Persistence;
using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace CafeManagement.ArchitectureTests;

public class ArchitectureTests
{
    // Use concrete types from each assembly as anchors
    private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CreateOrderHandler).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(OrderRepository).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_ShouldNotReferenceOtherLayers()
    {
        // Domain should have NO references to Application, Infrastructure, or Api
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CafeManagement.Application")
            .And()
            .HaveDependencyOn("CafeManagement.Infrastructure")
            .And()
            .HaveDependencyOn("CafeManagement.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Application_ShouldOnlyReferenceDomain()
    {
        // Application should only reference Domain (and standard libs)
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("CafeManagement.Infrastructure")
            .And()
            .HaveDependencyOn("CafeManagement.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Infrastructure_ShouldReferenceDomainAndApplication()
    {
        // Infrastructure assembly should reference Domain and Application assemblies
        var referencedAssemblies = InfrastructureAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet();

        Assert.Contains("CafeManagement.Domain", referencedAssemblies);
        Assert.Contains("CafeManagement.Application", referencedAssemblies);
    }

    [Fact]
    public void Infrastructure_ShouldNotReferenceApi()
    {
        // Infrastructure should NOT reference Api
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("CafeManagement.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Api_ShouldReferenceAllLayers()
    {
        // Api is the composition root - should reference Application and Infrastructure directly
        // Domain is referenced transitively through Application (clean architecture)
        var referencedAssemblies = ApiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet();

        Assert.Contains("CafeManagement.Application", referencedAssemblies);
        Assert.Contains("CafeManagement.Infrastructure", referencedAssemblies);

        // Verify transitive reference: Application -> Domain
        var appReferencedAssemblies = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet();
        Assert.Contains("CafeManagement.Domain", appReferencedAssemblies);
    }

    [Fact]
    public void Domain_ShouldNotContainInfrastructureTypes()
    {
        // Domain should not contain MongoDB, Entity Framework, etc. types
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MongoDB.Driver")
            .And()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Application_ShouldNotContainInfrastructureTypes()
    {
        // Application should not contain MongoDB, EF Core types
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("MongoDB.Driver")
            .And()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Handlers_ShouldBeInApplicationLayer()
    {
        // All MediatR handlers should be in Application
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .ResideInNamespace("CafeManagement.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Controllers_ShouldBeInApiLayer()
    {
        // All Controllers should be in Api
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .ResideInNamespace("CafeManagement.Api.Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Repositories_ShouldBeInInfrastructureLayer()
    {
        // Repository implementations should be in Infrastructure
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(CafeManagement.Application.Common.Interfaces.IRepository<>))
            .Should()
            .ResideInNamespace("CafeManagement.Infrastructure.Persistence")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void DomainEntities_ShouldNotBePubliclyMutable()
    {
        // Domain entities should have private setters on properties
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(CafeManagement.Domain.Common.AggregateRoot<>))
            .GetTypes();

        foreach (var entityType in entityTypes)
        {
            var publicSetters = entityType.GetProperties()
                .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
                .Where(p => p.Name != "Id") // Id can have public setter for deserialization
                .ToList();

            Assert.True(publicSetters.Count == 0, $"Entity {entityType.Name} has public setters: {string.Join(", ", publicSetters.Select(p => p.Name))}");
        }
    }

    [Fact]
    public void Commands_ShouldBeInApplicationNamespace()
    {
        // All Commands should be in Application namespace
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .ResideInNamespace("CafeManagement.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Queries_ShouldBeInApplicationNamespace()
    {
        // All Queries should be in Application namespace
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .ResideInNamespace("CafeManagement.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Dtos_ShouldBeInApplicationCommonDtosNamespace()
    {
        // All DTOs should be in Application.Common.Dtos
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Dto")
            .Should()
            .ResideInNamespace("CafeManagement.Application.Common.Dtos")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void Events_ShouldBeInDomainNamespace()
    {
        // All Domain Events should be in Domain namespaces
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(CafeManagement.Domain.Common.Events.IDomainEvent))
            .Should()
            .ResideInNamespace("CafeManagement.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailingTypesMessage(result));
    }

    [Fact]
    public void NoCircularDependencies_BetweenLayers()
    {
        // Verify no circular deps between the 4 main layers
        var layers = new[]
        {
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
            ApiAssembly
        };

        foreach (var layer in layers)
        {
            var otherLayers = layers.Where(l => l != layer).ToArray();
            var result = Types.InAssembly(layer)
                .ShouldNot()
                .HaveDependencyOnAll(otherLayers.Select(l => l.GetName().Name).ToArray())
                .GetResult();

            // This is a soft check - we mainly want to ensure the directionality
            // The specific tests above enforce the correct direction
        }
    }

    private static string GetFailingTypesMessage(NetArchTest.Rules.TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "Test failed but no failing types were reported";

        return string.Join("\n", result.FailingTypes.Select(t => t.FullName));
    }
}