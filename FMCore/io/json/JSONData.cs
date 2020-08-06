using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using GdsFMJson;

namespace GdsFMJson{

    public class JSONData
    {
        public static string WriteJSON(JSONDataItem jsonDataItem){
            return jsonDataItem.ToJSONString();
        }

        public static JSONDataItem ReadJSON(string jsonString){
            JSONTokeniser tokeniser = new JSONTokeniser(jsonString);
            
            JSONDataItem data = GetJSONDataItem(tokeniser);
                
            if (data == null){
                return new JSONDataError("Unknown JSON error.", tokeniser.GetCurrentSectionString());
            } else if (data.dataType == JSONDataType.JSON_ERROR) {
                Debug.Print (data.ToString());
            } else if (data.dataType != JSONDataType.JSON_OBJECT && data.dataType != JSONDataType.JSON_ARRAY) {
                return new JSONDataError("JSON Document malformed. Root node is not an object or an array", tokeniser.GetCurrentSectionString());
            }

            return data;
        }


        public static JSONDataItem CreateJSONDataItem(float value){
            return new JSONFloat(value);
        }

        public static JSONDataItem CreateJSONDataItem(int value){
            return new JSONInteger(value);
        }

        public static JSONDataItem CreateJSONDataItem(string value){
            return new JSONString(value);
        }
        
        public static JSONDataItem CreateJSONDataItem(bool value){
            return new JSONBool(value);
        }

        public static JSONDataItem GetJSONDataItem(JSONTokeniser tokeniser){
            JSONToken token = tokeniser.NextToken();
            //Print token
            switch (token.tokenType){
                case JSONToken.TOKEN_OPEN_CURLY:
                    return GetJSONObject(tokeniser);
                case JSONToken.TOKEN_OPEN_SQUARE:
                    return GetJSONArray(tokeniser);

                //Boxing
                case JSONToken.TOKEN_STRING:
                    return new JSONString((string)(token.value), false);
                case JSONToken.TOKEN_FLOAT:
                    return new JSONFloat((float)token.value);
                case JSONToken.TOKEN_UNPARSED_FLOAT:
                    return new JSONFloat(token.value.ToString());
                case JSONToken.TOKEN_INTEGER:
                    return new JSONInteger((int)token.value);
                case JSONToken.TOKEN_TRUE:
                    return new JSONBool(true);
                case JSONToken.TOKEN_FALSE:
                    return new JSONBool(false);
                case JSONToken.TOKEN_NULL:
                    return new JSONnull();
                default:
                    return new JSONNonData(token);
            }
        }

        static JSONDataItem GetJSONObject(JSONTokeniser tokeniser){
            JSONObject jsonObject = new JSONObject();
            JSONDataItem data1;
            JSONDataItem data2;
            
            //Check if this is an empty definition
            data1 = JSONData.GetJSONDataItem(tokeniser);
            if (data1.dataType == JSONDataType.JSON_NON_DATA && ((JSONNonData)data1).value.tokenType == JSONToken.TOKEN_CLOSE_CURLY){
                //End of object
                return jsonObject;
            }

            while (true){
                if (data1.dataType != JSONDataType.JSON_STRING){
                    return new JSONDataError("Expected item name, got " + data1, tokeniser.GetCurrentSectionString());
                }
                
                data2 = JSONData.GetJSONDataItem(tokeniser);
                
                if (data2.dataType != JSONDataType.JSON_NON_DATA){
                    return new JSONDataError("Expected ':', got " + data2, tokeniser.GetCurrentSectionString());
                } else {
                    var d2 = (JSONNonData) data2;
                    if (d2.value.tokenType != JSONToken.TOKEN_COLON){
                        return new JSONDataError("Expected ':', got " + d2.value, tokeniser.GetCurrentSectionString());
                    }
                }
                
                data2 = JSONData.GetJSONDataItem(tokeniser);
                
                if (data2.dataType == JSONDataType.JSON_ERROR){
                    return data2;
                } else if (data2.dataType == JSONDataType.JSON_NON_DATA){
                    return new JSONDataError("Expected item value, got " + ((JSONNonData)data2).value, tokeniser.GetCurrentSectionString());
                }
                
                jsonObject.AddItem(((JSONString)data1).value,data2);
                data2 = JSONData.GetJSONDataItem(tokeniser);
                
                if (data2.dataType != JSONDataType.JSON_NON_DATA){
                    return new JSONDataError("Expected ',' or '}', got " + data2, tokeniser.GetCurrentSectionString());
                } else {
                    if (((JSONNonData)data2).value.tokenType == JSONToken.TOKEN_CLOSE_CURLY){
                        break; //End of Object
                    } else if (((JSONNonData)data2).value.tokenType != JSONToken.TOKEN_COMMA){
                        return new JSONDataError("Expected ',' or '}', got " + ((JSONNonData)data2).value, tokeniser.GetCurrentSectionString());
                    }
                }
                data1 = JSONData.GetJSONDataItem(tokeniser);
            }

            return jsonObject;
        }
        
