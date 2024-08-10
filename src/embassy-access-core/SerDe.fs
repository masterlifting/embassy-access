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

            let _errorConverterType = typeof<Error'>
            
            override _.Write(writer: Utf8JsonWriter, value: Domain.RequestState, options: JsonSerializerOptions) =

                match value with
                | Domain.Created -> writer.WriteStringValue(CreatedState)
                | Domain.InProcess -> writer.WriteStringValue(InProcessState)
                | Domain.Completed -> writer.WriteStringValue(CompletedState)
                | Domain.Failed error ->
                    writer.WriteStartObject()
                    let errorConverter = options.GetConverter(_errorConverterType) :?> JsonConverter<Error'>
                    errorConverter.Write(writer, error, options)
                    writer.WriteEndObject()

            override _.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =

                match reader.TokenType with
                | JsonTokenType.String ->
                    match reader.GetString() with
                    | CreatedState -> Domain.Created
                    | InProcessState -> Domain.InProcess
                    | CompletedState -> Domain.Completed
                    | _ -> raise <| JsonException($"Unexpected state for '{nameof Domain.RequestState}'.")
                | JsonTokenType.StartObject ->
                    let errorConverter = options.GetConverter _errorConverterType :?> JsonConverter<Error'>
                    let error = errorConverter.Read(&reader, _errorConverterType, options)
                    Domain.Failed error
                | _ -> raise <| JsonException("Expected String or StartObject")
