# TLE Crawler

> Practiced two specific topics in depth:
>
> 1. **The "Modules" pattern for ASP.NET Core Minimal API** — a way to keep endpoint registration and DI wiring grouped by feature instead of dumped into `Program.cs`.
> 2. **Raw ADO.NET (`Microsoft.Data.SqlClient`)** — direct `SqlConnection` / `SqlCommand` / `SqlDataReader` usage, transactions, stored procedures, table-valued parameters and `SqlBulkCopy`, with no ORM in front of them.
>

---

## Solution layout

The solution follows a Clean-Architecture-ish split:

```
TLECrawler.sln
├── TLECrawler.Api/              # Minimal API host. Wires modules and runs the web app.
│   ├── Program.cs               # ~30 lines — almost everything is delegated.
│   ├── DependencyInjection.cs   # AddPresentation: configuration, logging, CORS.
│   └── Modules/                 # ★ the Modules pattern lives here
│       ├── Interfaces/IModule.cs
│       ├── Extensions/ModuleExtensions.cs
│       ├── AuthenticationModule.cs
│       ├── IterationModule.cs
│       ├── SQLModule.cs
│       └── TLEModule.cs
├── TLECrawler.Application/      # Abstractions only (interfaces for repositories and services).
├── TLECrawler.Domain/           # Records, enums, configuration POCOs.
├── TLECrawler.Infrastucture/    # ★ ADO.NET implementations live here
│   └── DAL/
│       ├── TLEDBFactory.cs      # Factory for SqlConnection / SqlCommand / SqlParameter.
│       ├── TLERepository.cs     # The richest sample of ADO.NET usage in the project.
│       └── IterationRepository.cs
├── Helpers/SqlHelper/           # Hand-written SQL strings (TLESQL, IterationsSQL).
└── TLECrawler.Tests/            # xUnit tests.
```

---

## 1. The "Modules" pattern for Minimal API

### Problem it solves

A vanilla Minimal API tends to grow into a single fat `Program.cs` containing:

- every `services.AddXxx(...)` call,
- every `app.MapGet/Post(...)` registration,
- glue code mixed with bootstrap.

The Modules pattern groups **DI registration** and **endpoint mapping** for one feature into a single class, then auto-discovers and registers all such classes via reflection. The host file stays tiny.

### Anatomy

**The contract** — every module implements `IModule` ([TLECrawler.Api/Modules/Interfaces/IModule.cs](TLECrawler.Api/Modules/Interfaces/IModule.cs)):

```csharp
public interface IModule
{
    IServiceCollection RegisterModule(IServiceCollection services);
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

**The discovery + dispatch** — `ModuleExtensions` finds every `IModule` in the assembly via reflection, instantiates them, and forwards both the DI and routing calls ([TLECrawler.Api/Modules/Extensions/ModuleExtensions.cs](TLECrawler.Api/Modules/Extensions/ModuleExtensions.cs)):

```csharp
public static class ModuleExtensions
{
    static readonly List<IModule> registeredModules = [];

    public static IServiceCollection RegisterModules(this IServiceCollection services)
    {
        foreach (var module in DiscoverModules())
        {
            module.RegisterModule(services);
            registeredModules.Add(module);
        }
        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        foreach (var module in registeredModules)
            module.MapEndpoints(app);
        return app;
    }

    private static IEnumerable<IModule> DiscoverModules() =>
        typeof(IModule).Assembly
            .GetTypes()
            .Where(p => p.IsClass && p.IsAssignableTo(typeof(IModule)))
            .Select(Activator.CreateInstance)
            .Cast<IModule>();
}
```

**A concrete module** — feature wiring + endpoints in one file ([TLECrawler.Api/Modules/IterationModule.cs](TLECrawler.Api/Modules/IterationModule.cs)):

```csharp
public class IterationModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services) =>
        services
            .AddTransient<IIterationRepository, IterationRepository>()
            .AddScoped<IIterationService, IterationService>();

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/GetLastIteration",
            (IIterationRepository repo) => Results.Ok(repo.GetLast()));

        endpoints.MapPost("/MakeNewIteration", async (IIterationService svc, ...) => { ... });

        return endpoints;
    }
}
```

**The host file stays minimal** ([TLECrawler.Api/Program.cs](TLECrawler.Api/Program.cs)):

```csharp
builder.Services
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services.RegisterModules();   // ← discovers and wires every IModule

