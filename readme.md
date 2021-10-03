![Nuget](https://img.shields.io/nuget/v/Reseed?color=blue&style=flat-square)
> **Disclaimer**: Versions of the library up to 1.0.0 don't follow semantic versioning and public api is a subject of possibly breaking changes. 

# About

*Reseed* library allows you to initialize and clean integration tests database in a convenient, reliable and fast way. 

It covers cases similar to what a few other libraries intend to do:
* [NDbUnit](https://github.com/NDbUnit/NDbUnit) inspired by [DbUnit](http://dbunit.sourceforge.net/);
* [NDbUnit2](https://github.com/NAnt2/NDbUnit2);
* [Respawn](https://github.com/jbogard/Respawn).

It's written in C# and targets netstandard2.0, therefore could be used by both .NET Framework and .NET/.NET Core applications.

This is how it's supposed to be used:
- you describe the state of database needed for your tests in the only or multiple data files;
- you initialize database prior to running any tests;
- you restore database to the previous state in between of the test runs, so that you're sure that each test has the same starting point;
- you clean database after running the tests.

As simple as that.

# Design

Main idea is not to insert data directly, as for example *NDbUnit* does, but to generate scripts, upon execution of which data will be inserted. This allows us to use some tricks to optimize speed of execution to stay as fast as possible. 

The only entry point to all the functionality is the static `Seeder` type, by using which you firstly [generate](TBD) scripts needed for the database initialization and cleanup and then [execute](TBD) those. 

Library is able to operate in a few modes, so that scripts it outputs could differ depending on the configuration. Scripts tend to be descriptive and transparent enough, feel free to inspect them before executing to know what is about to happen.

# Features and operation modes

* *Reseed* is able to order tables graph (tables are nodes, foreign keys are edges), so that foreign key constraints are respected;
* It detects mutual foreign key constraints on both tables and rows levels, so that such loops doesn't break anything;
* Data schema is read from the database itself, so you don't need to provide any metadata like XSD files with tables and columns descriptions;
* There is an optional data validation step, so that you will be notified if there are any issues in your data files;
* Data should be described as xml. Support for other formats will be added in future;
* MS SQL Server is the only database supported.

## Initialization

1. **Insert script**

    This is the most basic mode of operation. In this mode *Reseed* generates single insert script from all the data files. It either orders tables or disables foreign key constraints.

2. **Insert stored procedure**

    The same as the previous, but script is generated as a stored procedure, so we could save some network load to execute it. Could be beneficial for big amounts of data and large insert script.

3. **Temporary schema**

    This one is more advanced, thus less reliable, but at the same time it is able to provide a great speed boost. It firstly creates copy of the target tables in some temporary schema, then fills those with data using the insert script from the first step, making sure it's executed just once. Those temporary tables are then used to copy data from (sth like `INSERT columns INTO TABLE (SELECT columns FROM temp.TABLE)` is used). Such data copy is a lot faster than the initial insert, as we have all the data in the database already and just need to copy it.

4. **Sql bulk copy**

    Similar to the previous mode in regard to the temporary tables creation, but data is copied to the target tables with use of `SqlBulkCopy` type.

5. **BCP utility**

    WIP

## Cleanup
1. **Delete**

    In this cleanup mode library generates `DELETE FROM` statements to clean the tables. It's able to either order tables considering all their relations or to just disable foreign key constraints.

2. **Truncate**

    Pretty much as the previous one, but if it finds that table has no relations, `TRUNCATE TABLE` statement is used, which is a lot faster;    
    
3. **Switch tables**

    WIP

4. **Snapshots**

    WIP

# API and examples
- [Example of usage with NUnit](https://github.com/v-zubritsky/Reseed/tree/main/samples/Reseed.Samples.NUnit);

TBD
