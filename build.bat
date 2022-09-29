dotnet build src/NHibernate.Search.sln
copy build-common/teamcity-hibernate.cfg.xml src/NHibernate.Search.Tests/hibernate.cfg.xml
dotnet test src/NHibernate.Search.sln