using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using FastEndpoints;
using Xunit;
using Xunit.Categories;
using Assembly = System.Reflection.Assembly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Agenda.API.ArchitecturalTests;

[UnitTest]
public class VerticalSliceArchitectureTests
{
    private static readonly Assembly s_apiAssembly = typeof(Program).Assembly;
    private static Architecture s_apiArchitecture = new ArchLoader().LoadAssemblies(s_apiAssembly).Build();

    private static IType IEndpointType => s_apiArchitecture.GetITypeOfType(typeof(IEndpoint));

    private static readonly IType s_endpointWithRequestAndResponse = s_apiArchitecture.GetITypeOfType(typeof(Endpoint<,>));
    private static readonly IType s_endpointWithRequestOnly = s_apiArchitecture.GetITypeOfType(typeof(Endpoint<>));

    private static IObjectProvider<Class> EndpointsWithRequest => Classes().That().Are(Endpoints)
        .And()
        .AreAssignableTo(s_endpointWithRequestAndResponse);

    private static IObjectProvider<Class> EndpointsWithRequestAndResponse => Classes().That().Are(Endpoints)
        .And().AreAssignableTo(s_endpointWithRequestAndResponse)
        .And().AreNotAssignableTo(s_endpointWithRequestOnly);


    private static IObjectProvider<Class> Endpoints => Classes().That().ResideInAssembly(s_apiAssembly)
        .And().AreNotAbstract()
        .And().AreAssignableTo(typeof(IEndpoint));


    [Fact]
    public void Endpoints_should_be_in_vertical_slice_architecture()
    {
        IArchRule endpointsResideInResourceNamespace = Classes().That()
            .Are(Endpoints)
            .Should().ResideInNamespaceMatching(@"Agenda.API.Resources.*")
            .Because("Endpoints should be organized by feature (vertical slice) instead of technical details");

        IArchRule endpointsResideInItsOwnNamespace = Classes().That().Are(Endpoints)
            .Should()
            .FollowCustomCondition(endpoint =>
                                   {
                                       IEnumerable<Class> otherEndpoints = endpoint.Namespace.Classes.Where(c => !Equals(c,endpoint)
                                                                                                              && c.IsAssignableTo(IEndpointType));

                                       return new ConditionResult(endpoint,
                                                                  !otherEndpoints.Any(),
                                                                  "should be in its own namespace");
                                   },
                                   "be in its own namespace");

        endpointsResideInResourceNamespace
            .And(endpointsResideInItsOwnNamespace)
            .Check(s_apiArchitecture);

    }

    [Fact]
    public void Endpoint_should_reside_in_the_same_namespace_as_its_request()
    {
        IArchRule endpointResideInSameNamespaceAsRequest = Classes().That().Are(EndpointsWithRequest)
            .Should()
            .FollowCustomCondition(endpoint =>
                                   {
                                       GenericArgument request = endpoint.GetInheritsBaseClassDependencies().First().TargetGenericArguments.First();
                                       IType requestType = request.Type;
                                       Namespace requestNamespace = requestType.Namespace;
                                       bool requestNamespaceIsSameAsEndpointNamespace = requestNamespace.Equals(endpoint.Namespace);
                                       return new ConditionResult(endpoint,
                                                                  requestNamespaceIsSameAsEndpointNamespace,
                                                                  $"should reside in the same '{endpoint.Namespace}' namespace alongside the request it handles");
                                   },
                                   "reside in the same namespace as its request");

        endpointResideInSameNamespaceAsRequest.Check(s_apiArchitecture);
    }
}