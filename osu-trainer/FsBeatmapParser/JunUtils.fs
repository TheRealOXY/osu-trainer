﻿module JunUtils

open System
open System.Text.RegularExpressions

let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
    | true, v    -> Some v
    | false, _   -> None

let isTypeOf (tryParseFunc: string -> bool * _) = tryParseFunc >> function
    | true, v  -> true
    | false, _ -> false

let parseInt    = tryParseWith System.Int32.TryParse
let parseSingle = tryParseWith System.Single.TryParse
let parseDouble = tryParseWith System.Double.TryParse
let parseDecimal = tryParseWith System.Decimal.TryParse
let parseBool   = tryParseWith System.Boolean.TryParse

// active patterns for converting strings to other data types
let (|Int|_|)    = parseInt
let (|Single|_|) = parseSingle
let (|Double|_|) = parseDouble
let (|Bool|_|)   = parseBool
let (|Decimal|_|)   = parseDecimal

let isInt    = isTypeOf System.Int32.TryParse
let isSingle = isTypeOf System.Single.TryParse
let isDouble = isTypeOf System.Double.TryParse
let isDecimal = isTypeOf System.Decimal.TryParse
let isBool   = isTypeOf System.Boolean.TryParse

let toBool str =
    match str with
    | "1" -> true
    | _ -> false

// other parsing functions
let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern)
    if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None

let removeOuterQuotes (s:string) =
    match s with
    | Regex "^\"(.+)\"$" [inquotes] -> inquotes
    | _ -> s

let split separator (s:string) =
    let values = ResizeArray<_>()
    let rec gather start i =
        let add () = s.Substring(start,i-start) |> values.Add
        if i = s.Length then add()
        elif s.[i] = '"' then inQuotes start (i+1) 
        elif s.[i] = separator then add(); gather (i+1) (i+1) 
        else gather start (i+1)
    and inQuotes start i =
        if s.[i] = '"' then gather start (i+1)
        else inQuotes start (i+1)
    gather 0 0

    values.ToArray()
    |> Array.toList
    |> List.map removeOuterQuotes

let parseCsv = split ','
let parseSpaceSeparatedList = split ' '

let tryParseCsvInt (str:string) : list<int> option = 
    let items = parseCsv str
    let validInts = List.fold (fun acc cur -> acc && (isInt cur)) true items
    match validInts with
    | true -> Some(List.map int items)
    | false -> None


// check if a list of strings match the expected types when casted
let rec typesMatch vals types : bool = 
    match types with
    | t::ts -> 
        match t with
        | "int" ->
            match vals with
            | v::vs ->
                if (isInt v)
                then (typesMatch vs ts) // keep going!!!
                else false
            | _ -> false // out of values?
        | "decimal" -> 
            match vals with
            | v::vs ->
                if (isDecimal v)
                then (typesMatch vs ts) // keep going!!!
                else false
            | _ -> false // out of values?
        | _ ->
            match vals with
            | v::vs -> (typesMatch vs ts) // keep going!!!
            | _ -> false // out of values?
    | [] -> true // reached end, all checks passed


// print functions
let parseError obj =
    printfn "Error parsing %A" obj
    None

// parse an entire section
let rec parseSectionUsing parserfn lines = 
    match lines with
    | head::tail ->
        match parserfn head with
        | Some(data) -> data :: parseSectionUsing parserfn tail
        | None         -> parseSectionUsing parserfn tail
    | [] -> []
