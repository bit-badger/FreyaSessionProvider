module FreyaSessionProvider.InMemory

open System
open Types

type InMemorySessionStore () =

  let mutable sessions = Session list

  interface ISessionStore with
    
    member __.Get sessId useRolling expiry =
      match sessions |> List.tryFind (fun x -> x.sessionId = sessId) with
      | Some sess ->
          match useRolling with
          | true ->
              let newSess = { sess with expiry = DateTime.UtcNow + expiry }
              sessions <- newSess :: sessions |> List.filter (fun x -> x.sessionId <> sessId)
              Some newSess
          | false -> Some sess
      | None -> None
    
    member __.Store session =
      sessions <- session :: sessions

    member __.Save session =
      sessions <- session :: sessions |> List.filter (fun x -> x.sessionId <> session.sessionId)
    
    member __.Destroy sessId =
      sessions <- sessions |> List.filter (fun x -> x.sessionId <> sessId)
    
    member __.CheckExpired () =
      sessions <- sessions |> List.filter (fun x -> x.expiry < DateTime.UtcNow)
