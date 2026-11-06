// Copyright (c) Spellbound Studios. All rights reserved.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using Spellbound.Core.Logging;
using Spellbound.Core.Observations;
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
        public void AnOperationWithABlankNameOrDescriptionIsSkippedWithOneLineEach() {
            var faulty = DynamicOperations.Assembly("Zeta.Faulty", "fixture", null,
                new DynamicOperation("BlankName", " ", "An operation with no name."),
                new DynamicOperation("BlankDescription", "blank-description", ""),
                new DynamicOperation("Sound", "sound", "An operation declared beside two faulty ones."));
            var catalog = new OperationCatalog(() => new[] { Fixture, faulty, Core });
            Assert.IsNotNull(catalog.Describe("fixture", "sound"));
            Assert.IsNull(catalog.Describe("fixture", "blank-description"));
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.IsNotNull(catalog.Describe("core", "help"));
            Assert.AreEqual(2, _recording.CountOf(LogLevel.Error));
            Assert.AreEqual(2, _recording.Count);
            Assert.AreEqual("Operation on Zeta.Faulty.Operations.BlankName has an empty name and was skipped.",
                _recording.Entries[0].Message);
            Assert.AreEqual("Operation on Zeta.Faulty.Operations.BlankDescription has an empty description and was"
                            + " skipped.", _recording.Entries[1].Message);
        }

        [Test]
        public void HelpCountsOnlyTheSoundOperationsBesideFaultyOnes() {
            var sound = new OperationCatalog(() => new[] { Fixture }).Group("fixture").Operations.Count;
            var faulty = DynamicOperations.Assembly("Zeta.FaultyHelp", "fixture", null,
                new DynamicOperation("BlankName", "", "An operation with no name."),
                new DynamicOperation("BlankDescription", "blank-description", "  "),
                new DynamicOperation("Sound", "sound", "An operation declared beside two faulty ones."));
            var catalog = new OperationCatalog(() => new[] { Fixture, faulty, Core });
            var settings = UnityEngine.ScriptableObject.CreateInstance<OperationSettings>();
            var host = new OperationHost(catalog, () => settings);
            var text = host.Run("core help", OperationCaller.Development(), ObservationContext.Discard).Result.Text;
            StringAssert.StartsWith("succeeded: 2 groups, ", text);
            StringAssert.Contains("\nfixture: Operations that exist so the catalog and the host can be tested. ("
                                  + (sound + 1) + " operations)", text);
            StringAssert.Contains("\ncore: ", text);
            Assert.AreEqual(2, _recording.Count);
        }

        [Test]
        public void AnOperationWhoseAttributeCannotBeReadIsSkippedWithOneLine() {
            var unreadable = DynamicOperations.UnreadableAssembly("Zeta.Unreadable", "fixture");
            var catalog = new OperationCatalog(() => new[] { Fixture, unreadable, Core });
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.IsNotNull(catalog.Describe("core", "help"));
            Assert.AreEqual(1, _recording.Count);
            StringAssert.StartsWith("Operation on Zeta.Unreadable.Operations.Run could not be read and was skipped: ",
                _recording.Entries[0].Message);
        }

        [Test]
        public void AnOperationAttributeKeepsABlankNameAndDescription() {
            var attribute = new OperationAttribute("", " ", OperationEffect.Reads);
            Assert.AreEqual("", attribute.Name);
            Assert.AreEqual(" ", attribute.Description);
        }

        [Test]
        public void AnArgumentAttributeKeepsABlankDescription() {
            Assert.AreEqual(" ", new ArgumentAttribute(" ").Description);
        }

        [Test]
        public void AnOperationGroupAttributeKeepsABlankName() {
            var attribute = new OperationGroupAttribute("", "A job.");
            Assert.AreEqual("", attribute.Name);
            Assert.AreEqual("A job.", attribute.Job);
            Assert.AreEqual(" ", new OperationGroupAttribute(" ").Name);
        }

        [Test]
        public void AnOperationWithABlankArgumentDescriptionIsSkippedWithOneLine() {
            var faulty = DynamicOperations.AssemblyWithArgument("Zeta.FaultyArgument", "fixture", "blank-argument",
                "text", " ");
            var catalog = new OperationCatalog(() => new[] { Fixture, faulty, Core });
            Assert.IsNull(catalog.Describe("fixture", "blank-argument"));
            Assert.IsNotNull(catalog.Describe("fixture", "sound"));
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.IsNotNull(catalog.Describe("core", "help"));
            Assert.AreEqual(1, _recording.Count);
            Assert.AreEqual("Operation 'fixture blank-argument' has an argument 'text' with an empty description and"
                            + " was skipped.", _recording.Entries[0].Message);
        }

        [Test]
        public void AGroupWithABlankNameIsSkippedWithOneLineAndTheRestLoads() {
            var faulty = DynamicOperations.Assembly("Zeta.BlankGroup", " ", null, "orphan");
            var catalog = new OperationCatalog(() => new[] { Fixture, faulty, Core });
            var settings = UnityEngine.ScriptableObject.CreateInstance<OperationSettings>();
            var host = new OperationHost(catalog, () => settings);
            var text = host.Run("core help", OperationCaller.Development(), ObservationContext.Discard).Result.Text;
            StringAssert.StartsWith("succeeded: 2 groups, ", text);
            StringAssert.Contains("\nfixture: Operations that exist so the catalog and the host can be tested. (",
                text);
            Assert.IsNotNull(catalog.Describe("core", "help"));
            Assert.IsNotNull(catalog.Describe("fixture", "echo"));
            Assert.AreEqual(0, catalog.FindByName("orphan").Count);
            Assert.AreEqual(1, _recording.Count);
            Assert.AreEqual("Operation group in Zeta.BlankGroup has an empty name and was skipped.",
                _recording.Entries[0].Message);
        }

        [Test]
        public void AnInstanceInAnAssemblyWithABlankGroupNameIsNotRegistered() {
            var faulty = DynamicOperations.InstanceType("Zeta.BlankGroupInstance", "", "orphan");
            var instance = Activator.CreateInstance(faulty);
            var catalog = new OperationCatalog(() => new[] { Fixture });
            Assert.AreEqual(0, catalog.Register(instance));
            Assert.AreEqual(1, _recording.Count);
            Assert.AreEqual("Operation group in Zeta.BlankGroupInstance has an empty name and was skipped.",
                _recording.Entries[0].Message);
        }

        [Test]
        public void AnInstanceInAnAssemblyWhoseGroupCannotBeReadIsNotRegistered() {
            var faulty = DynamicOperations.UnreadableInstanceType("Zeta.UnreadableGroupInstance", "orphan");
            var instance = Activator.CreateInstance(faulty);
            var catalog = new OperationCatalog(() => new[] { Fixture });
            Assert.AreEqual(0, catalog.Register(instance));
            Assert.AreEqual(0, catalog.FindByName("orphan").Count);
            Assert.AreEqual(1, _recording.Count);
            StringAssert.StartsWith("Operation group of Zeta.UnreadableGroupInstance could not be read and was"
                                    + " skipped: ", _recording.Entries[0].Message);
        }

        [Test]
        public void AnAssemblyWhoseGroupCannotBeReadIsLeftOutOfTheScanWithOneLine() {
            var unreadable = DynamicOperations.UnreadableGroupAssembly("Zeta.UnreadableGroup");
            var marked = MarkedAssemblyScan.Assemblies(new[] { Fixture, unreadable });
            CollectionAssert.AreEqual(new[] { Fixture }, marked);
            Assert.AreEqual(1, _recording.Count);
            StringAssert.StartsWith("Operation group of Zeta.UnreadableGroup could not be read and was skipped: ",
                _recording.Entries[0].Message);
        }

        [Test]
        public void AnOperationDeclaredTwiceKeepsTheFirstAndLogsTheSecond() {
            var duplicate = DynamicOperations.Assembly("Zeta.Duplicate", "fixture", null, "echo");
            var catalog = new OperationCatalog(() => new[] { Fixture, duplicate });
            var host = new OperationHost(catalog, () => UnityEngine.ScriptableObject.CreateInstance<OperationSettings>());
            Assert.AreEqual("succeeded: hi", host.Run("fixture echo hi", OperationCaller.Development(), ObservationContext.Discard).Result.Text);
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
            Log.Emit(LogLevel.Info, "OperationCatalog load: " + assemblies + " marked assemblies, " + groups + " groups, " + operations
                                  + " operations, cold " + cold.LoadMilliseconds.ToString("F3") + " ms, warm "
                                  + warm.LoadMilliseconds.ToString("F3") + " ms");
            Assert.AreEqual(groups, warmGroups);
            Assert.Greater(operations, 0);
        }

        private static class DynamicOperations {
            private static readonly ConstructorInfo OperationConstructor = typeof(OperationAttribute).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(OperationEffect) });

            private static readonly ConstructorInfo GroupConstructor = typeof(OperationGroupAttribute).GetConstructor(
                new[] { typeof(string), typeof(string) });

            public static Assembly Assembly(string assemblyName, string group, string job, string operationName) {
                var operation = new DynamicOperation("Run", operationName, "A dynamic operation.");
                return Assembly(assemblyName, group, job, operation);
            }

            public static Assembly Assembly(string assemblyName, string group, string job,
                params DynamicOperation[] operations) {
                var builder = DefineAssembly(assemblyName, group, job, out var type);

                foreach (var operation in operations) {
                    var method = DefineMethod(type, operation.MethodName);
                    method.SetCustomAttribute(new CustomAttributeBuilder(OperationConstructor,
                        new object[] { operation.Name, operation.Description, OperationEffect.Reads }));
                }

                type.CreateType();
                return builder;
            }

            public static Assembly UnreadableAssembly(string assemblyName, string group) {
                var builder = DefineAssembly(assemblyName, group, null, out var type);
                var method = DefineMethod(type, "Run");
                method.SetCustomAttribute(OperationConstructor, new byte[] { 1, 0, 5, (byte)'r' });
                type.CreateType();
                return builder;
            }

            public static Assembly AssemblyWithArgument(string assemblyName, string group, string operationName,
                string parameterName, string argumentDescription) {
                var builder = DefineAssembly(assemblyName, group, null, out var type);
                var attributes = MethodAttributes.Public | MethodAttributes.Static;
                var method = type.DefineMethod("WithArgument", attributes, typeof(string), new[] { typeof(string) });
                method.SetCustomAttribute(new CustomAttributeBuilder(OperationConstructor,
                    new object[] { operationName, "An operation whose argument is declared.", OperationEffect.Reads }));
                var parameter = method.DefineParameter(1, ParameterAttributes.None, parameterName);
                var argumentConstructor = typeof(ArgumentAttribute).GetConstructor(new[] { typeof(string) });
                var argument = new CustomAttributeBuilder(argumentConstructor, new object[] { argumentDescription });
                parameter.SetCustomAttribute(argument);
                var generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ret);
                var sound = DefineMethod(type, "Sound");
                sound.SetCustomAttribute(new CustomAttributeBuilder(OperationConstructor,
                    new object[] { "sound", "An operation declared beside a faulty one.", OperationEffect.Reads }));
                type.CreateType();
                return builder;
            }

            public static Type InstanceType(string assemblyName, string group, string operationName) {
                var builder = NewAssembly(assemblyName);
                MarkGroup(builder, group, null);
                return DefineInstanceType(builder, assemblyName, operationName);
            }

            public static Type UnreadableInstanceType(string assemblyName, string operationName) {
                var builder = NewAssembly(assemblyName);
                builder.SetCustomAttribute(GroupConstructor, new byte[] { 1, 0, 5, (byte)'r' });
                return DefineInstanceType(builder, assemblyName, operationName);
            }

            private static Type DefineInstanceType(AssemblyBuilder builder, string assemblyName, string operationName) {
                var module = builder.DefineDynamicModule(assemblyName);
                var type = module.DefineType(assemblyName + ".Instance", TypeAttributes.Public | TypeAttributes.Class);
                type.DefineDefaultConstructor(MethodAttributes.Public);
                var method = type.DefineMethod("Run", MethodAttributes.Public, typeof(string), Type.EmptyTypes);
                method.SetCustomAttribute(new CustomAttributeBuilder(OperationConstructor,
                    new object[] { operationName, "A dynamic instance operation.", OperationEffect.Reads }));
                var generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldstr, "dynamic");
                generator.Emit(OpCodes.Ret);
                return type.CreateType();
            }

            public static Assembly UnreadableGroupAssembly(string assemblyName) {
                var builder = NewAssembly(assemblyName);
                builder.SetCustomAttribute(GroupConstructor, new byte[] { 1, 0, 5, (byte)'r' });
                builder.DefineDynamicModule(assemblyName);
                return builder;
            }

            private static AssemblyBuilder DefineAssembly(string assemblyName, string group, string job,
                out TypeBuilder type) {
                var builder = NewAssembly(assemblyName);
                MarkGroup(builder, group, job);
                var module = builder.DefineDynamicModule(assemblyName);
                type = module.DefineType(assemblyName + ".Operations",
                    TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class);
                return builder;
            }

            private static AssemblyBuilder NewAssembly(string assemblyName) {
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            }

            private static void MarkGroup(AssemblyBuilder builder, string group, string job) {
                builder.SetCustomAttribute(new CustomAttributeBuilder(GroupConstructor, new object[] { group, job }));
            }

            private static MethodBuilder DefineMethod(TypeBuilder type, string methodName) {
                var attributes = MethodAttributes.Public | MethodAttributes.Static;
                var method = type.DefineMethod(methodName, attributes, typeof(string), Type.EmptyTypes);
                var generator = method.GetILGenerator();
                generator.Emit(OpCodes.Ldstr, "dynamic");
                generator.Emit(OpCodes.Ret);
                return method;
            }
        }

        private readonly struct DynamicOperation {
            public string MethodName { get; }
            public string Name { get; }
            public string Description { get; }

            public DynamicOperation(string methodName, string name, string description) {
                MethodName = methodName;
                Name = name;
                Description = description;
            }
        }
    }
}
