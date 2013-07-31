﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using QuranPhone.Data;
using QuranPhone.Resources;
using QuranPhone.ViewModels;
using QuranPhone.Utils;
using Telerik.Windows.Controls;

namespace QuranPhone
{
    public partial class App : Application
    {
        private static MainViewModel mainViewModel = null;
        private static SearchViewModel searchViewModel = null;
        private static DetailsViewModel detailsViewModel = null;
        private static TranslationsListViewModel translationsListViewModel = null;
        private static SettingsViewModel settingsViewModel = null;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel MainViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (mainViewModel == null)
                    mainViewModel = new MainViewModel();

                return mainViewModel;
            }
        }

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The SearchViewModel object.</returns>
        public static SearchViewModel SearchViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (searchViewModel == null)
                    searchViewModel = new SearchViewModel();

                return searchViewModel;
            }
        }

        /// <summary>
        /// A static DetailsViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The DetailsViewModel object.</returns>
        public static DetailsViewModel DetailsViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (detailsViewModel == null)
                    detailsViewModel = new DetailsViewModel();

                return detailsViewModel;
            }
        }

        /// <summary>
        /// A static TranslationsListViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The TranslationsListViewModel object.</returns>
        public static TranslationsListViewModel TranslationsListViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (translationsListViewModel == null)
                    translationsListViewModel = new TranslationsListViewModel();

                return translationsListViewModel;
            }
        }

        /// <summary>
        /// A static SettingsViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The SettingsViewModel object.</returns>
        public static SettingsViewModel SettingsViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (settingsViewModel == null)
                    settingsViewModel = new SettingsViewModel();

                return settingsViewModel;
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Set theme
            ThemeManager.ToDarkTheme();

            // Toggle idle mode
            PhoneUtils.ToggleIdleMode();

            // Initialize directory
            QuranFileUtils.MakeQuranDirectory();
            QuranFileUtils.MakeQuranDatabaseDirectory();

            // Delete stuck files
            QuranFileUtils.DeleteStuckFiles();

            // One time fix for page 270
            OneTimePage270Fix();

            // Set the current thread culture
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("ar");
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar");

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                
                // commented, test user idle detection
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        private void OneTimePage270Fix()
        {
            var versionFromConfig = new Version(SettingsUtils.Get<string>(Constants.PREF_CURRENT_VERSION));
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            if (nameHelper.Version == new Version(0, 3, 2, 0) && versionFromConfig < nameHelper.Version)
            {
                var pageFileName = QuranFileUtils.GetPageFileName(270);
                var filePath = QuranFileUtils.GetImageFromSD(pageFileName);
                if (QuranFileUtils.FileExists(filePath.LocalPath))
                {
                    QuranFileUtils.DeleteFile(filePath.LocalPath);
                }
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // Ensure that application state is restored appropriately
            if (!App.MainViewModel.IsDataLoaded)
            {
                App.MainViewModel.LoadData();
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            LittleWatson.ReportException(e.Exception, "Navigation Failed");
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            LittleWatson.ReportException(e.ExceptionObject, "Unhandled Exception");
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            // Switch to only use Telerik, removing WPToolkit dependencies.
            var frame = new RadPhoneApplicationFrame();
            
            RootFrame = frame;
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            // only do this for windows phone 8
#if WINDOWS_PHONE_8
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
#endif
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection, true);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}