using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using System.Runtime.InteropServices;
using System.IO;
using Keewano;
using Keewano.Internal;

#pragma warning disable IDE1006 //We use other convention for function names, PublicFunc vs privateFunc 
#pragma warning disable S2696, S101, S3903

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100000)] //Needed to intercept button clicks before click callback
public partial class KeewanoSDK : MonoBehaviour
{
    private static KeewanoSDK m_instance;
    private bool m_internetReachable;
    private readonly object m_exceptionReportLock = new object();
    private KEventDispatcher m_dispatcher;
    private bool m_disableAutomaticButtonClickTracking;

    struct UserIdentifiers
    {
        public Guid InstallId;
        public Guid UserId;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void initBeforeSceneLoad()
    {
        GameObject go = new GameObject("Keewano");
        m_instance = go.AddComponent<KeewanoSDK>();
        DontDestroyOnLoad(go);
        KeewanoSettings settings = KeewanoSettings.Load();
        m_instance.Init(settings);
    }

    void Init(KeewanoSettings settings)
    {
        if (string.IsNullOrEmpty(settings.APIKey))
            Debug.LogError("[KeewanoSDK] No API key was provided.");

        m_disableAutomaticButtonClickTracking = settings.disableButtonTracking;

        UserIdentifiers uid = loadOrInitIdentifiers();

        if (!loadUserConsentState(out UserConsentState consentState))
        {
            consentState = settings.requirePlayerConsent ? UserConsentState.Pending : UserConsentState.NotRequired;
            atomicSaveUserConsentState(consentState);
        }

        Application.lowMemory += handleLowMemoryWarning;
        Application.logMessageReceivedThreaded += handleLogMessageReceivedThreaded;
        Application.deepLinkActivated += handleOnDeepLinkActivated;

        Guid dataSessionId = Guid.NewGuid();

#if KEEWANO_TEST_ENDPOINT
        //Used for internal testing
        string endpoint = Environment.GetEnvironmentVariable("KEEWANO_TEST_ENDPOINT");
        settings.APIKey = Environment.GetEnvironmentVariable("KEEWANO_TEST_API_KEY");
#else
        const string endpoint = "https://api.keewano.com/event/ingress/v1/data";
#endif

        string dispatcherWorkingDir = Application.persistentDataPath + "/Keewano/";
        m_dispatcher = new KEventDispatcher(dispatcherWorkingDir, endpoint, settings.APIKey, consentState, uid.InstallId, uid.UserId, dataSessionId);

        m_dispatcher.addEvent((ushort)KEvents.APP_LAUNCH, Application.version);

        if (Application.genuineCheckAvailable && !Application.genuine)
            m_dispatcher.addEvent((ushort)KEvents.GENUINITY_CHECK, false);

        m_dispatcher.addEvent((ushort)KEvents.PLATFORM, Application.platform.ToString());
        m_dispatcher.addEvent((ushort)KEvents.DEVICE_TYPE, SystemInfo.deviceModel);
        m_dispatcher.addEvent((ushort)KEvents.GPU_TYPE, SystemInfo.graphicsDeviceName);
        m_dispatcher.addEvent((ushort)KEvents.OS, SystemInfo.operatingSystem);
        m_dispatcher.addEvent((ushort)KEvents.RAM_SIZE, SystemInfo.systemMemorySize);
        m_dispatcher.addEvent((ushort)KEvents.VRAM_SIZE, SystemInfo.graphicsMemorySize);
        m_dispatcher.addEvent((ushort)KEvents.SCREEN_RESOLUTION, (ushort)Screen.currentResolution.width,
            (ushort)Screen.currentResolution.height);
        m_dispatcher.addEvent((ushort)KEvents.SYSTEM_LANG, Application.systemLanguage.ToString());

        SceneManager.sceneLoaded += handleSceneLoaded;
        SceneManager.sceneUnloaded += handleSceneUnloaded;
    }

    private void handleOnDeepLinkActivated(string link)
    {
        m_dispatcher.addEventLongString((ushort)KEvents.DEEP_LINK_ACTIVATED, link);
    }

    private static void handleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        m_instance.m_dispatcher.addEvent((ushort)KEvents.SCENE_LOADED, scene.name);
    }

