// Copyright 2026 Spellbound Studio Inc.

using System.Threading.Tasks;
using Spellbound.Core.Packing;

namespace Spellbound.Core.Objects {
    public interface IAwardReciever {
        Task<bool> Award<T>(T award) where T : ISmartPacker;
    }
}