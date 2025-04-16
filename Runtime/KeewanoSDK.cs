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
        UserIdentifiers uid = loadOrInitIdentifiers();
        if (string.IsNullOrEmpty(settings.APIKey))
            Debug.LogError("[KeewanoSDK] No API key was provided.");

        Application.lowMemory += handleLowMemoryWarning;
        Application.logMessageReceivedThreaded += handleLogMessageReceivedThreaded;
        Application.deepLinkActivated += handleOnDeepLinkActivated;

        Guid dataSessionId = Guid.NewGuid();

#if KEEWANO_TEST_ENDPOINT
        //Used for internal testing
        string endpoint = Environment.GetEnvironmentVariable("KEEWANO_TEST_ENDPOINT");
#else
        const string endpoint = "https://api.keewano.com/event/ingress/v1/data";
#endif

        string dispatcherWorkingDir = Application.persistentDataPath + "/Keewano/";
        m_dispatcher = new KEventDispatcher(dispatcherWorkingDir, endpoint, settings.APIKey, uid.InstallId, uid.UserId, dataSessionId);

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
        m_dispatcher.addEvent((ushort)KEvents.DEEP_LINK_ACTIVATED, link);
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
        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            GameObject clicked = EventSystem.current.currentSelectedGameObject;

            if (clicked && clicked.TryGetComponent<Button>(out Button btn))
                ReportButtonClick(btn.name);
        }

#if UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
            m_instance.m_dispatcher.ReportButtonClick("Android Device Back Button");
#endif
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
    game uses a custom UI subsystem or if certain interactive objects—treated as buttons
    during gameplay but not implemented as standard Unity.UI,
    use this method to manually report those button clicks.
     */
    static public void ReportButtonClick(string buttonName)
    {
        m_instance.m_dispatcher.ReportButtonClick(buttonName);
    }

    /**
      @brief Reports a window/popup open event.

      This method is used to capture the event when a window or popup is opened,
      helping to understand the context and scope of the user's actions.
     */
    static public void ReportWindowOpen(string windowName)
    {
        m_instance.m_dispatcher.ReportWindowOpen(windowName);
    }

    /**
      @brief Reports a window/popup close event.
      
      This method is used to capture the event when a window or popup is closed,
      helping to understand the context and scope of the user's actions.
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
    */
    static public void ReportInAppPurchase(string productName, uint priceUsdCents)
    {
        m_instance.m_dispatcher.ReportInAppPurchase(productName, priceUsdCents);
    }

    /**
        @brief Reports a marketing install campaign for this user.

        This method logs the marketing campaign that was used to acquire the user into the game.
        It is used to compare the performance of different marketing sources and their effectiveness in driving user acquisitions.
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
    */
    static public void ReportItemsExchange(string exchangeLocation, Item[] from, Item[] to)
    {
        ReadOnlySpan<Item> fromSpan = from == null ? ReadOnlySpan<Item>.Empty : to.AsSpan();
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
    }

    /**
        @brief Resets the user's items at a specified location.

        This function notifies the system to reset the user's items at the given location,
        typically used to initialize or correct the user's item balance.
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
     */
    static public void LogError(string message)
    {
        m_instance.m_dispatcher.LogError(message);
    }

    static public void ReportUserCountry(string countryName)
    {
        m_instance.m_dispatcher.ReportUserCountry(countryName);
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
}