var app = builder.Build();
app.MapEndpoints();                   // ← hooks every module's endpoints
app.Run();
```

### Trade-offs

| Pros                                                        | Cons                                                              |
| ----------------------------------------------------------- | ----------------------------------------------------------------- |
| Feature cohesion — endpoints + DI for a feature live together. | Reflection-based discovery is implicit; ordering is non-obvious.  |
| Adding a feature = adding one file. No central touch points. | `Activator.CreateInstance` requires a parameterless constructor.  |
| Easy to grep: "what does this module own?"                  | Modules can't depend on each other through DI at registration time.|
| Encourages thin `Program.cs`.                               | Slightly heavier than just calling `app.MapGet` directly.         |

---

## 2. ADO.NET usage — selected representative cases

The project deliberately avoids EF Core and Dapper. Every database call goes through `Microsoft.Data.SqlClient` primitives. The samples below are selected because each one demonstrates a distinct ADO.NET technique.

### 2.1 Connection factory + dependency-injected `SqlConnection`

`TLEDBFactory` ([TLECrawler.Infrastucture/DAL/TLEDBFactory.cs](TLECrawler.Infrastucture/DAL/TLEDBFactory.cs)) builds a connection string at runtime from `IDataProtection`-encrypted settings, and exposes helpers for `SqlCommand` / `SqlParameter` so repositories don't repeat boilerplate:

```csharp
public SqlConnection InitializeConnection()
{
    var options = _databaseOptions.Value;

    string Source   = _protector.Unprotect(options.DataSource);
    string Catalog  = _protector.Unprotect(options.InitialCatalog);
    string User     = _protector.Unprotect(options.UserID);
    string Password = _protector.Unprotect(options.Password);

    string cs =
        $"Data Source={Source};" +
        $"Initial Catalog={Catalog};" +
        $"User ID={User};" +
        $"Password={Password};" +
        $"Connect Timeout={options.Timeout};" +
        $"Encrypt=True;Pooling=True;Trust Server Certificate=True;";

    return new SqlConnection(cs);
}
```

**What it teaches:** the lifecycle is *connection-per-call* (factory hands back a fresh closed `SqlConnection`); pooling is left to the SQL client. Credentials never appear in plaintext at rest — they live in `appsettings.json` already encrypted by `IDataProtector`.

### 2.2 Reading a single row with `SqlDataReader` and binary columns

[`TLERepository.GetAsync(int id)`](TLECrawler.Infrastucture/DAL/TLERepository.cs:41) — a single-row read demonstrating `CommandBehavior.SingleResult` and binary streaming with `GetBytes`:

```csharp
public async Task<TLE> GetAsync(int id)
{
    string command = TLESQL.GetById(id);

    await using SqlConnection connection = _tleDataBase.InitializeConnection();
    await connection.OpenAsync();
    SqlCommand sqlCommand = _tleDataBase.CreateSqlCommand(connection, command, 600);
    using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SingleResult);

    byte[] hash = new byte[16];
    var bytesRead = reader.GetBytes(5, 0, hash, 0, 16);

    return new TLE(
        PublishDate: reader.GetDateTime(3),
        FirstRow:    reader.GetString(1),
        SecondRow:   reader.GetString(2),
        Hash:        hash,
        IterationId: reader.GetInt32(4));
}
```

**What it teaches:** typed accessors (`GetDateTime`, `GetString`, `GetInt32`) versus reading binary blobs into a pre-allocated buffer with `GetBytes`. `await using` for connections, `using` for readers.

### 2.3 Parameterised `IN (...)` queries built dynamically

[`TLERepository.GetAsync(IEnumerable<byte[]> hashCodes, int year)`](TLECrawler.Infrastucture/DAL/TLERepository.cs:62) — a hand-rolled batch lookup. The SQL helper builds a `WHERE Hash IN (@p1, @p2, ..., @pN)` clause for an arbitrary count, and parameters are bound positionally:

```csharp
string query = TLESQL.GetBatchFromPartitionByHash(HashCodes.Count, year);

