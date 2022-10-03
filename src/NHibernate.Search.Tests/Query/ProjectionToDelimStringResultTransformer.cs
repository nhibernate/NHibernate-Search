namespace NHibernate.Search.Tests.Query
{
    using System.Collections;

    using Transform;

    public class ProjectionToDelimStringResultTransformer : IResultTransformer
    {
        public object TransformTuple(object[] tuple, string[] aliases)
        {
            return string.Join(", ", tuple);
        }

        public IList TransformList(IList collection)
        {
            return collection;
        }
    }
}