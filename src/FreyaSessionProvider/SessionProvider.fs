namespace FreyaSessionProvider

open Aether
open Freya.Core
open Freya.Optics.Http
open Freya.Optics.Http.Cors
open Freya.Types.Http.State
open MiniGuids
open System
open System.IO
open System.Security.Cryptography
open Types

/// The Freya session provider
type SessionProvider private (config : SessionProviderConfig) =

  /// The name of the cookie in which we'll store our session ID
  let cookieName = Name config.cookieName

  /// The Unix epoch (not sure why this is showing undefined)
  let unixEpoch = DateTime (1970, 1, 1, 0, 0, 0)

  /// Encrypt a string, encoding it in base64
  let encrypt (value : string) =
    use enc       = config.crypto.CreateEncryptor ()
    use msEncrypt = new MemoryStream ()
    use csEncrypt = new CryptoStream (msEncrypt, enc, CryptoStreamMode.Write)
    use swEncrypt = new StreamWriter (csEncrypt)
    swEncrypt.Write (value)
    swEncrypt.Flush ()
    msEncrypt.ToArray () |> Convert.ToBase64String
    
  /// Decrypt an encoded base64 string
  let decrypt (base64 : string) =
    use dec       = config.crypto.CreateDecryptor ()
    use msDecrypt = new MemoryStream (Convert.FromBase64String base64)
    use csDecrypt = new CryptoStream (msDecrypt, dec, CryptoStreamMode.Read)
    use srDecrypt = new StreamReader (csDecrypt)
    srDecrypt.ReadToEnd ()
    
  /// Create a response cookie with the given session ID and expiration
  let responseCookie sessId expires =
    let encSessionId = encrypt sessId
    Console.WriteLine (sprintf "Setting cookie for session ID %s (encrypted as %s)" sessId encSessionId)
    SetCookie ((Pair (cookieName, Value (encrypt sessId))), Attributes [ Expires expires; HttpOnly ])

  /// Get the max age of a session
  let maxAge () = DateTime.UtcNow + config.expiry
  
  /// Add a response cookie with the session ID
  let addResponseCookie sessId =
    freya {
      match! Freya.Optic.get Response.Headers.setCookie_ with
      | Some _ -> ()
      | None -> do! Freya.Optic.set Response.Headers.setCookie_ (responseCookie sessId (maxAge ()) |> Some)
      }

  /// Create a session, returning the session ID
  let createSessionId =
    freya {
      let sessId = (MiniGuid.NewGuid >> string) ()
      Console.WriteLine (sprintf "Created new session ID %s" sessId)
      do! addResponseCookie sessId
      return sessId
      }

  /// Get the ID of the current session (creating one if one does not exist)
  let getSessionId =
    freya {
      // FIXME check SetCookie first; it has the ID of the current request if we generated it
      match! Freya.Optic.get (Request.Headers.cookie_) with
      | Some c ->
          let theCookie =
            (fst Cookie.pairs_) c
            |> List.tryFind (fun p -> Optic.get Pair.name_ p = cookieName)
          match theCookie with
          | Some p ->
              let (Value value) = Optic.get Pair.value_ p
              let decValue = decrypt value
              Console.WriteLine (sprintf "Got session from cookie; %s decrypted to %s" value decValue)
              return decrypt value
          | None -> return! createSessionId
      | None -> return! createSessionId
      }

  /// Get the session document, handling expired documents
  let getSession sessId =
    match config.store.Get sessId with
    | Some session ->
        match session.expiry < DateTime.UtcNow with
        | true -> 
            config.store.Destroy sessId
            None
        | false -> Some session
    | None -> None

  /// Create a configured instance of the SessionProvider class
  static member Create config : ISessionProvider = upcast SessionProvider config

  interface ISessionProvider with

    member __.TryGetValue name =
      freya {
        let! sessionId = getSessionId
        return
          match getSession sessionId with
          | Some sess -> match sess.data.ContainsKey name with true -> Some sess.data.[name] | false -> None
          | None -> None
        }
    
    member __.SetValue name item =
      freya {
        let! sessionId = getSessionId
        match getSession sessionId with
        | Some sess ->
            sess.data.[name] <- item
            config.store.Save { sess with expiry = maxAge () }
        | None ->
            { sessionId = sessionId
              expiry    = maxAge ()
              data      = [ name, item ] |> dict
              }
            |> config.store.Store
        do! addResponseCookie sessionId
        }
    
    member __.RemoveValue name =
      freya {
        let! sessionId = getSessionId
        match getSession sessionId with
        | Some sess ->
            match sess.data.ContainsKey name with
            | true ->
                sess.data.Remove name |> ignore
                config.store.Save sess
            | false -> ()
        | None -> ()
      }

    member __.DestroySession () =
      freya {
        let! sessionId = getSessionId
        config.store.Destroy sessionId
        // Expire the cookie
        do! Freya.Optic.set Response.Headers.setCookie_ (responseCookie sessionId unixEpoch |> Some)
        }