    private static void handleSceneUnloaded(Scene scene)
    {
        m_instance.m_dispatcher.addEvent((ushort)KEvents.SCENE_UNLOADED, scene.name);
    }

    void OnDestroy()
    {
        SceneManager.sceneUnloaded -= handleSceneUnloaded;
        SceneManager.sceneLoaded -= handleSceneLoaded;
        Application.deepLinkActivated -= handleOnDeepLinkActivated;
        Application.logMessageReceived -= handleLogMessageReceivedThreaded;
        Application.lowMemory -= handleLowMemoryWarning;

        m_dispatcher.Stop();
    }

    void Update()
    {
        if (!m_disableAutomaticButtonClickTracking)
        {
            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    GameObject clicked = eventSystem.currentSelectedGameObject;

                    if (clicked && clicked.TryGetComponent<Button>(out Button btn))
                        ReportButtonClick(btn.name);
                }
                else
                    Debug.LogWarning("Keewano SDK: EventSystem not found in the scene; automatic button-click detection unavailable.");
            }

#if UNITY_ANDROID
            if (Input.GetKeyDown(KeyCode.Escape))
                m_instance.m_dispatcher.ReportButtonClick("Android Device Back Button");
#endif
        }
        checkInternetReachability();
    }


    private void handleLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
            lock (m_exceptionReportLock)
            {
                LogError($"Caught exception {condition} at {stackTrace}");
            }
    }

    private void handleLowMemoryWarning()
    {
        m_dispatcher.ReportLowMemory();
    }

    private void checkInternetReachability()
    {
        bool connected = Application.internetReachability != NetworkReachability.NotReachable;
        if (connected != m_internetReachable)
        {
            m_internetReachable = connected;
            if (m_internetReachable)
                m_dispatcher.ReportInternetConnected();
            else
                m_dispatcher.ReportInternetDisconnected();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            m_dispatcher.ReportAppPause();
            m_dispatcher.SendNow();
        }
        else
            m_dispatcher.ReportAppResume();
    }

#if UNITY_EDITOR
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            m_dispatcher.SendNow();

    }
