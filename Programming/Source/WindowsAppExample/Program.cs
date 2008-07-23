// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Engine;
using Engine.FileSystem;

namespace WindowsAppExample
{
	/// <summary>
	/// Defines an input point in the application.
	/// </summary>
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			if( Debugger.IsAttached )
			{
				Main2();
			}
			else
			{
				try
				{
					Main2();
				}
				catch( Exception e )
				{
					Log.FatalAsException( e.ToString() );
				}
			}
		}

		static void Main2()
		{
			Log.DumpFileName = "Logs/WindowsAppExample.log";
			Log.DumpToFile( string.Format( "Windows Application Example {0}\r\n", 
				EngineVersionInformation.Version ) );

			if( !VirtualFileSystem.Init() )
				return;

			Log.Handlers.InfoHandler += delegate( string text )
			{
			};

			Log.Handlers.WarningHandler += delegate( string text )
			{
				MessageBox.Show( text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			};

			Log.Handlers.ErrorHandler += delegate( string text )
			{
				MessageBox.Show( text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
			};

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			EngineApp.ConfigName = "Configs/WindowsAppExample.config";

			EngineApp.Init( new WindowsAppEngineApp() );

			//Do message loop

			MainForm form = new MainForm();
			form.Show();
			while( form.Created )
			{
				Application.DoEvents();

				if( MainForm.Instance == null )
					break;

				bool allowRender = MainForm.Instance.Visible &&
					MainForm.Instance.WindowState != FormWindowState.Minimized &&
					MainForm.Instance.IsAllowRenderScene() &&
					Form.ActiveForm == MainForm.Instance;

				if( allowRender )
					form.RenderScene();
				else
					EngineApp.MessageLoopWaitMessage();
			}

			EngineApp.Shutdown();

			Log.DumpToFile( "Program END\r\n" );

			VirtualFileSystem.Shutdown();
		}
	}
}