using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YCleanup
{
    public partial class YCleanup : Form
    {
        public YCleanup()
        {
            InitializeComponent();
        }
        private delegate void LogMessageDelegate(string message, Color color);
        private void LogMessage(string message, Color color)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new LogMessageDelegate(LogMessage), new object[] { message, color });
            }
            else
            {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.SelectionLength = 0;
                richTextBox1.SelectionColor = color;
                richTextBox1.AppendText(DateTime.Now + " " + message + Environment.NewLine);
                richTextBox1.ScrollToCaret();
            }
        }

        private void YCleanup_Load(object sender, EventArgs e)
        {
           LogMessage("Program started.", Color.Blue); 
        }

        private List<string> GetFilesAndDirs(string rootPath, bool getFromSubDirectories,bool getDirs)
        {   
            List<string> file = new List<string>();
            try
            {
                DirectoryInfo info = new DirectoryInfo(rootPath);
                FileSystemInfo[] files = info.GetFileSystemInfos();
                foreach (FileSystemInfo item in files)
                {
                    if (item is FileInfo subFile)
                    {
                        file.Add(subFile.FullName);
                    }

                    if (item is DirectoryInfo subDir)
                    {
                        if (getDirs)
                        {
                            file.Add(subDir.FullName);
                        }

                        GetFilesAndDirs(subDir.FullName, getFromSubDirectories,getDirs);
                    }

                }
            }
            catch{}
            return file;
        }

        private Task DeleteObject(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    LogMessage($"Success deleted dir :{path}", Color.Green);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                    LogMessage($"Success deleted file :{path}", Color.Green);
                }
            }
            catch
            {
                LogMessage($"Failed deleted object :{path}", Color.Red);
            }

            return Task.CompletedTask;
        }

        private void Clean()
        {
            AddSecurityControlFolder(@"C:\Windows\Temp");
            foreach (var obj in GetFilesAndDirs(@"C:\Windows\Temp",true,true))
            {
                Task.Run(() => DeleteObject(obj));
            }
            foreach (var obj in GetFilesAndDirs(Path.GetTempPath(),true,true))
            {
                Task.Run(() => DeleteObject(obj));
            }
            foreach (var obj in GetFilesAndDirs(Environment.GetFolderPath(Environment.SpecialFolder.Cookies),true,true))
            {
                Task.Run(() => DeleteObject(obj));
            }
            foreach (var obj in GetFilesAndDirs(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),true,true))
            {
                Task.Run(() => DeleteObject(obj));
            }
            foreach (var obj in GetFilesAndDirs(@"C:\Windows\Installer",false,false))
            {
                Task.Run(() => DeleteObject(obj));
            }
            DialogResult result = MessageBox.Show("Do you want to clean dir 'Package Cache'?","YCleanup",MessageBoxButtons.YesNo,MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                foreach (var obj in GetFilesAndDirs(@"C:\ProgramData\Package Cache",true,true))
                {
                    Task.Run(() => DeleteObject(obj));
                }
            }
            foreach (var obj in GetFilesAndDirs(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\Local\\Microsoft\\Windows\\Explorer",false,false))
            {
                Task.Run(() => DeleteObject(obj));
            }

            Task.WaitAll();
            LogMessage("We will use /ResetBase to clean dir 'WinSXS'.",Color.Blue);
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            process.Close();
            
        }

        private void clean_Click(object sender, EventArgs e)
        {
            Clean();
        }
        private void AddSecurityControlFolder(string dirPath)
        {
            //获取文件夹信息
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            //获得该文件夹的所有访问权限
            System.Security.AccessControl.DirectorySecurity dirSecurity = dir.GetAccessControl(AccessControlSections.All);
            //设定文件ACL继承
            InheritanceFlags inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            //添加everyone用户组的访问权限规则 完全控制权限
            FileSystemAccessRule everyoneFileSystemAccessRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
            //添加Users用户组的访问权限规则 完全控制权限
            FileSystemAccessRule usersFileSystemAccessRule = new FileSystemAccessRule("Users", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
            dirSecurity.ModifyAccessRule(AccessControlModification.Add, everyoneFileSystemAccessRule,out var modified);
            dirSecurity.ModifyAccessRule(AccessControlModification.Add, usersFileSystemAccessRule, out modified);
            //设置访问权限
            dir.SetAccessControl(dirSecurity);
        }

        private void button1_Click(object sender, EventArgs e)
        {
           Environment.Exit(0); 
        }
    }
}