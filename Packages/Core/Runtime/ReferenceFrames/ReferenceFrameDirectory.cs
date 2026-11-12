// Copyright (c) Spellbound Studios. All rights reserved.

using System.Collections.Generic;

namespace Spellbound.Core.ReferenceFrames {
    public sealed class ReferenceFrameDirectory {
        private readonly Dictionary<ReferenceFrameId, IReferenceFrame> _frames =
            new Dictionary<ReferenceFrameId, IReferenceFrame>();

        public static IReferenceFrame World => WorldReferenceFrame.Instance;

        public int Count => _frames.Count;

        public bool TryRegister(IReferenceFrame frame) {
            if (frame == null || frame.Id.IsWorld || _frames.ContainsKey(frame.Id)) {
                return false;
            }

            _frames.Add(frame.Id, frame);
            return true;
        }

        public bool Unregister(ReferenceFrameId id) {
            return !id.IsWorld && _frames.Remove(id);
        }

        public bool TryGet(ReferenceFrameId id, out IReferenceFrame frame) {
            if (id.IsWorld) {
                frame = World;
                return true;
            }

            return _frames.TryGetValue(id, out frame);
        }

        public bool Contains(ReferenceFrameId id) {
            return id.IsWorld || _frames.ContainsKey(id);
        }
    }
}
