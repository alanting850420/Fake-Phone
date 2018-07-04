
using mshtml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Speech.Recognition;
using System.Globalization;
using System.Security.Principal;
using System.Configuration;

namespace BeanFunRegPop
{
    public partial class Form1 : Form
    {

        public System.Windows.Forms.Timer aTimer;

        public Boolean success = false;

        public String NewUrl = "";

        public String ASPCookie = "";
        
        public BeanfunClient BeanfunClient = new BeanfunClient();

        public String BFToken = "" , CallNumber ="" , VerifyWeb = "" , Path = "" , YourString = "";

        public Thread myThread, myThread1;

        public bool IsBeanfun = false;

        public FakeMyPhone FakeMyPhone = new FakeMyPhone();

        public int RunTime = 0 , ErrorTime = 0;

        public Configuration configuration;

        public RestClient c = new RestClient();

        public RestRequest r = new RestRequest();

        public IRestResponse d;

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref System.UInt32 pcchCookieData, int dwFlags, IntPtr lpReserved);
        private static string GetCookies(string url)
        {
            uint datasize = 1024;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x2000, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;

                cookieData = new StringBuilder((int)datasize);
                if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x00002000, IntPtr.Zero))
                    return null;
            }
            return cookieData.ToString();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (IsAdministrator() == true)
            {
                var appName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
                Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION", appName, 11000, Microsoft.Win32.RegistryValueKind.DWord);
                webBrowser1.ScriptErrorsSuppressed = true;
                Form1.CheckForIllegalCrossThreadCalls = false;
                if (GetSetting("Caller") != null) {
                    textBox1.Text = GetSetting("Caller");
                }
                if (GetSetting("Faker") != null)
                {
                    textBox3.Text = GetSetting("Faker");
                }
                if (GetSetting("Folder") != null)
                {
                    textBox2.Text = GetSetting("Folder");
                }
                if (GetSetting("Option") != null)
                {
                    comboBox1.SelectedIndex = Int32.Parse(GetSetting("Option"));
                }
                else {
                    comboBox1.SelectedIndex = 0;
                }
                if (GetSetting("Wait") != null)
                {
                    richTextBox1.Text = GetSetting("Wait");
                }
                if (GetSetting("Success") != null)
                {
                    richTextBox2.Text = GetSetting("Success");
                }
                if (GetSetting("Error") != null)
                {
                    richTextBox3.Text = GetSetting("Error");
                }
                if (GetSetting("login_email") != null)
                {
                    textBox4.Text = GetSetting("login_email");
                }
                if (GetSetting("login_pass") != null)
                {
                    textBox5.Text = GetSetting("login_pass");
                }
            }
            else
            {
                MessageBox.Show("請使用管理員身分開啟");
                Application.Exit();
            }
            
        }

        public static bool IsAdministrator()
        {
            //回傳True表示為管理員身分；反之則非管理員
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public SpeechRecognitionEngine engine;
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Length != 13) {
                MessageBox.Show("請輸入正確假電話。");
                return;
            }
            RunTime = 0;
            ErrorTime = 0;
            button1.Enabled = false;
            button2.Enabled = true;
            SetSetting("Caller", textBox1.Text);
            SetSetting("Faker", textBox3.Text);
            SetSetting("Folder", textBox2.Text);
            SetSetting("Option", comboBox1.SelectedIndex.ToString());
            SetSetting("Wait", richTextBox1.Text);
            SetSetting("Success", richTextBox2.Text);
            SetSetting("Error", richTextBox3.Text);
            SetSetting("login_email", textBox4.Text);
            SetSetting("login_pass", textBox5.Text);
            if (comboBox1.SelectedIndex == 0)
            {
                IsBeanfun = false;
            }
            else {
                IsBeanfun = true;
            }
            try
            {
                if (!IsBeanfun)
                {
                    FakeMyPhone = new FakeMyPhone();
                    string URL = "https://www.fakemyphone.com.tw/ajax.php?task=login";
                    NameValueCollection myParameters = new NameValueCollection();
                    myParameters.Add("ischecked", "");
                    myParameters.Add("login_email", textBox4.Text);
                    myParameters.Add("login_pass", textBox5.Text);
                    myParameters.Add("redirect", "/");
                    myParameters.Add("spam", "1530427562");
                    String response = Encoding.UTF8.GetString(FakeMyPhone.UploadValues(URL, myParameters));
                }
                myThread = new Thread(LoginToForm);
                myThread.IsBackground = true;
                myThread.Start();
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
            
        }

        public void LoginToForm() {
            URLTEXT.Text = "開始....";
            webBrowser1.DocumentCompleted += browser_DocumentCompleted;
            VerifyWeb = "";
            try
            {
                if (richTextBox1.Text != "")
                {
                    CallNumber = textBox3.Text;
                    using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                    {
                        writetext.WriteLine("0");
                    }
                    String[] lines = richTextBox1.Text.Split('\n');
                    String[] AcPw = lines[0].Split(new string[] { "\t" }, StringSplitOptions.None);
                    String Account = AcPw[0];
                    String Password = "";
                    String Phone = "";
                    if (AcPw.Length < 2)
                    {
                        Password = "ac0226ca";
                    }
                    else {
                        Password = AcPw[1];
                        Phone = AcPw[2];
                    }
                    URLTEXT.Text = Account + "取得Session....";
                    BeanfunClient = new BeanfunClient();
                    String sKey = GetSessionkey();
                    String aKey = RegularLogin(Account, Password, sKey);
                    URLTEXT.Text = Account + "登入中....";
                    NameValueCollection payload = new NameValueCollection();
                    payload.Add("SessionKey", sKey);
                    payload.Add("AuthKey", aKey);
                    Debug.WriteLine(sKey);
                    Debug.WriteLine(aKey);
                    Debug.WriteLine(BeanfunClient.ResponseHeaders);

                    BFToken = BeanfunClient.GetCookie("bfWebToken");
                    if (BeanfunClient.webtoken == "")
                    {
                        MessageBox.Show("NoWebToken");
                        myThread.Abort();
                    }
                    NewUrl = "https://tw.beanfun.com/beanfun_block/auth.aspx?channel=game_zone&page_and_query=game_start.aspx%3Fservice_code_and_region%3D610096_TE&web_token=" + BFToken;
                    webBrowser1.Navigate(NewUrl);
                    URLTEXT.Text = Account + "轉跳中....";
                    Thread.Sleep(1000);

                    ASPCookie = GetCookies(NewUrl).Split(new string[] { "ASP.NET_SessionId=" }, StringSplitOptions.None)[1].Split(';')[0]; ;


                    RunTime = RunTime + 1;
                    label9.Text = (ErrorTime).ToString() + "/" + RunTime.ToString();
                    if (!IsBeanfun)
                    {
                        String URL = "https://www.fakemyphone.com.tw/ajax.php?task=call";
                        NameValueCollection myParameters = new NameValueCollection();
                        //myParameters.Add("anonym", "");
                        myParameters.Add("checkcountry", "true");
                        myParameters.Add("from", CallNumber);
                        myParameters.Add("fromcountry", "886");
                        //myParameters.Add("ischecked", "");
                        myParameters.Add("lang", "zh");
                        myParameters.Add("my", textBox1.Text);
                        myParameters.Add("mycountry", "1");
                        //myParameters.Add("soundcatid", "");
                        myParameters.Add("soundloop", "true");
                        //myParameters.Add("spam", "");
                        //myParameters.Add("speak", "");
                        //myParameters.Add("speaklang", "");
                        myParameters.Add("speakloop", "false");
                        myParameters.Add("terms", "true");
                        myParameters.Add("to", "+886985646637");
                        myParameters.Add("tocountry", "886");
                        //myParameters.Add("to2", "");
                        myParameters.Add("to2country", "886");
                        //myParameters.Add("to3", "");
                        myParameters.Add("to3country", "886");
                        //myParameters.Add("to4", "");
                        myParameters.Add("to4country", "886");
                        myParameters.Add("usemyorweb", "my");
                        //myParameters.Add("voice", "");
                        try
                        {
                            URLTEXT.Text = Encoding.UTF8.GetString(FakeMyPhone.UploadValues(URL, myParameters));
                            if (URLTEXT.Text.Split(new string[] { "你没有足够的积分呼叫所有接收者" }, StringSplitOptions.None).Length > 1) {
                                URLTEXT.Text = "沒錢了........";
                                button1.Enabled = true;
                                button2.Enabled = false;
                                using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                                {
                                    writetext.WriteLine("3");
                                }
                                return;
                            }
                        }
                        catch (Exception Exc) {
                            URLTEXT.Text = "等待重新開始...撥電話" + Exc.Message;
                            Thread.Sleep(300000);
                            rsThread();
                            Thread.Sleep(1000);
                        }

                        webBrowser1.Navigate("https://tw.beanfun.com/TW/auth.aspx?channel=member&page_and_query=default.aspx%3Fservice_code%3D999999%26service_region%3DT0&web_token=" + BFToken);
                        Thread.Sleep(1000);
                        webBrowser1.Navigate("https://tw.beanfun.com/TW/locales/zh-TW/contents/member/verify_phone.aspx");
                        while (VerifyWeb == "")
                        {

                        }
                        String[] Veifys = VerifyWeb.Split(new string[] { " font-weight: bold;\">" }, StringSplitOptions.None);
                        String[] Veifysn = VerifyWeb.Split(new string[] { "var verify_sn = " }, StringSplitOptions.None);
                        String Verify = "";
                        String verify_sn = "";
                        if (Veifys.Length > 2 && Veifysn.Length > 1)
                        {
                            Verify = Veifys[2].Split('<')[0];
                            verify_sn = Veifysn[1].Split(';')[0];
                        }
                        else
                        {
                            Veifys = VerifyWeb.Split(new string[] { "id=\"ctl00_ContentPlaceHolder1_lblPassword\">" }, StringSplitOptions.None);
                            Verify = Veifys[1].Split('<')[0];
                            verify_sn = Veifysn[1].Split(';')[0];
                        }
                        using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\verify.txt"))
                        {
                            writetext.WriteLine(Verify);
                        }
                        using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                        {
                            writetext.WriteLine("1");
                        }
                        URLTEXT.Text = "驗證碼：" + Verify + " 撥號中...";

                        int Error = 0;
                        CheckStatus:
                        try
                        {
                            Error++;
                            string response = FakeMyPhone.DownloadString("https://www.fakemyphone.com.tw/fake-call/history");
                            String[] FakeNumber = response.Split(new string[] { "<td>" + CallNumber.Split('+')[1] }, StringSplitOptions.None);
                            while (FakeNumber.Length <= 1)
                            {
                                Thread.Sleep(500);
                                response = FakeMyPhone.DownloadString("https://www.fakemyphone.com.tw/fake-call/history");
                                FakeNumber = response.Split(new string[] { "<td>" + CallNumber.Split('+')[1] }, StringSplitOptions.None);
                            }
                            String BFNumber = FakeNumber[1].Split(new string[] { "</font><br>886985646637" }, StringSplitOptions.None)[0];
                            if (BFNumber.Split(new string[] { "green" }, StringSplitOptions.None).Length <= 1 && Error <= 130)
                            {
                                goto CheckStatus;
                            }
                            else
                            {
                                Error = 0;
                                URLTEXT.Text = "橘子已接電話....";
                                using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                                {
                                    writetext.WriteLine("2");
                                }
                            }
                        }
                        catch (Exception ex) {
                            URLTEXT.Text = "等待重新開始...讀電話" + ex.Message;
                            Thread.Sleep(300000);
                            rsThread();
                            Thread.Sleep(1000);
                        }
                        while (Error <= 15)
                        {
                            Thread.Sleep(2000);
                            c = new RestClient("https://tw.beanfun.com/TW/locales/zh-TW/contents/member/verify_phone_check.ashx?sn=" + verify_sn + "&step=296");
                            r = new RestRequest(Method.GET);
                            c.CookieContainer = new CookieContainer();
                            c.CookieContainer.Add(new Cookie("ASP.NET_SessionId", ASPCookie, "", "tw.beanfun.com"));
                            d = c.Execute(r);
                            String[] success = d.Content.Split(new string[] { "<result><code>1</code><desc>" }, StringSplitOptions.None);
                            if (success.Length > 1)
                            {
                                richTextBox2.AppendText(Account + "\t" + Password + "\t" + success[1].Split(new string[] { "</desc></result>" }, StringSplitOptions.None)[0] + "\n");
                                Path = @"" + textBox2.Text + "\\驗證成功.txt";
                                YourString = Account + "\t" + Password + "\t" + success[1].Split(new string[] { "</desc></result>" }, StringSplitOptions.None)[0] + Environment.NewLine;
                                File.AppendAllText(Path, YourString);
                                URLTEXT.Text = "驗證成功";
                                goto NewAc;
                            }
                            else
                            {
                                Error = Error + 1;
                            }
                        }
                        string ErroMSG = d.Content.Split(new string[] { "<desc>" }, StringSplitOptions.None)[1].Split(new string[] { "</desc>" }, StringSplitOptions.None)[0];
                        URLTEXT.Text = ErroMSG;
                        String Fake09Phone = CallNumber.Split(new string[] { "886" }, StringSplitOptions.None)[1];
                        richTextBox3.AppendText(Account + "\t" + Password + "\t0" + Fake09Phone + "\t" + ErroMSG + "\n");
                        Path = @"" + textBox2.Text + "\\驗證失敗.txt";
                        YourString = Account + "\t" + Password + Environment.NewLine;
                        File.AppendAllText(Path, YourString);
                        ErrorTime += 1;
                        label9.Text = (ErrorTime).ToString() + "/" + RunTime.ToString();
                        NewAc:
                        using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                        {
                            writetext.WriteLine("3");
                        }

                        int j = 0;
                        using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\驗證列表.txt"))
                        {
                            foreach (var line in lines)
                            {
                                if (j == 0)
                                {
                                    richTextBox1.Text = "";
                                }
                                else
                                {
                                    richTextBox1.Text += line;

                                    writetext.WriteLine(line);
                                    if (j != lines.Count() - 1)
                                    {
                                        richTextBox1.Text += '\n';
                                    }
                                }
                                j++;
                            }
                        }

                        richTextBox3.SelectionStart = richTextBox3.Text.Length;
                        richTextBox3.ScrollToCaret();
                        richTextBox2.SelectionStart = richTextBox2.Text.Length;
                        richTextBox2.ScrollToCaret();
                        textBox3.Text = "+886" + (Int64.Parse(CallNumber.Split(new string[] { "+886" }, StringSplitOptions.None)[1]) + 1).ToString();
                        
                        SetSetting("Wait", richTextBox1.Text);
                        SetSetting("Success", richTextBox2.Text);
                        SetSetting("Error", richTextBox3.Text);
                        SetSetting("Faker", textBox3.Text);

                        string[] state = System.IO.File.ReadAllLines(@"" + textBox2.Text + "\\state.txt");
                        while (state[0] != "0")
                        {
                            Thread.Sleep(500);
                            state = System.IO.File.ReadAllLines(@"" + textBox2.Text + "\\state.txt");
                        }

                        if (RunTime <= 10)
                        {
                            if (ErrorTime >= 3) {
                                using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                                {
                                    writetext.WriteLine("3");
                                }
                                URLTEXT.Text = "因10隻內3隻失敗，故停止。";
                                return;
                            }
                        }
                        else
                        {
                            if ((ErrorTime * 100 / RunTime) >= 30) {
                                using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                                {
                                    writetext.WriteLine("3");
                                }
                                URLTEXT.Text = "成功率低於3成，故停止。";
                                return;
                            }
                        }
                        URLTEXT.Text = "等待中...";
                        Thread.Sleep(30000);
                        LoginToForm();
                    }
                    else
                    {
                        if (success == false)
                        {
                            int sTime = 0;
                            for (var i = 1; i <= 5; i++)
                            {
                                var success = false;
                                while (success == false)
                                {
                                    URLTEXT.Text = Account + "-" + i.ToString() + "創建中....";
                                    var client = new RestClient("https://tw.beanfun.com/generic_handlers/gamezone.ashx");
                                    var request = new RestRequest(Method.POST);
                                    client.CookieContainer = new CookieContainer();
                                    client.CookieContainer.Add(new Cookie("ASP.NET_SessionId", ASPCookie, "", "tw.beanfun.com"));
                                    request.AddParameter("multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW", "------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"npsc\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"npsr\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"sadn\"\r\n\r\n" + i.ToString() + "\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"sag\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"sc\"\r\n\r\n610096\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"sr\"\r\n\r\nTE\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"strFunction\"\r\n\r\nAddServiceAccount\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW--", ParameterType.RequestBody);

                                    IRestResponse responses = client.Execute(request);
                                    try
                                    {
                                        JObject obj = JsonConvert.DeserializeObject<JObject>(responses.Content);
                                        if (Convert.ToInt32(obj["intResult"].ToString()) == 1)
                                        {
                                            URLTEXT.Text = Account + "_" + i.ToString() + "新增成功";
                                            success = true;
                                            richTextBox2.AppendText(Account + "_" + i.ToString() + "\t" + Password + "\n");
                                            sTime++;
                                        }
                                        else
                                        {
                                            if (obj["strOutstring"].ToString() == "Game account already exists")
                                            {
                                                success = true;
                                                URLTEXT.Text = Account + "_" + i.ToString() + "此帳號已經創建";
                                                richTextBox3.AppendText(Account + "_" + i.ToString() + "\t" + Password + "\t此帳號已經創建\n");
                                                ErrorTime++;
                                                break;
                                            }
                                            if (obj["strOutstring"].ToString() == "Maximum application reached")
                                            {
                                                success = true;
                                                URLTEXT.Text = Account + "_" + i.ToString() + "此帳號已經滿了";
                                                richTextBox3.AppendText(Account + "_" + i.ToString() + "\t" + Password + "\t此帳號已經滿了\n");
                                                ErrorTime++;
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        //richTextBox3.Text += (Account + "_" + i.ToString() + " " + ex.Message + "\n");
                                    }
                                    richTextBox3.SelectionStart = richTextBox3.Text.Length;
                                    richTextBox3.ScrollToCaret();
                                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                                    richTextBox2.ScrollToCaret();
                                    SetSetting("Success", richTextBox2.Text);
                                    SetSetting("Error", richTextBox3.Text);
                                }
                            }
                            success = false;
                            if (sTime == 5)
                            {
                                Path = @"" + textBox2.Text + "\\子號成功.txt";
                            }
                            else {
                                Path = @"" + textBox2.Text + "\\子號失敗.txt";
                            }

                            YourString = Account + "\t" + Password + "\t" + Phone + Environment.NewLine;
                            File.AppendAllText(Path, YourString);

                            webBrowser1.Navigate(NewUrl);

                            int j = 0;
                            using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\子號列表.txt"))
                            {
                                foreach (var line in lines)
                                {
                                    if (j == 0)
                                    {
                                        richTextBox1.Text = "";
                                    }
                                    else
                                    {
                                        richTextBox1.Text += line;

                                        writetext.WriteLine(line);
                                        if (j != lines.Count() - 1)
                                        {
                                            richTextBox1.Text += '\n';
                                        }
                                    }
                                    j++;
                                }
                            }
                            SetSetting("Wait", richTextBox1.Text);
                            URLTEXT.Text = "等待中...";
                            Thread.Sleep(10000);
                            LoginToForm();
                        }
                    }
                }
                else
                {
                    URLTEXT.Text = "完成";
                    button1.Enabled = true;
                    button2.Enabled = false;
                    using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                    {
                        writetext.WriteLine("3");
                    }
                    return;
                }
            }
            catch (Exception exception)
            {
                if (exception.Message != "執行緒已經中止。") {
                    URLTEXT.Text = "Crash了..." + exception.Message;
                    using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
                    {
                        writetext.WriteLine("3");
                    }

                    button1.Enabled = true;
                    button2.Enabled = false;
                }
            }
        }

        private void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var br = sender as WebBrowser;
            if (br.ReadyState != WebBrowserReadyState.Complete) {
                return;
            }
            if (br.Url.ToString() == "https://tw.beanfun.com/TW/locales/zh-TW/contents/member/verify_phone.aspx") {
                VerifyWeb = br.Document.Body.InnerHtml;
                Clipboard.SetText(VerifyWeb);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            URLTEXT.Text = "已停止";
            using (StreamWriter writetext = new StreamWriter(@"" + textBox2.Text + "\\state.txt"))
            {
                writetext.WriteLine("3");
            }
            myThread.Abort();
            if(myThread1.IsAlive == true)
                myThread1.Abort();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RunTime = 0;
            ErrorTime = 0;
        }

        public string GetSessionkey()
        {
            string response = BeanfunClient.DownloadString("https://tw.beanfun.com/beanfun_block/bflogin/default.aspx?service=999999_T0");
            response = BeanfunClient.ResponseUri.ToString();
            if (response == null)
            {
                MessageBox.Show("Session");
                myThread.Abort();
            }
            Regex regex = new Regex("skey=(.*)&display");
            if (!regex.IsMatch(response))
            {
                MessageBox.Show("sKey");
                myThread.Abort();
            }
            return regex.Match(response).Groups[1].Value;
        }

        public string RegularLogin(string id, string pass, string skey)
        {
            try
            {
                string response = BeanfunClient.DownloadString("https://tw.newlogin.beanfun.com/login/id-pass_form.aspx?skey=" + skey);
                Regex regex = new Regex("id=\"__VIEWSTATE\" value=\"(.*)\" />");
                if (!regex.IsMatch(response))
                {
                    MessageBox.Show("VIEWSTATE");
                    myThread.Abort();
                }

                string viewstate = regex.Match(response).Groups[1].Value;
                regex = new Regex("id=\"__EVENTVALIDATION\" value=\"(.*)\" />");
                if (!regex.IsMatch(response))
                {
                    MessageBox.Show("EVENTVALIDATION");
                    myThread.Abort();
                }

                string eventvalidation = regex.Match(response).Groups[1].Value;
                regex = new Regex("id=\"__VIEWSTATEGENERATOR\" value=\"(.*)\" />");
                if (!regex.IsMatch(response))
                {
                    MessageBox.Show("VIEWSTATEGENERATOR");
                    myThread.Abort();
                }

                string viewstateGenerator = regex.Match(response).Groups[1].Value;
                regex = new Regex("id=\"LBD_VCID_c_login_idpass_form_samplecaptcha\" value=\"(.*)\" />");
                if (!regex.IsMatch(response))
                {
                    MessageBox.Show("CCC");
                    myThread.Abort();
                }

                string samplecaptcha = regex.Match(response).Groups[1].Value;
                NameValueCollection payload = new NameValueCollection();
                payload.Add("__EVENTTARGET", "");
                payload.Add("__EVENTARGUMENT", "");
                payload.Add("__VIEWSTATE", viewstate);
                payload.Add("__VIEWSTATEGENERATOR", viewstateGenerator);
                payload.Add("__EVENTVALIDATION", eventvalidation);
                payload.Add("t_AccountID", id);
                payload.Add("t_Password", pass);
                payload.Add("CodeTextBox", "");
                payload.Add("btn_login.x", "0");
                payload.Add("btn_login.y", "0");
                payload.Add("LBD_VCID_c_login_idpass_form_samplecaptcha", samplecaptcha);

                response = Encoding.UTF8.GetString(BeanfunClient.UploadValues("https://tw.newlogin.beanfun.com/login/id-pass_form.aspx?skey=" + skey, payload));

                regex = new Regex("akey=(.*)");
                if (!regex.IsMatch(BeanfunClient.ResponseUri.ToString()))
                {
                    MessageBox.Show("aKey");
                    myThread.Abort();
                }
                string akey = regex.Match(BeanfunClient.ResponseUri.ToString()).Groups[1].Value;

                return akey;
            }
            catch (Exception e)
            {
                //this.errmsg = "LoginUnknown\n\n" + e.Message + "\n" + e.StackTrace;
                return null;
            }
        }
        
        public void rsThread()
        {
            myThread1 = new Thread(stopThread1);
            myThread1.Start();
        }

        public void stopThread1() {
            try
            {
                myThread.Abort();
                myThread = new Thread(LoginToForm);
                myThread.IsBackground = true;
                myThread.Start();
            }
            catch (Exception ex)
            {

            }
        }

        private string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        private void SetSetting(string key, string value)
        {
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
