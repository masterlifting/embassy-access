module EA.Italian.Services.Prenotami.Web.Html.Common

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open EA.Italian.Services.Domain.Prenotami

let pageHasError page =
    page
    |> Html.getNode "//span[@id='ctl00_MainContent_lblCodeErr']"
    |> Result.bind (function
        | None -> Ok page
        | Some node ->
            match node.InnerText with
            | AP.IsString text ->
                Error
                <| Operation {
                    Message = text
                    Code = Constants.ErrorCode.PAGE_HAS_ERROR |> Custom |> Some
                }
            | _ -> Ok page)

let pageHasInconsistentState validate page =
    page
    |> Html.getNode "//span[@id='ctl00_MainContent_Content'] | //span[@id='ctl00_MainContent_Label_Message']"
    |> Result.bind (function
        | None -> Ok page
        | Some node ->
            match node.InnerHtml with
            | AP.IsString text ->
                let text = Regex.Replace(text, @"<[^>]*>", Environment.NewLine)
                let text = Regex.Replace(text, @"\s+", " ")
                text |> validate
            | _ -> Ok page)
