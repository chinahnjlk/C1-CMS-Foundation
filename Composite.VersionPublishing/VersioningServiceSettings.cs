﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Composite.Data;
using Composite.Data.Types;

namespace Composite.VersionPublishing
{
    public class VersioningServiceSettings 
    {
        private readonly VersionFilteringSettings _versionFilteringSettings;
        public VersionFilteringSettings VersionFilteringSettings => _versionFilteringSettings;

        public static VersioningServiceSettings NoFiltering()
        {
            return new VersioningServiceSettings(VersionFilteringMode.None, DateTime.Now);
        }

        public static VersioningServiceSettings Published(DateTime time)
        {
            return new VersioningServiceSettings(VersionFilteringMode.Published, time);
        }

        public static VersioningServiceSettings Published()
        {
            return new VersioningServiceSettings(VersionFilteringMode.Published, DateTime.Now);
        }

        public static VersioningServiceSettings MostRelevant(DateTime time)
        {
            return new VersioningServiceSettings(VersionFilteringMode.Relevant, time);
        }

        public static VersioningServiceSettings MostRelevant()
        {
            return new VersioningServiceSettings(VersionFilteringMode.Relevant, DateTime.Now);
        }

        public static VersioningServiceSettings ByName(string versionName)
        {
            return new VersioningServiceSettings(versionName);
        }

        private VersioningServiceSettings(string versionName)
        {
            if (!DataFacade.HasDataInterceptor<IPage>())//TODO: should I care
                DataFacade.SetDataInterceptor<IPage>(new PageVersionFilteringDataInterceptor());

            _versionFilteringSettings = new VersionFilteringSettings
            {
                FilteringMode = VersionFilteringMode.Published,
                VersionName = versionName
            };
        }

        private VersioningServiceSettings(VersionFilteringMode filteringMode, DateTime time)
        {
            if(!DataFacade.HasDataInterceptor<IPage>())//TODO: should I care
                DataFacade.SetDataInterceptor<IPage>(new PageVersionFilteringDataInterceptor());

            _versionFilteringSettings = new VersionFilteringSettings
            {
                FilteringMode = filteringMode,
                Time = time
            };
        }

        public void ChangeProperties(VersionFilteringMode filteringMode, DateTime? time)
        {
            _versionFilteringSettings.FilteringMode = filteringMode;
            if (time != null)
            {
                _versionFilteringSettings.Time = time;
            }
        }
        
    }
}
