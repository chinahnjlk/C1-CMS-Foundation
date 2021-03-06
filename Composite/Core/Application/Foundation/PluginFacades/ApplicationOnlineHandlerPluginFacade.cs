﻿using System;
using System.Configuration;
using Composite.Core.Application.Plugins.ApplicationOnlineHandler;
using Composite.Core.Application.Plugins.ApplicationOnlineHandler.Runtime;
using Composite.Core.Collections.Generic;
using Composite.C1Console.Events;


namespace Composite.Core.Application.Foundation.PluginFacades
{
    internal static class ApplicationOnlineHandlerPluginFacade
    {
        private static ResourceLocker<Resources> _resourceLocker = new ResourceLocker<Resources>(new Resources(), Resources.Initialize);



        static ApplicationOnlineHandlerPluginFacade()
        {
            GlobalEventSystemFacade.SubscribeToFlushEvent(OnFlushEvent);
        }



        public static void TurnApplicationOffline()
        {
            IApplicationOnlineHandler applicationOnlineHandler = GetApplicationOnlineHandler();

            applicationOnlineHandler.TurnApplicationOffline();
        }



        public static void TurnApplicationOnline()
        {
            IApplicationOnlineHandler applicationOnlineHandler = GetApplicationOnlineHandler();

            applicationOnlineHandler.TurnApplicationOnline();
        }



        public static bool IsApplicationOnline()
        {
            IApplicationOnlineHandler applicationOnlineHandler = GetApplicationOnlineHandler();

            return applicationOnlineHandler.IsApplicationOnline();
        }



        public static bool CanPutApplicationOffline(out string errorMessage)
        {
            return GetApplicationOnlineHandler().CanPutApplicationOffline(out errorMessage);
        }



        private static IApplicationOnlineHandler GetApplicationOnlineHandler()
        {
            IApplicationOnlineHandler handler = null;

            using (_resourceLocker.Locker)
            {
                if (_resourceLocker.Resources.HandlerCache == null)
                {
                    try
                    {
                        handler = _resourceLocker.Resources.Factory.CreateDefault();

                        _resourceLocker.Resources.HandlerCache = handler;
                    }
                    catch (ArgumentException ex)
                    {
                        HandleConfigurationError(ex);
                    }
                    catch (ConfigurationErrorsException ex)
                    {
                        HandleConfigurationError(ex);
                    }
                }
                else
                {
                    handler = _resourceLocker.Resources.HandlerCache;
                }
            }

            return handler;
        }



        private static void Flush()
        {
            _resourceLocker.ResetInitialization();
        }



        private static void OnFlushEvent(FlushEventArgs args)
        {
            Flush();
        }



        private static void HandleConfigurationError(Exception ex)
        {
            Flush();

            throw new ConfigurationErrorsException(string.Format("Failed to load the configuration section '{0}' from the configuration.", ApplicationOnlineHandlerSettings.SectionName), ex);
        }



        private sealed class Resources
        {
            public ApplicationOnlineHandlerFactory Factory { get; set; }
            public IApplicationOnlineHandler HandlerCache { get; set; }

            public static void Initialize(Resources resources)
            {
                try
                {
                    resources.Factory = new ApplicationOnlineHandlerFactory();
                }
                catch (NullReferenceException ex)
                {
                    HandleConfigurationError(ex);
                }
                catch (ConfigurationErrorsException ex)
                {
                    HandleConfigurationError(ex);
                }

                resources.HandlerCache = null;
            }
        }
    }
}
