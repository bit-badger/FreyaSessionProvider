module FreyaSessionProvider.RavenDB

open Raven.Client.Documents
open System
open Types

/// Module to keep this similar-but-different session from being directly visible
module DocumentType =
  
  open System.Collections.Generic

  /// Shorthand for turning a session ID into a document ID
  let toDocumentId = sprintf "Sessions/%s"

  /// Shorthand for turning a document ID into a session ID
  let toSessionId (docId : string) = docId.Replace ("Sessions/", "")

  /// The document form that is stored in RavenDB
  [<CLIMutable>]
  type RavenSession =
    { Id     : string
      Expiry : DateTime
      Data   : IDictionary<string, string>
      }
  /// Functions to manipulate the RavenDB-format session document
  module RavenSession =
    /// Convert a Freya session to the form we need to store in RavenDB
    let ofSession session =
      { Id     = toDocumentId session.sessionId
        Expiry = session.expiry
        Data   = session.data
        }
    /// Convert a stored session from RavenDB to a Freya session
    let toSession session =
      { sessionId = toSessionId session.Id
        expiry    = session.Expiry
        data      = session.Data
        }

open DocumentType

/// A RavenDB-based session store for Freya sessions
type RavenDBSessionStore (store : IDocumentStore) =
  
  /// Log an error
  let logError m (ex : exn) =
    let err = Console.Error
    err.WriteLine (sprintf "[FreyaSession-RavenDB] %s - %s" m <| ex.GetType().Name)
    err.WriteLine ex.StackTrace

  interface ISessionStore with

    member __.Get sessId useRolling expiry =
      try
        use docSession = store.OpenSession ()
        match (toDocumentId >> docSession.Load<RavenSession> >> box >> Option.ofObj) sessId with
        | Some doc ->
            let sess = unbox<RavenSession> doc
            match useRolling with
            | true ->
                let newExpiry = DateTime.UtcNow + expiry
                docSession.Advanced.Patch (sess.Id, (fun x -> x.Expiry), newExpiry)
                docSession.SaveChanges ()
                { sess with Expiry = newExpiry }
            | false -> sess
            |> (RavenSession.toSession >> Some)
        | None -> None
      with ex ->
        logError "Get" ex
        None

    member __.Store session =
      try
        use docSession = store.OpenSession ()
        docSession.Store (RavenSession.ofSession session, toDocumentId session.sessionId)
        docSession.SaveChanges ()
      with ex -> logError "Store" ex
    
    member __.Save session =
      try
        use docSession = store.OpenSession ()
        docSession.Advanced.Patch (toDocumentId session.sessionId, (fun x -> x.Data),   session.data)
        docSession.Advanced.Patch (toDocumentId session.sessionId, (fun x -> x.Expiry), session.expiry)
        docSession.SaveChanges ()
      with ex -> logError "Save" ex
    
    member __.Destroy sessionId =
      try
        let docSession = store.OpenSession ()
        docSession.Delete (toDocumentId sessionId)
        docSession.SaveChanges ()
      with ex -> logError "Destroy" ex
    
    member __.CheckExpired () =
      try
        use docSession = store.OpenSession ()
        // TODO get syntax
        docSession.SaveChanges ()
      with ex -> logError "CheckExpired" ex
