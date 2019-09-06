module FreyaSessionProvider.RavenDB

open Raven.Client.Documents
open Types

/// Module to keep this similar-but-different session from being directly visible
module DocumentType =
  
  open System
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
  
  interface ISessionStore with

    member __.Get sessId =
      use docSession = store.OpenSession ()
      match (toDocumentId >> docSession.Load<RavenSession> >> box >> Option.ofObj) sessId with
      | Some doc -> (unbox<RavenSession> >> RavenSession.toSession >> Some) doc
      | None -> None

    member __.Store session =
      use docSession = store.OpenSession ()
      docSession.Store (RavenSession.ofSession session, toDocumentId session.sessionId)
      docSession.SaveChanges ()
    
    member __.Save session =
      use docSession = store.OpenSession ()
      docSession.Advanced.Patch (toDocumentId session.sessionId, (fun x -> x.data),   session.data)
      docSession.Advanced.Patch (toDocumentId session.sessionId, (fun (x : Session) -> x.expiry), session.expiry)
      docSession.SaveChanges ()
    
    member __.Destroy sessionId =
      let docSession = store.OpenSession ()
      docSession.Delete (toDocumentId sessionId)
      docSession.SaveChanges ()

