﻿module JwtWebSharperSitelet.Cryptography

open System
open System.Security.Cryptography

let private saltSize = 32
let private keyLength = 64
let private iterations = 10000
let private hashSize = saltSize + keyLength + sizeof<int>

let hash password =
    use pbkdf2 = new Rfc2898DeriveBytes(password, saltSize, iterations)
    let salt = pbkdf2.Salt
    let keyBytes = pbkdf2.GetBytes(keyLength)
    let iterationBytes = if BitConverter.IsLittleEndian then BitConverter.GetBytes(iterations) else BitConverter.GetBytes(iterations) |> Array.rev

    let hashedPassword = Array.zeroCreate<byte> hashSize
    Buffer.BlockCopy(salt, 0, hashedPassword, 1, saltSize)
    Buffer.BlockCopy(keyBytes, 0, hashedPassword, saltSize + 1, keyLength)
    Buffer.BlockCopy(iterationBytes, 0, hashedPassword, saltSize + keyLength + 1, sizeof<int>)

    Convert.ToBase64String(hashedPassword)

let verify hashedPassword (password:string) =
    let hashedPassword = Convert.FromBase64String(hashedPassword)

    if hashedPassword.Length <> hashSize || hashedPassword.[0] <> byte 0 then
        false
    else
        let salt = Array.zeroCreate<byte> saltSize
        let keyBytes = Array.zeroCreate<byte> keyLength
        let iterationBytes = Array.zeroCreate<byte> sizeof<int>

        Buffer.BlockCopy(hashedPassword, 1, salt, 0, saltSize)
        Buffer.BlockCopy(hashedPassword, saltSize + 1, keyBytes, 0, keyLength)
        Buffer.BlockCopy(hashedPassword, saltSize + keyLength + 1, iterationBytes, 0, sizeof<int>);
        
        let iterations = BitConverter.ToInt32((if BitConverter.IsLittleEndian then iterationBytes else iterationBytes |> Array.rev), 0)

        use pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations)
        let challengeBytes = pbkdf2.GetBytes(32)

        match Seq.compareWith (fun a b -> if a = b then 0 else 1) hashedPassword challengeBytes with
        | v when v = 0 -> true
        | _ -> false
