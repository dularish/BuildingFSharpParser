﻿module MathematicalExpressionParser
open System
open ParserBuildingBlocks
open System.Collections.Generic

let mutable variables = dict [
                "variableA", "1";
                "variableB", "2";
                "variableC", "variableB * variableB"
                ]

let masterVariables = dict [
                "pi", Math.PI.ToString();
                "e", Math.E.ToString()]

type BinaryOperator =
    | Plus
    | Minus
    | Divide
    | Multiply

type UnaryOperator = 
    | Exp
    | Sin
    | Cos
    | Tan
    | ASin
    | ACos
    | ATan
    | Sinh
    | Cosh
    | Tanh
    | ASinh
    | ACosh
    | ATanh
    | Log
    | Ln
    | Floor
    | Ceil
    | Sqrt
    | Abs

type Operator =
    | BinaryOperator of BinaryOperator
    | UnaryOperator of UnaryOperator

type Brackets =
    | BracketOpen
    | BracketClose

type Expression = 
    | Constant of double
    | BinaryExpression of Expression*BinaryOperator*Expression
    | UnaryExpression of UnaryOperator * Expression

type Token = 
    | DoubleConstant of double
    | BinaryOperator of BinaryOperator
    | UnaryOperator of UnaryOperator
    | Bracket of Brackets
    | Variable of string
    | MasterKeywordVariable of string

let doubleToUnion input =
    Token.DoubleConstant input

let parseDouble =
    (many1 parseDigit) .>>. (opt ((pChar '.') .>>. (many1 parseDigit) ))
    |>> (fun (wholeNums, decimalPart) ->
        match decimalPart with
        | Some (decimalPoint, decimalPartDigits) ->
            String(List.toArray(wholeNums @ [decimalPoint] @ decimalPartDigits))
            |> double |> doubleToUnion
        | None ->
            String(List.toArray(wholeNums)) |> double |> doubleToUnion)

let arithmeticOps = 
        ['+';'-';'/';'*']

let unaryOps =
    ["exp";"sin";"cos";"tan";"acos";"asin";"atan";"sinh";"cosh";"tanh";"asinh";"acosh";"atanh";"log";"ln";"floor";"ceil";"sqrt";"abs"]

let arithmeticCharToUnion input =
    if input = '+' then (BinaryOperator (Plus))
    elif input = '-' then (BinaryOperator (Minus))
    elif input = '*' then (BinaryOperator (Multiply))
    else (BinaryOperator (Divide))

let unaryStrToUnion input =
    if input = "exp" then (UnaryOperator (Exp))
    elif input = "sin" then (UnaryOperator (Sin))
    elif input = "cos" then (UnaryOperator (Cos))
    elif input = "tan" then (UnaryOperator (Tan))
    elif input = "asin" then (UnaryOperator (ASin))
    elif input = "acos" then (UnaryOperator (ACos))
    elif input = "atan" then (UnaryOperator (ATan))
    elif input = "sinh" then (UnaryOperator (Sinh))
    elif input = "cosh" then (UnaryOperator (Cosh))
    elif input = "tanh" then (UnaryOperator (Tanh))
    elif input = "asinh" then (UnaryOperator (ASinh))
    elif input = "acosh" then (UnaryOperator (ACosh))
    elif input = "atanh" then (UnaryOperator (ATanh))
    elif input = "log" then (UnaryOperator (Log))
    elif input = "ln" then (UnaryOperator (Ln))
    elif input = "floor" then (UnaryOperator (Floor))
    elif input = "ceil" then (UnaryOperator (Ceil))
    elif input = "sqrt" then (UnaryOperator (Sqrt))
    else (UnaryOperator (Abs))

let parseArithmeticOp =
    arithmeticOps
    |> anyOf
    |>> arithmeticCharToUnion

let parseUnaryOp =
    unaryOps
    |> Seq.sortByDescending (fun x -> x.Length)
    |> Seq.map (fun x -> pString x)
    |> List.ofSeq
    |> choice
    |>> unaryStrToUnion

let brackets = 
    ['(';')']

let parseBrackets inputString= 
    let result = run (brackets |> anyOf) inputString
    match result with
    | Success ('(', remainingText1) ->
        Success (BracketOpen, remainingText1)
    | Success (')', remainingText2) ->
        Success (BracketClose, remainingText2)
    | Success (_,_) ->
        Failure "some error"
    | Failure err -> Failure err


