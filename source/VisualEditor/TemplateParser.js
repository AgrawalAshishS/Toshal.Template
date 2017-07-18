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
        type : type,
        name: "",
        tag : ""
    }
}

function fromTemplateToTokens(templateText){
    var len = templateText.length;

    var retList = new Array();
    var lineNumber = 1;
    var charIndex = 1;
    var split = newSplit(1, 1, 1);

	for(var i=0; i < len; i++){
        if (templateText.charAt(i) == '<')
        {
            if (i + 1 < len)
            {
                if (templateText[i + 1] == '%')
                {
                    if (split.content.length > 0)
                    {
                        retList.push(split);
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
                if (templateText[i + 1] == '>')
                {
                    if (split.content.length > 0)
                    {
                        retList.push(split);
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
        retList.push(split);
    }

    var returnArray = new Array();
    processArray(0, returnArray, retList, 0);
    return returnArray;
}

function processArray(i, currentArray, finalArray, level){

    for(; i < finalArray.length; i++){
        var split = finalArray[i];
        var regularVal = split.content;
        var lowerVal = regularVal.toLowerCase();
        if(regularVal == "") continue;

        if(lowerVal.indexOf("<%=") == 0){
            var tempStr = regularVal.replace("<%=", "").replace("%>", "");
            
            var token = newToken(split, "token");
            setTagAndName(tempStr, token);
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous_new_line") == 0){
            var token = newToken(split, "token");
            token.tag = "remove_previous_new_line";
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous ") == 0){
            var tempStr = regularVal.replace("<%", "").replace("%>", "");
            var token = newToken(split, "token");
            setTagAndName(tempStr, token);
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%end") == 0 || lowerVal.indexOf("<%else") == 0){
            return i;
        }
        else if(lowerVal.indexOf("<%if") == 0){
            var tempStr = regularVal.replace("<%", "").replace(" THEN%>", "");
            var token = newToken(split, "block");
            token.subItems = new Array();
            setTagAndName(tempStr, token);
            currentArray.push(token);
            i = processArray(i+1, token.subItems, finalArray, level+1);
            
            while(i < finalArray.length && finalArray[i].content.toLowerCase().indexOf("<%else") == 0){
                split = finalArray[i];
                regularVal = split.content;
                lowerVal = regularVal.toLowerCase();
                tempStr = regularVal.replace("<%", "").replace(" THEN%>", "");
                
                var token = newToken(split, "block");
                token.subItems = new Array();
                setTagAndName(tempStr, token);
                currentArray.push(token);
                i = processArray(i+1, token.subItems, finalArray, level+1);
            }
        }
        else if(lowerVal.indexOf("<%") == 0 ){
            var tempStr = regularVal.replace("<%", "").replace("%>", "");
            var token = newToken(split, "block");
            token.subItems = new Array();
            setTagAndName(tempStr, token);
            currentArray.push(token);
            i = processArray(i+1, token.subItems, finalArray, level+1);
        }
        else{
            var token = newToken(split, "content");
            token.name = regularVal;
            currentArray.push(token);
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

function fromTokenToTemplate(tokenArray){
    var retVal = "";
    for(var i =0; i < tokenArray.length; i++){
        var token = tokenArray[i];
        if(token.type == "content" && token.name != "") {
            retVal += token.name;
            continue;
        }else if (token.type == "token"){
            if(token.tag == "remove_previous_new_line"){
                retVal += "<%REMOVE_PREVIOUS_NEW_LINE%>"
            }else if(token.tag == "remove_previous"){
                retVal += "<%REMOVE_PREVIOUS " + token.name + " %>";
            }else{
                var tempStr = token.tag + " " + token.name + " ";
                retVal += "<%="+ tempStr.trim() + "%>";
            }
            continue;
        }
        
        if(token.tag == "if"){
            retVal += "<%IF " + token.name + " THEN%>";
            retVal += fromTokenToTemplate(token.subItems);
            if(i+1 < tokenArray.length){
                //end of elseif block
                if(tokenArray[i+1].tag.indexOf("else") != 0){
                    retVal += "<%ENDIF%>";
                }
            }else{
                retVal += "<%ENDIF%>";
            }
            continue;
        }else if(token.tag.indexOf("elseif") == 0){
            retVal += "<%ELSEIF " + token.name + " THEN%>";
            retVal += fromTokenToTemplate(token.subItems);
            if(i+1 < tokenArray.length){
                //end of elseif block
                if(tokenArray[i+1].tag.indexOf("else") != 0){
                    retVal += "<%ENDIF%>";
                }
            }else{
                retVal += "<%ENDIF%>";
            }
            continue;
        }else if(token.tag.indexOf("else") == 0){
            retVal += "<%ELSE%>";
            retVal += fromTokenToTemplate(token.subItems);
            retVal += "<%ENDIF%>";
            continue;
        }
        
        var tokenNameString = token.tag.toUpperCase() + " " + token.name;
        retVal += "<%" + tokenNameString.trim() + "%>";
        retVal += fromTokenToTemplate(token.subItems);
        if(token.tag == 'foreach'){
            retVal += "<%ENDFOR%>";
            continue;
        }
        retVal += "<%END" + token.tag.toUpperCase() + "%>";
    }
    
    return retVal;
}