SqlCommand command = _tleDataBase.CreateSqlCommand(connection, query, 600);
for (int i = 0; i < HashCodes.Count; i++)
{
    command.Parameters.Add($"@p{i + 1}", SqlDbType.Binary).Value = HashCodes[i];
}
using SqlDataReader reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync()) { /* map row */ }
```

**What it teaches:** how to keep SQL injection-safe even when the query shape itself depends on input size — the *list length* is interpolated, but the *values* are parameters.

### 2.4 Stored procedure with output parameters

[`IterationRepository.InitializeIteration()`](TLECrawler.Infrastucture/DAL/IterationRepository.cs:25) — calls a stored procedure that allocates an ID and timestamp on the server side and returns them via `OUTPUT` parameters:

```csharp
var outputId = _tleDataBase.CreateSqlParameter(
    "@output_id", SqlDbType.Int, -1, ParameterDirection.Output);

var isRepeat = _tleDataBase.CreateSqlParameter(
    "@isRepeat",  SqlDbType.Bit, false);

var outputStart = _tleDataBase.CreateSqlParameter(
    "@output_startDateTime", SqlDbType.DateTime, DateTime.UtcNow, ParameterDirection.Output);

using SqlConnection connection = _tleDataBase.InitializeConnection();
connection.Open();
_tleDataBase.ExecuteStoredProcedure(connection, "saveIteration",
    [outputId, isRepeat, outputStart]);

return Convert.ToInt32(outputId.Value);
```

**What it teaches:** `ParameterDirection.Output`, `CommandType.StoredProcedure`, and reading the populated `.Value` back after `ExecuteNonQuery`.

### 2.5 Table-Valued Parameters (TVP) — passing a collection as a single parameter

[`TLERepository.GetByTvpHashesAsync(...)`](TLECrawler.Infrastucture/DAL/TLERepository.cs:132) — instead of building a long `IN (...)` clause, the entire batch of hashes is shipped as a single `SqlDbType.Structured` parameter against a user-defined table type `HashTableType`:

```csharp
var hashTable = new DataTable();
hashTable.Columns.Add("Hash", typeof(byte[]));
foreach (var hash in hashCodes)
    hashTable.Rows.Add(hash);

SqlCommand command = new("GetTLEsByHashes", connection, transaction)
{
    CommandType = CommandType.StoredProcedure,
};
SqlParameter tvp = new("@Hashes", SqlDbType.Structured)
{
    TypeName = "HashTableType",
    Value    = hashTable
};
command.Parameters.Add(tvp);

using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync()) { /* map row by name */ }
await transaction.CommitAsync();
```

**What it teaches:** TVPs are the idiomatic way to send a *set* into a stored procedure. The reader uses **named** column access (`reader.GetString("FirstRow")`) instead of ordinals — robust against column-order changes.

### 2.6 High-throughput inserts with `SqlBulkCopy`

[`TLERepository.InsertManyAsync(IEnumerable<TLE>)`](TLECrawler.Infrastucture/DAL/TLERepository.cs:283) — bulk insert via `SqlBulkCopy`, which uses SQL Server's bulk-load protocol and is orders of magnitude faster than per-row `INSERT`s:

```csharp
DataTable TLEDataTable = CreateInMemoryTleDataTable([.. tles]);

await using SqlConnection connection = _tleDataBase.InitializeConnection();
await connection.OpenAsync();
using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

