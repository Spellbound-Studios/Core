// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Operations;
using Spellbound.Core.Tests.OperationFixture;
using Spellbound.Core.Tests.OperationFixture.Second;

namespace Spellbound.Core.Tests {
    public class OperationCatalogTests {
        private static readonly Assembly Fixture = typeof(FixtureOperations).Assembly;
        private static readonly Assembly Second = typeof(SecondFixtureOperations).Assembly;
        private static readonly Assembly Core = typeof(OperationCatalog).Assembly;

        private RecordingLogSink _recording;
        private Log.SinkSuspension _suspension;

        [SetUp]
        public void SetUp() {
            _suspension = Log.SuspendSinks();
            _recording = new RecordingLogSink(LogLevel.Verbose);
            Log.AddSink(_recording, null, LogLevel.Verbose);
        }

        [TearDown]
        public void TearDown() {
            Log.RemoveSink(_recording);
            _suspension.Dispose();
        }

        [Test]
        public void DefaultCatalogFindsTheCoreAndFixtureGroups() {
            var catalog = new OperationCatalog();
            var core = catalog.Group("core");
            var fixture = catalog.Group("fixture");
            Assert.IsNotNull(core);
            Assert.IsNotNull(fixture);
            Assert.IsNotNull(core.Job);
            Assert.IsNotNull(catalog.Describe("core", "help"));
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.AreEqual(0, _recording.Count, string.Join("\n", _recording.Entries.Select(entry => entry.Message)));
        }

        [Test]
        public void ASecondAssemblyWithoutAJobMergesIntoTheGroup() {
            var catalog = new OperationCatalog(() => new[] { Fixture, Second });
            var group = catalog.Group("fixture");
            Assert.AreEqual(2, group.Assemblies.Count);
            Assert.IsNotNull(catalog.Describe("fixture", "second-echo"));
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.AreEqual("Operations that exist so the catalog and the host can be tested.", group.Job);
            Assert.AreEqual(0, _recording.Count);
        }

        [Test]
        public void TwoGroupsWithOneNameAndTwoJobsKeepTheFirstAndLogTheSecond() {
            var clash = DynamicOperations.Assembly("Zeta.Clash", "fixture", "Another job entirely", "clash-only");
            var catalog = new OperationCatalog(() => new[] { Fixture, clash });
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.IsNull(catalog.Describe("fixture", "clash-only"));
            CollectionAssert.AreEqual(new[] { Fixture.GetName().Name }, catalog.Group("fixture").Assemblies);
            Assert.AreEqual(1, _recording.Count);
            StringAssert.Contains("Two operation groups are named 'fixture'", _recording.Entries[0].Message);
            StringAssert.Contains("Zeta.Clash", _recording.Entries[0].Message);
        }

        [Test]
        public void AnOperationDeclaredTwiceKeepsTheFirstAndLogsTheSecond() {
            var duplicate = DynamicOperations.Assembly("Zeta.Duplicate", "fixture", null, "echo");
            var catalog = new OperationCatalog(() => new[] { Fixture, duplicate });
            var host = new OperationHost(catalog, () => UnityEngine.ScriptableObject.CreateInstance<OperationSettings>());
            Assert.AreEqual("succeeded: hi", host.Run("fixture echo hi", OperationCaller.Development()).Result.Text);
            Assert.AreEqual(1, _recording.Count);
            StringAssert.Contains("declared twice", _recording.Entries[0].Message);
        }

        [Test]
        public void IdentifierIsTheStableHashOfGroupDotName() {
            var catalog = new OperationCatalog(() => new[] { Fixture });
            var echo = catalog.Describe("fixture", "echo");
            Assert.AreEqual(Hashing.StableHash.Fnv1A32("fixture.echo"), echo.Identifier);
            Assert.AreSame(echo, catalog.Describe(echo.Identifier));
        }

        [Test]
        public void DescriptionCarriesArgumentsEffectAudienceAndAvailability() {
            var catalog = new OperationCatalog(() => new[] { Fixture });
            var add = catalog.Describe("fixture", "add");
            Assert.AreEqual(2, add.Arguments.Count);
            Assert.AreEqual("left", add.Arguments[0].Name);
            Assert.IsFalse(add.Arguments[0].HasDefault);
            Assert.AreEqual("right", add.Arguments[1].Name);
            Assert.IsTrue(add.Arguments[1].HasDefault);
            Assert.AreEqual("1", add.Arguments[1].DefaultText);
            Assert.AreEqual("The left operand.", add.Arguments[0].Summary);
            Assert.AreEqual(OperationEffect.Reads, add.Effect);
            Assert.AreEqual(OperationAudiences.Developer, add.Audience);
            Assert.AreEqual(OperationAvailability.Anytime, add.Availability);
            Assert.AreEqual("fixture add --left <integer> [--right <integer> = 1]", add.Usage());
            var secret = catalog.Describe("fixture", "secret");
            Assert.AreEqual(OperationAudiences.Approver, secret.Audience);
            Assert.AreEqual(OperationEffect.Writes, secret.Effect);
            Assert.AreEqual(OperationAvailability.WhilePlaying, catalog.Describe("fixture", "playing-only").Availability);
            CollectionAssert.AreEqual(new[] { "Common", "Rare", "Epic" }, catalog.Describe("fixture", "tier").Arguments[0].LegalValues);
        }