let parseCloseBracket = pChar ')' |>> fun(_) -> Token.Bracket BracketClose
let parseOpenBracket = pChar '(' |>> fun(_) -> Token.Bracket BracketOpen

let masterVariableNameToToken inputString =
    MasterKeywordVariable (inputString)

let parseMasterVariable =
    masterVariables.Keys
    |> Seq.sortByDescending (fun x -> x.Length)
    |> Seq.map (fun x -> pString x)
    |> List.ofSeq
    |> choice
    |>> masterVariableNameToToken

let variableNameToToken inputString =
    Variable (inputString)

let parseVariable = fun() ->
    variables.Keys
    |> Seq.sortByDescending (fun x -> x.Length)
    |> Seq.map (fun x -> pString x)
    |> List.ofSeq
    |> choice
    |>> variableNameToToken

let parseSpaces = many (pChar ' ') 
let parseAToken = fun() ->
    if variables.Count > 0 then
        parseSpaces >>. (parseOpenBracket <|> parseCloseBracket <|> parseUnaryOp <|> parseArithmeticOp <|> parseDouble <|> (parseVariable()) <|> parseMasterVariable)
    else
        parseSpaces >>. (parseOpenBracket <|> parseCloseBracket <|> parseArithmeticOp <|> parseDouble <|> parseMasterVariable)

let parseTwoTokens =
    (parseAToken() .>>. parseAToken())

type EntryType = 
    | Token of Token
    | TokenList of EntryType list

let getPriority operator =
    if (operator = (BinaryOperator.Plus)) then 1
    elif (operator = (BinaryOperator.Minus)) then 1
    elif (operator = (BinaryOperator.Multiply)) then 2
    elif (operator = (BinaryOperator.Divide)) then 2
    else 0

type MathExpressionParsingFailureType =
    | InsufficientParanthesis of string
    | TooManyParanthesis of string
    | MissingOperator of string
    | EmptyParanthesis of string
    | TrailingNegativeSign of string
    | InvalidSymbolFollowedByNegativeSign of string
    | SequencingOfOperatorsNotAllowed of string
    | OperatorNotExpected of string
    | UnhandledInput of string
    | EmptyExpression of string
    | UnrecognizedInput of string
    | EmptyVariableExpression of string
    | VariableParsingFailed of string
    | VariableDoesNotExists of string
    | UnexpectedToken of string
    | InvalidExpressionTerm of string

type MathExpressionParserResult =
    | ExpressionParsingSuccess of Expression option
    | ExpressionParsingFailure of MathExpressionParsingFailureType