using (SqlBulkCopy sqlBulkCopy = new(connection, SqlBulkCopyOptions.CheckConstraints, transaction))
{
    sqlBulkCopy.ColumnMappings.Add("FirstRow",    "FirstRow");
    sqlBulkCopy.ColumnMappings.Add("SecondRow",   "SecondRow");
    sqlBulkCopy.ColumnMappings.Add("PublishDate", "PublishDate");
    sqlBulkCopy.ColumnMappings.Add("Hash",        "Hash");
    sqlBulkCopy.ColumnMappings.Add("IterationId", "IterationId");

    await sqlBulkCopy.WriteToServerAsync(TLEDataTable);
    sqlBulkCopy.DestinationTableName = "dbo.TLEs";
}
await transaction.CommitAsync();
```

**What it teaches:** the `DataTable` ↔ destination column **mapping** (column names don't have to match), `SqlBulkCopyOptions.CheckConstraints` to keep validation on, and wrapping the bulk write in a transaction so failures roll back atomically.

### 2.7 Transactional stored-procedure insert with rollback

[`TLERepository.InsertOneAsync(TLE)`](TLECrawler.Infrastucture/DAL/TLERepository.cs:245) — an explicit transaction created via `BeginTransactionAsync`, with a try/catch that calls `RollbackAsync` if the procedure throws (and a nested guard around rollback itself, since rollback can also fail):

```csharp
await using SqlConnection connection = _tleDataBase.InitializeConnection();
await connection.OpenAsync();
await using DbTransaction dbTransaction =
    await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
var transaction = (SqlTransaction)dbTransaction;

try
{
    await _tleDataBase.ExecuteStoredProcedureAsNonQueryAsync(connection, "writeTLE",
    [
        _tleDataBase.CreateSqlParameter("@firstRow",    SqlDbType.VarChar,  tle.FirstRow),
        _tleDataBase.CreateSqlParameter("@secondRow",   SqlDbType.VarChar,  tle.SecondRow),
        _tleDataBase.CreateSqlParameter("@publishDate", SqlDbType.DateTime, tle.PublishDate),
        _tleDataBase.CreateSqlParameter("@hash",        SqlDbType.Binary,   tle.Hash),
        _tleDataBase.CreateSqlParameter("@iterationId", SqlDbType.Int,      tle.IterationId)
    ], transaction);

    await transaction.CommitAsync();
}
catch (Exception ex)
{
    try   { await transaction.RollbackAsync(); }
    catch (Exception rollbackEx) { _logger.LogError(rollbackEx, "Failed to rollback transaction"); }
    throw;
}
```

**What it teaches:** explicit isolation levels, async commit/rollback, and the realistic failure mode that *rollback itself* can fail.

### 2.8 Streaming a large result set with `CommandBehavior.SequentialAccess`

[`TLERepository.GetFromPartitionAsync(int partitionYear)`](TLECrawler.Infrastucture/DAL/TLERepository.cs:217) — uses `CommandBehavior.SequentialAccess`, which tells the reader to stream columns left-to-right without buffering each row in full. Important when rows contain large binary payloads.

```csharp
using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
while (reader.Read()) { /* must read columns in declared order */ }
```

**What it teaches:** the cost/benefit of `SequentialAccess` — lower memory, but you must access columns in order and only once.

---

## Configuration

The application reads the following sections from `appsettings.json` (gitignored):

- `Database` → `DataBaseSettings` (DataSource, InitialCatalog, UserID, Password — all expected to be `IDataProtector`-encrypted strings).
- `SpaceTrackLinks` → URLs.
- `SessionSettings` → schedule of polling times.
- `UserCredentials` → space-track.org login (encrypted).

The encrypted values are produced by the debug-only endpoint `POST /GetEncryptedUser` (`AuthenticationModule`) and `GET /GetDatabaseEncryption` (`SQLModule`), then pasted into `appsettings.json`.

### Test environment variables

`TLECrawler.Tests` reads its DB and user credentials from environment variables (no longer hardcoded):

- `TLECRAWLER_TEST_DB_CONNECTION` — full ADO.NET connection string for the test database.
- `TLECRAWLER_TEST_USER_IDENTITY` — space-track.org login used in tests.
- `TLECRAWLER_TEST_USER_PASSWORD` — corresponding password.

---

## Running

```bash
dotnet restore
dotnet build
dotnet run --project TLECrawler.Api
```

Swagger UI: `https://localhost:7221/swagger`.
