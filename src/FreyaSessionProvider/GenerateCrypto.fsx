
open System
open System.Security.Cryptography

let b64 = Convert.ToBase64String
let blank () = Console.WriteLine ""
let aes = Aes.Create ()

blank ()
Console.WriteLine "The code below will initialize AES cryptography for use in encrypting the"
Console.WriteLine "  session cookie payload."
blank ()
Console.WriteLine "  // 'let' instead of 'use', because this is going into a singleton"
Console.WriteLine "  let aes = Aes.Create ()"
Console.WriteLine (sprintf "  aes.Key <- Convert.FromBase64String \"%s\"" (b64 aes.Key))
Console.WriteLine (sprintf "  aes.IV <- Convert.FromBase64String \"%s\"" (b64 aes.IV))
Console.WriteLine "  SessionProvider.Create"
Console.WriteLine "    { SessionProviderConfig.defaults with"
Console.WriteLine "        store = (* Your Store *)"
Console.WriteLine "        crypto = aes"
Console.WriteLine "      }"
Console.WriteLine "  // returns ISessionProvider"
blank ()
Console.WriteLine "NOTE: Hard-coding keys and values is not a best practice; these should be moved"
Console.WriteLine "      to environment variables or configuration files instead."
blank ()
