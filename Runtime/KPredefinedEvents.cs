namespace Keewano.Internal
{
    internal enum KEvents : ushort
    {
        APP_LAUNCH = 2,
        SESSION_START = 3,
        SESSION_END = 4,
        GENUINITY_CHECK = 5,
        DEVICE_TYPE = 6,
        GPU_TYPE = 7,
        OS = 8,
        RAM_SIZE = 9,
        VRAM_SIZE = 10,
        SCREEN_RESOLUTION = 11,
        SYSTEM_LANG = 12,
        ERROR_MSG = 13,
        LOW_MEM_WARNING = 14,
        INSTALL_CAMPAIGN = 15,
        SCENE_LOADED = 16,
        SCENE_UNLOADED = 17,
        DEEP_LINK_ACTIVATED = 18,
        INTERNET_DISCONNECTED = 19,
        BUTTON_CLICK = 20,
        EMPTY_SPACE_CLICK = 21,
        WINDOW_OPEN = 22,
        WINDOW_CLOSE = 23,
        ITEMS_EXCHANGE = 24,
        DAY_IN_GAME_STARTED = 25,
        COUNTRY = 26,

        APP_PAUSE = 28,
        APP_RESUME = 29,
        INTERNET_CONNECTED = 30,

        PURCHASE_PRODUCT_ID = 32,
        PURCHASE_PRODUCT_PRICE_USD_CENTS = 33,
        PLATFORM = 34,

        PURCHASE_TIMESTAMP = 35,
        AB_TEST_ASSIGNMENT = 36,

        ITEMS_RESET = 37,
        USER_ID_ASSIGNED = 38,

        POINTER1_DOWN = 39,
        POINTER1_UP = 40,

        BATCH_DROPPED = 42,

        GAME_LANG = 43,
        ONBOARDING_MILESTONE = 50,
        ITEMS_PURCHASED_GRANT = 54,

    }

    enum KBatchDropReason : uint
    {
        BROKEN_CUSTOM_EVENT_MAPPING = 1,
        TOO_MANY_UNSENT_EVENTS = 2
    }
}
