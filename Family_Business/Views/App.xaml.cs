
using System;
using System.Windows;

namespace Family_Business
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();

            app.InitializeComponent();
 
            app.Run();
        }
    }
}