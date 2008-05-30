using System;
using System.Collections.Generic;

namespace NHibernate.Search.Engine
{
    public abstract class LoaderHelper
    {
        private static readonly List<System.Type> objectNotFoundExceptions = new List<System.Type>();

        static LoaderHelper()
        {
            // TODO: Add NHibernate ObjectNotFoundException
        }

        public static bool IsObjectNotFoundException(Exception e)
        {
            bool objectNotFound = false;

            System.Type type = e.GetType();
            foreach (System.Type clazz in objectNotFoundExceptions)
            {
                if (clazz.IsAssignableFrom(type))
                {
                    objectNotFound = true;
                    break;
                }
            }

            return objectNotFound;
        }
    }
}