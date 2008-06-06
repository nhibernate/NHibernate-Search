using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using NHibernate.Search.Attributes;
using NHibernate.Util;

namespace NHibernate.Search.Bridge.Builtin
{
    public class DateBridge : ITwoWayStringBridge, IParameterizedBridge
    {
        public static readonly ITwoWayStringBridge DATE_DAY = new DateBridge(Resolution.Day);
        public static readonly ITwoWayStringBridge DATE_HOUR = new DateBridge(Resolution.Hour);
        public static readonly ITwoWayStringBridge DATE_MILLISECOND = new DateBridge(Resolution.Millisecond);
        public static readonly ITwoWayStringBridge DATE_MINUTE = new DateBridge(Resolution.Minute);
        public static readonly ITwoWayStringBridge DATE_MONTH = new DateBridge(Resolution.Month);
        public static readonly ITwoWayStringBridge DATE_SECOND = new DateBridge(Resolution.Second);
        public static readonly ITwoWayStringBridge DATE_YEAR = new DateBridge(Resolution.Year);

        private DateTools.Resolution resolution;

        public DateBridge()
        {
        }

        public DateBridge(Resolution resolution)
        {
            SetResolution(resolution);
        }

        #region IParameterizedBridge Members

        public void SetParameterValues(Dictionary<string, object> parameters)
        {
            object res = parameters["resolution"];
            Resolution hibResolution;
            if (res is string)
                hibResolution = (Resolution) Enum.Parse(typeof(Resolution), res.ToString());
            else
                hibResolution = (Resolution) res;

            SetResolution(hibResolution);
        }

        #endregion

        #region ITwoWayStringBridge Members

        public Object StringToObject(String stringValue)
        {
            if (StringHelper.IsEmpty(stringValue)) return null;
            try
            {
                return DateTools.StringToDate(stringValue);
            }
            catch (Exception e)
            {
                throw new HibernateException("Unable to parse into date: " + stringValue, e);
            }
        }

        public String ObjectToString(Object obj)
        {
            return obj != null ? DateTools.DateToString((DateTime) obj, resolution) : null;
        }

        #endregion

        private void SetResolution(Resolution hibResolution)
        {
            switch (hibResolution)
            {
                case Resolution.Year:
                    resolution = DateTools.Resolution.YEAR;
                    break;
                case Resolution.Month:
                    resolution = DateTools.Resolution.MONTH;
                    break;
                case Resolution.Day:
                    resolution = DateTools.Resolution.DAY;
                    break;
                case Resolution.Hour:
                    resolution = DateTools.Resolution.HOUR;
                    break;
                case Resolution.Minute:
                    resolution = DateTools.Resolution.MINUTE;
                    break;
                case Resolution.Second:
                    resolution = DateTools.Resolution.SECOND;
                    break;
                case Resolution.Millisecond:
                    resolution = DateTools.Resolution.MILLISECOND;
                    break;
                default:
                    throw new AssertionFailure("Unknown Resolution: " + hibResolution);
            }
        }
    }
}