using System;

namespace Keewano.Internal
{
    public enum CustomEventType
    {
        //These indices must be matching the indices on our server
        None = 0,
        String = 1,
        UnsignedInt = 2,
        Bool = 3,
        Timestamp = 4,
        UnsignedShortVec2 = 5
    }

    [Serializable]
    public struct CustomEvent
    {
        public const ushort FIRST_CUSTOM_EVENT_ID = 2500;
        public string n;
        public CustomEventType t;

        public CustomEvent(string name, CustomEventType type)
        {
            n = name;
            t = type;
        }
    }

}