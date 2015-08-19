using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using MSExcel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using RDotNet;
using System.IO;
using System.Threading;
using System.Diagnostics;


namespace PrimerDimerToolWeb
{
    public partial class WebForm2 : System.Web.UI.Page
    {
        string resultlink = null;
        protected REngine engine = null;
        private string statusText = null;
        private String key;
        public WebForm2()
        {
//            Environment.SetEnvironmentVariable("PATH","D:/Install/primer3-win-bin-2.3.6");
//            Environment.SetEnvironmentVariable("JAVA_HOME", "D:/Install/Java/jdk1.7.0_60");
//            REngine.SetEnvironmentVariables("D:/Install/R/R-3.1.2/bin/x64", "D:/Install/R/R-3.1.2");
            string env=System.Environment.GetEnvironmentVariable("Path");
            string rpath = System.Environment.GetEnvironmentVariable("R_HOME");
            REngine.SetEnvironmentVariables();
            engine = REngine.GetInstance();
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            string time = Request.QueryString["t"];
            key = Request.QueryString["guid"];
            if (String.IsNullOrEmpty(key)) key = Guid.NewGuid().ToString();
            statusText = getStatus(key);
            if (time != null)
            {
                Response.Clear();
                Response.Write(statusText);
                Response.End();
            }
            else
            {
                G.Text = key;
                if (System.IO.File.Exists(Server.MapPath(key + ".txt")))
                {
                    System.IO.File.Delete(Server.MapPath(key + ".txt"));
                }
            }
        }
        private void SaveData(string key , string stasutText)
        {
            WriteLog();
            Cache.Insert(key, statusText);
        }
        private string getStatus(string key)
        {
            string data =  Convert.ToString(Cache.Get(key));
            if (String.IsNullOrEmpty(data))
            {
                return "";
            }
            else
            {
                return data;
            }
        }
        private void WriteLog()
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(Server.MapPath(key + ".txt"), true);
            sw.WriteLine("Status = " + statusText + System.DateTime.Now.ToString());
            sw.Close();
        }
        protected void Button1_Click(object sender, EventArgs e)
        {
            resultlink = null;
            statusText = "starting ...";
            SaveData(key, statusText);
            if (FileUpload1.HasFile)
            {
                string fileName = this.FileUpload1.FileName;
                string mapPath = HttpContext.Current.Request.MapPath("~/");
                ThreadTransfer2 transfer2 = new ThreadTransfer2(fileName, mapPath);
                Thread thread = new Thread(new ParameterizedThreadStart(ProcessTask));
                thread.Start(transfer2);
            }
            
        }
        protected void ProcessTask(Object transferObj){
            ThreadTransfer2 transfer2 = (ThreadTransfer2)transferObj;
            string fileName = transfer2.fileName;
            string mapPath = transfer2.mappath;
            string fileExtensionApplication = System.IO.Path.GetExtension(fileName);
            string newFileName = Guid.NewGuid().ToString() + fileExtensionApplication;
            
            string newFilePath = mapPath + newFileName;
            string tmpNewFilePath = newFilePath;
            statusText = "uploading ...";
            SaveData(key, statusText);
            this.FileUpload1.SaveAs(tmpNewFilePath);
            DataTable dt = read_primer_sequence(tmpNewFilePath);
            string[,] primerMat = getPrimerMat(dt);
            statusText = "preparing ...";
            SaveData(key, statusText);

                CharacterMatrix primer = engine.CreateCharacterMatrix(primerMat);
                string rand_file = System.IO.Path.GetRandomFileName();
                string tmp_path = mapPath + rand_file;
                string primer3path = "D:/Install/primer3-win-bin-2.3.6";

                if (Directory.Exists(tmp_path))
                {
                    DirectoryInfo di = new DirectoryInfo(tmp_path);
                    di.Delete(true);
                }
                else if (File.Exists(tmp_path))
                {
                    FileInfo fi = new FileInfo(tmp_path);
                    fi.Delete();
                }
                Directory.CreateDirectory(tmp_path);
                //                  engine.Evaluate("library(Biostrings)");
                engine.Evaluate("library(xlsx)");
                string script_path = "F:/Tools_in_work/PrimerDimerToolWeb/PrimerDimerToolWeb/" + "primer_dimer_check.R";

                engine.Evaluate("source(\"" + script_path + "\")");
                engine.SetSymbol("primer", primer);
                engine.SetSymbol("tmp_dir", engine.CreateCharacter(tmp_path));
                engine.SetSymbol("primer", primer);
                engine.SetSymbol("primer3dir", engine.CreateCharacter(primer3path));
                string nProcess = "5";
                if (nProcess != null)
                {
                    engine.SetSymbol("nprocess", engine.CreateInteger(Convert.ToInt32(nProcess)));
                }
                else
                {
                    engine.SetSymbol("nprocess", engine.CreateInteger(4));
                }
                engine.SetSymbol("outputfile", engine.CreateCharacter(tmpNewFilePath));
                string[] bat_cmds = engine.Evaluate("prepare_bat(tmp_dir,primer,primer3dir,nprocess)").AsCharacter().ToArray();
                statusText = "dimer calculating ...";
                SaveData(key, statusText);
                AutoResetEvent[] resets = new AutoResetEvent[bat_cmds.Length];

                for (int i = 0; i < bat_cmds.Length; i++)
                {
                    resets[i] = new AutoResetEvent(false);
                    WebForm2.ThreadTransfer transfer = new WebForm2.ThreadTransfer(bat_cmds[i], resets[i]);
                    Thread thread = new Thread(new ParameterizedThreadStart(run_cmd));
                    thread.Start(transfer);
                }
                foreach (var v in resets)
                {
                    v.WaitOne();
                }
                statusText = "result generating ...";
                SaveData(key, statusText);
                engine.Evaluate("output_result(tmp_dir,primer,outputfile)");
                statusText = "complete";
                SaveData(key, statusText);
                resultlink = newFileName;
                
                Cache.Remove(key);


            
        }
        static void run_cmd(object obj)
        {
            WebForm2.ThreadTransfer transfer = (WebForm2.ThreadTransfer)obj;
            Execute(transfer.cmd,10);
            transfer.evt.Set();
        }
        public static string Execute(string command, int seconds)
        {
            string output = "";
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())
                    {
                        if (seconds == 0)
                        {
                            process.WaitForExit();
                        }
                        else
                        {
                            process.WaitForExit(seconds);
                        }
                        output = process.StandardOutput.ReadToEnd();
                    }
                }
                catch
                {
                }
                finally
                {
                    if (process != null)
                        process.Close();
                }
            }
            return output;
        }
        private static string[,] getPrimerMat(DataTable dt)
        {
            string[,] primerMat = new string[dt.Rows.Count, 3];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string seqID = dt.Rows[i]["seqID"].ToString();
                string f_seq = dt.Rows[i]["f_sequence"].ToString();
                string r_seq = dt.Rows[i]["r_sequence"].ToString();
                primerMat[i, 0] = seqID;
                primerMat[i, 1] = f_seq;
                primerMat[i, 2] = r_seq;
            }
            return primerMat;
        }

        protected string getResultLink() {
            if (resultlink == null)
            {
                return resultlink;
            }
            else
            {
                return "<p><a href='"+resultlink+"'>Result</a></p>";
            }
        }
        private DataTable read_primer_sequence(string filename)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("seqID");
            dt.Columns.Add("f_sequence");
            dt.Columns.Add("r_sequence");

            MSExcel.Application excelApp = new MSExcel.Application();
            excelApp.Workbooks.Open(filename);
            //string tabName = excelApp.Workbooks[1].Worksheets[1].Name;
            excelApp.Workbooks[1].Worksheets[1].Activate();

            int i = 2;
            while (true)
            {
                DataRow dr = dt.NewRow();

                string id = Convert.ToString(excelApp.Cells[i, 1].Value);
                string f_sequence = excelApp.Cells[i, 2].Value;
                string r_sequence = excelApp.Cells[i, 3].Value;
                if (id == null || id.Length < 1 || id.Length < 1) break;
                dr[0] = id;
                dr[1] = f_sequence;
                dr[2] = r_sequence;
                dt.Rows.Add(dr);
                i++;
            }

            release_excel_app(ref excelApp);
            return dt;
        }

        [DllImport(@"C:\Windows\System32\User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);
        private void release_excel_app(ref MSExcel.Application excelApp)
        {
            excelApp.Workbooks[1].Close();
            excelApp.Quit();
            if (excelApp != null)
            {
                int lpdwProcessID;
                GetWindowThreadProcessId(new IntPtr(excelApp.Hwnd), out lpdwProcessID);
                System.Diagnostics.Process.GetProcessById(lpdwProcessID).Kill();
            }
        }
        public class ThreadTransfer
        {
            public string cmd;
            public AutoResetEvent evt;
            public ThreadTransfer(string cmd, AutoResetEvent evt)
            {
                this.cmd = cmd;
                this.evt = evt;
            }
        }
        public class ThreadTransfer2
        {
            public string fileName;
            public string mappath;
            public ThreadTransfer2(string fileName, string mapPath)
            {
                this.fileName = fileName;
                this.mappath = mapPath;
            }
        }
    }
 
}