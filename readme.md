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
- you insert, delete or restore data to the previous state in between of the test runs, so that you're sure that each test has the same data to operate on;
- you clean database after running the tests.

As simple as that.

# Problem

Integration tests operating on a real database do modify the data, so some sort of isolation is required to avoid inter-test dependencies, which are usually a reason of random failures and flaky tests. 

There are a few ways to address that issue, each of which has its pros and cons. E.g:
* Make tests responsible for restoring the data they change;
* Prepare data needed in the tests themselves and assert in some smart way on this data only;
* Create and initialize fresh database from scratch for every test;
* Use database backups to restore database to the point you need;
* Use database snapshots for the restore;
* Wrap each test in transaction and revert it afterwards;
* Execute scripts to delete all the data and insert it again.

Reseed implements some of the concepts above and takes care of the database state management, while allowing you to focus on testing the business logic instead.  

# Design

Main idea is not to insert data directly, as for example NDbUnit does, but to [generate scripts](#seed-actions-generation) for the database initialization and cleanup and then [execute](#seed-actions-execution) those, when needed. This gives more control to the consumer and makes some optimization tricks possible. Scripts tend to be descriptive and well-formed in order to be readable by human and could, if needed, be adjusted manually before the execution.

It's possible to generate scripts for data seeding or cleanup only or both, depends on your needs.

The only entry point to all the functionality is the `Reseeder` class and this is how the library usage might look in its simplest form:

```csharp

var reseeder = new Reseeder("Server=myServerName\myInstanceName;Database=myDataBase;User Id=myUsername;Password=myPassword;");
var seedActions = reseeder.Generate(SeedMode.Basic(
    BasicInsertDefinition.Script(),
    CleanupDefinition.Script(CleanupMode.PreferTruncate(), CleanupTarget.Excluding())),
    DataProviders.Xml(".\Data"));

reseeder.Execute(seedActions.PrepareDatabase);
reseeder.Execute(seedActions.RestoreData);
reseeder.Execute(seedActions.CleanupDatabase);
```

See the [Examples](https://github.com/v-zubritsky/Reseed#examples) section below to get acquainted with the API and library behavior or check the [Samples](#samples) section to see how it could be integrated into your application testing framework.

# Features

* It's possible to generate scripts for either data seeding or cleanup only or both;
* Reseed is able to order tables graph (tables are nodes, foreign keys are edges), so that foreign key constraints are respected in the insertion and deletion scripts;
* Alternatively Reseed could simply disable foreign key constraints to deal with such data dependecies;
* It detects cyclic foreign key dependencies on both tables and rows levels, so that loops don't break anything. More on this in [Constraints resolution](#constraints-resolution);
* You could specify [Custom cleanup scripts](#custom-cleanup-scripts) for specific tables to ignore rows during data cleanup;
* Data schema is read from the database and there is no need to describe it manually (e.g NDbUnit requires XSD files);
* There is an optional data validation step, which allows to detect data inconsistencies (like invalid foreign key values);
* It's possible to omit identity columns, Reseed will generate them for you;

# Limitations
* Data could be described in xml files only, support for other formats is to be added. Alternatively you could provide your own implementation of `IDataProvider` type. See [Data Providers](#data-providers) for details and examples;
* MS SQL Server is the only database supported for now. 

# Seed actions generation

Scripts that Reseed generates are named "seed actions" and are represented by the `SeedActions` type. It contains all the actions grouped in a few stages with a property per stage. Every stage is basically an ordered collection of seed actions, while each action in its turn is an instance of `ISeedAction` type. 

```csharp
class SeedActions 
{
    IReadOnlyCollection<OrderedItem<ISeedAction>> PrepareDatabase;
    IReadOnlyCollection<OrderedItem<ISeedAction>> RestoreData;
    IReadOnlyCollection<OrderedItem<ISeedAction>> CleanupDatabase;
}
```

The stages are:
- **PrepareDatabase**

    At this stage Reseed executes some infrastructure related actions like creation of temporary database objects. Should be executed the first and once per test fixtures.

- **RestoreData**

    Actions on these stage do actually restore the data to its initial state. You might think of it as a combination of data cleanup followed by data insertion. Should be executed before every test run.

- **CleanupDatabase**

    At this stage objects created on the `PrepareDatabase` stage are deleted if there were any. Should be executed the last and once per test fixtures.

To generate actions you use `Reseeder.Generate` instance method by configuring generation behavior the way you like, see [Operation modes](#operation-modes) documentation section for additional details. 

```csharp
Reseeder.Generate(AnySeedMode);
```

# Seed actions execution

To execute the actions you simply pass a collection representing a specific stage to the `Reseeder.Execute` instance method.

```csharp
Reseeder.Execute(IReadOnlyCollection<OrderedItem<ISeedAction>>, TimeSpan?);
```

If you have `Reseeder reseeder` and `SeedActions seedActions` variables, then it's:  

```csharp
reseeder.Execute(seedActions.PrepareDatabase);
reseeder.Execute(seedActions.RestoreData);
reseeder.Execute(seedActions.CleanupDatabase);
```

Optionally you could provide a `TimeSpan` to specify an action execution timeout; otherwise defaults of the underlying system are used (e.g 30 seconds for `SqlCommand`).

```csharp
reseeder.Execute(seedActions.RestoreData, TimeSpan.FromSeconds(5));
```

# Operation modes

Reseed is able to operate in a few modes, so that actions it outputs could differ depending on the configuration. See the [Performance](#performance) section for some starting data and do some tests for your case to choose the mode, which suits your case the best.  

Static `SeedMode` type is an entry point to the Reseed behaviour configuration.

### Basic mode

```csharp
SeedMode.Basic(BasicInsertDefinition, AnyCleanupDefinition, IDataProvider);
```
    
This is the most basic and the most robust mode of operation. In this mode Reseed generates single insert script for all the entities as well as the only delete script to cleanup database. Data restore is a combination of delete and insert scripts executed one after another. 
    
You might have insertion logic generated either as a script or stored procedure. The latter could be beneficial for large insert scripts.

```csharp
BasicInsertDefinition.Script();
BasicInsertDefinition.Procedure(ObjectName);
```
   
### Temporary tables mode

```csharp
SeedMode.TemporaryTables(string, TemporaryTablesInsertDefinition, AnyCleanupDefinition, IDataProvider);
```

This mode is more tricky, thus less reliable, but at the same time it is able to provide a great speed boost.

Reseed firstly creates copy of the target tables in some temporary schema, then fills those with data with use of insert script and copies data to the target tables in the end. Therefore insertion, which is slower than the internal database data transfer, is executed just once.
    
There are a few ways to transfer the data, which are exposed at `TemporaryTablesInsertDefinition` type:
1. **Script**:
    ```csharp
    TemporaryTablesInsertDefinition.Script();
    ```

    Data is copied with use of insert statements like `INSERT columns INTO TABLE (SELECT columns FROM temp.TABLE)`.

2. **Procedure**:
    ```csharp
    TemporaryTablesInsertDefinition.Procedure(ObjectName);
    ```

    Similarly to the `Script` mode it uses insert statements, but the insert logic is saved as stored procedure.

3. **Sql bulk copy**:
    ```csharp
    TemporaryTablesInsertDefinition.SqlBulkCopy(Func<SqlBulkCopyOptions, SqlBulkCopyOptions>)
    ```

    [SqlBulkCopy](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy?view=dotnet-plat-ext-5.0) type is used to transfer data to the target tables.

4. **BCP** (Work in progress):
    Uses [BCP utility](https://docs.microsoft.com/en-us/sql/tools/bcp-utility?view=sql-server-ver15) to copy the data.

### CleanupOnly mode

Sometimes you want to manage data insertion on your own and need just a database cleanup, this is the mode to be used then. Note that you don't need to specify `IDataProvider` for that case, which is a reasonable behavior as you don't need any data to be inserted.

```csharp
SeedMode.CleanupOnly(CleanupDefinition);
```
As a result you'll get `SeedActions` instance, which does nothing but database cleanup during its `RestoreData` phase. See [Data cleanup](data-cleanup) section for cleanup configuration options.

# Data cleanup 

There is a posibility to configure the way Reseed generates data cleanup scripts, which are needed for some of the seed modes. Configuration is done with use of `CleanupDefinition` type.

If you don't want Reseed to execute cleanup for you at all, then you could simply use `NoCleanup` cleanup definition. Just make sure to take care of cleaning the database on your own or it will most likely fail on attempt to insert duplicated rows once.

Otherwise data cleanup logic could be represented either as a script or a stored procedure:

```csharp
CleanupDefinition.NoCleanup();
CleanupDefinition.Script(CleanupMode, CleanupTarget);
CleanupDefinition.Procedure(ObjectName, CleanupMode, CleanupTarget);
```

### Data cleanup configuration 

If you want cleanup to be executed, then you need to choose, which database objects should be cleaned and how. Use `CleanupTarget` type for that aim.

There are two ways to choose cleanup targets:

- Either include every schema/table and specify the ones to ignore

    ```charp
    CleanupTarget.Excluding(
        Func<ExcludingCleanupFilter, ExcludingCleanupFilter>,
        IReadOnlyCollection<(ObjectName table, string script)>);
    ```
- Or on contrary start with an empty tables set and explicitly include the ones to clean

    ```charp
    CleanupTarget.Including(
        Func<IncludingCleanupFilter, IncludingCleanupFilter>,
        IReadOnlyCollection<(ObjectName table, string script)>);
    ```
    
It's possible to include/exclude the whole schema or a single table.
   
### Data cleanup modes
   
Also you should specify how each table will be cleaned. There are a few cleanup modes available:   
 
1. **Delete**

     ```csharp
    CleanupMode.Delete(ConstraintResolutionBehavior);
    ```

    In this cleanup mode library generates `DELETE FROM` statement to clean the table. Foreign key constraints are respected and either tables are ordered to prevent deletion failures or constraints are disabled, the former is used by default.

2. **Prefer truncate**

    ```csharp
    CleanupMode.PreferTruncate(ObjectName[], ConstraintResolutionBehavior); 
    ``` 

    Pretty much as the previous one, but if Reseed finds that table has no relations, then `TRUNCATE TABLE` statement is used, which should be a lot faster; `DELETE FROM` is used otherwise. It's possible to explicitly specify tables to use `DELETE FROM` for and to choose constraints resolution behavior. 
    
3. **Truncate**

    ```csharp
     CleanupMode.Truncate(ObjectName[], ConstraintResolutionBehavior);
    ```

    Reseed uses `TRUNCATE TABLE` for every table in spite of the foreign keys presence. It drops foreign keys and recreates them as it's not possible to use `TRUNCATE TABLE` statement otherwise. Similarly to the `PreferTruncate` mode you might force usage `DELETE FROM` for some of the tables and choose constraints resolution behavior for those. 
    
4. **Switch tables** (Work in progress)

    TBD 
    
Here is how full `CleanupDefinition` setup might look like:

```csharp
CleanupDefinition.Script(
    CleanupMode.PreferTruncate(),
    CleanupTarget.Including(c => c.IncludeSchemas("dbo"))))
```

### Custom cleanup scripts

Sometimes you don't need to clean all the rows from the table, so you might want to provide custom deletion scripts for specific tables.

E.g you have a superadmin user with `Id=1`, which you want to be never deleted. Here is how the setup could look like for that case:

```csharp
CleanupDefinition.Script(
    CleanupMode.PreferTruncate(),
    CleanupTarget.Including(
        c => c.IncludeSchemas("dbo"),
        new[]
        {
            (new ObjectName("User"), "DELETE FROM [dbo].[User] WHERE Id <> 1")
        })))
```

# Constraints resolution
TBD

# Data providers
TBD

# API

TBD

# Performance

TBD

# Examples

Let's take a look on a simple seeding example. E.g there is the only table with user data and we'd like to test our application behavior upon a database with a few user entities.

Script to create our database might look like this.
```sql
CREATE TABLE [dbo].[User] (
    Id int NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL,
    ManagerId int NULL,
    Age int NULL
    
    CONSTRAINT [FK_User_ManagerId] FOREIGN KEY (ManagerId) REFERENCES [dbo].[User](Id)
)
```

And we have the only data file to represent the database state, which is `Users.xml` (name could actually be any). 
```xml
<Users>
    <User>
        <FirstName>John</FirstName>
        <LastName>Doe</LastName>
        <Age>23</Age>
        <ManagerId>2</ManagerId>
    </User>
    <User>
        <Id>2</Id>
        <FirstName>Alice</FirstName>
        <LastName>Bart</LastName>
    </User>
    <User>
        <FirstName>Bart</FirstName>
        <LastName>Henessy</LastName>
        <Age>56</Age>
        <ManagerId>2</ManagerId>
    </User>
</Users>
```

We're going to use `Basic` `SeedMode` in this example. This configuration is the simplest and is often the most reasonable.
```csharp
SeedMode.Basic(
    BasicInsertDefinition.Script(),
    CleanupDefinition.Script(
        CleanupMode.PreferTruncate(), 
        CleanupTarget.Including(f => f.IncludeSchemas("dbo"))))
```

This is what we get generated. `PrepareDatabase` and `CleanupDatabase` are empty collections as no internal database objects are needed in this mode of operation, while `RestoreData` stage contains two scripts: one to clean the data and another one to insert.

Delete script looks this way:
```sql
ALTER TABLE [dbo].[User] NOCHECK CONSTRAINT [FK_User_ManagerId]
DELETE FROM [dbo].[User];
ALTER TABLE [dbo].[User] CHECK CONSTRAINT [FK_User_ManagerId]
```

A few things to note here: 
- `User` table has foreign key constraint to itself and to address that dependency cycle Reseed disables the constraint;
- Even though we used `PreferTruncate` mode, `DELETE FROM` statement is used, that's also due to the foreign key constraint presence.

And here is the insert script:
```sql
SET IDENTITY_INSERT [dbo].[User] ON
INSERT INTO [dbo].[User] WITH (TABLOCKX) (
	[Id], [FirstName], [LastName]
)
VALUES 
	(2, 'Alice', 'Bart')
INSERT INTO [dbo].[User] WITH (TABLOCKX) (
	[Id], [FirstName], [LastName], [ManagerId], [Age]
)
VALUES 
	(1, 'John', 'Doe', 2, 23), (3, 'Bart', 'Henessy', 2, 56)
SET IDENTITY_INSERT [dbo].[User] OFF
```

A few notes in regard to the insert script:
- We specified `Id` column value equal `2` for the only record, but as it's an identity column, Reseed has taken care of that and provided value `1` and `3` for anothers row automatically;
- Order of entities in the script doesn't match the order in the data file, this is due to the foreign key constraint, which needs rows ordering to respect it. Row with `Id=2` should be inserted the first as the other rows has `ManagerId=2`; insertion of rows in another order will either fail or required FK disabling, which is slower that ordering;
- We have two rows with same columns (`Id=1` and `Id=3`), those were combined to the only `INSERT INTO` clause;
- Optional `Age` column was present for one row and omitted for another.

# Samples
- [Example of usage with NUnit](https://github.com/v-zubritsky/Reseed/tree/main/samples/Reseed.Samples.NUnit);