//This implementation is a Mathematical expression parser which parses by applying BODMAS
let rec tryParseMathExpression input (stackExp:Expression option) (stackOp: BinaryOperator option) (memoryOp: BinaryOperator option) (isOpened:bool) (refVariables:string list)=
    
    //Resuable function to handle the expression that is found, handling for different conditions of stackExp, stackOp, remaining expression
    let handleFoundExpression = fun(expression: Expression, remaining, variablesRefPassed) ->
        match stackExp with
        | None ->
            (tryParseMathExpression remaining (Some expression) (None) (memoryOp) (isOpened) variablesRefPassed)
        | Some someStackExp ->
            let expressionMatchedCaseResult = run (parseAToken()) remaining
            match expressionMatchedCaseResult with
            | Success (Token.BinaryOperator opMatched, remainingCase1) ->
                //Compare the priorities
                //If matched priority is higher then stackExp operator (tryParseMathExpression)
                //Otherwise tryParseMathExpression remainingCase1 (Expression of stackExp stackOp expressionMatched) (opMatched)

                match (stackOp) with
                | Some someStackOp ->
                    if (getPriority opMatched) > (getPriority someStackOp) then
                        let resultToEnd, remainingCase11, isResultOpened, resultVariablesRef = tryParseMathExpression remainingCase1 (Some expression) (Some opMatched) (stackOp) (isOpened) variablesRefPassed
                        match resultToEnd with
                        | ExpressionParsingSuccess parsingResult ->
                            match parsingResult with
                            |Some someResultToEnd ->
                                tryParseMathExpression remainingCase11 (Some (Expression.BinaryExpression (someStackExp, someStackOp, someResultToEnd))) (None) (memoryOp) (isResultOpened) resultVariablesRef //(if isOpened then not isClosed else isOpened)
                            | None ->
                                (ExpressionParsingFailure (OperatorNotExpected remaining), input, isResultOpened, resultVariablesRef) //Problem with evaluating remainder of expression but now the program doesn't know what to do with the some stackExp and some stackOp
                        | ExpressionParsingFailure failure ->
                            (ExpressionParsingFailure failure, input , isResultOpened, resultVariablesRef)
                    else
                        let constMatchHandlingWithoutMemoryOp = fun() ->
                            let resultToEnd, remainingCase12, isResultOpened, resultVariablesRef = tryParseMathExpression remainingCase1 (None) (None) (Some opMatched) (isOpened) variablesRefPassed
                            match resultToEnd with
                            | ExpressionParsingSuccess parsingResult ->
                                match parsingResult with
                                |Some someResultToEnd ->
                                    tryParseMathExpression remainingCase12 (Some(Expression.BinaryExpression (Expression.BinaryExpression (someStackExp, someStackOp, expression) , opMatched, someResultToEnd))) (None) (memoryOp) (isResultOpened) resultVariablesRef //(if isOpened then not isClosed else isOpened)
                                | None ->
                                    (ExpressionParsingFailure (OperatorNotExpected remaining), input, isResultOpened, resultVariablesRef) //Problem with evaluating remainder of expression but now the program doesn't know what to do with the some stackExp and some stackOp
                            | ExpressionParsingFailure failure ->
                                (ExpressionParsingFailure failure, input, isResultOpened, resultVariablesRef)
                        match memoryOp with
                        | Some someMemoryOp ->
                            if (getPriority opMatched > getPriority someMemoryOp) then
                                constMatchHandlingWithoutMemoryOp()
                            else
                                (ExpressionParsingSuccess (Some(BinaryExpression (someStackExp, someStackOp, expression))),remaining, isOpened, variablesRefPassed)
                        | None ->
                            constMatchHandlingWithoutMemoryOp()
                | None ->
                    (ExpressionParsingFailure (MissingOperator input), input, false, variablesRefPassed) //Unexpected code during proper input This is possible during negative number input//When an expression(double/unaryExp/Variable) is matched there should either have been (some stackExp and some stackOp) or (None stackExp and None stackOp)//The first either or case fails here
                    
            | Success (Token.Bracket BracketClose, remainingCase2) ->
                match stackOp with
                    | Some someStackOp ->
                        tryParseMathExpression remaining (Some (Expression.BinaryExpression (someStackExp, someStackOp, expression))) (None) (memoryOp) (isOpened) variablesRefPassed
                    | _ -> 
                        (ExpressionParsingFailure (MissingOperator input), input, isOpened, variablesRefPassed)//Empty expressions
            | Failure _ ->
                match stackOp with
                | Some someStackOp ->
                    (ExpressionParsingSuccess (Some (Expression.BinaryExpression (someStackExp, someStackOp, expression))), remaining, isOpened, variablesRefPassed) //This could be the end of expression
                | None ->
                    (ExpressionParsingFailure (MissingOperator input), input, isOpened, variablesRefPassed)//Just returning the stackExp, but the program doesn't know what to do with the the parsed constMatched
            | _ ->
                (ExpressionParsingFailure (UnhandledInput input), input, false, variablesRefPassed)//Invalid input, an expression(double/unaryExp/Variable) should be followed by either bracket close or an arithmetic operator

    let rec getTheNextTermAsExpression = fun(expressionString:string) ->
        let nextToken = run (parseAToken()) expressionString
        match nextToken with
        | Success (Token.Bracket BracketOpen, remainingAfterBracketOpen) ->
            let resultToEndOfBracket, finalRemaining, isResultOpened, resultVariablesRef = tryParseMathExpression remainingAfterBracketOpen (None) (None) (None) (true) []
            if isResultOpened then
                (ExpressionParsingFailure (InsufficientParanthesis expressionString), expressionString, isResultOpened , resultVariablesRef)
            else
                match resultToEndOfBracket with
                | ExpressionParsingSuccess parsedResult ->
                    match parsedResult with
                    | Some someResultToOtherEnd ->
                        (ExpressionParsingSuccess (parsedResult), finalRemaining, isResultOpened, resultVariablesRef)
                    | None ->
                        (ExpressionParsingFailure (EmptyExpression expressionString), finalRemaining, isResultOpened, resultVariablesRef)
                | ExpressionParsingFailure failure ->
                    (ExpressionParsingFailure failure, expressionString, isResultOpened, resultVariablesRef)
        | Success (Token.DoubleConstant doubleConstant, remainingAfterDoubleConstant) ->
            (ExpressionParsingSuccess (Some (Expression.Constant doubleConstant)), remainingAfterDoubleConstant, false, [])
        | Success (Token.Variable variableName, remainingAfterVariable) ->
            if variables.ContainsKey(variableName) then
                let parserResult, remainingString, isResultOpened, _ = tryParseMathExpression variables.[variableName] (None) (None) (None) (false) []
                match parserResult with
                | ExpressionParsingSuccess expOpt ->
                    match expOpt with
                    | Some expression ->
                        (ExpressionParsingSuccess (expOpt), remainingAfterVariable, false, [variableName])
                    | None ->
                        (ExpressionParsingFailure (EmptyVariableExpression (sprintf "Variable %s does not have an expression" variableName)), expressionString, isOpened, [])
                | ExpressionParsingFailure failure ->
                    (ExpressionParsingFailure (VariableParsingFailed (sprintf "Unable to parse the expression related to the variable %s" variableName)), expressionString, isOpened, [])
            else
                (ExpressionParsingFailure (VariableDoesNotExists (sprintf "Variable %s existed during parsing doesn't exists during evaluation" variableName)), expressionString, isOpened, refVariables)
        | Success (Token.MasterKeywordVariable masterKeywordVariable, remainingAfterMasterVariable) ->
            if masterVariables.ContainsKey(masterKeywordVariable) then
                let parserResult, remainingString, isResultOpened, _ = tryParseMathExpression masterVariables.[masterKeywordVariable] (None) (None) (None) (false) []
                match parserResult with
                | ExpressionParsingSuccess expOpt ->
                    match expOpt with
                    | Some expression ->
                        (ExpressionParsingSuccess (expOpt), remainingAfterMasterVariable, false, [])
                    | None ->
                        (ExpressionParsingFailure (EmptyVariableExpression (sprintf "Master Variable %s does not have an expression" masterKeywordVariable)), expressionString, isOpened, refVariables)
                | ExpressionParsingFailure failure ->
                    (ExpressionParsingFailure (VariableParsingFailed (sprintf "Unable to parse the expression related to the master variable %s" masterKeywordVariable)), expressionString, isOpened, refVariables)
            else
                (ExpressionParsingFailure (VariableDoesNotExists (sprintf "Master Variable %s existed during parsing doesn't exists during evaluation" masterKeywordVariable)), expressionString, isOpened, refVariables)
        | Success (Token.UnaryOperator unaryOp, remainingAfterUnaryOp) ->
            let expressionTermResult, remainingAfterExprTerm, isOpenedAfterExprTerm, refVariablesInUnaryExp = getTheNextTermAsExpression(remainingAfterUnaryOp)
            match expressionTermResult with
            | ExpressionParsingSuccess expOpt ->
                match expOpt with
                | Some expression ->
                    let expressionToReturn = UnaryExpression (unaryOp, expression)
                    (ExpressionParsingSuccess (Some expressionToReturn), remainingAfterExprTerm, false, refVariablesInUnaryExp)
                | None ->
                    (ExpressionParsingFailure (EmptyVariableExpression (sprintf "Unary operator %A does not have a valid expression" unaryOp)), expressionString, isOpened, refVariables)
            | ExpressionParsingFailure failure ->
                (ExpressionParsingFailure (VariableParsingFailed (sprintf "Unable to parse the expression related to the Unary operator %A" unaryOp)), expressionString, isOpened, refVariables)
        | _ ->
            (ExpressionParsingFailure (InvalidExpressionTerm (sprintf "Unable to find a valid expression term in the input string : %A" expressionString)), expressionString, isOpened, refVariables)

    let result = run (parseAToken()) input
    match result with
    | Success (Token.Bracket BracketClose, remaining) ->
        //Validate whether stackOp is None - Pending
        if isOpened then
            (ExpressionParsingSuccess stackExp,remaining, not isOpened, refVariables)//Setting isOpened to false because BracketClose is found now
        else
            (ExpressionParsingFailure (TooManyParanthesis input) , input, isOpened, refVariables)//Not changing the state of isOpened as it's no more possible to close
    | Success (Token.Bracket _, _)//This would definitely have to be OpenBracket
    | Success (Token.DoubleConstant _, _)
    | Success (Token.MasterKeywordVariable _, _)
    | Success (Token.Variable _, _)
    | Success (Token.UnaryOperator _, _) ->
        let (resultToEndOfBracket, finalRemaining, isResultOpened, resultVariablesRef) = getTheNextTermAsExpression(input)
        let totalVariablesRef = (refVariables @ resultVariablesRef) |> List.distinct
        match resultToEndOfBracket with
        | ExpressionParsingSuccess parsedResult ->
            match parsedResult with
            | Some someResultToOtherEnd ->
                handleFoundExpression(someResultToOtherEnd, finalRemaining, totalVariablesRef)
            | None ->
                (ExpressionParsingFailure (EmptyExpression input), finalRemaining, isOpened, totalVariablesRef)//This is not expected when user enters multiplication even before brackets
        | ExpressionParsingFailure failure ->
            (ExpressionParsingFailure failure, input, isOpened, totalVariablesRef)
    | Success (Token.BinaryOperator opMatched, remaining) ->
        let opMatchedHandlingWithoutMemory = fun() ->
            match opMatched with
            | Minus ->
                match stackExp with
                | None ->
                    match stackOp with
                    | None ->
                        let (exprTermResult, remainingAfterExpressionTerm, isResultOpened, resultVariablesRef) = getTheNextTermAsExpression(remaining)
                        let totalVariablesRef = (refVariables @ resultVariablesRef) |> List.distinct
                        match exprTermResult with
                        | ExpressionParsingSuccess parsedResult ->
                            match parsedResult with
                            | Some someResultToOtherEnd ->
                                handleFoundExpression(BinaryExpression ( Expression.Constant (-1.0), BinaryOperator.Multiply, someResultToOtherEnd), remainingAfterExpressionTerm, totalVariablesRef)
                            | None ->
                                (ExpressionParsingFailure (EmptyExpression remaining), input, isOpened, totalVariablesRef)//This is not expected when user enters multiplication even before brackets
                        | ExpressionParsingFailure failure ->
                            (ExpressionParsingFailure failure, remaining, isOpened, totalVariablesRef)
                    | _ ->
                        (ExpressionParsingFailure (SequencingOfOperatorsNotAllowed input), input, isOpened, refVariables) // When a operator is matched stackOp should have been empty
                | Some someStackExp ->
                    match stackOp with
                    | None ->
                        (tryParseMathExpression remaining (stackExp) (Some opMatched) (memoryOp) (isOpened) refVariables)
                    | Some _ ->
                        (ExpressionParsingFailure (SequencingOfOperatorsNotAllowed input), input, isOpened, refVariables) //When an operator is matched, when stackExp is non-empty, stackOp should have been empty
            | _ ->
                match stackOp with
                | None ->
                    (tryParseMathExpression remaining (stackExp) (Some opMatched) (memoryOp) (isOpened) refVariables)
                | Some _ ->
                    (ExpressionParsingFailure (SequencingOfOperatorsNotAllowed input), input, isOpened, refVariables) //When an operator is matched, when stackExp is non-empty, stackOp should have been empty

        match memoryOp with
        | Some someMemoryOp ->
            if getPriority opMatched <= getPriority someMemoryOp then
                match stackExp with
                | Some someStackExp ->
                    (ExpressionParsingSuccess stackExp, input, isOpened, refVariables)
                | None ->
                    (ExpressionParsingFailure (OperatorNotExpected input), input , isOpened, refVariables)
            else 
                opMatchedHandlingWithoutMemory()
        | None ->
            opMatchedHandlingWithoutMemory()
    | Failure (_) ->
        if isOpened then
            (ExpressionParsingFailure (InsufficientParanthesis input), input, isOpened, refVariables)//Possibly throw an error here
        else
            match input.Trim().Length with
            | 0 ->
                //Operator in stackOp but incomplete expression is handled somewhere else
                match stackExp with
                | Some someStackExp ->
                    (ExpressionParsingSuccess stackExp,input, isOpened, refVariables)//Every expression is designed to terminate from here or from any of the None lines
                | None ->
                    (ExpressionParsingFailure (EmptyParanthesis input), input, isOpened, refVariables)
            | _ ->
                (ExpressionParsingFailure (UnrecognizedInput input), input, isOpened, refVariables)

 
