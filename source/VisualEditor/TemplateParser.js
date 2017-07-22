function newSplit(ln, lnCharPos, charPos ){
    return {
        startLineNumber : ln,
        startLineCharPosition : lnCharPos,
        startCharPosition : charPos,
        content : ""
    }
}

function newToken(split, type){
    return {
        startLineNumber : split.startLineNumber,
        startLineCharPosition : split.startLineCharPosition,
        startCharPosition : split.startCharPosition,
        endCharPosition : split.startCharPosition,
        type : type,
        name: "",
        tag : ""
    }
}

function newEmptyToken(type, includeSubItem){
    var retVal = {
        startLineNumber : 0,
        startLineCharPosition : 0,
        startCharPosition : -1,
        endCharPosition : 0,
        type : type,
        name: "sample",
        tag : "sample"
    };
    if(includeSubItem){
        retVal.tag = "set";
        retVal.subItems = new Array();
    }
    return retVal;
}

function fromTemplateToTokens(templateText){
    var len = templateText.length;

    var splitArray = new Array();
    var lineNumber = 1;
    var charIndex = 1;
    var split = newSplit(1, 1, 1);

    for(var i=0; i < len; i++){
        if (templateText.charAt(i) == '<')
        {
            if (i + 1 < len)
            {
                if (templateText.charAt(i + 1) == '%')
                {
                    if (split.content.length > 0)
                    {
                        splitArray.push(split);
                    }

                    split = newSplit(lineNumber, charIndex, i);
                    split.content = "<%";
                    i++;
                    charIndex++;
                    charIndex++;
                    continue;
                }
            }
        }
        else if (templateText.charAt(i) == '%')
        {
            if (i + 1 < len)
            {
                if (templateText.charAt(i + 1) == '>')
                {
                    if (split.content.length > 0)
                    {
                        splitArray.push(split);
                    }
                    split.content += "%>";
                    split = newSplit(lineNumber, charIndex, i);
                    i++;
                    charIndex++;
                    charIndex++;
                    continue;
                }
            }
        }

        split.content += templateText.charAt(i);
        charIndex++;

        if (templateText.charAt(i) == '\n')
        {
            lineNumber++;
            charIndex = 0;
        }
    }

    if (split.content.length > 0)
    {
        splitArray.push(split);
    }

    var tokenArray = new Array();
    convertSplitsToToken(0, tokenArray, splitArray, 0);
    return tokenArray;
}

function convertSplitsToToken(i, tokenArray, splitArray, level){

    for(; i < splitArray.length; i++){
        var split = splitArray[i];
        var regularVal = split.content;
        var lowerVal = regularVal.toLowerCase();
        if(regularVal == "") continue;

        if(lowerVal.indexOf("<%=") == 0){
            var tempStr = regularVal.replace("<%=", "").replace("%>", "");

            var token = newToken(split, "token");
            setTagAndName(tempStr, token);
            tokenArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous_new_line") == 0){
            var token = newToken(split, "token");
            token.tag = "remove_previous_new_line";
            tokenArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous ") == 0){
            var tempStr = regularVal.replace("<%", "").replace("%>", "");
            var token = newToken(split, "token");
            setTagAndName(tempStr, token);
            tokenArray.push(token);
        }
        else if(lowerVal.indexOf("<%end") == 0 || lowerVal.indexOf("<%else") == 0){
            return i;
        }
        else if(lowerVal.indexOf("<%if") == 0){
            var tempStr = regularVal.replace("<%", "").replace(" THEN%>", "");
            var token = newToken(split, "block");
            token.subItems = new Array();
            setTagAndName(tempStr, token);
            tokenArray.push(token);
            i = convertSplitsToToken(i+1, token.subItems, splitArray, level+1);

            while(i < splitArray.length && splitArray[i].content.toLowerCase().indexOf("<%else") == 0){
                split = splitArray[i];
                regularVal = split.content;
                lowerVal = regularVal.toLowerCase();
                tempStr = regularVal.replace("<%", "").replace(" THEN%>", "").replace("%>", "");

                var token = newToken(split, "block");
                token.subItems = new Array();
                setTagAndName(tempStr, token);
                tokenArray.push(token);
                i = convertSplitsToToken(i+1, token.subItems, splitArray, level+1);
            }
        }
        else if(lowerVal.indexOf("<%") == 0 ){
            var tempStr = regularVal.replace("<%", "").replace("%>", "");
            var token = newToken(split, "block");
            token.subItems = new Array();
            setTagAndName(tempStr, token);
            tokenArray.push(token);
            i = convertSplitsToToken(i+1, token.subItems, splitArray, level+1);
        }
        else{
            var token = newToken(split, "content");
            token.name = regularVal;
            tokenArray.push(token);
        }
    }

    return i;
}

