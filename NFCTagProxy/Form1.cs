using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NFCTagProxy
{
    public partial class Form1 : Form
    {
        [DllImport("OrangeEasyAPI.dll", EntryPoint = "GetCardID", CharSet = CharSet.Ansi)]
        private extern static int GetCardID(ref Byte id, ref IntPtr length);

        private SimpleHttpd httpd;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                httpd = new SimpleHttpd(3501);
                httpd.ExternalMethod += new SimpleHttpd.ExternaMethodHandler(httpd_ExternalMethod);
                httpd.ExternalAccessEnable = true;
                httpd.StartHttpd();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lunch Faild.");
                this.Dispose();

            }
        }

        private void httpd_ExternalMethod(object sender, SimpleHttpd.ExternalMethodArgs e)
        {
            try
            {

                SimpleJson response = new SimpleJson();

                byte[] byCardID = new byte[10];
                IntPtr length = new IntPtr();
                String strID = "";

                // ICを読取ります
                if (GetCardID(ref byCardID[0], ref length) == 0)
                {
                    // 読取り成功時にICのIDを1バイトごと読み出します
                    for (int i = 0; i < length.ToInt32(); ++i)
                    {
                        strID += byCardID[i].ToString("X2");
                    }
                    response.elements["error"] = "false";
                }
                else
                {
                    // 読取り失敗時にメッセージを表示します
                    response.elements["error"] = "true";
                    strID = "read error";
                }
                // 結果表示します
                response.elements["IDm"] = strID;
                e.response = response.CreateJsonP("readTag");
            }
            catch (Exception ex)
            {
                label1.Text = "Read Error";
            }

        }

    }
}
