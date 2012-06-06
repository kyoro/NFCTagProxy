/**************************************
 * 
 * Simple JSON Class
 * 
 * ハッシュテーブルをJSONに変換するクラス
 * (C) Kyosuke INOUE / kyoro 2009
 *  
 *************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

/// <summary>
/// JSON簡易生成クラス
/// </summary>
class SimpleJson
{
    /// <summary>
    /// JSON生成用ハッシュテーブル
    /// </summary>
    public Hashtable elements;

    /// <summary>
    /// JSON簡易生成クラス
    /// </summary>
    public SimpleJson()
    {
        elements = new Hashtable();
        elements.Clear();
    }
    /// <summary>
    /// JSON簡易生成クラス
    /// </summary>
    /// <param name="hash">JSON生成用ハッシュテーブル</param>
    public SimpleJson(Hashtable hash)
    {
        elements = hash;
    }

    /// <summary>
    /// JSON文字列を生成する
    /// </summary>
    /// <returns></returns>
    public string CreateJson()
    {
        string jsonText = "";

        foreach (string key in elements.Keys)
        {
            if(jsonText != ""){
                jsonText += ",";
            }
            jsonText += "\"" + key + "\":\"" + elements[key] + "\"";
        }
        jsonText = "{" + jsonText + "}";
        return jsonText;
    }
    /// <summary>
    /// JSONP文字列を生成する
    /// </summary>
    /// <param name="functionName">コールバック関数名</param>
    /// <returns></returns>
    public string CreateJsonP(string functionName)
    {
        return functionName + "(" + CreateJson() + ");";
    }

}