        static JSONDataItem GetJSONArray(JSONTokeniser tokeniser){
            JSONArray jsonArray = new JSONArray();;
            JSONDataItem data;
            
            // Check for empty array
            data = JSONData.GetJSONDataItem(tokeniser);
            if ((data.dataType == JSONDataType.JSON_NON_DATA) && ((JSONNonData)data).value.tokenType == JSONToken.TOKEN_CLOSE_SQUARE){
                return jsonArray;
            }
            
            while(true){
                if (data.dataType == JSONDataType.JSON_NON_DATA){
                    return new JSONDataError("Expected data value, got " + data, tokeniser.GetCurrentSectionString());
                } else if (data.dataType == JSONDataType.JSON_ERROR){
                    return data;
                }
                jsonArray.AddItem(data);
                
                data = JSONData.GetJSONDataItem(tokeniser);
                
                if (data.dataType == JSONDataType.JSON_NON_DATA){
                    JSONToken token = ((JSONNonData)data).value;
                    if (token.tokenType == JSONToken.TOKEN_COMMA){
                        data = JSONData.GetJSONDataItem(tokeniser);
                        continue;
                    } else if (token.tokenType == JSONToken.TOKEN_CLOSE_SQUARE){
                        break; //End of Array
                    } else {
                        return new JSONDataError("Expected ',' or '], got " + token, tokeniser.GetCurrentSectionString());
                    }
                } else {
                    return new JSONDataError("Expected ',' or '], got " + data, tokeniser.GetCurrentSectionString());
                }
            }

            return jsonArray;
        }	

        public static string EscapeJSON( string input ){
            int ch;
            StringBuilder retString = new StringBuilder(input.Length);
            int lastSlice = 0;

            for (int i=0; i < input.Length; i++){
                ch = input[i];
                if ( (ch > 127) || (ch < 32) || (ch == 92) || (ch == 34) || (ch == 47)) {
                    retString.Append(input.Slice(lastSlice,i));
                    if (ch == 34){ //quote
                        retString.Append("\\q");
                    } else if (ch == 10){ //newline
                        retString.Append("\\n");
                    } else if (ch == 13){ //return
                        retString.Append("\\r");
                    } else if (ch == 92){ //back slash
                        retString.Append("\\\\");
                    } else if (ch == 47){ //forward slash
                        retString.Append("\\/");
                    } else if (ch > 127){ //unicode
                        retString.Append("\\u");
                        // retString.Append(IntToHexString(ch));
                    } else if (ch == 8){ //backspace
                        retString.Append("\\b");
                    } else if (ch == 12){ //linefeed
                        retString.Append("\\f");
                    } else if (ch == 9){ //tab
                        retString.Append("\\t");
                    }
                    lastSlice = i+1;
                }
            }
            
            // retString.Append(input[lastSlice..]);
            retString.Append(input.Substring(lastSlice));
            string s = retString.ToString();

            return s;
        }

        static string IntToHexString( int input ){
            string[] retString = new String[4];
            int index = 3;
            int nibble;
            while (input > 0){
                nibble = input & 0xF;
                if (nibble < 10){
                    retString[index] = ((char)(48+nibble)).ToString();
                } else {
                    retString[index] = ((char)(55+nibble)).ToString();
                }
                index -=1;
                input >>= 4;
            }

            while (index >= 0){
                retString[index] = "0";
                index -= 1;
            }
            return String.Concat(retString);
        }

