module EA.Telegram.Domain

open Infrastructure
open EA.Domain
open Web.Telegram.Domain

module Key =
    open System
    open System.Text
    open System.IO
    open System.IO.Compression
    open System.Security.Cryptography

    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

    [<Literal>]
    let Chats = "chats"

    [<Literal>]
    let SUB = "SUB"

    [<Literal>]
    let INF = "INF"

    [<Literal>]
    let APT = "APT"

    [<Literal>]
    let CNF = "CNF"
    
    let encryptionKey = Encoding.UTF8.GetBytes("your-very-strong-secret-key-32bytes") // 32 bytes

    let encrypt (plainText: string) =
        use aes = Aes.Create()
        aes.KeySize <- 128
        aes.Key <- encryptionKey
        aes.GenerateIV()
        let iv = aes.IV
        use encryptor = aes.CreateEncryptor(aes.Key, iv)
        use ms = new MemoryStream()
        use cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
        use writer = new StreamWriter(cs)
        writer.Write(plainText)
        writer.Flush()
        cs.FlushFinalBlock()
        let encryptedData = ms.ToArray()
        let result = Array.append iv encryptedData
        Convert.ToBase64String(result)

    let decrypt (cipherText: string) =
        let fullCipher = Convert.FromBase64String(cipherText)
        let iv = Array.sub fullCipher 0 16
        let cipherBytes = Array.sub fullCipher 16 (fullCipher.Length - 16)
        use aes = Aes.Create()
        aes.KeySize <- 128
        aes.Key <- encryptionKey
        aes.IV <- iv
        use decryptor = aes.CreateDecryptor(aes.Key, aes.IV)
        use ms = new MemoryStream(cipherBytes)
        use cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)
        use reader = new StreamReader(cs)
        reader.ReadToEnd()

    let compress (input: string) =
        let bytes = Encoding.UTF8.GetBytes(input)
        use outputStream = new MemoryStream()
        use gzip = new GZipStream(outputStream, CompressionMode.Compress)
        gzip.Write(bytes, 0, bytes.Length)
        gzip.Close()
        let base64 = Convert.ToBase64String(outputStream.ToArray())
        base64.Replace('+', '-').Replace('/', '_').TrimEnd('=')

    let decompress (input: string) =
        let base64 = input.Replace('-', '+').Replace('_', '/')
        let paddedBase64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')
        let compressedBytes = Convert.FromBase64String(paddedBase64)
        use inputStream = new MemoryStream(compressedBytes)
        use gzip = new GZipStream(inputStream, CompressionMode.Decompress)
        use reader = new StreamReader(gzip, Encoding.UTF8)
        reader.ReadToEnd()

    let toHash (input: string) =
        using (SHA256.Create()) (fun sha256 ->
            let bytes = Encoding.UTF8.GetBytes(input)
            let hash = sha256.ComputeHash(bytes)
            BitConverter.ToString(hash).Replace("-", "").ToLower())

    let serialize (payload: Payload) =
        Json.serialize payload |> Result.defaultValue "" |> compress |> encrypt

    let deserialize (data: string) =
        let json = data |> decrypt |>  decompress

        match json |> Json.deserialize<Payload> with
        | Ok payload -> Some payload
        | Error _ -> None

    let wrap' payload = serialize payload

    let unwrap' (value: string) =
        deserialize value |> Option.defaultValue { Route = ""; Data = Map.empty }

    let wrap (values: string seq) = values |> String.concat "|"
    let unwrap (value: string) = value |> fun x -> x.Split '|'

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

module Response =
    open System.Threading
    open Web.Telegram.Domain.Producer
    open Microsoft.Extensions.Configuration

    type Text =
        | Embassies of (ChatId -> Async<Result<Data, Error'>>)
        | UserEmbassies of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | Subscribe of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | NoText

    type Callback =
        | Countries of ((ChatId * int) -> Async<Result<Data, Error'>>)
        | Cities of ((ChatId * int) -> Async<Result<Data, Error'>>)
        | UserCountries of ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | UserCities of ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | SubscriptionRequest of ((ChatId * int) -> Async<Result<Data, Error'>>)
        | UserSubscriptions of
            ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | ConfirmAppointment of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | NoCallback


module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
