/**************************************
 * 
 * Simple Embeded HTTP Server 0.01
 * 
 * .NET Framework向け組み込み用簡易HTTPサーバ
 * (C) Kyosuke INOUE / kyoro 2009
 *  
 *************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;


class SimpleHttpd
{
    //デフォルト設定
    private const string    DEFAULT_SERVER_NAME     = "Simple Embeded HTTP Server (C) Kyosuke INOUE 2009";  
    private const int       DEFAULT_PORT            = 3535;
    private const string    DEFAULT_PREFIX          = "*";
    private const string    DEFAULT_METHOD          = "method";
    private const Boolean   DEFAULT_ACCESS          = false;
    private const Boolean   DEFAULT_EXTERNAL_ACCESS = false;
    private const string    DEFAULT_DOCUMENT_ROOT   = "document_root";
    private const string    DEFAULT_TEMPLATE        = "template";
    private const string    DEFAULT_SPLITTER        = "\\";
    private const string    DEFAULT_404_PAGE        = "404.html";
    private const string    DEFAULT_403_PAGE        = "403.html";
    private const string    DAFAULT_500_MESSAGE     = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\"><html><head><title>500 Internal Server Error.</title></head><body><h1>Internal Server Error</h1><p>Sorry!</p><hr><address>$SERVER_NAME</address></body></html>";

    /// <summary>
    /// パス区切り文字
    /// </summary>
    private string pathSplitter = DEFAULT_SPLITTER;

    /// <summary>
    /// HTTPリスナ
    /// </summary>
    private HttpListener listener = new HttpListener();

    /// <summary>
    /// 稼動ステータス
    /// </summary>
    private Boolean isRunning = false;

    /// <summary>
    /// httpd待ち受けスレッド
    /// </summary>
    private Thread httpd;

    /// <summary>
    /// 待ち受けポート番号
    /// </summary>
    public int PortNumber = DEFAULT_PORT;

    /// <summary>
    /// ドキュメントルートのパス
    /// </summary>
    public string DocumentRoot = DEFAULT_DOCUMENT_ROOT;

    /// <summary>
    /// テンプレートディレクトリ
    /// </summary>
    public string TemplateDirectory = DEFAULT_TEMPLATE;

    /// <summary>
    /// ホスト名のプレフィックス
    /// </summary>
    public string HostnamePrefix = DEFAULT_PREFIX;

    /// <summary>
    /// ファイルアクセス許可
    /// </summary>
    public Boolean FileAccessEnable = DEFAULT_ACCESS;

    /// <summary>
    /// 外部メソッドの実行を許可
    /// </summary>
    public Boolean ExternalAccessEnable = DEFAULT_EXTERNAL_ACCESS;

    /// <summary>
    /// メソッドのURL名前空間
    /// </summary>
    public string MethodNamespace = DEFAULT_METHOD;

    /// <summary>
    /// サーバ名
    /// </summary>
    public string ServerName = DEFAULT_SERVER_NAME;
    
    /// <summary>
    /// サーバ内部エラー時のメッセージ
    /// </summary>
    public string InternalServerErrorMessage = DAFAULT_500_MESSAGE;
    
    /// <summary>
    /// ログの書き出しを許可
    /// </summary>
    public Boolean LogWriteEnable = false;
    
    /// <summary>
    /// 拡張メソッド引数
    /// </summary>
    public class ExternalMethodArgs : EventArgs
    {

        /// <summary>
        /// 外部メソッドに渡されたクエリ
        /// </summary>
        public string[] query;

        /// <summary>
        /// レスポンステキスト
        /// </summary>
        public string response;
    }

    /// <summary>
    /// 拡張メソッドデリゲート
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ExternaMethodHandler(object sender, ExternalMethodArgs e);

    /// <summary>
    /// 拡張メソッド
    /// </summary>
    public event ExternaMethodHandler ExternalMethod;
    protected virtual string OnExternalMethod(ExternalMethodArgs e)
    {

        if (ExternalMethod != null)
        {
            ExternalMethod(this, e);
        }

        return e.response;
    }

    /// <summary>
    /// OS依存のパス区切り文字を取得する
    /// </summary>
    /// <returns>パス区切り文字</returns>
    public string getPathSplitter()
    {
        return Path.DirectorySeparatorChar.ToString();
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SimpleHttpd()
    {

        //設定初期化
        initDefaultSettings();
    }
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="port">待ち受けポート番号</param>
    public SimpleHttpd(int port)
    {
        //設定初期化
        initDefaultSettings();
        PortNumber = port;
    }

    /// <summary>
    /// 設定の初期化
    /// </summary>
    private void initDefaultSettings()
    {
        //パス区切り文字の設定
        pathSplitter = getPathSplitter();

        //初期化
        string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        DocumentRoot = baseDirectory + DEFAULT_DOCUMENT_ROOT + pathSplitter;
        TemplateDirectory = baseDirectory + DEFAULT_TEMPLATE + pathSplitter;
        PortNumber = DEFAULT_PORT;

    }

    /// <summary>
    /// サーバの終了
    /// </summary>
    /// <returns></returns>
    public Boolean StopHttpd()
    {
        if (isRunning)
        {
            listener.Stop();
            httpd.Abort();
            httpd.Join();
            httpd = null;
            isRunning = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// サーバの起動
    /// </summary>
    /// <returns></returns>
    public Boolean StartHttpd()
    {
        //稼動状況を確認
        if (isRunning)
        {
            return false;
        }

        isRunning = true;

        string prefix;
        if (PortNumber == 80)
        {
            prefix = "http://" + HostnamePrefix + "/";
        }
        else
        {
            prefix = "http://" + HostnamePrefix + ":" + PortNumber.ToString() + "/";

        }

        try
        {
            //プレフィックスの登録
            listener.Prefixes.Add(prefix);
            //リスナ起動
            listener.Start();
            //待ち受け開始
            httpd = new Thread(HttpdWorker);
            httpd.IsBackground = true;
            httpd.Start();
        }
        catch
        {
            isRunning = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// httpd待機スレッド
    /// </summary>
    private void HttpdWorker()
    {
        while (isRunning)
        {
            HttpListenerContext context = listener.GetContext();
            Thread request = new Thread(RequestWorker);
            request.IsBackground = true;
            request.Start(context);
        }
    }

    /// <summary>
    /// リクエスト処理スレッド
    /// </summary>
    /// <param name="context"></param>
    private void RequestWorker(object param)
    {
        HttpListenerContext httpContext = (HttpListenerContext)param;
        HttpListenerRequest httpRequest = httpContext.Request;
        HttpListenerResponse httpResponse = httpContext.Response;

        //生リクエストを表示
        LogWrite("[request] " + httpRequest.RawUrl);

        string[] query = httpRequest.RawUrl.Split('/');

        string externalResponseText = null;
        //メソッド呼び出しの場合はイベントを発生
        if (query[1] == MethodNamespace && query.Length > 2 && ExternalAccessEnable)
        {
            ExternalMethodArgs e = new ExternalMethodArgs();
            string[] methodQuery = new string[query.Length - 2];
            for (int i = 2; i < query.Length; i++)
            {
                methodQuery[i - 2] = query[i];
            }
            e.query = methodQuery;
            e.response = null;
            externalResponseText = OnExternalMethod(e);
        }

        //拡張メソッドの戻り値があればレスポンスを返す
        if (externalResponseText != null)
        {
            httpResponse.StatusCode = 200;
            byte[] httpResponseBytes = StringToBytes(externalResponseText);
            httpResponse.OutputStream.Write(httpResponseBytes, 0, httpResponseBytes.Length);
        }
        else
        {
            //拡張メソッド外応答
            string path = DocumentRoot + httpRequest.RawUrl.Replace("/", pathSplitter);
            if (FileAccessEnable)
            {
                //ファイルが無ければ404
                if (File.Exists(path))
                {
                    httpResponse.StatusCode = 200;
                }
                else
                {
                    httpResponse.StatusCode = 404;
                    path = TemplateDirectory + DEFAULT_404_PAGE;
                }
            }
            else
            {
                httpResponse.StatusCode = 403;
                path = TemplateDirectory + DEFAULT_403_PAGE;
            }

            //pathを確認し、ファイルが存在すればレスポンスを返す
            if (File.Exists(path))
            {
                byte[] content = File.ReadAllBytes(path);
                httpResponse.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                httpResponse.StatusCode = 500;
                byte[] httpResponseBytes = StringToBytes(InternalServerErrorMessage.Replace("$SERVER_NAME",ServerName) );
                httpResponse.OutputStream.Write(httpResponseBytes, 0, httpResponseBytes.Length);
            }
        }
        try{
            httpResponse.Close();
        }catch{
        }
    }

    /// <summary>
    /// 文字列をバイト列に変換
    /// </summary>
    /// <param name="originalTesxt">文字列</param>
    /// <returns></returns>
    private byte[] StringToBytes(string originalTesxt)
    {
        Encoding unicode = Encoding.UTF8;
        byte[] unicodeBytes = unicode.GetBytes(originalTesxt);
        return unicodeBytes;
    }
    
    private void LogWrite(string message)
    {
        if(LogWriteEnable){
            Console.WriteLine(message);
        }
    }
    
    

}