let listPatternMatching =
    match [1;2;3;4] with
    | first::second::someList::someList2 ->
        printfn "Multiple matching possible %A %A %A %A" first second someList someList2
        0
    | _ ->
        printfn "No, not possible" 
        0



type ExpressionEvaluationError =
    | UnexpectedToken of string
    | InvalidOperatorUse of string
    | UnRecognizedToken of string
    | DivideByZeroAttempted of string
    | UnBalancedParanthesis of string
    | ParsingError of MathExpressionParsingFailureType

type ExpressionEvaluationResult =
    | EvaluationSuccess of double
    | EvaluationFailure of ExpressionEvaluationError

let computeBinaryExpression operand1 operator operand2 =
    match operator with
    | Plus ->
        EvaluationSuccess (operand1 + operand2)
    | Minus ->
        EvaluationSuccess (operand1 - operand2)
    | Multiply ->
        EvaluationSuccess (operand1 * operand2)
    | Divide ->
        if operand2 = 0. then
            EvaluationFailure (DivideByZeroAttempted ("Expression tried to evaluate : " + operand1.ToString() + operator.ToString() + operand2.ToString()))
        else
            EvaluationSuccess (operand1 / operand2)

let computeUnaryExpression operator operand1 =
    match operator with
    | Exp ->
        EvaluationSuccess (Math.Pow(Math.E, operand1))
    | Sin ->
        EvaluationSuccess (Math.Sin(operand1))
    | Cos ->
        EvaluationSuccess (Math.Cos(operand1))
    | Tan ->
        EvaluationSuccess (Math.Tan(operand1))
    | Sinh ->
        EvaluationSuccess (Math.Sinh(operand1))
    | Cosh ->
        EvaluationSuccess (Math.Cosh(operand1))
    | Tanh ->
        EvaluationSuccess (Math.Tanh(operand1))
    | ASin ->
        EvaluationSuccess (1./Math.Sin(operand1))
    | ACos ->
        EvaluationSuccess (1./Math.Cos(operand1))
    | ATan ->
        EvaluationSuccess (1./Math.Tan(operand1))
    | ASinh ->
        EvaluationSuccess (1./Math.Sinh(operand1))
    | ACosh ->
        EvaluationSuccess (1./Math.Cosh(operand1))
    | ATanh ->
        EvaluationSuccess (1./Math.Tanh(operand1))
    | Log ->
        EvaluationSuccess (Math.Log10(operand1))
    | Ln ->
        EvaluationSuccess (Math.Log(operand1))
    | Floor ->
        EvaluationSuccess (Math.Floor(operand1))
    | Ceil ->
        EvaluationSuccess (Math.Ceiling(operand1))
    | Sqrt ->
        EvaluationSuccess (Math.Sqrt(operand1))
    | Abs ->
        EvaluationSuccess (Math.Abs(operand1))

