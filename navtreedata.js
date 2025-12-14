/*
 @licstart  The following is the entire license notice for the JavaScript code in this file.

 The MIT License (MIT)

 Copyright (C) 1997-2020 by Dimitri van Heesch

 Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 and associated documentation files (the "Software"), to deal in the Software without restriction,
 including without limitation the rights to use, copy, modify, merge, publish, distribute,
 sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all copies or
 substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

 @licend  The above is the entire license notice for the JavaScript code in this file
*/
var NAVTREE =
[
  [ "Keewano Unity SDK", "index.html", [
    [ "Overview", "index.html", null ],
    [ "Quick Start Guide", "QuickStart.html", [
      [ "Installation", "QuickStart.html#Install", null ],
      [ "Configure your Project", "QuickStart.html#Configure", null ],
      [ "Identify Users", "QuickStart.html#UserId", null ],
      [ "Report Events", "QuickStart.html#ReportEvents", null ],
      [ "Privacy Compliance", "QuickStart.html#Default", null ],
      [ "User Consent", "QuickStart.html#Optional", null ],
      [ "Optional Button Click Tracking Control", "QuickStart.html#ButtonClickControl", null ]
    ] ],
    [ "Event Types", "EventTypes.html", [
      [ "Automatic Event Types", "EventTypes.html#AutomaticEvents", null ],
      [ "Manual Event Types", "EventTypes.html#ManualEvents", null ],
      [ "Custom Events", "EventTypes.html#CustomEventsSection", null ]
    ] ],
    [ "Data Format Specifications", "DataFormatSpecs.html", [
      [ "String Parameters", "DataFormatSpecs.html#StringFormat", [
        [ "Non-Empty Requirement", "DataFormatSpecs.html#StringNonEmpty", null ],
        [ "Maximum Length", "DataFormatSpecs.html#StringLength", null ],
        [ "Exceptions", "DataFormatSpecs.html#StringException", null ],
        [ "Editor Validation", "DataFormatSpecs.html#StringValidation", null ]
      ] ]
    ] ],
    [ "Tutorial Tracking", "onboarding_milestone.html", [
      [ "Overview", "onboarding_milestone.html#tutorial_overview", null ],
      [ "Why Track Tutorials?", "onboarding_milestone.html#rationale", null ],
      [ "First-Time User Experience (FTUE)", "onboarding_milestone.html#ftue_tracking", [
        [ "When to Use ReportOnboardingMilestone", "onboarding_milestone.html#ftue_when", null ],
        [ "Usage", "onboarding_milestone.html#ftue_usage", null ],
        [ "How Funnels Are Built", "onboarding_milestone.html#ftue_funnel", null ],
        [ "Example", "onboarding_milestone.html#ftue_example", null ]
      ] ],
      [ "Tutorials for Advanced Game Features", "onboarding_milestone.html#advanced_tutorials", [
        [ "When to Use Custom Tutorial Events", "onboarding_milestone.html#tutorial_scenario", null ],
        [ "Creating Tutorial Custom Events", "onboarding_milestone.html#tutorial_creation", null ],
        [ "Creating Multiple Tutorial Events", "onboarding_milestone.html#tutorial_multiple", null ],
        [ "FTUE vs Feature Tutorials", "onboarding_milestone.html#tutorial_comparison", null ]
      ] ]
    ] ],
    [ "In-Game Balance", "ItemExchange.html", [
      [ "Defining an \"Item\"", "ItemExchange.html#ItemConcept", null ],
      [ "Item Names Identify Items Across All APIs", "ItemExchange.html#ItemConsistency", null ],
      [ "Items Reset", "ItemExchange.html#Items", null ],
      [ "Items Exchange", "ItemExchange.html#Exchange", null ],
      [ "Examples", "ItemExchange.html#Examples", [
        [ "Player Spends 5 Gold Coins and a Healing Potion to Get 1 Vial of Eternal Life", "ItemExchange.html#Example1", null ],
        [ "Player Loses a Broken Sword (One-Sided Transaction)", "ItemExchange.html#Example2", null ],
        [ "Player Receives a Reward (One-Sided Transaction)", "ItemExchange.html#Example3", null ],
        [ "Match3 Level Scenario", "ItemExchange.html#Example4", null ],
        [ "User Balance Fix Reset by Support Team", "ItemExchange.html#Example6", null ],
        [ "Coffee Shop: Pay with Voucher or Credits + Stamp Card", "ItemExchange.html#Example7", null ],
        [ "Road Trip Day: Reset, Mid-Trip Changes, End-of-Day Settlement", "ItemExchange.html#Example9", null ]
      ] ]
    ] ],
    [ "In-App Purchases", "InAppPurchases.html", [
      [ "Overview", "InAppPurchases.html#iap_overview", null ],
      [ "IAP vs Virtual Economy", "InAppPurchases.html#iap_distinction", null ],
      [ "The Two IAP Steps", "InAppPurchases.html#iap_methods", [
        [ "Step 1: ReportInAppPurchase - Track the Monetary Transaction", "InAppPurchases.html#iap_purchase", null ],
        [ "Step 2: ReportInAppPurchaseItemsGranted - Track the Items Delivered", "InAppPurchases.html#iap_items_granted", null ]
      ] ],
      [ "Why Are These Two Steps Separated?", "InAppPurchases.html#iap_why_separated", [
        [ "Common Cases for Separation", "InAppPurchases.html#iap_separation_reasons", null ],
        [ "Example: Monthly Login Package", "InAppPurchases.html#iap_separation_example", null ]
      ] ],
      [ "The Complete Purchase Flow", "InAppPurchases.html#iap_flow", [
        [ "Flow Diagram", "InAppPurchases.html#iap_flow_diagram", null ]
      ] ],
      [ "Common Scenarios", "InAppPurchases.html#iap_scenarios", [
        [ "Scenario 1: Immediate Grant (Synchronous)", "InAppPurchases.html#iap_scenario_immediate", null ],
        [ "Scenario 2: Delayed Grant (Asynchronous)", "InAppPurchases.html#iap_scenario_delayed", null ],
        [ "Scenario 3: Monthly/Daily Packages", "InAppPurchases.html#iap_scenario_monthly", null ],
        [ "Scenario 4: Bundle with Multiple Items", "InAppPurchases.html#iap_scenario_bundle", null ],
        [ "Scenario 5: Purchase Validated but Grant Fails", "InAppPurchases.html#iap_scenario_failed", null ]
      ] ],
      [ "Best Practices", "InAppPurchases.html#iap_best_practices", [
        [ "Server Validation is Critical", "InAppPurchases.html#iap_validation_critical", null ],
        [ "Use Consistent Product IDs", "InAppPurchases.html#iap_product_ids", null ],
        [ "Only Report Actually Granted Items", "InAppPurchases.html#iap_only_granted_items", null ],
        [ "Report Purchase Once Only", "InAppPurchases.html#iap_report_once", null ]
      ] ],
      [ "String Parameter Limits", "InAppPurchases.html#iap_string_limits", null ],
      [ "See Also", "InAppPurchases.html#iap_see_also", null ]
    ] ],
    [ "Ad Revenue", "AdRevenue.html", [
      [ "Overview", "AdRevenue.html#ad_overview", null ],
      [ "Ad Revenue vs IAP", "AdRevenue.html#ad_distinction", null ],
      [ "The Two Ad Revenue Steps", "AdRevenue.html#ad_methods", [
        [ "Step 1: ReportAdRevenue - Track the Ad Revenue", "AdRevenue.html#ad_revenue_method", null ],
        [ "Step 2: ReportAdItemsGranted - Track Rewarded Ad Items", "AdRevenue.html#ad_items_granted_method", null ]
      ] ],
      [ "Placement Naming Best Practices", "AdRevenue.html#ad_placement_naming", [
        [ "Use Game-Specific Context, Not Ad Types", "AdRevenue.html#ad_placement_context", null ],
        [ "Placement Naming Examples", "AdRevenue.html#ad_placement_examples", null ],
        [ "Keep Placement Names Static", "AdRevenue.html#ad_placement_static", null ],
        [ "Why Context Matters", "AdRevenue.html#ad_placement_rationale", null ]
      ] ],
      [ "Why Are These Two Steps Separated?", "AdRevenue.html#ad_why_separated", [
        [ "Common Cases", "AdRevenue.html#ad_separation_reasons", null ]
      ] ],
      [ "Common Scenarios", "AdRevenue.html#ad_scenarios", [
        [ "Scenario 1: Interstitial Ad Between Levels (No Reward)", "AdRevenue.html#ad_scenario_interstitial", null ],
        [ "Scenario 2: Out of Energy - Watch to Refill", "AdRevenue.html#ad_scenario_rewarded_energy", null ],
        [ "Scenario 3: Level Failed - Watch for Extra Moves", "AdRevenue.html#ad_scenario_level_failed", null ],
        [ "Scenario 4: Daily Bonus - Watch to Double Reward", "AdRevenue.html#ad_scenario_daily_bonus", null ],
        [ "Scenario 5: Main Menu Banner (No Reward)", "AdRevenue.html#ad_scenario_banner", null ],
        [ "Scenario 6: Shop - Watch for Free Coins", "AdRevenue.html#ad_scenario_shop", null ],
        [ "Scenario 7: Multiple Item Rewards", "AdRevenue.html#ad_scenario_multiple_items", null ]
      ] ],
      [ "Best Practices", "AdRevenue.html#ad_best_practices", [
        [ "Always Use Ad Network Callbacks", "AdRevenue.html#ad_use_callbacks", null ],
        [ "Only Call ReportAdItemsGranted for Rewarded Ads", "AdRevenue.html#ad_only_rewarded_grants", null ],
        [ "Use Consistent Placement Names", "AdRevenue.html#ad_consistent_placement", null ],
        [ "Only Report Actually Granted Items", "AdRevenue.html#ad_only_granted_items", null ]
      ] ],
      [ "String Parameter Limits", "AdRevenue.html#ad_string_limits", null ],
      [ "See Also", "AdRevenue.html#ad_see_also", null ]
    ] ],
    [ "Subscription Revenue", "SubscriptionRevenue.html", [
      [ "Overview", "SubscriptionRevenue.html#sub_overview", null ],
      [ "The Two Subscription Steps", "SubscriptionRevenue.html#sub_methods", [
        [ "Step 1: ReportSubscriptionRevenue - Track Subscription Billing", "SubscriptionRevenue.html#sub_revenue_method", null ],
        [ "Step 2: ReportSubscriptionItemsGranted - Track Subscriber Rewards", "SubscriptionRevenue.html#sub_items_granted_method", null ]
      ] ],
      [ "Common Scenarios", "SubscriptionRevenue.html#sub_scenarios", [
        [ "Scenario 1: Initial Subscription Purchase", "SubscriptionRevenue.html#sub_scenario_initial", null ],
        [ "Scenario 2: Free Trial Period", "SubscriptionRevenue.html#sub_scenario_trial", null ],
        [ "Scenario 3: Subscription Renewal", "SubscriptionRevenue.html#sub_scenario_renewal", null ],
        [ "Scenario 4: Daily Subscriber Rewards", "SubscriptionRevenue.html#sub_scenario_daily", null ],
        [ "Scenario 5: Multiple Item Rewards", "SubscriptionRevenue.html#sub_scenario_multiple", null ]
      ] ],
      [ "Best Practices", "SubscriptionRevenue.html#sub_best_practices", [
        [ "Use Consistent Package Names", "SubscriptionRevenue.html#sub_consistent_package", null ]
      ] ],
      [ "String Parameter Limits", "SubscriptionRevenue.html#sub_string_limits", null ],
      [ "See Also", "SubscriptionRevenue.html#sub_see_also", null ]
    ] ],
    [ "Marketing Campaign", "install_campaign.html", [
      [ "Overview", "install_campaign.html#campaign_overview", null ],
      [ "Description", "install_campaign.html#campaign_description", null ],
      [ "Use Cases", "install_campaign.html#campaign_use_cases", null ],
      [ "Example", "install_campaign.html#campaign_example", null ]
    ] ],
    [ "Windows and Popups", "Windows.html", null ],
    [ "Custom Events", "CustomEventsPage.html", [
      [ "Adding Custom Events", "CustomEventsPage.html#custom_events_adding", null ],
      [ "String Parameter Limits", "CustomEventsPage.html#custom_events_string_limits", null ],
      [ "Merge Conflicts", "CustomEventsPage.html#custom_events_merge_conflicts", null ]
    ] ],
    [ "Step-by-Step Example Integration", "DummyGameIntegration.html", [
      [ "Overview", "DummyGameIntegration.html#Overview", null ],
      [ "Key Concept: Context-First Events", "DummyGameIntegration.html#ConceptShift", null ],
      [ "Game Name", "DummyGameIntegration.html#GameName", null ],
      [ "Game Description", "DummyGameIntegration.html#GameDescription", null ],
      [ "Integration Scenarios", "DummyGameIntegration.html#IntegrationScenarios", [
        [ "Scenario 1: First Launch &amp; Consent", "DummyGameIntegration.html#Scenario_1", null ],
        [ "Scenario 2: Player Registration &amp; Sign-In", "DummyGameIntegration.html#Scenario_2", null ],
        [ "Scenario 3: Entering Main Menu &amp; Changing Language", "DummyGameIntegration.html#Scenario_3", null ],
        [ "Scenario 4: FTUE Milestones Tracking", "DummyGameIntegration.html#Scenario_4", null ],
        [ "Scenario 5: Level Start &amp; Completion", "DummyGameIntegration.html#Scenario_5", null ],
        [ "Scenario 6: In-Game Combat &amp; Power-Ups", "DummyGameIntegration.html#Scenario_6", null ],
        [ "Scenario 7: Store Purchases (IAP)", "DummyGameIntegration.html#Scenario_7", null ],
        [ "Scenario 8: Daily Resets", "DummyGameIntegration.html#Scenario_8", null ],
        [ "Scenario 9: Non Unity.UI Buttons &amp; Window Flow", "DummyGameIntegration.html#Scenario_9", null ],
        [ "Scenario 10: Advanced Item &amp; Goal Tracking", "DummyGameIntegration.html#Scenario_10", null ],
        [ "Scenario 11: Custom Error Reporting", "DummyGameIntegration.html#Scenario_11", null ]
      ] ]
    ] ],
    [ "SDK Integration Testing", "IntegrationTesting.html", [
      [ "Safely test without polluting production", "IntegrationTesting.html#WhyTest", null ]
    ] ],
    [ "Offline Analytics", "OfflineAnalytics.html", [
      [ "Your Data is Never Lost", "OfflineAnalytics.html#OfflineOverview", null ],
      [ "How It Works", "OfflineAnalytics.html#OfflineHow", null ]
    ] ],
    [ "Data Privacy", "DataPrivacy.html", [
      [ "Default Privacy Compliance", "DataPrivacy.html#DP_default", null ],
      [ "Optional User Consent", "DataPrivacy.html#DP_consent", null ],
      [ "Configuring Consent Requirement", "DataPrivacy.html#DP_configure", null ],
      [ "SetUserConsent Method", "DataPrivacy.html#DP_method", null ],
      [ "Example Usage", "DataPrivacy.html#DP_example", null ]
    ] ],
    [ "API Reference", "annotated.html", [
      [ "Class List", "annotated.html", "annotated_dup" ],
      [ "Class Members", "functions.html", [
        [ "All", "functions.html", null ],
        [ "Functions", "functions_func.html", null ],
        [ "Variables", "functions_vars.html", null ]
      ] ]
    ] ]
  ] ]
];

var NAVTREEINDEX =
[
"AdRevenue.html"
];

var SYNCONMSG = 'click to disable panel synchronization';
var SYNCOFFMSG = 'click to enable panel synchronization';
var LISTOFALLMEMBERS = 'List of all members';