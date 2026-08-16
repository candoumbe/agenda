#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Fluent.Syntax.Elements.Types.Classes;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using FastEndpoints;
using Xunit;
using Xunit.OpenCategories.V3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Architecture = ArchUnitNET.Domain.Architecture;
using Assembly = System.Reflection.Assembly;

namespace Agenda.API.ArchitecturalTests
{
    [UnitTest]
    public class AuthenticationArchitectureTests
    {
        private static readonly Assembly s_apiAssembly = typeof(Program).Assembly;
        private static readonly Architecture s_apiArchitecture = new ArchLoader().LoadAssemblies(s_apiAssembly).Build();

        // TODO(#578): remove allow-list entries once these endpoints drop AllowAnonymous().
        // Phase 4 surfaced these as still-anonymous endpoints inherited from before Keycloak; tracked separately so the
        // architecture rule remains green and Bishop can revisit the actual security decisions.
        private static readonly HashSet<string> s_anonymousEndpointAllowList =
        [
            "Agenda.API.Features.Appointments.v1.Create.CreateAppointmentEndpoint",
            "Agenda.API.Features.Appointments.v1.GetById.GetAppointmentByIdEndpoint",
            "Agenda.API.Features.Appointments.v1.Search.SearchAppointmentsEndpoint",
            "Agenda.API.Features.Appointments.v1.Update.PatchAppointmentByIdEndpoint",
        ];

        private static GivenClassesConjunctionWithDescription Endpoints => Classes().That().ResideInAssembly(s_apiAssembly)
            .And().AreNotAbstract()
            .And().AreAssignableTo(typeof(IEndpoint))
            .As("Endpoints");

        [Fact]
        public void Endpoints_should_be_authenticated_or_explicitly_allow_listed()
        {
            IArchRule rule = Endpoints
                .Should()
                .FollowCustomCondition(endpoint =>
                                       {
                                           bool callsAllowAnonymous = endpoint.Members
                                               .SelectMany(member => member.GetMethodCallDependencies())
                                               .Any(dependency => dependency.TargetMember.Name.StartsWith("AllowAnonymous"));

                                           bool isAllowListed = s_anonymousEndpointAllowList.Contains(endpoint.FullName);

                                           bool conforms = !callsAllowAnonymous || isAllowListed;

                                           return new ConditionResult(endpoint,
                                                                      conforms,
                                                                      $"calls AllowAnonymous() but is not in the explicit anonymous allow-list ('{endpoint.FullName}')");
                                       },
                                       "be authenticated or appear in the named anonymous allow-list");

            rule.Check(s_apiArchitecture);
        }

        [Fact]
        public void Authentication_types_should_live_outside_features()
        {
            IArchRule rule = Classes().That().ResideInAssembly(s_apiAssembly)
                .And().HaveNameMatching(".*(JwtBearerOptions|KeycloakOptions|AuthenticationHandler).*")
                .Should().NotResideInNamespaceMatching(@"Agenda\.API\.Features\..*")
                .Because("authentication wiring must remain a cross-cutting concern outside the vertical slices")
                .WithoutRequiringPositiveResults();

            rule.Check(s_apiArchitecture);
        }

        [Fact]
        public void Features_should_not_reference_the_keycloak_sdk_directly()
        {
            IArchRule rule = Classes().That().ResideInAssembly(s_apiAssembly)
                .And().ResideInNamespaceMatching(@"Agenda\.API\.Features\..*")
                .Should()
                .FollowCustomCondition(featureClass =>
                                       {
                                           IEnumerable<IType> dependencies = featureClass.GetTypeDependencies();
                                           IType offending = dependencies.FirstOrDefault(IsForbiddenAuthDependency);

                                           return new ConditionResult(featureClass,
                                                                      offending is null,
                                                                      $"references forbidden authentication SDK type '{offending?.FullName}'");
                                       },
                                       "not reference Aspire.Keycloak / Keycloak / JwtBearer SDK types directly");

            rule.Check(s_apiArchitecture);
        }

        private static bool IsForbiddenAuthDependency(IType type)
        {
            string ns = type.Namespace?.FullName ?? string.Empty;
            return ns.StartsWith("Aspire.Keycloak", System.StringComparison.Ordinal)
                   || ns.StartsWith("Keycloak.", System.StringComparison.Ordinal)
                   || ns.StartsWith("Microsoft.AspNetCore.Authentication.JwtBearer", System.StringComparison.Ordinal);
        }
    }
}
#endif
