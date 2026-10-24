// Copyright 2026 Spellbound Studio Inc.

using System.Threading.Tasks;
using Spellbound.Core.ObjectData;
using Spellbound.Core.Packing;

namespace Spellbound.Core.Surfaces {
    public interface IDispatchSurface {
        bool Dispatch<TDispatch>(TDispatch dispatch)
                where TDispatch : IPackerDispatch;

    }
}