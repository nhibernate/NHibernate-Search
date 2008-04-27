using System;
using NHibernate.Util;

namespace NHibernate.Search.Bridge.Builtin {
    public class ValueTypeBridge<T> : SimpleBridge
        where T : struct {
        #region Delegates

        public delegate T Parse(string str);

        #endregion

        public Parse parse = (Parse) Delegate.CreateDelegate(typeof (Parse), typeof (T)
                                                                                 .GetMethod("Parse",
                                                                                            new System.Type[]
                                                                                                {typeof (string)}));

        public override Object StringToObject(String stringValue) {
            if (StringHelper.IsEmpty(stringValue)) return null;
            return parse(stringValue);
        }
        }
}