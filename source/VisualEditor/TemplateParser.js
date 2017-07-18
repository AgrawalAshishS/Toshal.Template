
function fromTemplateToTokens(srcString){
	var currentStr ="";
	var finalArray = new Array();
	var stringLength = srcString.length;

	for(var i=0; i < stringLength; i++){
		var currentChar = srcString.charAt(i);
		//if its last char no need to check;
		if(i+1 == stringLength){
			currentStr += currentChar;
			finalArray.push(currentStr);
			break;
		}

		//Token started;
		if(currentChar == "<" && srcString.charAt(i+1) == "%"){
            if(currentStr != "") finalArray.push(currentStr);
			currentStr = "";
			for(;i < stringLength; i++){
				currentChar = srcString.charAt(i);
				//String is ending;
				if(i+1 == stringLength){
					currentStr += currentChar;
					i++;
					currentStr += srcString.charAt(i);
					finalArray.push(currentStr);
					continue;
				}
				//tag ends;
				if(currentChar == "%" && srcString.charAt(i+1) == ">"){
					i++;
					currentStr += "%>";
					finalArray.push(currentStr);
					currentStr = "";
					break;
				}else{
					currentStr += currentChar;
				}
			}
		}else{
			currentStr += currentChar;
		}
	}

    var returnArray = new Array();
    processArray(0, returnArray, finalArray, 0);
    //console.log(finalArray);
    return returnArray;
}

function processArray(i, currentArray, finalArray, level){

    for(; i < finalArray.length; i++){
        var regularVal = finalArray[i];
        var lowerVal = regularVal.toLowerCase();
        if(regularVal == "") continue;

        if(lowerVal.indexOf("<%=") == 0){
            var tempStr = regularVal.replace("<%=", "").replace("%>", "");
            var token = {};
            token.type = "token";
            token.name = "";
            setTagAndName(tempStr, token);
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous_new_line") == 0){
            var token = {};
            token.tag = "remove_previous_new_line";
            token.type = "token";
            token.name = "";
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous ") == 0){
            var tempStr = regularVal.replace("<%", "").replace("%>", "");
            var token = {};
            token.type = "token";
            token.name = "";
            setTagAndName(tempStr, token);
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%end") == 0 || lowerVal.indexOf("<%else") == 0){
            return i;
        }
        else if(lowerVal.indexOf("<%if") == 0){
            var tempStr = regularVal.replace("<%", "").replace(" THEN%>", "");
            var token = {};
            token.type = "block";
            token.name = "";
            token.subItems = new Array();
            setTagAndName(tempStr, token);
            currentArray.push(token);
            i = processArray(i+1, token.subItems, finalArray, level+1);
            
            while(i < finalArray.length && finalArray[i].toLowerCase().indexOf("<%else") == 0){
                regularVal = finalArray[i];
                lowerVal = regularVal.toLowerCase();
                tempStr = regularVal.replace("<%", "").replace(" THEN%>", "");
                
                var token = {};
                token.type = "block";
                token.name = "";
                token.subItems = new Array();
                setTagAndName(tempStr, token);
                currentArray.push(token);
                i = processArray(i+1, token.subItems, finalArray, level+1);
            }
        }
        else if(lowerVal.indexOf("<%") == 0 ){
            var tempStr = regularVal.replace("<%", "").replace("%>", "");
            var token = {};
            token.type = "block";
            token.name = "";
            token.subItems = new Array();
            setTagAndName(tempStr, token);
            currentArray.push(token);
            i = processArray(i+1, token.subItems, finalArray, level+1);
        }
        else{
            var token = {};
            token.name = regularVal;
            token.type = "content";
            token.tag = "";
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