let rec EvaluateExpression (exp: Expression option) =
    match exp with
    | Some someExp ->
        match someExp with
        | Constant doubleConstant ->
            EvaluationSuccess doubleConstant
        | BinaryExpression (exp1, operator, exp2) ->
            let exp1Result = EvaluateExpression (Some exp1)
            let exp2Result = EvaluateExpression (Some exp2)
            match (exp1Result, exp2Result) with
            | (EvaluationSuccess exp1Dbl, EvaluationSuccess exp2Dbl) ->
                computeBinaryExpression exp1Dbl operator exp2Dbl
            | (EvaluationFailure failure, _) ->
                EvaluationFailure failure
            | (_ , EvaluationFailure failure) ->
                EvaluationFailure failure
        | UnaryExpression (unaryOp, exp) ->
            let expResult = EvaluateExpression (Some exp)
            match expResult with
            | EvaluationSuccess expDbl ->
                computeUnaryExpression unaryOp expDbl
            | EvaluationFailure failure ->
                EvaluationFailure failure
    | None ->
        EvaluationFailure (UnRecognizedToken "Null expression - Not expected")

let parseAndEvaluateExpression (expressionString) (variablesDict:IDictionary<string,string>) =
    variables <- variablesDict
    let parsedExpression = tryParseMathExpression expressionString (None) (None) (None) (false) []
    parsedExpression
    |> fun(expResult, remainingString, _, variablesRef) ->
        match expResult with
        | ExpressionParsingSuccess exp ->
            ((EvaluateExpression exp), remainingString, Seq.ofList variablesRef)
        | ExpressionParsingFailure failure ->
            (EvaluationFailure (ParsingError failure) , remainingString, Seq.ofList variablesRef)

