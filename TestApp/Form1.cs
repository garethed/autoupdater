using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace TestApp
{
  public partial class Form1 : Form
  {
    string PersistenceData;
    public Form1()
    {
      InitializeComponent();

      PersistenceData = AppDomain.CurrentDomain.GetData("AUTOUPDATE_PERSISTENCE_DATA") as string ?? "";

      label1.Text = File.GetLastWriteTime(Assembly.GetCallingAssembly().Location).Second.ToString() + " " + PersistenceData;
    }

    protected override void  OnFormClosing(FormClosingEventArgs e)
    {
     	 base.OnFormClosing(e);
      AppDomain.CurrentDomain.SetData("AUTOUPDATE_PERSISTENCE_DATA", PersistenceData + ".");
    }

    private void label1_Click(object sender, EventArgs e)
    {
      
    }
  }
}
