# bam.data.repositories

Repository pattern implementation with automatic DAO code generation, schema management, and POCO-to-database mapping.

## Overview

`bam.data.repositories` provides the core repository pattern for the Bam framework, enabling CRUD operations on plain C# objects (POCOs) backed by relational databases. The central class, `DaoRepository`, automatically generates Data Access Object (DAO) assemblies from POCO types at runtime using Roslyn compilation. Developers define simple POCO classes, register them with the repository, and get full Create/Retrieve/Update/Delete/Query functionality without writing any SQL or database mapping code.

The system works by analyzing POCO types to produce a `TypeSchema` that captures table structures, foreign key relationships, and cross-reference (many-to-many) associations. It then generates DAO source code from templates, compiles it into an assembly, and ensures the database schema matches. Wrapper types are generated that support lazy loading of child collections, and all operations handle UUID/CUID-based universal identification automatically.

The library also provides `DaoInheritanceRepository`, which supports type inheritance hierarchies in the database schema (one table per type in the hierarchy), and `SchemaRepositoryGenerator`, which generates strongly-typed schema-specific repository classes complete with typed query methods.

## Key Classes

| Class | Description |
|---|---|
| `Repository` | Abstract base class defining the repository interface: Save, Create, Retrieve, Update, Delete, Query, with validation and event hooks. |
| `DaoRepository` | Primary repository implementation. Auto-generates DAO assemblies, ensures database schema, provides full CRUD and query operations. |
| `DaoInheritanceRepository` | Extension of `DaoRepository` supporting type inheritance with per-type tables and SQL-based insert/update/delete. |
| `SchemaRepositoryGenerator` | Code generator that produces schema-specific `DaoRepository` subclasses with typed methods from templates. |
| `TypeToDaoGenerator` | Orchestrates the pipeline from POCO types to compiled DAO assembly: type analysis, schema extraction, code generation, compilation. |
| `RepoData` | Abstract base class for persistable objects. Provides `Id`, `Uuid`, `Cuid`, `Created` properties, and persistence methods. |
| `AuditRepoData` | Extends `RepoData` with `CreatedBy`, `ModifiedBy`, `Modified`, `Deleted` audit fields. |
| `CompositeKeyAuditRepoData` | Extends `AuditRepoData` with composite key support via `[CompositeKey]`-decorated properties. |
| `KeyedAuditRepoData` | Extends `CompositeKeyAuditRepoData` with a `Key` property computed from composite key hash. Supports `SaveByKey` for upsert semantics. |
| `DaoRepoData` / `DaoRepoData<T>` | Wrapper types that associate a POCO with its `DaoRepository` for self-save operations. |
| `WrapperGenerator` / `TemplatedWrapperGenerator` | Generates wrapper classes for POCOs that support lazy-loaded collection properties. |
| `MongoRepository` | (Incomplete) Repository implementation backed by MongoDB. Most query methods throw `NotImplementedException`. |
| `DatabaseFactory` | Factory for creating database instances by database type (SQLite, MySQL, PostgreSQL, etc.). |
| `Dto` | Utility class for generating Data Transfer Objects from DAO types, stripping methods while keeping column properties. |
| `CompositeKeyHashProvider` | Provides hash-based composite keys from `[CompositeKey]`-decorated properties. |
| `DaoProtoFileGenerator` / `ProtoFileGenerator` | Generates Protocol Buffer `.proto` files from POCO types. |

## Dependencies

### Project References
- bam.base
- bam.configuration
- bam.data.schema
- bam.data.config
- bam.data.firebird
- bam.data.mssql
- bam.data.mysql
- bam.data.oracle
- bam.data.postgres
- bam.data
- bam.logging

### Additional References
- Microsoft.CodeAnalysis (direct assembly reference for Roslyn compilation)

### Embedded Resource Templates
- ChildPrimaryKeyProperty.tmpl
- Dto.tmpl
- ForeignKeyProperty.tmpl
- SchemaRepository.tmpl
- SchemaRepositoryAddType.tmpl
- SchemaRepositoryMethods.tmpl
- Wrapper.tmpl
- XrefLeftProperty.tmpl
- XrefRightProperty.tmpl

### Target Framework
- net10.0

## Usage Examples

### Basic CRUD with DaoRepository
```csharp
// Define a POCO
public class Customer : AuditRepoData
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Create and configure repository
var repo = new DaoRepository(schemaProvider, daoGenerator, wrapperGenerator);
repo.AddType<Customer>();
repo.Database = myDatabase;

// Create
Customer created = repo.Create(new Customer { Name = "Acme", Email = "info@acme.com" });

// Retrieve by ID
Customer retrieved = repo.Retrieve<Customer>(created.Id);

// Retrieve by UUID
Customer byUuid = repo.Retrieve<Customer>(created.Uuid);

// Update
created.Name = "Acme Corp";
Customer updated = repo.Update(created);

// Delete
bool deleted = repo.Delete(created);
```

### Querying
```csharp
// Query with dictionary
IEnumerable<Customer> results = repo.Query<Customer>(
    new Dictionary<string, object> { { "Name", "Acme" } }
);

// Query with dynamic object
IEnumerable<Customer> results = repo.Query<Customer>(
    new { Name = "Acme" }
);

// Query with QueryFilter
IEnumerable<Customer> results = repo.Query<Customer>(
    Query.Where("Name") == "Acme"
);

// Query with expression
IEnumerable<Customer> results = repo.Query<Customer>(
    c => c.Name == "Acme"
);
```

### Using KeyedAuditRepoData for upsert
```csharp
public class Setting : KeyedAuditRepoData
{
    [CompositeKey]
    public string Category { get; set; }

    [CompositeKey]
    public string SettingName { get; set; }

    public string Value { get; set; }
}

// SaveByKey will update if Key matches, create otherwise
Setting setting = new Setting { Category = "App", SettingName = "Theme", Value = "Dark" };
Setting saved = repo.Save<Setting>(setting);
```

## Known Gaps / Not Yet Implemented

- **`MongoRepository`**: Most methods throw `NotImplementedException` including `Retrieve` (by int, long, ulong, string), `RetrieveAll`, `BatchRetrieveAll`, `Query` (multiple overloads), `Update`, `Delete`, and `Query(IQueryFilter)`. The class is described as "An incomplete Repository implementation using Mongo as a backing data store."
- **`WrapperModel`**: Three properties (`LeftXrefs`, `RightXrefs`, `ChildPrimaryKeys`) throw `NotImplementedException`.
- **`DaoInheritanceRepository.Create`**: Has a TODO for implementing transaction functionality in `SqlStringBuilder`.
- **`DaoRepository.SetDaoCollectionValues`**: Has a TODO to define `IMetaProvider` and inject it instead of direct `Meta.SetUuid`/`Meta.SetCuid` calls.
- **`DaoRepository.LoadForeignKeyCollection`**: Has a TODO to review why `Dao.MapUlongToLong` is necessary.
- **`FsRepoData`**: Has a TODO to rename to `NamedRepoData`.
- **`CompositeHandleProvider`**: Has a TODO to use for generating hash keys based on `[KeyHandle]` properties.
- **`ManyToMany<TL,TR>`**: Has a TODO to explore this pattern later.
- **`TypeToDaoGenerator`**: Has TODOs to encapsulate assembly reference functionality and move `DefaultAssembliesToReference`.
