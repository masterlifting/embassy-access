module internal EmbassyAccess.SerDe

open System
open Infrastructure

[<RequireQualifiedAccess>]
module Json =
    open System.Text.Json
    
    module Converter =
        open System.Text.Json.Serialization

        type RequestState() =
            inherit JsonConverter<Domain.RequestState>()

            [<Literal>]
            let CreatedState = "Created"
            [<Literal>]
            let InProcessState = "InProcess"
            [<Literal>]
            let CompletedState = "Completed"
            [<Literal>]
            let FailedType = "Failed"
            [<Literal>]
            let ErrorType = "Error"

            override _.Write(writer: Utf8JsonWriter, value: Domain.RequestState, options: JsonSerializerOptions) =
                
                match value with
                | Domain.Created -> writer.WriteStringValue(CreatedState)
                | Domain.InProcess -> writer.WriteStringValue(InProcessState)
                | Domain.Completed -> writer.WriteStringValue(CompletedState)
                | Domain.Failed error ->
                    writer.WriteStartObject()
                    let errorConverter = options.GetConverter(typeof<Error'>) :?> JsonConverter<Error'>
                    errorConverter.Write(writer, error, options)
                    writer.WriteEndObject()

            override _.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =

                let token = reader.TokenType

                if token = JsonTokenType.String then
                    let state = reader.GetString()
                    match state with
                    | CreatedState -> Domain.Created
                    | InProcessState -> Domain.InProcess
                    | CompletedState -> Domain.Completed
                    | _ -> raise <| JsonException($"Unexpected state: {state}")
                elif token = JsonTokenType.StartObject then
                    let errorConverter = options.GetConverter(typeof<Error'>) :?> JsonConverter<Error'>
                    let error = errorConverter.Read(&reader, typeof<Error'>, options)
                    Domain.Failed error
                else
                    raise <| JsonException("Expected String or StartObject")

                

