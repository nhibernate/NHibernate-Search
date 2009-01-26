namespace NHibernate.Search.Tests.Query
{
    using System.Collections;

    using Transform;

    public class ProjectionToDelimStringResultTransformer : IResultTransformer
    {
        public object TransformTuple(object[] tuple, string[] aliases)
        {
            string s = tuple[0].ToString();
            for (int i = 1; i < tuple.Length; i++)
            {
                s += ", " + tuple[i];
            }

            return s;
        }

        public IList TransformList(IList collection)
        {
            return collection;
        }
    }
}