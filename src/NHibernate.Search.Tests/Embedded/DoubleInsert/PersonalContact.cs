using System;

namespace NHibernate.Search.Tests.Embedded.DoubleInsert
{
    using Attributes;

    [Indexed]
    public class PersonalContact : Contact
    {
        [Field(Index = Index.Tokenized, Store = Store.Yes)] 
        private string firstname;

        [Field(Index = Index.Tokenized, Store = Store.Yes)] 
        private string surname;

        private DateTime dateOfBirth;

        private bool notifyBirthDay;

        [Field(Index = Index.Tokenized, Store = Store.Yes)] 
        private string myFacesUrl;

        private int reminderCount;

        private bool reset;

        /// <summary>
        /// 
        /// </summary>
        public string Firstname
        {
            get { return firstname; }
            set { firstname = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Surname
        {
            get { return surname; }
            set { surname = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime DateOfBirth
        {
            get { return dateOfBirth; }
            set { dateOfBirth = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool NotifyBirthDay
        {
            get { return notifyBirthDay; }
            set { notifyBirthDay = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string MyFacesUrl
        {
            get { return myFacesUrl; }
            set { myFacesUrl = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int ReminderCount
        {
            get { return reminderCount; }
            set { reminderCount = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Reset
        {
            get { return reset; }
            set { reset = value; }
        }
    }
}
