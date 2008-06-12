namespace NHibernate.Search.Tests.Embedded
{
    public interface Person
    {
        string Name { get; set; }
        Address Address { get; set; }
    }
}