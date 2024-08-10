module internal EmbassyAccess.SerDe

open System
open Infrastructure

[<RequireQualifiedAccess>]
module Json =
    open System.Text.Json

    module Converters =
        open System.Text.Json.Serialization
        open EmbassyAccess.Domain

        type ErrorConverter() =
            inherit JsonConverter<Error'>()

            override _.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
                let mutable result: Error' option = None

                if reader.TokenType = JsonTokenType.StartObject then
                    let error = JsonSerializer.Deserialize<Error'> (reader,options)
                    result <- Some error
               

        type RequestStateConverter() =
            inherit JsonConverter<RequestState>()

            override _.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
                options.Converters.Add (ErrorConverter())

                let mutable result: RequestState option = None

                if reader.TokenType = JsonTokenType.String then
                    let value = reader.GetString()

                    result <-
                        match value with
                        | "Created" -> Some Created
                        | "InProcess" -> Some InProcess
                        | "Completed" -> Some Completed
                        | _ -> None

                if reader.TokenType = JsonTokenType.StartObject then
                    let error = JsonSerializer.Deserialize<Error'> (reader,options)
                    result <- Some(Failed error)

                match result with
                | Some state -> state
                | None -> raise (JsonException($"Unknown request state {reader.GetString()}"))

            override _.Write(writer: Utf8JsonWriter, value: RequestState, options: JsonSerializerOptions) =
                options.Converters.Add (ErrorConverter())
                
                match value with
                | Created -> writer.WriteStringValue("Created")
                | InProcess -> writer.WriteStringValue("InProcess")
                | Completed -> writer.WriteStringValue("Completed")
                | Failed error ->
                    writer.WriteStartObject()
                    JsonSerializer.Serialize(writer, error, options)
                    writer.WriteEndObject()