function setTagAndName(tokenString, token){
    var split = tokenString.trim().split(" ");
    token.tag = split[0].toLowerCase();
    if(split.length > 1){
        split = split.splice(1);
        token.name = split.join(" ");
    }
}


//REUSEFOREACH
var templateString;
function fromTokenToTemplateMain(tokenArray){
    templateString = "";
    fromTokenToTemplate(tokenArray);
    return templateString;
}

function fromTokenToTemplate(tokenArray){
    if(tokenArray == undefined) return;
    for(var i =0; i < tokenArray.length; i++){
        var token = tokenArray[i];
        if(token.type == "content" && token.name != "") {
            token.startCharPosition = templateString.length;
            templateString += token.name;
            token.endCharPosition = templateString.length;
            continue;
        }else if (token.type == "token"){
            token.startCharPosition = templateString.length;
            if(token.tag == "remove_previous_new_line"){
                templateString += "<%REMOVE_PREVIOUS_NEW_LINE%>"
            }else if(token.tag == "remove_previous"){
                templateString += "<%REMOVE_PREVIOUS " + token.name + " %>";
            }else{
                var tempStr = token.tag + " " + token.name + " ";
                templateString += "<%="+ tempStr.trim() + "%>";
            }
            token.endCharPosition = templateString.length;
            continue;
        }

        if(token.tag == "if"){
            token.startCharPosition = templateString.length;
            templateString += "<%IF " + token.name + " THEN%>";
            fromTokenToTemplate(token.subItems);
            if(i+1 < tokenArray.length){
                //end of elseif block
                if(tokenArray[i+1].tag.indexOf("else") != 0){
                    templateString += "<%ENDIF%>";
                }
            }else{
                templateString += "<%ENDIF%>";
            }
            token.endCharPosition = templateString.length;
            continue;
        }else if(token.tag.indexOf("elseif") == 0){
            token.startCharPosition = templateString.length;
            templateString += "<%ELSEIF " + token.name + " THEN%>";
            fromTokenToTemplate(token.subItems);
            if(i+1 < tokenArray.length){
                //end of elseif block
                if(tokenArray[i+1].tag.indexOf("else") != 0){
                    templateString += "<%ENDIF%>";
                }
            }else{
                templateString += "<%ENDIF%>";
            }
            token.endCharPosition = templateString.length;
            continue;
        }else if(token.tag.indexOf("else") == 0){
            token.startCharPosition = templateString.length;
            templateString += "<%ELSE%>";
            fromTokenToTemplate(token.subItems);
            templateString += "<%ENDIF%>";
            token.endCharPosition = templateString.length;
            continue;
        }

        var tokenNameString = token.tag.toUpperCase() + " " + token.name;
        token.startCharPosition = templateString.length;
        templateString += "<%" + tokenNameString.trim() + "%>";
        fromTokenToTemplate(token.subItems);
        if(token.tag == 'foreach'){
            templateString += "<%ENDFOR%>";
            token.endCharPosition = templateString.length;
            continue;
        }
        templateString += "<%END" + token.tag.toUpperCase() + "%>";
        token.endCharPosition = templateString.length;
    }
}
