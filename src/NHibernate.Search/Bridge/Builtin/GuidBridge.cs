using System;

namespace NHibernate.Search.Bridge.Builtin {
    public class GuidBridge : SimpleBridge {

        public override object StringToObject(string stringValue) {
            return new Guid(stringValue);
        }
    }
}