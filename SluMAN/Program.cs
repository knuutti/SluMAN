using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

namespace SluMAN
{
    static class Program
    {


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Files SluMAN ships with (data, controllerskins, mods, autosplitters) are opened by
            // relative path, so a shortcut with another working folder mustn't break them.
            Directory.SetCurrentDirectory(Application.StartupPath);
            UserData.Prepare();

            Start();
        }

        public static Form AttachPS3Form;
        public static void Start()
        {
            AttachPS3Form = new AttachPS3Form();
            Application.Run(AttachPS3Form);
        }
    }
}
