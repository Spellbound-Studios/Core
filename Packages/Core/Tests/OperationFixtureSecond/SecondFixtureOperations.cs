// Copyright (c) Spellbound Studios. All rights reserved.

using Spellbound.Core.Operations;

[assembly: OperationGroup("fixture")]

namespace Spellbound.Core.Tests.OperationFixture.Second {
    public static class SecondFixtureOperations {
        [Operation("second-echo", "Returns the text it was given, from the second assembly of the group.", OperationEffect.Reads)]
        public static string SecondEcho([Argument("The text to return.")] string text) => "second " + text;
    }
}
