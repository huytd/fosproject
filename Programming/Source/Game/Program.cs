// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.Utils;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines an input point in the application.
	/// </summary>
	public static class Program
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
			Log.DumpFileName = "Logs/Game.log";
			Log.DumpToFile( string.Format( "Game {0}\r\n", EngineVersionInformation.Version ) );

			if( !VirtualFileSystem.Init() )
				return;

			EngineApp.ConfigName = "Configs/Game.config";
			EngineApp.UseSystemMouseDeviceForRelativeMode = true;
			EngineApp.AllowJoysticksAndCustomInputDevices = true;
			EngineApp.AllowChangeVideoMode = true;

			EngineApp.Init( new GameEngineApp() );
			EngineApp.Instance.WindowTitle = "Viet Heroes - Fight or Surrender";
			EngineApp.Instance.Icon = Game.Properties.Resources.Logo;

			EngineConsole.Init();

			EngineApp.Instance.Config.RegisterClassParameters( typeof( GameEngineApp ) );

			if( EngineApp.Instance.Create() )
				EngineApp.Instance.Run();

			EngineApp.Shutdown();

			Log.DumpToFile( "Program END\r\n" );

			VirtualFileSystem.Shutdown();
		}
	}
}