#endif

    /**
     @brief Sets the user's consent for data collection and analytics.

     This method is used to inform the KeewanoSDK whether the player has granted or denied consent for data collection,
     in compliance with privacy regulations such as GDPR and CCPA.

     If the "Require Player Consent" option is enabled, the SDK will buffer analytics data locally until you call this method:
     - If you call `SetUserConsent(true)`, all buffered data will be sent to the server, and the SDK will continue to collect and send analytics data from that point onward.
     - If you call `SetUserConsent(false)`, all buffered data will be discarded and no further data will be collected or sent.

     If the "Require Player Consent" option is disabled, the SDK will start collecting and sending data automatically on app launch.

     @param consentGiven
         Pass `true` if the player has granted consent for data collection, or `false` if the player has denied consent.

     Usage:
     - Show your own consent dialog to the player at app launch or at an appropriate time.
     - Call `KeewanoSDK.SetUserConsent(true)` or `KeewanoSDK.SetUserConsent(false)` based on the player's choice.

    @sa 
    Please refer to @ref DataPrivacy for more information on the topic.
     */
    static public void SetUserConsent(bool consentGiven)
    {
        UserConsentState state = m_instance.m_dispatcher.SetUserConsent(consentGiven);
        atomicSaveUserConsentState(state);
    }

    /**
    @brief Retrieves the unique installation ID for the game.
     
    KeewanoSDK generates a unique ID for each game installation, enabling anonymous
    tracking of events from the same installation while ensuring compliance with
    privacy policies such as GDPR and CCPA.
     
    To associate the installation with your in-game user ID, use the \c SetUserId
    method.
     
    @sa SetUserId
     */
    static public Guid GetInstallId()
    {
        return loadOrInitIdentifiers().InstallId;
    }

    /**
    @brief Reports a button click event.

    KeewanoSDK automatically collects button click events when using Unity.UI system,
    capturing the button GameObject names to infer user actions. However, if your
    game uses a custom UI subsystem or if certain interactive objects-treated as buttons
    during gameplay but not implemented as standard Unity.UI,
    use this method to manually report those button clicks.

    @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void ReportButtonClick(string buttonName)
    {
        m_instance.m_dispatcher.ReportButtonClick(buttonName);
    }

    /**
      @brief Reports a window/popup open event.

      This method is used to capture the event when a window or popup is opened,
      helping to understand the context and scope of the user's actions.

      @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void ReportWindowOpen(string windowName)
    {
        m_instance.m_dispatcher.ReportWindowOpen(windowName);
    }

    /**
      @brief Reports a window/popup close event.

      This method is used to capture the event when a window or popup is closed,
      helping to understand the context and scope of the user's actions.

      @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void ReportWindowClose(string windowName)
    {
        m_instance.m_dispatcher.ReportWindowClose(windowName);
    }

    /**
     @brief Reports an in-app purchase event.

     Use this method to log an in-app purchase by specifying the product name and
     the price in US cents. This event helps track purchase activity within the
     %Keewano system for further analysis and reporting.

     @note This method should ONLY be called after the purchase has been validated by your server.
           Never report purchases immediately upon store callback - always validate with Apple/Google first.
     @sa \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportInAppPurchase(string productName, uint priceUsdCents)
    {
        m_instance.m_dispatcher.ReportInAppPurchase(productName, priceUsdCents);
    }

    /**
        @brief Reports items granted from an in-app purchase.

        Use this method to track virtual items or currencies granted to the player after completing
        an in-app purchase. This is especially useful when items are granted asynchronously or at a
        different time than the purchase event itself (e.g., server-side validation, delayed grants).

        @param productName The product ID as defined in app stores (Apple App Store, Google Play).
        @param items An array of items granted to the user from this purchase.

        @note This method should ONLY be called after the purchase has been validated by your server
              and the items have been successfully granted to the player.

        @sa ReportInAppPurchase, ReportItemsExchange, \ref InAppPurchases, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportInAppPurchaseItemsGranted(string productName, Item[] items)
    {
        ReadOnlySpan<Item> itemsSpan = items == null ? ReadOnlySpan<Item>.Empty : items.AsSpan();
        m_instance.m_dispatcher.ReportInAppPurchaseItemsGranted(productName, itemsSpan);
    }

    /**
        @brief Reports items granted from an in-app purchase using read-only spans.

        Use this method to track virtual items or currencies granted to the player after completing
        an in-app purchase. This is especially useful when items are granted asynchronously or at a
        different time than the purchase event itself (e.g., server-side validation, delayed grants).

        @param productName The product ID as defined in app stores (Apple App Store, Google Play).
        @param items A read-only span of items granted to the user from this purchase.

        @note This method should ONLY be called after the purchase has been validated by your server
              and the items have been successfully granted to the player.

        @sa ReportInAppPurchase, ReportItemsExchange, \ref InAppPurchases, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportInAppPurchaseItemsGranted(string productName, ReadOnlySpan<Keewano.Item> items)
    {
        m_instance.m_dispatcher.ReportInAppPurchaseItemsGranted(productName, items);
    }

    /**
     @brief Reports an ad revenue event.

     Use this method to log revenue generated from displaying an advertisement by specifying the
     placement name and the revenue amount in US cents. This event helps track ad monetization
     activity within the %Keewano system for further analysis and reporting.

     @param placement The ad placement identifier (e.g., "main_menu_banner", "level_complete_interstitial").
     @param revenueUsdCents The revenue generated from the ad impression in US cents.

     @sa ReportAdItemsGranted, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportAdRevenue(string placement, uint revenueUsdCents)
    {
        m_instance.m_dispatcher.ReportAdRevenue(placement, revenueUsdCents);
    }

    /**
        @brief Reports items granted from watching an advertisement.

        Use this method to track virtual items or currencies granted to the player after watching
        an advertisement (e.g., rewarded video ads).

        @param placement The ad placement identifier where the ad was shown.
        @param items An array of items granted to the user from watching this ad.

        @sa ReportAdRevenue, ReportItemsExchange, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportAdItemsGranted(string placement, Item[] items)
    {
        ReadOnlySpan<Item> itemsSpan = items == null ? ReadOnlySpan<Item>.Empty : items.AsSpan();
        m_instance.m_dispatcher.ReportAdItemsGranted(placement, itemsSpan);
    }

    /**
        @brief Reports items granted from watching an advertisement using read-only spans.

        Use this method to track virtual items or currencies granted to the player after watching
        an advertisement (e.g., rewarded video ads).

        @param placement The ad placement identifier where the ad was shown.
        @param items A read-only span of items granted to the user from watching this ad.

        @sa ReportAdRevenue, ReportItemsExchange, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportAdItemsGranted(string placement, ReadOnlySpan<Keewano.Item> items)
    {
        m_instance.m_dispatcher.ReportAdItemsGranted(placement, items);
    }

    /**
     @brief Reports a subscription revenue event.

     Use this method to log revenue generated from subscription billing (initial purchase, trial conversion, or renewal).
     This should be called when your app validates the receipt and detects a billing event.

     @param packageName The subscription package identifier (e.g., "vip_monthly", "premium_yearly").
     @param revenueUsdCents The revenue generated from this billing event in US cents.

     @note This method should be called for each billing event: initial purchase, trial-to-paid conversion, and each renewal.
           When the app launches, validate receipts and report any billing events that occurred since last app launch.

     @sa ReportSubscriptionItemsGranted, \ref SubscriptionRevenue, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportSubscriptionRevenue(string packageName, uint revenueUsdCents)
    {
        m_instance.m_dispatcher.ReportSubscriptionRevenue(packageName, revenueUsdCents);
    }

    /**
        @brief Reports items granted from a subscription.

        Use this method to track virtual items or currencies granted to the player from an active subscription
        (e.g., monthly VIP rewards, daily subscription bonuses).

        @param packageName The subscription package identifier.
        @param items An array of items granted to the user from this subscription.

        @sa ReportSubscriptionRevenue, ReportItemsExchange, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportSubscriptionItemsGranted(string packageName, Item[] items)
    {
        ReadOnlySpan<Item> itemsSpan = items == null ? ReadOnlySpan<Item>.Empty : items.AsSpan();
        m_instance.m_dispatcher.ReportSubscriptionItemsGranted(packageName, itemsSpan);
    }

    /**
        @brief Reports items granted from a subscription using read-only spans.

        Use this method to track virtual items or currencies granted to the player from an active subscription
        (e.g., monthly VIP rewards, daily subscription bonuses).

        @param packageName The subscription package identifier.
        @param items A read-only span of items granted to the user from this subscription.

        @sa ReportSubscriptionRevenue, ReportItemsExchange, \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportSubscriptionItemsGranted(string packageName, ReadOnlySpan<Keewano.Item> items)
    {
        m_instance.m_dispatcher.ReportSubscriptionItemsGranted(packageName, items);
    }

    /**
        @brief Reports a marketing install campaign for this user.

        This method logs the marketing campaign that was used to acquire the user into the game.
        It is used to compare the performance of different marketing sources and their effectiveness in driving user acquisitions.

        @sa \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportInstallCampaign(string campaignName)
    {
        m_instance.m_dispatcher.ReportInstallCampaign(campaignName);
    }

    /**
        @brief Reports the game language.

        This method logs the game's language setting. While the SDK automatically collects the system language,
        this method is useful for detecting cases where the game language differs from the system language,
        which may indicate that the language was manually changed by the user or that the game lacks proper localization.

        @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void ReportGameLanguage(string language)
    {
        m_instance.m_dispatcher.ReportGameLanguage(language);
    }

    /**
       @brief Report that a user has reached a milestone during the onboarding (game tutorial) process.

       The data reported using this method will be used to automatically generate an FTUE (first-time user experience)
       funnel for your players and will be used by our AI to investigate the behavior of users who are churning during the onboarding process.

       @note Each milestone should have a unique name and must not be reported more than once during onboarding.
             This ensures that analytics accurately reflect the user's progress, while still allowing flexibility.
       @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void ReportOnboardingMilestone(string milestoneName)
    {
        m_instance.m_dispatcher.ReportOnboardingMilestone(milestoneName);
    }

    /**
       @brief Marks the user as a participant in an specific test group.

       A/B testing is a technique used to compare two or more versions of a feature to see which one performs better.
       In game development, this helps you understand which changes improve gameplay, player satisfaction, or overall balance.

       When you assign users to test groups, you can compare how different versions of a feature perform with real players.
       For example, you might test different reward systems or control schemes by splitting your players into groups,
       then analyze their behavior and feedback to make data-driven improvements.

       @sa \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportABTestGroupAssignment(string testName, char group)
    {
        m_instance.m_dispatcher.AssignToABTestGroup(testName, group);
    }

    /**
        @brief Reports an item exchange event using item arrays.

        This function notifies the system of an item exchange event occurring at a specified location.
        The 'from' items represent those deducted from the user's balance, while the 'to' items represent
        those added. For one-sided transactions, pass null for the corresponding parameter.

        @param exchangeLocation The location or context of the item exchange.
        @param from An array of items to be removed from the user's balance (can be null).
        @param to An array of items to be added to the user's balance (can be null).

        @sa \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportItemsExchange(string exchangeLocation, Item[] from, Item[] to)
    {
        ReadOnlySpan<Item> fromSpan = from == null ? ReadOnlySpan<Item>.Empty : from.AsSpan();
        ReadOnlySpan<Item> toSpan = to == null ? ReadOnlySpan<Item>.Empty : to.AsSpan();

        m_instance.m_dispatcher.ReportItemExchange(exchangeLocation, fromSpan, toSpan);
    }

    /**
        @brief Reports an item exchange event using read-only spans.

        This function notifies the system of an item exchange event occurring at a specified location.
        The 'from' items represent those deducted from the user's balance, while the 'to' items represent
        those added. For one-sided transactions, pass an empty span for the corresponding parameter.

        @param exchangeLocation The location or context of the item exchange.
        @param from A read-only span of items to be removed from the user's balance.
        @param to A read-only span of items to be added to the user's balance.

        @sa \ref DataFormatSpecs for string parameter requirements.
    */
    static public void ReportItemsExchange(string exchangeLocation, ReadOnlySpan<Keewano.Item> from, ReadOnlySpan<Keewano.Item> to)
    {
        m_instance.m_dispatcher.ReportItemExchange(exchangeLocation, from, to);
    }

    /** 
      @brief Sets the user identifier using a UInt64 value.
   
      The method is for setting the user identifier, which is used to correlate
      a player's behavior in the %Keewano system with the corresponding user in the game systems.
   
      @note The user identifier should only be assigned once per game installation and cannot be reassigned.
   
      Use the UInt64 version for numeric user identifiers or the Guid version if you are using Guids.
    */
    static public void SetUserId(UInt64 uid)
    {
        byte b0 = (byte)(uid);
        byte b1 = (byte)(uid >> 8);
        byte b2 = (byte)(uid >> 16);
        byte b3 = (byte)(uid >> 24);
        byte b4 = (byte)(uid >> 32);
        byte b5 = (byte)(uid >> 40);
        byte b6 = (byte)(uid >> 48);
        byte b7 = (byte)(uid >> 56);

        Guid guid = new Guid(0, 0, 0, b0, b1, b2, b3, b4, b5, b6, b7);
        SetUserId(guid);
    }

    /**
        @brief Sets the user identifier using a Guid.

        The method is for setting the user identifier, which is used to correlate
        a player's behavior in the %Keewano system with the corresponding user in the game systems.

        @note The user identifier should only be assigned once per game installation and cannot be reassigned.

        Use the UInt64 version for numeric user identifiers or the Guid version if you are using Guids. 
    */
    static public void SetUserId(Guid uid)
    {
        UserIdentifiers ids = loadOrInitIdentifiers();
        ids.UserId = uid;
        atomicSaveIdentifiers(ids);
        m_instance.m_dispatcher.SetUserId(uid);
    }

    /**
        @brief Resets the user's items at a specified location.

        This function notifies the system to reset the user's items at the given location,
        typically used to initialize or correct the user's item balance.

        @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void ReportItemsReset(string location, Item[] items)
    {
        m_instance.m_dispatcher.ReportItemsReset(location, items);
    }

    /**
   @brief Marks the device as a test user.

   Marking a device as a test user allows %Keewano AI to ignore any unusual behavior
   and bugs reported from it during statistics calculations and investigations.
   This is especially useful during game testing and integrations.

   Use this to:
   1. **Isolate Test Data:**
      - Use this to keep test events separate from production analytics.
   2. **Test SDK Integration:**
      - Use this when integrating the KeewanoSDK to verify that event reporting works correctly.
   3. **Debug Features:**
      - Use this when testing new features or changes in your game.

   Usage:
    - Call this API method to explicitly mark and identify test users, enabling
      clearer validation and debugging during your integration with the game.

   @sa \ref DataFormatSpecs for string parameter requirements.
    */
    static public void MarkAsTestUser(string testerName)
    {
        m_instance.m_dispatcher.SetTestUserName(testerName);
    }

    /**
    @brief Reports a technical issue encountered during gameplay.

    Use this method to log any additional or custom errors that may not be captured by the system.
    This is particularly useful for recording issues unique to your application's behavior.
    Keewano AI automatically gathers exceptions and Unity error log messages.

    @sa \ref DataFormatSpecs for string parameter requirements.
     */
    static public void LogError(string message)
    {
        m_instance.m_dispatcher.LogError(message);
    }

    static UserIdentifiers loadOrInitIdentifiers()
    {
        string filename = $"{Application.persistentDataPath}/Keewano_Ids";
        UserIdentifiers id = new UserIdentifiers();
        bool success = false;

        try
        {
            if (File.Exists(filename))
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    int size = Marshal.SizeOf<UserIdentifiers>();
                    if (fs.Length >= size)
                    {
                        Span<UserIdentifiers> span = MemoryMarshal.CreateSpan(ref id, 1);
                        Span<byte> byteSpan = MemoryMarshal.AsBytes(span);
                        success = fs.Read(byteSpan) == size;
                    }
                }
            }
        }
        catch { /*Nothing to do_here*/}

        if (!success)
        {
            id.InstallId = Guid.NewGuid();
            id.UserId = Guid.Empty;
            atomicSaveIdentifiers(id);
        }

        return id;
    }

    static void atomicSaveIdentifiers(UserIdentifiers identifiers)
    {
        try
        {
            string tmpFilename = $"{Application.persistentDataPath}/Keewano_Ids.tmp";
            string finalFilename = $"{Application.persistentDataPath}/Keewano_Ids";
            using (FileStream fs = new FileStream(tmpFilename, FileMode.Create))
            {
                ReadOnlySpan<UserIdentifiers> span = MemoryMarshal.CreateReadOnlySpan(ref identifiers, 1);
                ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(span);
                fs.Write(byteSpan);
            }

            File.Delete(finalFilename);
            File.Move(tmpFilename, finalFilename);
        }
        catch { /*Nothing to do_here*/}
    }

    static void atomicSaveUserConsentState(UserConsentState state)
    {
        try
        {
            string tmpFilename = $"{Application.persistentDataPath}/Keewano_UserConsent.tmp";
            string finalFilename = $"{Application.persistentDataPath}/Keewano_UserConsent";
            using (FileStream fs = new FileStream(tmpFilename, FileMode.Create))
            {
                Span<UserConsentState> span = MemoryMarshal.CreateSpan(ref state, 1);
                Span<byte> byteSpan = MemoryMarshal.AsBytes(span);
                fs.Write(byteSpan);
            }

            File.Delete(finalFilename);
            File.Move(tmpFilename, finalFilename);
        }
        catch { /*Nothing to do_here*/}
    }

    static bool loadUserConsentState(out UserConsentState state)
    {
        string filename = $"{Application.persistentDataPath}/Keewano_UserConsent";
        state = UserConsentState.NotRequired;
        bool success = false;

        try
        {
            if (File.Exists(filename))
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    int size = Marshal.SizeOf<UserConsentState>();
                    if (fs.Length >= size)
                    {
                        Span<UserConsentState> span = MemoryMarshal.CreateSpan(ref state, 1);
                        Span<byte> byteSpan = MemoryMarshal.AsBytes(span);
                        success = fs.Read(byteSpan) == size;
                    }
                }
            }
        }
        catch { /*Nothing to do_here*/}

        return success;
    }
}