        public static string UnEscapeJSON(string input){
            int escIndex = input.IndexOf("\\");
            
            if (escIndex == -1)  return input;
            
            int copyStartIndex = 0;
            StringBuilder retString = new StringBuilder(input.Length);
            
            while (escIndex != -1){
                retString.Append( input.Slice(copyStartIndex,escIndex) );
                switch ((int)input[escIndex+1]){
                    case 110: //n - newline
                        retString.Append( "\n" );
                        break;
                    case 34: //quote
                        retString.Append( "\"" );
                        break;
                    case 116: //tab
                        retString.Append( "\t" );
                        break;
                    case 92: // backslash
                        retString.Append( "\\" );
                        break;
                    case 47: ///
                        retString.Append( "/" );
                        break;
                    case 114: //r carriage return
                        retString.Append( "\r" );			
                        break;
                    case 102: //f form feed
                        retString.Append( "\f" );
                        break;
                    case 98: //b backspace
                        retString.Append( "\b" );	
                        break;
                    case 117: //u unicode
                        retString.Append( UnEscapeUnicode(input.Slice(escIndex+2,escIndex+6))	);
                        escIndex += 4;
                        break;
                }
                copyStartIndex = escIndex+2;
                escIndex = input.IndexOf("\\",copyStartIndex);
            }

            // if (copyStartIndex < input.Length)     retString.Append( input[copyStartIndex..] );
            if (copyStartIndex < input.Length)     retString.Append( input.Substring(copyStartIndex) );
            

            return retString.ToString();
        }
        
        static int HexCharToInt(int character){
            if (character >= 48 && character <= 57){ //0-9//
                return character-48;
            } else if (character >= 65 && character <= 70){ //A-F//
                return character - 55;
            } else if (character >= 97 && character <= 102){ //a-f//
                return character - 87;
            }
            return 0;
        }

        static string UnEscapeUnicode(string hexString){
            int charCode = 0;
            for (int i=0; i < 4; i++){
                charCode <<= 4;
                charCode += HexCharToInt(hexString[i]);
            }
            return ((char)(charCode)).ToString();
        }
    }


    public class JSONDataType{
        //TODO: Change to typesafe enum pattern? Performance issues, maybe?//
        public const int JSON_ERROR = -1;
        public const int JSON_OBJECT = 1;
        public const int JSON_ARRAY = 2;
        public const int JSON_FLOAT = 3;
        public const int JSON_INTEGER = 4;
        public const int JSON_STRING = 5;
        public const int JSON_BOOL = 6;
        public const int JSON_NULL = 7;
        public const int JSON_OBJECT_MEMBER = 8;
        public const int JSON_NON_DATA = 9;
    }

    public abstract class JSONDataItem{

        public int dataType = JSONDataType.JSON_NULL;

        public int ToInt(){
            Debug.Print ("Unsupported conversion to Int for " + this.ToString());
            return -1;
        }

        public float ToFloat(){
            Debug.Print ("Unsupported conversion to Float for " + this.ToString());
            return -1.0f;
        }

        public bool ToBool(){
            Debug.Print ("Unsupported conversion to Bool for " + this.ToString());
            return false;
        }

        //Method ToPrettyString() Abstract
        public override abstract string ToString();
        public string ToJSONString(){ 
            return ToString();
        }
    }

    class JSONDataError : JSONDataItem{
        string value;
        
        public JSONDataError (string errorDescription, string location){
            dataType = JSONDataType.JSON_ERROR;
            value = errorDescription + "\nJSON Location: " + location;
        }

        public override string ToString(){
            return value;
        }
    }

    public class JSONNonData : JSONDataItem {
        public JSONToken value;
        
        public JSONNonData(JSONToken token) {
            dataType = JSONDataType.JSON_NON_DATA;
            value = token;
        }

        public override string ToString(){
            return "Non Data";
        }
    }

    public class JSONFloat : JSONDataItem {
        float value;
        string unparsedStr;
        bool unparsed = false;
        
        public JSONFloat(float value){
            dataType = JSONDataType.JSON_FLOAT ;
            this.value = value;
        }

        //This constructor creates a float container that stores the unparsed
        //value string. This is to spread the load of parsing the data
        //as parsing floats is very expensive on Android.
        public JSONFloat(string unparsedStr){
            dataType = JSONDataType.JSON_FLOAT;
            this.unparsedStr = unparsedStr;
            this.unparsed = true;
        }
        
        public void Parse(){
            if (unparsed){
                value = Convert.ToSingle(unparsedStr);
                unparsed = false;
            }
        }
        
        public new int ToInt(){
            Parse();
            return (int)(value);
        }

        public new float ToFloat(){
            Parse();
            return value;
        }

        public override string ToString(){
            Parse();
            return Convert.ToString(value);
        }
    }

    class JSONInteger : JSONDataItem {
        int value;
        
