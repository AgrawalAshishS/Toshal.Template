var processor = {};
processor.process(tokenArray, contextName, contextObject, outputString){
    for(var i=0; i < tokenArray.length; i++){
        var token = tokenArray[i];
        
        if(token.type == "content"){
            outputString += token.name;
        }else if(token.type == "token"){
            if(token.tags == "remove_previous_new_line"){
                if(outputString[outputString.length -1] == "\n"){
                    outputString = outputString.slice(0, -1);
                }
            }else if(token.tags == "remove_previous"){
                var num = parseInt(token.name);
                if(num == NaN) continue;
                outputString = outputString.slice(0, num*-1);
            }else{
                var value = processor.tokenValueProvider(token.tag, token.name, contextName, contextObject);
                if(value == undefined || value == null || value == "") continue;
                outputString += value;
            }
        }else{
            if(token.tag = "foreach"){
                var value = processor.loopValueProvider(token.tag, token.name, contextName, contextObject);
                if(value == undefined || value == null || typeof(value) != 'Array') continue;
                for(var j=0; j < value.length; j++){
                    processor.process(token.)
                }
                outputString += value;
            }
        }
    }

    return outputString;
}