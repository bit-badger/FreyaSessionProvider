namespace FreyaSessionProvider

open Raven.Client.Documents
open Types

/// A RavenDB-based session store for Freya sessions
type RavenDBSessionStore (store : IDocumentStore) =
  
  /// Shorthand for making a session document ID
  let sessDocId = sprintf "Sessions/%s"

  interface ISessionStore with

    member __.Get sessId =
      use docSession = store.OpenSession ()
      docSession.Load<Session> (sessDocId sessId)

    member __.Store session =
      use docSession = store.OpenSession ()
      docSession.Store (session, sessDocId session.sessionId)
      docSession.SaveChanges ()
    
    member __.Save session =
      use docSession = store.OpenSession ()
      docSession.Advanced.Patch (sessDocId session.sessionId, (fun x -> x.data),   session.data)
      docSession.Advanced.Patch (sessDocId session.sessionId, (fun (x : Session) -> x.expiry), session.expiry)
      docSession.SaveChanges ()
    
    member __.Destroy sessionId =
      let docSession = store.OpenSession ()
      docSession.Delete (sessDocId sessionId)
      docSession.SaveChanges ()

