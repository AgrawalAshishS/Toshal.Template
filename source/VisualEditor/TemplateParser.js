
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
        var lowerVal = finalArray[i].toLowerCase();

        if(lowerVal.indexOf("<%=") == 0){
            var tempStr = lowerVal.replace("<%=", "").replace("%>", "");
            var token = {};
            token.type = "token";
            setTagAndName(tempStr, token);
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%remove_previous_new_line") == 0){
            var token = {};
            token.tag = "remove_previous_new_line";
            token.type = "token";
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%REMOVE_PREVIOUS ") == 0){
            var tempStr = lowerVal.replace("<%", "").replace("%>", "");
            var token = {};
            token.type = "token";
            setTagAndName(tempStr, token);
            currentArray.push(token);
        }
        else if(lowerVal.indexOf("<%end") == 0 || lowerVal.indexOf("<%else") == 0){
            return i;
        }
        else if(lowerVal.indexOf("<%if") == 0){
            var tempStr = lowerVal.replace("<%", "").replace(" then%>", "");
            var token = {};
            token.type = "block";
            setTagAndName(tempStr, token);
            token.subItems = new Array();
            currentArray.push(token);
            i = processArray(i+1, token.subItems, finalArray, level+1);
            
            while(i < finalArray.length && finalArray[i].toLowerCase().indexOf("<%else") == 0){
                lowerVal = finalArray[i].toLowerCase();
                tempStr = lowerVal.replace("<%", "").replace(" then%>", "");
                
                var token = {};
                setTagAndName(tempStr, token);
                token.type = "block";
                token.subItems = new Array();
                currentArray.push(token);
                i = processArray(i+1, token.subItems, finalArray, level+1);
            }
        }
        else if(lowerVal.indexOf("<%") == 0 ){
            var tempStr = lowerVal.replace("<%", "").replace("%>", "");
            var token = {};
            setTagAndName(tempStr, token);
            token.type = "block";
            token.subItems = new Array();
            currentArray.push(token);
            i = processArray(i+1, token.subItems, finalArray, level+1);
        }
        else{
            var token = {};
            token.name = finalArray[i];
            token.type = "content";
            currentArray.push(token);
        }
    }

    return i;
}

function setTagAndName(tokenString, token){
    var split = tokenString.trim().split(" ");
    token.tag = split[0];
    if(split.length > 1){
        split = split.splice(1);
        token.name = split.join(" ");
    }
}