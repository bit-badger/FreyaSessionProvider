module FreyaSessionProvider.Types

open Freya.Core
open System
open System.Collections.Generic
open System.Security.Cryptography

/// A collection of values associated with the session
[<CLIMutable>]
type Session =
  { /// The ID of the session
    sessionId : string
    /// The expiration date/time for the session
    expiry : DateTime
    /// The current data for the session
    data : IDictionary<string, string>
    }


/// Describes the required functionality of a session store
type ISessionStore =
  /// Get a session from the store
  abstract Get : string -> bool -> TimeSpan -> Session option
  /// Store a new session
  abstract Store : Session -> unit
  /// Update a session
  abstract Save : Session -> unit
  /// Delete a session's persistence
  abstract Destroy : string -> unit
  /// Delete expired sessions
  abstract CheckExpired : unit -> unit


/// Configuration for the session provider
type SessionProviderConfig =
  { /// The name of the session cookie
    cookieName : string
    /// For how long sessions are valid
    expiry : TimeSpan
    /// The session store implementation to use
    store : ISessionStore
    /// The cryptography implementation to use to encode the session cookie payload
    crypto : SymmetricAlgorithm
    /// Whether to reset the expiration of a session on every access
    rollingSessions : bool
    /// How frequently to check for and delete expired sessions
    expiryCheck : TimeSpan
    }

/// Functions to support the session provider configuration
module SessionProviderConfig =
  /// Session store that simply throws exceptions
  let private throwsSessionStore =
    let ex = NotImplementedException "You must provide a session store implementation"
    { new ISessionStore with
        member __.Get _ _ _       = raise ex
        member __.Store _         = raise ex
        member __.Save _          = raise ex
        member __.Destroy _       = raise ex
        member __.CheckExpired () = raise ex
      }
  /// Default values for session configuration
  let defaults =
    { cookieName      = ".FreyaSession"
      expiry          = TimeSpan (1, 0, 0)
      store           = throwsSessionStore
      crypto          = null
      rollingSessions = true
      expiryCheck     = TimeSpan (0, 1, 0)
      }


/// Session provider main interface
type ISessionProvider =
  /// Try to get a value from the current session
  abstract TryGetValue : string -> Freya<string option>
  /// Set a value in the current session
  abstract SetValue : string -> string -> Freya<unit>
  /// Remove a value from the current session
  abstract RemoveValue : string -> Freya<unit>
  /// Remove the entire session
  abstract DestroySession : unit -> Freya<unit>
