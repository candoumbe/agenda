#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Fluent.Syntax.Elements.Types.Classes;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using FastEndpoints;
using Xunit;
using Xunit.Categories;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace Agenda.API.ArchitecturalTests
{
    [UnitTest]
    public class VerticalSliceArchitectureTests
    {
        private static readonly Assembly s_apiAssembly = typeof(Program).Assembly;
        private static readonly Architecture s_apiArchitecture = new ArchLoader().LoadAssemblies(s_apiAssembly).Build();

        private static readonly IType s_endpointType = s_apiArchitecture.GetITypeOfType(typeof(IEndpoint));

        private static readonly IType s_endpointWithRequestAndResponse = s_apiArchitecture.GetITypeOfType(typeof(Endpoint<,>));


        private static GivenClassesConjunction EndpointsWithRequest => Endpoints
            .And().AreAssignableTo(s_endpointWithRequestAndResponse);

        private static GivenClassesConjunction EndpointsWithRequestAndResponse => Endpoints
            .And().AreAssignableTo(s_endpointWithRequestAndResponse);

        private static GivenClassesConjunctionWithDescription Endpoints => Classes().That().ResideInAssembly(s_apiAssembly)
            .And().AreNotAbstract()
            .And().AreAssignableTo(typeof(IEndpoint))
            .As("Endpoints");


        [Fact]
        public void Endpoints_should_be_in_vertical_slice_architecture()
        {
            IArchRule endpointsResideInResourceNamespace = Endpoints
                .Should().ResideInNamespaceMatching(@"Agenda.API.Features.*")
                .Because("Endpoints should be organized by feature (vertical slice) instead of technical details");

            IArchRule endpointsResideInItsOwnNamespace = Endpoints
                .Should()
                .FollowCustomCondition(endpoint =>
                                       {
                                           IEnumerable<Class> otherEndpoints = endpoint.Namespace.Classes.Where(c => !Equals(c, endpoint)
                                                                                                                     && c.IsAssignableTo(s_endpointType));

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
            IArchRule endpointResideInSameNamespaceAsRequest = EndpointsWithRequest
                .Should()
                .FollowCustomCondition(endpoint =>
                                       {
                                           GenericArgument request = endpoint.GetInheritsBaseClassDependencies().First().TargetGenericArguments.First();
                                           IType requestType = request.Type;

                                           Namespace requestNamespace = requestType.Namespace;
                                           bool requestNamespaceIsSameAsEndpointNamespace = requestNamespace.Equals(endpoint.Namespace);
                                           return new ConditionResult(endpoint,
                                                                      requestNamespaceIsSameAsEndpointNamespace,
                                                                      $"should not use request type reside in the same '{endpoint.Namespace}' namespace ('{requestType.Name}' is in '{requestNamespace}')");
                                       },
                                       "reside in the same namespace as its request");

            endpointResideInSameNamespaceAsRequest.Check(s_apiArchitecture);
        }
    }
}
#endif