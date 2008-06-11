using System;
using System.Collections.Generic;
using System.Text;

namespace NHibernate.Search.Tests.Embedded
{
    public interface Person 
    {
        string Name { get; set; }
        Address Address { get; set; }
    }
}
