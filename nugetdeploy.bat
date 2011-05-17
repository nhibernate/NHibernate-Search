nuget pack NHibernate.Search.nuspec
nuget delete "NHibernate.Search" "2.0.2.4000" %1
nuget push -source http://packages.nuget.org/v1/ NHibernate.Search.2.0.2.4000.nupkg %1