        public JSONInteger(int value) {
            dataType = JSONDataType.JSON_INTEGER;
            this.value = value;
        }

        public new int ToInt(){
            return value;
        }

        public new float ToFloat(){
            return (float)(value);
        }

        public override string ToString(){
            return Convert.ToString(value);
        }
    }

    class JSONString : JSONDataItem {        
        public string value;
        string jsonReady = "";
        
        
        public JSONString(string value, bool isMonkeyString = true) {
            dataType = JSONDataType.JSON_STRING;
            if (!isMonkeyString){
                this.value = JSONData.UnEscapeJSON(value);
                jsonReady = "\"" + value + "\"";
            } else {
                this.value = value;
            }
        }

        public new string ToJSONString(){
            if (jsonReady == ""){
                jsonReady = "\"" + JSONData.EscapeJSON(value) + "\"";
            }
            return jsonReady;
        }

        public override string ToString(){
            return value;
        }

    }

    class JSONBool : JSONDataItem {
        bool value;
            
        public JSONBool(bool value){
            dataType = JSONDataType.JSON_BOOL;
            this.value = value;
        }

        public new bool ToBool(){
            return value;
        }
        
        public override string ToString(){
            if (value){
                return "true";
            } else {
                return "false";
            }
        }

        public new string ToJSONString(){
            if (value){
                return "true";
            } else {
                return "false";
            }
        }

    }

    class JSONnull : JSONDataItem{
        // object value = null; //Necessary?
        
        public override string ToString(){
            dataType = JSONDataType.JSON_NULL;
            return "NULL";
        }
    }

    public class JSONArray : JSONDataItem, IEnumerable<JSONDataItem>{
        List<JSONDataItem> values = new List<JSONDataItem>();
        
        public JSONArray(){
            dataType = JSONDataType.JSON_ARRAY;
        }

        public void AddPrim( bool value ){
            values.Add(JSONData.CreateJSONDataItem(value));
        }
        
        public void AddPrim( int value ){
            values.Add(JSONData.CreateJSONDataItem(value));
        }
        
        public void AddPrim( float value ){
            values.Add(JSONData.CreateJSONDataItem(value));
        }
        
        public void AddPrim( string value ){
            values.Add(JSONData.CreateJSONDataItem(value));
        }
        
        public void AddItem( JSONDataItem dataItem ){
            values.Add(dataItem);
        }
        
        public void RemoveItem( JSONDataItem dataItem ){
            values.RemoveAll(x => x==dataItem);
            
        }
        
        public new string ToJSONString(){
            StringBuilder retString = new StringBuilder(values.Count*2+5);
            bool first = true;
            retString.Append("[");
            foreach (var v in values){
                if (first){
                    first = false;
                } else {
                    retString.Append(",");
                }
                retString.Append(v.ToJSONString());
            }
            
            retString.Append("]");
            
            return retString.ToString();
        }
        
        public override string ToString(){
            StringBuilder retString = new StringBuilder(values.Count*2+5);
            bool first = true;
            
            retString.Append("[");
            
            foreach (var v in values){
                if (first){
                    first = false;
                } else {
                    retString.Append(",");
                }
                retString.Append(v.ToString());
            }
            
            retString.Append("]");
            
            return retString.ToString();
        }

        List<JSONDataItem>.Enumerator Enumerator(){
            return (List<JSONDataItem>.Enumerator)(values.GetEnumerator());
        }
        
        public IEnumerator<JSONDataItem> GetEnumerator(){
            // foreach(JSONDataItem item in values)
            // {yield return item;}
            return values.GetEnumerator();
        }

        
        //Indexer
        public JSONDataItem this[int i]
        {
            get{return values[i];}
            set{values[i] = value;}
        }

        public int Length {get => values.Count; }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()  {return GetEnumerator();}

        void Clear(){
            values.Clear();
        }
    }

    class JSONObjectMember : JSONDataItem{
        string name;
        JSONDataItem dataItem;

        JSONObjectMember(string name, JSONDataItem dataItem){
            dataType = JSONDataType.JSON_OBJECT_MEMBER;
            this.name = name;
            this.dataItem = dataItem;
        }

        new bool ToBool(){
            return dataItem.ToBool();
        }
        
        new int ToInt(){
            return dataItem.ToInt();
        }
        
        new float ToFloat(){
            return dataItem.ToFloat();
        }
        
        public override string ToString(){
            return dataItem.ToString();
        }

        public new string ToJSONString(){
            return dataItem.ToJSONString();
        }
    }