        [Test]
        public void ParameterNamesBecomeKebabCase() {
            var catalog = new OperationCatalog(() => new[] { Fixture });
            Assert.AreEqual("max-hit-points", catalog.Describe("fixture", "camel-case-name").Arguments[0].Name);
        }

        [Test]
        public void ContextParameterIsNotAnArgument() {
            var catalog = new OperationCatalog(() => new[] { Fixture });
            var help = catalog.Describe("fixture", "wipe");
            Assert.AreEqual(0, help.Arguments.Count);
        }

        [Test]
        public void FindListsByPrefixAndFindByNameAcrossGroups() {
            var catalog = new OperationCatalog(() => new[] { Fixture, Core });
            var found = catalog.Find("fixture e");
            CollectionAssert.AreEquivalent(new[] { "fixture echo", "fixture editing-only" }, found.Select(operation => operation.Line));
            Assert.AreEqual(1, catalog.FindByName("help").Count);
            Assert.AreEqual(0, catalog.FindByName("absent").Count);
        }

        [Test]
        public void GroupsAndOperationsAreSorted() {
            var catalog = new OperationCatalog(() => new[] { Fixture, Core });
            CollectionAssert.AreEqual(new[] { "core", "fixture" }, catalog.Groups.Select(group => group.Name));
            var names = catalog.Group("fixture").Operations.Select(operation => operation.Name).ToArray();
            CollectionAssert.AreEqual(names.OrderBy(name => name, StringComparer.Ordinal), names);
        }

        [Test]
        public void RegisteredInstanceAddsLiveOperationsUntilUnregistered() {
            var catalog = new OperationCatalog(() => new[] { Fixture });
            var counter = new FixtureLiveCounter();
            Assert.IsNull(catalog.Describe("fixture", "count-up"));
            Assert.AreEqual(1, catalog.Register(counter));
            Assert.IsNotNull(catalog.Describe("fixture", "count-up"));
            Assert.AreEqual(0, catalog.Register(counter));
            Assert.AreEqual(1, catalog.Unregister(counter));
            Assert.IsNull(catalog.Describe("fixture", "count-up"));
            Assert.AreEqual(0, catalog.Unregister(counter));
        }

        [Test]
        public void MarkedAssemblyScanFindsOnlyMarkedAssemblies() {
            var marked = MarkedAssemblyScan.Assemblies(new[] { Fixture, typeof(NUnit.Framework.Assert).Assembly, Core });
            CollectionAssert.AreEqual(new[] { Core.GetName().Name, Fixture.GetName().Name }, marked.Select(assembly => assembly.GetName().Name));
            var parsers = MarkedAssemblyScan.TypesWith<ArgumentParserAttribute>(new[] { Fixture });
            CollectionAssert.AreEqual(new[] { typeof(FixtureCoordinateParser) }, parsers);
        }

        [Test]
        public void ReloadForgetsRegisteredInstances() {
            var catalog = new OperationCatalog(() => new[] { Fixture });
            catalog.Register(new FixtureLiveCounter());
            catalog.Reload();
            Assert.IsNull(catalog.Describe("fixture", "count-up"));
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
        }

        [Test]
        public void LoadCostIsMeasuredOnTheProjectAssemblySet() {
            var cold = new OperationCatalog();
            var groups = cold.Groups.Count;
            var operations = cold.Groups.Sum(group => group.Operations.Count);
            var warm = new OperationCatalog();
            var warmGroups = warm.Groups.Count;
            var assemblies = cold.MarkedAssemblies().Count;
            UnityEngine.Debug.Log("OperationCatalog load: " + assemblies + " marked assemblies, " + groups + " groups, " + operations
                                  + " operations, cold " + cold.LoadMilliseconds.ToString("F3") + " ms, warm "
                                  + warm.LoadMilliseconds.ToString("F3") + " ms");
            Assert.AreEqual(groups, warmGroups);
            Assert.Greater(operations, 0);
        }

        private static class DynamicOperations {
            public static Assembly Assembly(string assemblyName, string group, string job, string operationName) {
                var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
                var groupConstructor = typeof(OperationGroupAttribute).GetConstructor(new[] { typeof(string), typeof(string) });
                builder.SetCustomAttribute(new CustomAttributeBuilder(groupConstructor, new object[] { group, job }));
                var module = builder.DefineDynamicModule(assemblyName);
                var type = module.DefineType(assemblyName + ".Operations",
                    TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class);
                var method = type.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);
                var operationConstructor = typeof(OperationAttribute).GetConstructor(new[] { typeof(string), typeof(string), typeof(OperationEffect) });
                method.SetCustomAttribute(new CustomAttributeBuilder(operationConstructor,
                    new object[] { operationName, "A dynamic operation.", OperationEffect.Reads }));
                var generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldstr, "dynamic");
                generator.Emit(OpCodes.Ret);
                type.CreateType();
                return builder;
            }
        }
    }
}
