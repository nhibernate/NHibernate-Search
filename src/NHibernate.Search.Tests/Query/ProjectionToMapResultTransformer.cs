namespace NHibernate.Search.Tests.Query
{
    using System.Collections;
    using System.Collections.Generic;

    using Transform;

    public class ProjectionToMapResultTransformer : IResultTransformer
    {
        public object TransformTuple(object[] tuple, string[] aliases)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            for (int i = 0; i < tuple.Length; i++)
            {
                string key = aliases[i];
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = tuple[i];
                }
            }

            return result;
        }

        public IList TransformList(IList collection)
        {
            return collection;
        }
    }
}