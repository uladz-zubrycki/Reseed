[![NuGet version (Reseed)](https://img.shields.io/nuget/v/Reseed?color=blue&style=flat-square)](https://www.nuget.org/packages/Reseed/)
> **Disclaimer**: Versions of the library up to 1.0.0 don't follow semantic versioning and public api is a subject of possibly breaking changes. 

# About

Reseed library allows you to initialize and clean integration tests database in a convenient, reliable and fast way. 

It covers cases similar to what a few other libraries intend to do:
* [NDbUnit](https://github.com/NDbUnit/NDbUnit) and [NDbUnit2](https://github.com/NAnt2/NDbUnit2) inspired by [DbUnit](http://dbunit.sourceforge.net/dbunit/index.html);
* [Respawn](https://github.com/jbogard/Respawn).

Library is written in C# and targets `netstandard2.0`, therefore could be used by both `.NET Framework` and `.NET`/`.NET Core` applications.

This is how it's supposed to be used:
- you describe the state of database needed for your tests;
- you initialize database prior to running any tests, so that Reseed is able to do its magic;
- you restore database to the previous state in between of the test runs, so that you're sure that each test has the same data to operate on;
- you clean database after running the tests.

As simple as that.

# Problem

Integration tests operating on a real database do modify the data, so some sort of isolation is required to avoid inter-test dependencies, which are usually the reason of random failures and flaky tests. 

There are a few ways to achieve that, each of which has its pros and cons. E.g:
* Make tests responsible for restoring the data they change;
* Prepare data needed in the tests themselves and assert in some smart way on this data only;
* Create and initialize fresh database from scratch for every test;
* Use database backups to restore database to the point you need;
* Use database snapshots for the restore;
* Wrap each test in transaction and revert it afterwards;
* Execute scripts to delete all the data and insert it again.

Reseed implements some of the concepts above and takes care of the database state management, while allowing you to focus on testing the business logic instead.  

# Design

Main idea is not to insert data directly, as for example NDbUnit does, but to generate actions (read "actions" as "scripts"), upon execution of which data will be inserted or restored to the previous state. This gives more control to the consumer and makes some optimization tricks possible. Actions tend to be descriptive and well-formed  in order to be readable by human and could even be extended manually.

The only entry point to all the functionality is the static `Reseeder` type, by using which you firstly generate actions needed for the database initialization and cleanup and then execute those. 

Here is how the library usage might look in its simplest form:

```csharp

var reseeder = new Reseeder("Server=myServerName\myInstanceName;Database=myDataBase;User Id=myUsername;Password=myPassword;");
var seedActions = reseeder.Generate(
    SeedMode.Simple(
        SimpleInsertDefinition.Script(),
        CleanupDefinition.Script(CleanupConfiguration.IncludeAll(CleanupMode.PreferTruncate()))),
    DataProviders.Xml(".\Data"));

reseeder.Execute(seedActions.PrepareDatabase);
reseeder.Execute(seedActions.RestoreData);
reseeder.Execute(seedActions.CleanupDatabase);
```

# Features

* Reseed is able to order tables graph (tables are nodes, foreign keys are edges), so that foreign key constraints are respected;
* Alternatively Reseed could disable foreign key constraints to deal with data dependecies;
* It detects cyclic foreign key dependencies on both tables and rows levels, so that such loops don't break anything. More on this in [Ordering TBD](TBD);
* You could specify [Custom delete scripts](https://github.com/v-zubritsky/Reseed/blob/main/readme.md#custom-cleanup-scripts) for specific tables to ignore some rows;
* Data schema is read from the database itself, so you don't need to provide any metadata like XSD files with tables and columns descriptions;
* There is a data validation step, so that you will be notified if there are any issues with the data you provide;
* It's possible to omit Identity columns, Reseed will generate them for you;

# Limitations
* Data could be described in xml files only. Support for other formats will be added in future. Alternatively you could provide your own implementation of `IDataProvider` type. See [Data Providers TBD](TBD) for details and examples;
* MS SQL Server is the only database supported for now. 

# Operation modes

Reseed is able to operate in a few modes, so that actions it outputs could differ depending on the configuration. Use `SeedMode` type to choose one.

1. **Simple**

    This is the most basic and the most robust mode of operation. In this mode Reseed generates single insert script for all the entities as well as the only delete script to cleanup database. While data restore is a combination of delete and insert scripts executed one after another. 
    
    It's also possible to save these scripts as stored procedures to save some IO load. It could be beneficial for big amounts of data resulting in large scripts.
   
2. **Temporary tables**

    This mode is more advanced, thus less reliable, but at the same time it is able to provide a great speed boost.
    
    Reseed firstly creates copy of the target tables in some temporary schema, then fills those with data using the insert script from the first step and copies data to the target tables in the end. Therefore insertion which is slower than internal database data transfer is executed just once.
    
    There are a few ways to transfer the data, use `TemporaryTablesInsertDefinition` to choose the fastest for your case:
    1. **Script**:
    Data is copied with use of insert statements like `INSERT columns INTO TABLE (SELECT columns FROM temp.TABLE)`;
    2. **Procedure**:
    Same as `Script` mode it uses insert statements, but the insert logic is saved as stored procedure.
    3. **Sql bulk copy**:
    [SqlBulkCopy](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy?view=dotnet-plat-ext-5.0) type is used to transfer data to the target tables.
    3. **BCP** (Work in progress):
    Uses [BCP utility](https://docs.microsoft.com/en-us/sql/tools/bcp-utility?view=sql-server-ver15) to copy the data.
    
See the [Performance benchmarks TBD](TBD) and do some tests for your case to choose the mode, which suits the best.  

# Cleanup configuration

In some of the operation modes Reseed generates delete script to clean the data, it's possible to configure the library behavior by specifying the tables/schemas to clean as well as choosing how those should be cleaned.

Configuration is done with use of `CleanupDefinition` type, which serves as a facade and provides a few factory methods.

It's possible to either generate script, which will be executed as is or to save it as stored procedure by using `CleanupDefinition.Script` and `CleanupDefinition.Procedure` methods accordingly. 

Then you need to choose database objects to clean, you should use `CleanupConfiguration` for that aim. It's possible to either include every schema/table and exclude some using `CleanupConfiguration.IncludeAll` or on contrary start with an empty set and explicitly include the ones you need with help of `CleanupConfiguration.ExcludeAll`. You could either include/exclude the whole schema or a single table.

And the last thing to care about is a `CleanupMode`, as there are a few providing different performance:

1. **Delete**

    In this cleanup mode library generates `DELETE FROM` statements to clean the tables. It's able to either order tables considering all their relations or to just disable foreign key constraints, this is controlled by `ConstraintResolutionBehavior` enum (ordering is used by default).
    
    ```csharp
    CleanupMode.Delete(), 
    ```

2. **Prefer truncate**

    Pretty much as the previous one, but if Reseed finds that table has no relations `TRUNCATE TABLE` statement is used, which should be a lot faster. `DELETE FROM` is used otherwise.  
    
    ```csharp
    CleanupMode.PreferTruncate(), 
    ```
    
    You could explicitly specify tables to use `DELETE FROM` for as well as choose whether tables should be ordered by their relations or foreign key constraints should be disabled (see `ConstraintResolutionBehavior`).
    
    ```csharp
    CleanupMode.PreferTruncate(
        new [] { new ObjectName("TableToDelete") }, 
        ConstraintResolutionBehavior.DisableConstraints)
    ```
    
2. **Truncate**

    Uses `TRUNCATE TABLE` in spite of the table relations presence. It has to disable foreign key constraints if there are any, so it could actually be slower than the previous one;   
    
     ```csharp
    CleanupDefinition.Script(CleanupConfiguration.IncludeNone(
        CleanupMode.Truncate(), 
        c => c.IncludeSchemas("dbo"))))
    ```
    
    Similarly to the mode above you could use `DELETE FROM` for some tables and either order tables to resolve dependencies or to disable foreign key constraints (see `ConstraintResolutionBehavior`).
    
     ```csharp
    CleanupMode.Truncate(
        new [] { new ObjectName("TableToDelete") }, 
        ConstraintResolutionBehavior.DisableConstraints)
    ```
    
3. **Switch tables** (Work in progress)

    TBD 
    
Here is how full `CleanupDefinition` setup might look like:

```csharp
CleanupDefinition.Script(CleanupConfiguration.IncludeNone(
    CleanupMode.PreferTruncate(), 
    c => c.IncludeSchemas("dbo"))))
```

## Custom cleanup scripts

Sometimes you don't need to clean all the rows from the table, so it's possible to specify some custom deletion script for that case.

E.g you have a superadmin user with `Id=1`, which you want to be always present in the database. Here is how the setup could look like for that case:

```csharp
CleanupDefinition.Script(CleanupConfiguration.IncludeNone(
    CleanupMode.PreferTruncate(),
    c => c.IncludeSchemas("dbo"),
    new[]
    {
        (new ObjectName("User"), "DELETE FROM [dbo].[User] WHERE Id <> 1")
    })))
```

Tables with custom scrips are used as if `DELETE FROM` behavior was specified for them. It's your responsibility to make sure that custom scripts don't break anything.

# API

TBD

# Examples
- [Example of usage with NUnit](https://github.com/v-zubritsky/Reseed/tree/main/samples/Reseed.Samples.NUnit);

TBD