    public class JSONObject : JSONDataItem, IEnumerable<KeyValuePair<String, JSONDataItem>>{
        Dictionary<String, JSONDataItem> values = new Dictionary<String, JSONDataItem>();
        
        public JSONObject(){
            dataType = JSONDataType.JSON_OBJECT;
        }

        public void AddPrim( string name, bool value ){
            values[name] = JSONData.CreateJSONDataItem(value);
        }
        
        public void AddPrim( string name, int value ){
            values[name] = JSONData.CreateJSONDataItem(value);
        }

        public void AddPrim( string name, double value){
            //TODO:  FIXME!  Stupid little hack for now to squish down to float.  Pretty sure JSON is double-precision with numbers....
            values[name] = JSONData.CreateJSONDataItem((float) value);
        }

        public void AddPrim( string name, float value ){
            values[name] = JSONData.CreateJSONDataItem(value);
        }
        
        public void AddPrim( string name, string value ){
            values[name] = JSONData.CreateJSONDataItem(value);
        }
        
        public void AddItem( string name, JSONDataItem dataItem ){
            values[name] = dataItem;
        }
        
        public void RemoveItem( string name ){
            values.Remove(name);
        }
        
        public JSONDataItem GetItem( string name ){
            if (values.ContainsKey(name)) return values[name];  else throw new KeyNotFoundException("The key '" + name + "' was not found.");
        }
        
        public string GetItem( string name, string defaultValue ){
            JSONDataItem item = values.ContainsKey(name)? values[name] : null;
            if (item != null){
                return item.ToString();
            }
            return defaultValue;
        }
        
        public int GetItem( string name, int defaultValue ){
            JSONDataItem item = values.ContainsKey(name)? values[name] : null;
            if (item != null){
                return item.ToInt();
            }
            return defaultValue;
        }
        
        public float GetItem( string name, float defaultValue ){
            JSONDataItem item = values.ContainsKey(name)? values[name] : null;
            if (item != null){
                return item.ToFloat();
            }
            return defaultValue;
        }
        
        public bool GetItem( string name, bool defaultValue ){
            JSONDataItem item = values.ContainsKey(name)? values[name] : null;
            if (item != null){
                return item.ToBool();
            }
            return defaultValue;
        }
        
        
        public new string ToJSONString(){
            StringBuilder retString = new StringBuilder(values.Count*5+5);
            bool first = true;
            
            retString.Append("{");
            
            foreach (var v in values){
                if (first){
                    first = false;
                } else {
                    retString.Append(",");
                }
                retString.Append("\"");
                retString.Append(JSONData.EscapeJSON(v.Key));
                retString.Append("\":");
                retString.Append(v.Value.ToJSONString());
            }
            retString.Append("}");
            return retString.ToString();
        }

        public override string ToString(){
            StringBuilder retString = new StringBuilder(values.Count*5+5);
            bool first = true;
            
            retString.Append("{");
            
            foreach (var v in values){
                if (first){
                    first = false;
                } else {
                    retString.Append(",");
                }
                retString.Append("\"");
                retString.Append(v.Key);
                retString.Append("\":");
                retString.Append(v.Value);
            }
            retString.Append("}");
            return retString.ToString();
        }

        public void Clear(){
            values.Clear();
        }
        
        public Dictionary<String,JSONDataItem>.KeyCollection Names(){
            return values.Keys;
        }
        
        public Dictionary<String,JSONDataItem>.ValueCollection Items(){
            return values.Values;
        }

        // public JSONObjectEnumerator ObjectEnumerator(){
        //     return new JSONObjectEnumerator(NodeEnumerator<String,JSONDataItem>(values.GetEnumerator()));
        // }

        public IEnumerator<KeyValuePair<String, JSONDataItem>> GetEnumerator(){
            // foreach(JSONDataItem item in values)
            // {yield return item;}
            return values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()  {return GetEnumerator();}


    }

    // class JSONObjectEnumerator{
    //     NodeEnumerator<String,JSONDataItem> enumerator;
    //     JSONObjectEnumerator( NodeEnumerator<String,JSONDataItem> enumerator){
    //         this.enumerator = enumerator;
    //     }

    //     bool HasNext(){
    //         return this.enumerator.HasNext();
    //     }
        
    //     JSONObject NextObjectMember(){
    //         map.Node<String,JSONDataItem> node = enumerator.NextObject();
    //         return new JSONObjectMember(node.Key, node.Value);
    //     }
    // }


}