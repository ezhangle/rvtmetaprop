#region Namespaces
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace rvtmetaprop
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
#if DEBUG
    static string _default_folder = "C:/a/vs/rvtmetaprop/test";
#else
    static string _default_folder = "";
#endif // _DEBUG

    
    static string _filename = "";

    /// <summary>
    /// Select a specified file in the given folder.
    /// </summary>
    /// <param name="folder">Initial folder.</param>
    /// <param name="filename">Selected filename on 
    /// success.</param>
    /// <returns>Return true if a file was successfully 
    /// selected.</returns>
    static bool FileSelect(
      string folder,
      string title,
      string filter,
      ref string filename )
    {
      bool rc = false;
      using( OpenFileDialog dlg = new OpenFileDialog() )
      {
        dlg.Title = title;
        dlg.CheckFileExists = true;
        dlg.CheckPathExists = true;
        dlg.InitialDirectory = folder;
        dlg.FileName = filename;
        dlg.Filter = filter;
        rc = ( DialogResult.OK == dlg.ShowDialog() );
        filename = dlg.FileName;
      }
      return rc;
    }

    /// <summary>
    /// Select a meta property file in the given folder.
    /// </summary>
    /// <param name="folder">Initial folder.</param>
    /// <param name="filename">Selected filename on 
    /// success.</param>
    /// <returns>Return true if a file was successfully 
    /// selected.</returns>
    static public bool FileSelectMetaProp(
      string folder,
      ref string filename )
    {
      return FileSelect( folder,
        "Select meta property file",
        //"CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|All Files|*.*",
        "Meta Property Files (*.csv;*.json)|*.csv;*.json|All Files|*.*",
        ref filename );
    }
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      if( !FileSelectMetaProp(
        _default_folder,
        ref _filename ) )
      {
        return Result.Cancelled;
      }

      _default_folder = Path.GetDirectoryName( _filename );

      List<MetaProp> props = null;

      if( _filename.ToLower().EndsWith( ".json" ) )
      {
        string s = File.ReadAllText( _filename );
        props = JsonConvert.DeserializeObject<List<MetaProp>>( s );
        Debug.Print( props.Count + " props deserialised" );
      }
      else if( _filename.ToLower().EndsWith( ".csv" ) )
      {
        IEnumerable < IList < string >> a = EasyCsv.FromFile( _filename );
        int n = a.Count();
        Debug.Print( n + " props deserialised" );
        props = new List<MetaProp>(n);
        foreach( IList<string> rec in a )
        {
          props.Add( new MetaProp( rec ) );
        }
      }
      else
      {
        message = "Unhandled meta property file format: " 
          + Path.GetExtension( _filename );
        return Result.Failed;
      }

      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Import Forge Meta Properties" );
        tx.Commit();
      }

      return Result.Succeeded;
    }
  }
}