let examplesForMathematicalExpressionParser =

    let successTestCases = 
        Map.empty
            //Region begin - TestcasesCreated by me
            .Add("23", 23. |> double)
            .Add("23 + 42",65.)
            .Add("(23 + 42)",65.)
            .Add("(23 + (22 + 43))",88.)
            .Add("(23 + 22) + 53",98.)
            .Add("((23 + 22) + 53)",98.)
            .Add("(23 + 24) + (25 + 26)",98.)
            .Add("21 + 22 + 23 + 24 + 25 + 26",141.)
            .Add("1 + 2 * 3 + 5 + 6 * 8",60.)
            .Add("1 - 2 * 3",-5.)
            .Add("1*(-3)",-3.)
            .Add("(1 + 2 + 3 * 3 * (1 + 2))", 30.)
            //Region end - TestcasesCreated by me
            //Region begin - TestCases from https://lukaszwrobel.pl/blog/math-parser-part-4-tests/
            .Add("2 + 3", 5. |> double)
            .Add("2 * 3",6.)
            .Add("89", 89.0)
            .Add("   12        -  8   ", 4.)
            .Add("142        -9   ", 133.)
            .Add("72+  15", 87.)
            .Add(" 12*  4", 48.)
            .Add(" 50/10", 5.)
            .Add("2.5", 2.5)
            .Add("4*2.5 + 8.5+1.5 / 3.0", 19.)
            .Add("5.0005 + 0.0095", 5.01)
            .Add("67+2", 69.)
            .Add(" 2-7", -5.)
            .Add("5*7 ", 35.)
            .Add("8/4", 2.)
            .Add("2 -4 +6 -1 -1- 0 +8", 10.)
            .Add("1 -1   + 2   - 2   +  4 - 4 +    6", 6.)
            .Add(" 2*3 - 4*5 + 6/3 ", -12.)
            .Add("2*3*4/8 -   5/2*4 +  6 + 0/3   ", -1.)
            .Add("10/4", 2.5)
            .Add("5/3", 1.66)
            .Add("3 + 8/5 -1 -2*5", -6.4)
            .Add(" -5 + 2", -3.)
            .Add("(2)", 2.)
            .Add("(5 + 2*3 - 1 + 7 * 8)", 66.)
            .Add("(67 + 2 * 3 - 67 + 2/1 - 7)", 1.)
            .Add("(2) + (17*2-30) * (5)+2 - (8/2)*4", 8.)
            .Add("(((((5)))))", 5.)
            .Add("(( ((2)) + 4))*((5))", 30.)
            //End region - Testcases from https://lukaszwrobel.pl/blog/math-parser-part-4-tests/

    let errorCases = 
        Map.empty
            .Add("  6 + c", ExpressionEvaluationError.ParsingError (MathExpressionParsingFailureType.UnrecognizedInput "c should not be recognized"))
            .Add("  7 & 2", ExpressionEvaluationError.ParsingError (MathExpressionParsingFailureType.UnrecognizedInput "& should not be recognized"))
            .Add("  %", ExpressionEvaluationError.UnexpectedToken "")
            .Add(" 5 + + 6", ExpressionEvaluationError.InvalidOperatorUse "")
            .Add("5/0", ExpressionEvaluationError.DivideByZeroAttempted "")
            .Add(" 2 - 1 + 14/0 + 7", ExpressionEvaluationError.DivideByZeroAttempted "")
            .Add("(5*7/5) + (23) - 5 * (98-4)/(6*7-42)", ExpressionEvaluationError.DivideByZeroAttempted "")
            .Add("2 + (5 * 2", ExpressionEvaluationError.UnBalancedParanthesis "")
            .Add("(((((4))))", ExpressionEvaluationError.UnBalancedParanthesis "")
            .Add("((2)) * ((3", ExpressionEvaluationError.UnBalancedParanthesis "")
            .Add("((9)) * ((1)", ExpressionEvaluationError.UnBalancedParanthesis "")

    let variableTestCases =
        ["variableA + variableB"; "variableC + variableA"]

    let developerDeliberatedNotHandledCases =
        [
        "2 + 2(4)"; //Not supported by existing Simpla expression parser
        "(1 + 2)(1 + 4)"; //Not supported by existing Simpla expression parser
        "asin 0"; //Existing Simpla expression parser returns -Infinity
        "2*-2"//Existing simpla expression parser supports this//If this has to be supported, then the expression "2*--2" would also be supported
        ]

    //let listOfExpressions = [exp1;exp2;exp3;exp4;exp5;exp6;exp7;exp8;exp9;exp10;exp11]
    //let listOfExpressions = ["(1 + 2 + 3 * 3 * (1 + 2))"]
    //let listOfExpressions = ["21 + 22 + 23 + 24 + 25 + 26"]
    //let listOfExpressions = [" 5 + + 6"]
            
    listPatternMatching |> ignore
    //listOfExpressions |> List.iter printResult
    let mutable countOfFailed = 0
    let mutable countOfSuccess = 0
    let printFailureCase (key:string) (expectedValue: double) =
        let evaluatedResult = parseAndEvaluateExpression key variables
        match evaluatedResult with
        | EvaluationFailure someString, _, variablesRef ->
            printfn "\nFailure to evaluate %s" key
            printfn "Evaluation Failure message : %A" someString
            countOfFailed <- countOfFailed + 1
        | EvaluationSuccess evaluatedValue , remaining, variablesRef ->
            if remaining.Trim().Length > 0 then
                printfn "The following expression evaluated successfully but some text remaining"
                printfn "Expression : %A" key
                printfn "Remaining String : %A" remaining
                printfn "Variables referenced : %A" variablesRef

            if evaluatedValue <> expectedValue then
                printfn "The following expression does not evaluate to the expected value :"
                printfn "Expression : %A" key
                printfn "Expected value : %A" expectedValue
                printfn "Obtained value : %A" evaluatedValue
                countOfFailed <- countOfFailed + 1

    let printAllCases (key:string) (expectedValue: double) =
        let evaluatedResult = parseAndEvaluateExpression key variables
        match evaluatedResult with
        | EvaluationFailure someString, _, variablesRef ->
            printfn "\nFailure to evaluate %s" key
            printfn "Evaluation Failure message : %A" someString
            countOfFailed <- countOfFailed + 1
        | EvaluationSuccess evaluatedValue , remaining , variablesRef->
            if remaining.Trim().Length > 0 then
                printfn "The following expression evaluated successfully but some text remaining"
                printfn "Expression : %A" key
                printfn "Remaining String : %A" remaining
                printfn "Variables Referenced : %A" variablesRef
            else
                printfn "Expression : %A" key
                printfn "Expected value : %A" expectedValue
                printfn "Obtained value : %A" evaluatedValue
                printfn "Variables Referenced : %A" variablesRef
                printfn "Result : %A" (if expectedValue = evaluatedValue then "Success" else "Failure")
                if expectedValue = evaluatedValue then
                    countOfSuccess <- countOfSuccess + 1
                else
                    countOfFailed <- countOfFailed + 1
                    

    let printResult (key:string) =
        let evaluatedResult = parseAndEvaluateExpression key variables
        match evaluatedResult with
        | EvaluationFailure someString, _, variablesRef ->
            printfn "\nFailure to evaluate %s" key
            printfn "Evaluation Failure message : %A" someString
        | EvaluationSuccess evaluatedValue , remaining, variablesRef ->
            if remaining.Trim().Length > 0 then
                printfn "The following expression evaluated successfully but some text remaining"
                printfn "Expression : %A" key
                printfn "Remaining String : %A" remaining
                printfn "Variables referenced : %A" variablesRef
            else
                printfn "Expression : %A" key
                printfn "Obtained value : %A" evaluatedValue
                printfn "Variables referenced : %A" variablesRef


    printfn "Success Test Cases check : "
    successTestCases |> Map.iter printAllCases
    printfn "\n\nNumber of failed cases : %A " countOfFailed
    printfn "Number of success cases : %A " countOfSuccess

    printfn "Failure test cases error messages check :"
    errorCases |> Map.toSeq |> Seq.map fst |> Seq.iter printResult

    printfn "\n\nVariables test cases : "
    variableTestCases |> List.iter printResult

    printfn "\n\nDeveloper deliberately didn't handle this, cases :"
    developerDeliberatedNotHandledCases |> List.iter printResult
    
