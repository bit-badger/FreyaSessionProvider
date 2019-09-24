module FreyaSessionProvider.Relational

open System.Data.Common

/// Configuration for the relational Freya session provider
type RelationalSessionConfig =
  { /// The DbProviderFactory to use for establishing connections
    factory: DbProviderFactory
    /// The connection string to use for the underlying data store
    connectionString : string
    /// The schema in which the session table is found (defaults to None)
    schema : string option
    /// The table name for the session store (defaults to "Session")
    table : string
    }
module RelationalSessionConfig =
  /// Default values for the relational session configuration
  let defaults =
    { factory          = null
      connectionString = ""
      schema           = None
      table            = "Session"
      }

type RelationalSessionStore (config) =

  do ()
  // TODO: stopped here
  