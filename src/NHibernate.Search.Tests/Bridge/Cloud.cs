using System;
using NHibernate.Search.Attributes;
using Index = NHibernate.Search.Attributes.Index;

namespace NHibernate.Search.Tests.Bridge
{
    [Indexed]
    public class Cloud
    {
        private String customFieldBridge;
        private String customStringBridge;
        private DateTime? dateTime;
        private DateTime? dateTimeDay;
        private DateTime? dateTimeHour;
        private DateTime? dateTimeMillisecond;
        private DateTime? dateTimeMinute;
        private DateTime? dateTimeMonth;
        private DateTime? dateTimeSecond;
        private DateTime? dateTimeYear;
        private double? double1;
        private double double2;
        private float? float1;
        private float float2;
        private int id;
        private int? int1;
        private int int2;
        private long? long1;
        private long long2;
        private bool storm;
        private string string1;
        private CloudType type;
        private object[] fred = {"dividedBy"};

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        [FieldBridge(typeof(TruncateFieldBridge))]
        public virtual string CustomFieldBridge
        {
            get { return customFieldBridge; }
            set { customFieldBridge = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        [FieldBridge(typeof(TruncateStringBridge))]
        [Parameter("dividedBy", 4)]
        public virtual string CustomStringBridge
        {
            get { return customStringBridge; }
            set { customStringBridge = value; }
        }

        [DocumentId]
        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual long? Long1
        {
            get { return long1; }
            set { long1 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual long Long2
        {
            get { return long2; }
            set { long2 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual int? Int1
        {
            get { return int1; }
            set { int1 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual int Int2
        {
            get { return int2; }
            set { int2 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual double? Double1
        {
            get { return double1; }
            set { double1 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual double Double2
        {
            get { return double2; }
            set { double2 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual float? Float1
        {
            get { return float1; }
            set { float1 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual float Float2
        {
            get { return float2; }
            set { float2 = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual string String1
        {
            get { return string1; }
            set { string1 = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        public virtual DateTime? DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Year)]
        public virtual DateTime? DateTimeYear
        {
            get { return dateTimeYear; }
            set { dateTimeYear = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Month)]
        public virtual DateTime? DateTimeMonth
        {
            get { return dateTimeMonth; }
            set { dateTimeMonth = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Day)]
        public virtual DateTime? DateTimeDay
        {
            get { return dateTimeDay; }
            set { dateTimeDay = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Hour)]
        public virtual DateTime? DateTimeHour
        {
            get { return dateTimeHour; }
            set { dateTimeHour = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Minute)]
        public virtual DateTime? DateTimeMinute
        {
            get { return dateTimeMinute; }
            set { dateTimeMinute = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Second)]
        public virtual DateTime? DateTimeSecond
        {
            get { return dateTimeSecond; }
            set { dateTimeSecond = value; }
        }

        [Field(Index.UnTokenized, Store = Attributes.Store.Yes)]
        [DateBridge(Resolution.Millisecond)]
        public virtual DateTime? DateTimeMillisecond
        {
            get { return dateTimeMillisecond; }
            set { dateTimeMillisecond = value; }
        }

        [Field(Index.Tokenized, Store = Attributes.Store.Yes)]
        public virtual CloudType Type
        {
            get { return type; }
            set { type = value; }
        }

        [Field(Index.Tokenized)]
        public virtual bool Storm
        {
            get { return storm; }
            set { storm = value; }
        }
    }
}