module internal EA.Embassies.Russian.Kdmid.Web.Html

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Parser
open EA.Embassies.Russian.Kdmid.Domain

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
